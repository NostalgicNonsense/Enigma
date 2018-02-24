/////////////////////////////////////////////////////////////////////////////////
//
//	vp_Grenade.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	this script will apply a rigidbody impulse to its gameobject
//					in the moment it awakes (spawns), and kill it in 'LifeTime'
//					seconds. if used as a projectile it is perfect for grenades
//					and the inflictor (original source) of the damage will be
//					reported to the damage handler. the vp_DamageHandler on the
//					gameobject should have an explosion as a death spawn object
//
///////////////////////////////////////////////////////////////////////////////// 

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(vp_DamageHandler))]

public class vp_Grenade : MonoBehaviour
{

	public float LifeTime = 3.0f;
	public float RigidbodyForce = 10.0f;		// this force will be applied to the rigidbody when spawned
	public float RigidbodySpin = 0.0f;			// this much random torque will be applied to rigidbody when spawned.
												// NOTE: spin is currently not recommended for use with the UFPS multiplayer add-on, since rigidbodies are not yet serialized!

	protected Rigidbody m_Rigidbody = null;
	protected Transform m_Source = null;				// immediate cause of the damage
	protected Transform m_OriginalSource = null;		// initial cause of the damage


	/// <summary>
	/// 
	/// </summary>
	protected virtual void Awake()
	{
		m_Rigidbody = GetComponent<Rigidbody>();
	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void OnEnable()
	{

		if (m_Rigidbody == null)
			return;

		// destroy the grenade object in 'lifetime' seconds. this will only work
		// if the object has a vp_DamageHandler-derived component on it
		vp_Timer.In(LifeTime, ()=>
		{
			transform.SendMessage("DieBySources", new Transform[] { m_Source, m_OriginalSource }, SendMessageOptions.DontRequireReceiver);
		});

		// apply force on spawn
		if (RigidbodyForce != 0.0f)
			m_Rigidbody.AddForce((transform.forward * RigidbodyForce), ForceMode.Impulse); 
		if (RigidbodySpin != 0.0f)
			m_Rigidbody.AddTorque(Random.rotation.eulerAngles * RigidbodySpin);
		
	}


	/// <summary>
	/// sets the inflictor (original source) of any resulting damage.
	/// this is called by the 'vp_Shooter' script and is picked up by
	/// various other scripts, especially in UFPS multiplayer add-on.
	/// NOTE: this method must be called between spawning, and before
	/// 'OnEnable' is called.
	/// </summary>
	public void SetSource(Transform source)
	{
		m_Source = transform;
		m_OriginalSource = source;
	}


}