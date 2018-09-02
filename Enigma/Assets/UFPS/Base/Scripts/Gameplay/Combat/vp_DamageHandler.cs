/////////////////////////////////////////////////////////////////////////////////
//
//	vp_DamageHandler.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	class for having a gameobject take damage, die and respawn.
//					any other object can do damage on this monobehaviour like so:
//					    hitObject.SendMessage(Damage, 1.0f, SendMessageOptions.DontRequireReceiver);
//
///////////////////////////////////////////////////////////////////////////////// 

using UnityEngine;
using System;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

#if UNITY_5_4_OR_NEWER
using UnityEngine.SceneManagement;
#endif


public class vp_DamageHandler : MonoBehaviour
{

	// health and death
	public float MaxHealth = 1.0f;						// initial health of the object instance, to be reset on respawn
	public GameObject [] DeathSpawnObjects = null;		// gameobjects to spawn when object dies.
														// TIP: could be fx, could also be rigidbody rubble
	public float MinDeathDelay = 0.0f;					// random timespan in seconds to delay death. good for cool serial explosions
	public float MaxDeathDelay = 0.0f;
	public float CurrentHealth = 0.0f;					// current health of the object instance
	protected bool m_InstaKill = false;                 // temporarily disables death delay, for example: on death by impact

	[HideInInspector]
	public float LastDamageTime = 0;                    // for any external scripts, e.g. 'vp_Regenerator' that need to know about this

	// sounds
	public AudioClip DeathSound = null;					// sound to play upon death
	protected AudioSource m_Audio = null;
	
	// impact damage
	public float ImpactDamageThreshold = 10;
	public float ImpactDamageMultiplier = 0.0f;

	// NOTE: these variables have been made obsolete and are now found in
	// the vp_Respawner component. there is temporary logic in this class
	// to help make the transition easier

	[HideInInspector]
	public bool Respawns = false;
	[HideInInspector]
	public float MinRespawnTime = -99999.0f;
	[HideInInspector]
	public float MaxRespawnTime = -99999.0f;
	[HideInInspector]
	public float RespawnCheckRadius = -99999.0f;
	[HideInInspector]
	public AudioClip RespawnSound = null;
	[HideInInspector]
	public GameObject DeathEffect = null;

	// cache of known damagehandlers, stored by collider (for optimization)
	protected static Dictionary<Collider, vp_DamageHandler> m_Instances = null;
	public static Dictionary<Collider, vp_DamageHandler> Instances
	{
		get
		{
			if (m_Instances == null)
				m_Instances = new Dictionary<Collider, vp_DamageHandler>(100);
			return m_Instances;
		}
	}
	protected static vp_DamageHandler m_GetDamageHandlerOfColliderResult = null;

	// NOTE: these variables are obsolete and will be removed
	protected Vector3 m_StartPosition;
	protected Quaternion m_StartRotation;

#if UNITY_EDITOR
	[vp_HelpBox(typeof(vp_DamageHandler), UnityEditor.MessageType.None, typeof(vp_DamageHandler), null, true, vp_PropertyDrawerUtility.Space.Nothing)]
	public float helpbox;
#endif

	// --- lazy initialization  for performance ---

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

	protected vp_Respawner m_Respawner = null;
	public vp_Respawner Respawner
	{
		get
		{
			if (m_Respawner == null)
				m_Respawner = GetComponent<vp_Respawner>();
			return m_Respawner;
		}
	}

	/// <summary>
	/// returns the IMMEDIATE source of damage (default: ourselves, which
	/// will be the cause if we take falling damage). can also be set to
	/// another transform, such as the transform of an exploding grenade,
	/// or in the cause of bullets: the transform of the person pulling the
	/// trigger
	/// </summary>
	protected Transform Source
	{
		get
		{
			if (m_Source == null)
				m_Source = Transform;
			return m_Source;
		}
		set
		{
			m_Source = value;
		}
	}
	protected Transform m_Source = null;

	
	/// <summary>
	/// returns the ORIGINAL source of damage, such as the transform of
	/// the person who threw the grenade that killed us. in the case of
	/// of bullets the original source will be same as the source, i.e.
	/// the person who pulled the trigger
	/// </summary>
	protected Transform OriginalSource
	{
		get
		{
			if (m_OriginalSource == null)
				m_OriginalSource = Transform;
			return m_OriginalSource;
		}
		set
		{
			m_OriginalSource = value;
		}
	}
	protected Transform m_OriginalSource = null;


	[Obsolete("This property will be removed in an upcoming release.")]
	public Transform Sender
	{
		get
		{
			return Source;
		}
		set
		{
			Source = value;
		}
	}



