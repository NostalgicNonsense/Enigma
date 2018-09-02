/////////////////////////////////////////////////////////////////////////////////
//
//	vp_Respawner.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	this script allows a gameobject to respawn in the same position,
//					or at random, tagged vp_SpawnPoints after its 'Die' method has
//					been called
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

#if UNITY_5_4_OR_NEWER
using UnityEngine.SceneManagement;
#endif

[System.Serializable]
public class vp_Respawner : MonoBehaviour
{

	public enum SpawnMode
	{
		SamePosition,
		SpawnPoint
	}

	// describes what to do when the spawnpoint is obstructed
	public enum ObstructionSolver
	{
		Wait,				// wait for 'RespawnTime' seconds and try again (good for items and powerups)
		AdjustPlacement		// try to find the closest valid position. if no position is found, wait. (good for players and AI)
	}

	public SpawnMode m_SpawnMode = SpawnMode.SamePosition;
	public string SpawnPointTag = "";

	public ObstructionSolver m_ObstructionSolver = ObstructionSolver.Wait;
	public float ObstructionRadius = 1.0f;	// area around object which must be clear of other objects before respawn
		
	public float MinRespawnTime = 3.0f;		// random timespan in seconds to delay respawn
	public float MaxRespawnTime = 3.0f;
	public float LastRespawnTime = 0.0f;	// the last point in time at which we respawned. intended use: to prevent hurting a player who has just respawned
	public bool SpawnOnAwake = false;

	public AudioClip SpawnSound = null;		// sound to play upon respawn
	public GameObject [] SpawnFXPrefabs = null;	// e.g. a particle effect to be played upon respawn

	#if UNITY_EDITOR
	[vp_HelpBox(typeof(vp_Respawner), UnityEditor.MessageType.None, typeof(vp_Respawner), null, true)]
	public float helpbox;
	#endif

	protected Vector3 m_InitialPosition = Vector3.zero;		// initial position detected and used for respawn
	protected Quaternion m_InitialRotation;		// initial rotation detected and used for respawn
	protected vp_Placement Placement = new vp_Placement();

	protected bool m_IsInitialSpawnOnAwake = false;

	protected vp_Timer.Handle m_RespawnTimer = new vp_Timer.Handle();

	// cache of known respawners, stored by collider (for optimization)
	protected static Dictionary<Collider, vp_Respawner> m_Instances = null;
	public static Dictionary<Collider, vp_Respawner> Instances
	{
		get
		{
			if (m_Instances == null)
				m_Instances = new Dictionary<Collider, vp_Respawner>(100);
			return m_Instances;
		}
	}
	protected static vp_Respawner m_GetInstanceResult = null;

	protected Renderer m_Renderer = null;
	public Renderer Renderer
	{
		get
		{
			if (m_Renderer == null)
				m_Renderer = GetComponent<Renderer>();
			return m_Renderer;
		}
	}

	protected Transform m_Transform = null;
	public Transform Transform
	{
		get
		{
			if (m_Transform == null)
				m_Transform = transform;
			return m_Transform;
		}
	}

	protected AudioSource m_Audio = null;
	protected AudioSource Audio
	{
		get
		{
			if (m_Audio == null)
			{
				m_Audio = GetComponent<AudioSource>();
				if (m_Audio == null)
					m_Audio = gameObject.AddComponent<AudioSource>();
			}
			return m_Audio;
		}
	}

	protected Collider m_Collider = null;
	protected Collider Collider
	{
		get
		{
			if (m_Collider == null)
				m_Collider = GetComponent<Collider>();
			return m_Collider;
		}
	}

