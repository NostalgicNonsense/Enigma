/////////////////////////////////////////////////////////////////////////////////
//
//	vp_Powerup.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	a generic powerup base class which can be inherited to create
//					various types of powerups. a powerup may be a billboard
//					or a 3d object. it may float in the air using bob and/or rotate.
//					it can be made a physical object if you add a rigidbody to it
//					
//					NOTES:
//
//					1) this script can not be added to a gameobject directly.
//					instead, you must create a class derived from this one, with
//					an overridden 'TryGive' method in it, and add that script instead
//
//					2) always put powerup gameobjects in the 'vp_Layer.Pickup' layer
//					(default: 26) or they will receive bullet decals and may cause
//					buggy player physics
//
//					3) if you wish to use a rigidbody for the powerup, the gamobject
//					will need a second, non-trigger collider. if so, be sure to make
//					the rigidbody collider smaller than the trigger collider, or it may
//					disable the powerup or briefly block player movement on contact
//
//					4) in VR the billboard will have a slightly off angle in the editor,
//					however in a standalone build it will look correct
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(SphereCollider))]
[RequireComponent(typeof(AudioSource))]

public abstract class vp_Powerup : MonoBehaviour
{

	protected Transform m_Transform = null;
	protected Rigidbody m_Rigidbody = null;
	protected AudioSource m_Audio = null;
	protected Renderer m_Renderer = null;

	public List<string> RecipientTags = new List<string>();

	Collider m_LastCollider = null;
	vp_PlayerEventHandler m_Recipient = null;

	public string GiveMessage = "Got a powerup!";
	public string FailMessage = "You currently can't get this powerup!";

	// position
	protected Vector3 m_SpawnPosition = Vector3.zero;
	protected Vector3 m_SpawnScale = Vector3.zero;

	// appearance
	public bool Billboard = false;

	// motion
	public Vector3 Spin = Vector3.zero;				// rotation speed
	public float BobAmp = 0.0f;						// air floating strength
	public float BobRate = 0.0f;					// air floating speed
	public float BobOffset = -1.0f;					// bob offset for making powerups in a row float independently
	public Vector3 RigidbodyForce = Vector3.zero;	// in case powerup has a rigidbody, this force will be applied to it when spawned
	public float RigidbodySpin = 0.0f;				// this much random torque will be applied to rigidbody when spawned

	// after triggered, the powerup will respawn in this many seconds
	public float RespawnDuration = 10.0f;
	public float RespawnScaleUpDuration = 0.0f;
	public float RemoveDuration = 0.0f;

	// sounds
	public AudioClip PickupSound = null;		// player triggers the powerup
	public AudioClip PickupFailSound = null;	// player failed to pick up the powerup (i.e. ammo full)
	public AudioClip RespawnSound = null;		// powerup respawns
	public bool PickupSoundSlomo = true;
	public bool FailSoundSlomo = true;
	public bool RespawnSoundSlomo = true;

	// when this is true, the powerup has been triggered and will
	// disappear as soon as the pickup sound has finished playing
	protected bool m_Depleted = false;

	// this boolean prevents the trigger from playing fail sounds
	// continuously if the player is standing inside the trigger
	protected bool m_AlreadyFailed = false;

	protected vp_Timer.Handle m_RespawnTimer = new vp_Timer.Handle();