	/// <summary>
	/// 
	/// </summary>
	protected virtual void Awake()
	{
		
		m_Audio = GetComponent<AudioSource>();

		CurrentHealth = MaxHealth;

		// check for obsolete respawn-related parameters, create a vp_Respawner
		// component (if necessary) and disable such values on this component
		// NOTE: this check is temporary and will be removed in the future
		CheckForObsoleteParams();

		Instances.Add(GetComponent<Collider>(), this);

	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void OnEnable()
	{

#if UNITY_5_4_OR_NEWER
		SceneManager.sceneLoaded += OnLevelLoad;
#endif

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
	/// reduces current health by 'damage' points and kills the
	/// object if health runs out
	/// </summary>
	public virtual void Damage(float damage)
	{
		Damage(new vp_DamageInfo(damage, null));
	}
	public virtual void Damage(vp_DamageInfo damageInfo)
	{

		if (!enabled)
			return;

		if (!vp_Utility.IsActive(gameObject))
			return;

		// damage is always done in singleplayer, but only in multiplayer if you are the master
		if (!vp_Gameplay.IsMaster)
			return;

		if (CurrentHealth <= 0.0f)
			return;

		if (damageInfo != null)
		{
			if (damageInfo.Source != null)
				Source = damageInfo.Source;
			if (damageInfo.OriginalSource != null)
				OriginalSource = damageInfo.OriginalSource;
			//Debug.Log("Damage! Source: " + damageInfo.Source + " ... " + "OriginalSource: " + damageInfo.OriginalSource);
		}

		// if we somehow shot ourselves with a bullet, ignore it
		if ((damageInfo.Type == vp_DamageInfo.DamageType.Bullet) && (m_Source == Transform))
			return;

		// --- damage will be inflicted ---

		LastDamageTime = Time.time;

		// subtract damage from health
		CurrentHealth = Mathf.Min(CurrentHealth - damageInfo.Damage, MaxHealth);

		// in multiplayer, report damage for score tracking purposes
		if (vp_Gameplay.IsMultiplayer && (damageInfo.Source != null))
			vp_GlobalEvent<Transform, Transform, float>.Send("TransmitDamage", Transform.root, damageInfo.OriginalSource, damageInfo.Damage);

		// detect and transmit death as event
		if (CurrentHealth <= 0.0f)
		{
			// send the 'Die' message, to be picked up by vp_DamageHandlers and vp_Respawners
			if (m_InstaKill)
				SendMessage("Die");
			else
				vp_Timer.In(UnityEngine.Random.Range(MinDeathDelay, MaxDeathDelay), delegate() { SendMessage("Die"); });
		}

	}


	/// <summary>
	/// this 'SendMessage' target will instantly kill the transform
	/// with info about the cause - and original cause - for death.
	/// its argument must be an object array containing 2 transforms.
	/// the first transform should be the immediate cause of a death
	/// (for example: a grenade). the second transform should be
	/// the original cause for this happening (the player who threw
	/// the grenade). the grenade will trigger a damage arrow
	/// pointing to it in the pain HUD. the player would typically
	/// get score in multiplayer
	/// </summary>
	public virtual void DieBySources(Transform[] sourceAndOriginalSource)
	{

		if (sourceAndOriginalSource.Length != 2)
		{
			Debug.LogWarning("Warning (" + this + ") 'DieBySources' argument must contain 2 transforms.");
			return;
		}

		Source = sourceAndOriginalSource[0];
		OriginalSource = sourceAndOriginalSource[1];

		Die();

	}


	/// <summary>
	/// this 'SendMessage' target will instantly kill the transform
	/// with info about the immediate cause for death. 'source'
	/// will trigger a damage arrow pointing to it in the pain HUD.
	/// if it's a player, it would typically get score in multiplayer
	/// </summary>
	public virtual void DieBySource(Transform source)
	{

		OriginalSource = Source = source;
		Die();

	}


	/// <summary>
	/// removes the object, plays the death effect and schedules
	/// a respawn if enabled, otherwise destroys the object
	/// </summary>
	public virtual void Die()
	{

		if (!enabled || !vp_Utility.IsActive(gameObject))
			return;

		if (m_Audio != null)
		{
			m_Audio.pitch = Time.timeScale;
			m_Audio.PlayOneShot(DeathSound);
		}

		foreach (GameObject o in DeathSpawnObjects)
		{
			if (o != null)
			{
				GameObject g = (GameObject)vp_Utility.Instantiate(o, Transform.position, Transform.rotation);
				if ((Source != null) && (g != null))
					vp_TargetEvent<Transform>.Send(g.transform, "SetSource", OriginalSource);
			}
		}

		if (Respawner == null)
		{
			vp_Utility.Destroy(gameObject);
		}
		else
		{
			RemoveBulletHoles();
			vp_Utility.Activate(gameObject, false);
		}

		m_InstaKill = false;

		if (vp_Gameplay.IsMultiplayer && vp_Gameplay.IsMaster)
		{
			//Debug.Log("sending kill event from master scene to vp_MasterClient");
			vp_GlobalEvent<Transform>.Send("TransmitKill", transform.root);
		}

	}


	/// <summary>
	/// resets health, position, angle and motion
	/// </summary>
	protected virtual void Reset()
	{

		CurrentHealth = MaxHealth;
		Source = null;
		OriginalSource = null;

	}
	

	/// <summary>
	/// removes any bullet decals currently childed to this object.
	/// NOTE: the decals must be in the 'Debris' layer
	/// </summary>
	protected virtual void RemoveBulletHoles()
	{

		foreach(Transform t in Transform)
		{
			if(t.gameObject.layer == vp_Layer.Debris)
				vp_Utility.Destroy(t.gameObject);
		}

	}


	/// <summary>
	/// calculates and applies impact damage
	/// </summary>
	protected virtual void OnCollisionEnter(Collision collision)
	{

		float force = collision.relativeVelocity.sqrMagnitude * 0.1f;

		float damage = (force > ImpactDamageThreshold) ? (force * ImpactDamageMultiplier) : 0.0f;

		if (damage <= 0.0f)
			return;

		if (CurrentHealth - damage <= 0.0f)
			m_InstaKill = true;

		Damage(new vp_DamageInfo(damage, collision.collider.transform, vp_DamageInfo.DamageType.Impact));

	}


	/// <summary>
	/// retrieves, finds and caches target damagehandlers for more
	/// efficient fetching in the future
	/// </summary>
	public static vp_DamageHandler GetDamageHandlerOfCollider(Collider col)
	{

		m_GetDamageHandlerOfColliderResult = null;

		// try to fetch a known damagehandler on this target,
		if (!Instances.TryGetValue(col, out m_GetDamageHandlerOfColliderResult))
		{

			// no damagehandler info on record for this collider: see if we can find
			// one on the transform or any of its ancestors (the lowest ancestor with
			// a damagehandler will be cached as belonging to this collider)
			Transform t = col.transform;
			while ((t != null) && (m_GetDamageHandlerOfColliderResult == null))
			{
				m_GetDamageHandlerOfColliderResult = t.GetComponent<vp_DamageHandler>();
				t = t.parent;
			}

			Instances.Add(col, m_GetDamageHandlerOfColliderResult);		// add result to the dictionary (even if null)

		}

		return m_GetDamageHandlerOfColliderResult;

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


	/// <summary>
	/// Obsolete
	/// </summary>
	protected virtual void Respawn()
	{
	}


	/// <summary>
	/// Obsolete
	/// </summary>
	protected virtual void Reactivate()
	{
	}


	// -------- everything below this line is temp helper logic related to vp_Respawner transition in v1.4 --------
	

	/// <summary>
	/// 
	/// </summary>
	void CheckForObsoleteParams()
	{

		if (DeathEffect != null)
			Debug.LogWarning(this + "'DeathEffect' is obsolete! Please use the 'DeathSpawnObjects' array instead.");

		string parms = "";

		if (Respawns != false)
			parms += "Respawns, ";
		if (MinRespawnTime != -99999.0f)
			parms += "MinRespawnTime, ";
		if (MaxRespawnTime != -99999.0f)
			parms += "MaxRespawnTime, ";
		if (RespawnCheckRadius != -99999.0f)
			parms += "RespawnCheckRadius, ";
		if (RespawnSound != null)
			parms += "RespawnSound, ";

		if (parms != "")
		{
			parms = parms.Remove(parms.LastIndexOf(", "));
			Debug.LogWarning(string.Format("Warning + (" + this + ") The following parameters are obsolete: \"{0}\". Creating a temp vp_Respawner component. To remove this warning, see the UFPS menu -> Wizards -> Convert Old DamageHandlers.", parms));
			CreateTempRespawner();
		}

	}


	/// <summary>
	/// 
	/// </summary>
	public bool CreateTempRespawner()
	{

		if (GetComponent<vp_Respawner>() || GetComponent<vp_PlayerRespawner>())
		{
			DisableOldParams();	// we do this for the case where a prefab is updated with a vp_Respawner, but the damagehandler instance has overridden legacy params
			return false;
		}
		else
			CreateRespawnerForDamageHandler(this);
		DisableOldParams();
		return true;
	}


	/// <summary>
	/// 
	/// </summary>
	public static int GenerateRespawnersForAllDamageHandlers()
	{

		// --- update old vp_PlayerDamageHandlers to the new vp_FPPlayerDamageHandler ---

		vp_PlayerDamageHandler[] oldPlayerDamageHandlers = FindObjectsOfType(typeof(vp_PlayerDamageHandler)) as vp_PlayerDamageHandler[];
		if (oldPlayerDamageHandlers != null && oldPlayerDamageHandlers.Length > 0)
		{
			foreach (vp_PlayerDamageHandler p in oldPlayerDamageHandlers)
			{

				// if this vp_PlayerDamageHandler is on the same transform as a
				// vp_FPPlayerEventHandler we will boldly assume that it's an
				// object from UFPS 1.4.6b or older which needs to be updated.
				// if not, it might be a new and valid vp_PlayerDamageHandler
				// (added to something like a remote player) and we'll leave it

				if (p.transform.GetComponent<vp_FPPlayerEventHandler>() == null)
					continue;

				vp_FPPlayerDamageHandler n = p.gameObject.AddComponent<vp_FPPlayerDamageHandler>();

				n.AllowFallDamage = p.AllowFallDamage;
				n.DeathEffect = p.DeathEffect;
				n.DeathSound = p.DeathSound;
				n.DeathSpawnObjects = p.DeathSpawnObjects;
				n.FallDamageThreshold = p.FallDamageThreshold;
				n.ImpactDamageMultiplier = p.ImpactDamageMultiplier;
				n.ImpactDamageThreshold = p.ImpactDamageThreshold;
				n.m_Audio = p.m_Audio;
				n.CurrentHealth = p.CurrentHealth;
				n.m_StartPosition = p.m_StartPosition;
				n.m_StartRotation = p.m_StartRotation;
				n.MaxDeathDelay = p.MaxDeathDelay;
				n.MaxHealth = p.MaxHealth;
				n.MaxRespawnTime = p.MaxRespawnTime;
				n.MinDeathDelay = p.MinDeathDelay;
				n.MinRespawnTime = p.MinRespawnTime;
				n.RespawnCheckRadius = p.RespawnCheckRadius;
				n.Respawns = p.Respawns;
				n.RespawnSound = p.RespawnSound;

				DestroyImmediate(p);
			}

		}

		// --- move respawn variables of all damagehandlers to new respawner components ---

		vp_DamageHandler[] damageHandlers = FindObjectsOfType(typeof(vp_DamageHandler)) as vp_DamageHandler[];
		vp_DamageHandler[] FPPlayerDamageHandlers = FindObjectsOfType(typeof(vp_FPPlayerDamageHandler)) as vp_DamageHandler[];

		int amountOfObjectsUpdated = 0;

		foreach (vp_DamageHandler d in damageHandlers)
		{
			if (d.CreateTempRespawner())
				amountOfObjectsUpdated++;
		}

		foreach (vp_DamageHandler d in FPPlayerDamageHandlers)
		{
			if (d.CreateTempRespawner())
				amountOfObjectsUpdated++;
		}

		return amountOfObjectsUpdated;

	}


	/// <summary>
	/// 
	/// </summary>
	void DisableOldParams()
	{
		Respawns = false;
		MinRespawnTime = -99999.0f;
		MaxRespawnTime = -99999.0f;
		RespawnCheckRadius = -99999.0f;
		RespawnSound = null;
#if UNITY_EDITOR
		EditorUtility.SetDirty(this);
#endif
	}


	/// <summary>
	/// 
	/// </summary>
	static void CreateRespawnerForDamageHandler(vp_DamageHandler damageHandler)
	{

		if (damageHandler.gameObject.GetComponent<vp_Respawner>() || damageHandler.gameObject.GetComponent<vp_PlayerRespawner>())
			return;

		vp_Respawner respawner = null;

		if(damageHandler is vp_FPPlayerDamageHandler)
			respawner = damageHandler.gameObject.AddComponent<vp_PlayerRespawner>();
		else
			respawner = damageHandler.gameObject.AddComponent<vp_Respawner>();

		if (respawner == null)
			return;

		if (damageHandler.MinRespawnTime != -99999.0f)
			respawner.MinRespawnTime = damageHandler.MinRespawnTime;
		if (damageHandler.MaxRespawnTime != -99999.0f)
			respawner.MaxRespawnTime = damageHandler.MaxRespawnTime;
		if (damageHandler.RespawnCheckRadius != -99999.0f)
			respawner.ObstructionRadius = damageHandler.RespawnCheckRadius;
		if (damageHandler.RespawnSound != null)
			respawner.SpawnSound = damageHandler.RespawnSound;

	}


}