	protected Rigidbody m_Rigidbody = null;
	protected Rigidbody Rigidbody
	{
		get
		{
			if (m_Rigidbody == null)
				m_Rigidbody = GetComponent<Rigidbody>();
			return m_Rigidbody;
		}
	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void Awake()
	{

		Placement.Position = m_InitialPosition = Transform.position;
		Placement.Rotation = m_InitialRotation = Transform.rotation;

		if (m_SpawnMode == SpawnMode.SamePosition)
			SpawnPointTag = "";

		if (SpawnOnAwake)
		{
			m_IsInitialSpawnOnAwake = true;
			vp_Utility.Activate(gameObject, false);
			PickSpawnPoint();
		}

	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void OnEnable()
	{

#if UNITY_5_4_OR_NEWER
		SceneManager.sceneLoaded += OnLevelLoad;
#endif

		if ((Collider != null) && !Instances.ContainsValue(this))
			Instances.Add(Collider, this);

	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void OnDisable()
	{

#if UNITY_5_4_OR_NEWER
		SceneManager.sceneLoaded -= OnLevelLoad;
#endif

	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void SpawnFX()
	{

		if (!m_IsInitialSpawnOnAwake)
		{

			if (Audio != null)
			{
				Audio.pitch = Time.timeScale;
				Audio.PlayOneShot(SpawnSound);
			}

			// spawn effects gameobjects
			if (SpawnFXPrefabs != null && SpawnFXPrefabs.Length > 0)
			{
				foreach (GameObject fx in SpawnFXPrefabs)
				{
					if (fx != null)
						vp_Utility.Instantiate(fx, Transform.position, Transform.rotation);
				}
			}
		}

		m_IsInitialSpawnOnAwake = false;

	}
	

	/// <summary>
	/// event target, typically sent by vp_DamageHandler or vp_ItemPickup
	/// </summary>
	protected virtual void Die()
	{
		vp_Timer.In(UnityEngine.Random.Range(MinRespawnTime, MaxRespawnTime), PickSpawnPoint, m_RespawnTimer);
	}


	/// <summary>
	/// respawns the object if no other object is occupying the respawn area.
	/// otherwise reschedules respawning. NOTE: this method can only run in
	/// singleplayer and by a master in multiplayer. multiplayer _clients_
	/// will instead use the version of 'GetSpawnPoint' that takes a position
	/// and rotation (called directly by the master)
	/// </summary>
	public virtual void PickSpawnPoint()
	{

		// return if the object has been destroyed (for example
		// as a result of loading a new level while it was gone)
		if (this == null)
			return;

		// if mode is 'SamePosition' or the level has no spawnpoints, go to initial position
		if ((m_SpawnMode == SpawnMode.SamePosition) || (vp_SpawnPoint.SpawnPoints.Count < 1))
		{

			Placement.Position = m_InitialPosition;
			Placement.Rotation = m_InitialRotation;
			// if an object the size of 'RespawnCheckRadius' can't fit at
			// 'm_InitialPosition' ...
			if (Placement.IsObstructed(ObstructionRadius))
			{
				switch (m_ObstructionSolver)
				{
					case ObstructionSolver.Wait:
						// ... just try again later!
						vp_Timer.In(UnityEngine.Random.Range(MinRespawnTime, MaxRespawnTime), PickSpawnPoint, m_RespawnTimer);
						return;
					case ObstructionSolver.AdjustPlacement:
						// try to adjust the position ...
						if (!vp_Placement.AdjustPosition(Placement, ObstructionRadius))
						{
							// ... and only if we failed to adjust the position, try again later
							vp_Timer.In(UnityEngine.Random.Range(MinRespawnTime, MaxRespawnTime), PickSpawnPoint, m_RespawnTimer);
							return;
						}
						break;
				}
			}
		}
		else
		{

			// placement will be calculated by the spawnpoint system.
			// NOTE: the obstruction solution logic becomes slightly
			// different with spawnpoints
			switch (m_ObstructionSolver)
			{
				case ObstructionSolver.Wait:
					// if an object the size of 'RespawnCheckRadius' can't fit at
					// this random spawnpoint ...
					Placement = vp_SpawnPoint.GetRandomPlacement(0.0f, SpawnPointTag);
					if (Placement == null)
					{
						Placement = new vp_Placement();
						m_SpawnMode = SpawnMode.SamePosition;
						PickSpawnPoint();
					}
					// NOTE: no 'snap to ground' in this mode since the snap logic
					// of 'GetRandomPlacement' is dependent on its input value
					if (Placement.IsObstructed(ObstructionRadius))
					{
						// ... skip trying to adjust the position and try again later
						vp_Timer.In(UnityEngine.Random.Range(MinRespawnTime, MaxRespawnTime), PickSpawnPoint, m_RespawnTimer);
						return;
					}
					break;
				case ObstructionSolver.AdjustPlacement:
					// if an object the size of 'RespawnCheckRadius' can't fit at
					// this random spawnpoint and we fail to adjust the position ...
					Placement = vp_SpawnPoint.GetRandomPlacement(ObstructionRadius, SpawnPointTag);
					if (Placement == null)
					{
						// ... try again later
						vp_Timer.In(UnityEngine.Random.Range(MinRespawnTime, MaxRespawnTime), PickSpawnPoint, m_RespawnTimer);
						return;
					}
					break;
			}

		}

		Respawn();

	}


	/// <summary>
	/// forces an object's 'Placement' to 'position', 'rotation' and
	/// respawns it at that point
	/// </summary>
	public virtual void PickSpawnPoint(Vector3 position, Quaternion rotation)
	{

		Placement.Position = position;
		Placement.Rotation = rotation;

		Respawn();

	}


	/// <summary>
	/// 
	/// </summary>
	public virtual void Respawn()
	{

		LastRespawnTime = Time.time;

		// reactivate and reset
		vp_Utility.Activate(gameObject);
		SpawnFX();

		// in multiplayer, send a message to the network system if we're the master / host
		if (vp_Gameplay.IsMultiplayer && vp_Gameplay.IsMaster)
		{
			vp_GlobalEvent<Transform, vp_Placement>.Send("TransmitRespawn", transform.root, Placement);
		}

		SendMessage("Reset");		// will trigger on vp_Respawners + vp_DamageHandlers

		// reset placement to start position for next respawn since it may
		// have been adjusted by obstruction logic during respawn
		Placement.Position = m_InitialPosition;
		Placement.Rotation = m_InitialRotation;
		// NOTE: this should end up affecting mainly items and powerups since
		// players typically use vp_SpawnPoints to fetch a new Placement
		// every time

	}
	
	
	/// <summary>
	/// event target. resets position, angle and motion
	/// </summary>
	public virtual void Reset()
	{

		if (!Application.isPlaying)
			return;

		Transform.position = Placement.Position;
		Transform.rotation = Placement.Rotation;

		if (Rigidbody != null && !Rigidbody.isKinematic)
		{
			Rigidbody.angularVelocity = Vector3.zero;
			Rigidbody.velocity = Vector3.zero;
		}

	}


	/// <summary>
	/// resets every respawn timer in the scene. for resetting the
	/// game or for cloud multiplayer (when a master leaves and a
	/// new one takes over the game). if 'reInitTimers' is true then
	/// all inactive respawners will reinitialize with a new random
	/// timespan between their min- and max respawn times. if false,
	/// (default) all objects will respawn immediately
	/// </summary>
	public static void ResetAll(bool reInitTimers = false)
	{

		foreach (vp_Respawner r in Instances.Values)
		{
			if (r == null)
				continue;

			// see if this object is dead and needs respawning
			if (!vp_Utility.IsActive(r.gameObject) ||											// a pickup or prop (gets deactivated when dead)
				((r is vp_PlayerRespawner) && ((r as vp_PlayerRespawner).Player.Dead.Active)))	// a player (still active when dead so we need to check the 'Dead' event)
			{
				if (reInitTimers)		// start the respawn timer all over again
					r.Die();
				else
					r.PickSpawnPoint();	// instantly respawn the object
			}
		}

		// NOTE: we're blatantly assuming here that pickups and props will only be deactivated
		// as a result of having been killed. you may want to change this for your game in case
		// you are activating / deactivating objects from scripted sequences

	}


	/// <summary>
	/// retrieves, finds and caches target respawners for more
	/// efficient fetching in the future
	/// </summary>
	public static vp_Respawner GetByCollider(Collider col)
	{

		// try to fetch a known respawner on this target
		if (!Instances.TryGetValue(col, out m_GetInstanceResult))
		{
			// no respawners on record: see if there is one
			m_GetInstanceResult = col.transform.root.GetComponentInChildren<vp_Respawner>();
			Instances.Add(col, m_GetInstanceResult);		// add result to the dictionary (even if null)
		}

		return m_GetInstanceResult;

	}


	/// <summary>
	/// resets the cache of colliders and damagehandlers on level load
	/// </summary>
#if UNITY_5_4_OR_NEWER
	protected void OnLevelLoad(Scene scene, LoadSceneMode mode)
#else
	protected void OnLevelWasLoaded()
#endif
	{

		Instances.Clear();

	}

}