	/// <summary>
	/// 
	/// </summary>
	protected virtual void Start()
	{

		m_Transform = transform;
		m_Rigidbody = GetComponent<Rigidbody>();
		m_Audio = GetComponent<AudioSource>();
		m_Renderer = GetComponent<Renderer>();

		// set the main collider of this gameobject to be a trigger
		GetComponent<Collider>().isTrigger = true;

		// some default audio settings
		m_Audio.clip = PickupSound;
		m_Audio.playOnAwake = false;
		m_Audio.minDistance = 3;
		m_Audio.maxDistance = 150;
		m_Audio.rolloffMode = AudioRolloffMode.Linear;
		m_Audio.dopplerLevel = 0.0f;

		// store the initial position
		m_SpawnPosition = m_Transform.position;
		m_SpawnScale = m_Transform.localScale;
		RespawnScaleUpDuration = (m_Rigidbody == null) ? Mathf.Abs(RespawnScaleUpDuration) : 0.0f;

		// give bob a random start offset if value is not set
		if (BobOffset == -1.0f)
			BobOffset = Random.value;

		if (RecipientTags.Count == 0)
			RecipientTags.Add("Player");

		if (RemoveDuration != 0.0f)
			vp_Timer.In(RemoveDuration, Remove);

		if (m_Rigidbody != null)
		{
			if(RigidbodyForce != Vector3.zero)
				m_Rigidbody.AddForce(RigidbodyForce, ForceMode.Impulse);
			if(RigidbodySpin != 0.0f)
				m_Rigidbody.AddTorque(Random.rotation.eulerAngles * RigidbodySpin);
		}
	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void Update()
	{

		// handle rotation and bob, if enabled
		UpdateMotion();

		// remove the powerup if it has been depleted and the pickup
		// sound has stopped playing
		if (m_Depleted && !m_Audio.isPlaying)
			Remove();

		if (!m_Depleted && (m_Rigidbody != null) && m_Rigidbody.IsSleeping() && !m_Rigidbody.isKinematic)
		{
			m_Rigidbody.isKinematic = true;
			foreach (Collider c in GetComponents<Collider>())
			{
				if (!c.isTrigger)
					c.enabled = false;
			}
		}

	}


	/// <summary>
	/// rotates object depending on whether it's a camera oriented
	/// billboard, a static object or a spinning 3d object. if neither
	/// 'Billboard' nor 'Spin' are set, it just sits still
	/// </summary>
	protected virtual void UpdateMotion()
	{

		// cancel scripted motion if we have rigidbody physics
		if (m_Rigidbody != null)
			return;

		if (Billboard)
		{

			// in VR, we must rotate the billboard towards the eye camera that is
			// currently rendering, or the angle will become askew. however, this
			// only works in a standalone build since 'Camera.current' will return
			// an arbitrary scene view camera in the editor
			if (vp_Gameplay.IsVR && !Application.isEditor)
			{
				if (Camera.current != null)
					m_Transform.LookAt(Camera.current.transform);
			}
			else
			{
				// we are either in the editor or not in VR: look at the main camera
				if (Camera.main != null)
					m_Transform.LookAt(Camera.main.transform);
			}

		}
		else
			m_Transform.localEulerAngles += Spin * Time.deltaTime;

		// if we have bob values, make the powerup float up and down in the air
		if (BobRate != 0.0f && BobAmp != 0.0f)
			m_Transform.position = m_SpawnPosition + Vector3.up *
				(Mathf.Cos((Time.time + BobOffset) * (BobRate * 10.0f)) * BobAmp);

		if (m_Transform.localScale != m_SpawnScale)
			m_Transform.localScale = Vector3.Lerp(m_Transform.localScale, m_SpawnScale, Time.deltaTime / RespawnScaleUpDuration);
			
	}


	/// <summary>
	/// this is triggered when an object enters the collider
	/// </summary>
	protected virtual void OnTriggerEnter(Collider col)
	{

		// only do something if the trigger is still active
		if (m_Depleted)
			return;

		// see if the colliding object was a valid recipient
		foreach(string s in RecipientTags)
		{
			if(col.gameObject.tag == s)
				goto isRecipient;
		}
		return;
		isRecipient:

		// if collider is not the same as last time we were picked up
		// (or this is the first time) scan the collider gameobject for
		// a player event handler
		if (col != m_LastCollider)
			m_Recipient = col.gameObject.GetComponent<vp_PlayerEventHandler>();

		if (m_Recipient == null)
			return;

		if (TryGive(m_Recipient))
		{
			m_Audio.pitch = PickupSoundSlomo ? Time.timeScale : 1.0f;
			m_Audio.Play();
			m_Renderer.enabled = false;
			m_Depleted = true;
			if(m_Recipient is vp_FPPlayerEventHandler)
				(m_Recipient as vp_FPPlayerEventHandler).HUDText.Send(GiveMessage);
		}
		else if (!m_AlreadyFailed)
		{
			if (!vp_Gameplay.IsMultiplayer || (vp_Gameplay.IsMultiplayer && (m_Recipient is vp_FPPlayerEventHandler)))
			{
				m_Audio.pitch = FailSoundSlomo ? Time.timeScale : 1.0f;
				m_Audio.PlayOneShot(PickupFailSound);
			}
			m_AlreadyFailed = true;
			if (m_Recipient is vp_FPPlayerEventHandler)
				(m_Recipient as vp_FPPlayerEventHandler).HUDText.Send(FailMessage);
		}

	}


	/// <summary>
	/// this is executed when an object leaves the trigger
	/// </summary>
	protected virtual void OnTriggerExit(Collider col)
	{

		// reset fail status
		m_AlreadyFailed = false;

	}


	/// <summary>
	/// IMPORTANT: this method must be overridden by the implementing powerup class.
	/// see 'vp_HealthPowerup', 'vp_SpeedPowerup' and 'vp_SlomoPowerup' for examples
	/// </summary>
	protected virtual bool TryGive(vp_PlayerEventHandler player)
	{

		return true;

	}


	/// <summary>
	/// deactivates this powerup and respawns it in 'RespawnDuration' seconds. if
	/// 'RespawnDuration' is zero, the powerup will be destroyed for good
	/// </summary>
	protected virtual void Remove()
	{

		if (this == null)
			return;

		if (RespawnDuration == 0.0f)
			vp_Utility.Destroy(gameObject);
		else
		{
			if (!m_RespawnTimer.Active)
			{
				vp_Utility.Activate(gameObject, false);
				vp_Timer.In(RespawnDuration, Respawn, m_RespawnTimer);
			}
		}

	}


	/// <summary>
	/// handles respawn position, scaleup, rendering activation
	/// and sets a new random bob value if applicable
	/// </summary>
	protected virtual void Respawn()
	{

		if (m_Transform == null)
			return;

		m_RespawnTimer.Cancel();	// cancel timer in case we didn't get here via timer

		m_Transform.position = m_SpawnPosition;

		if ((m_Rigidbody == null) && RespawnScaleUpDuration > 0.0f)
			m_Transform.localScale = Vector3.zero;

		m_Renderer.enabled = true;
		vp_Utility.Activate(gameObject);
		m_Audio.pitch = (RespawnSoundSlomo ? Time.timeScale : 1.0f);
		m_Audio.PlayOneShot(RespawnSound);
		m_Depleted = false;

		if (BobOffset == -1.0f)
			BobOffset = Random.value;

		if (m_Rigidbody != null)
		{
			m_Rigidbody.isKinematic = false;
			foreach (Collider c in GetComponents<Collider>())
			{
				if (!c.isTrigger)
					c.enabled = true;
			}
		}

	}


}
