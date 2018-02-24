/////////////////////////////////////////////////////////////////////////////////
//
//	vp_RigidbodyImpulse.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	this script will apply a rigidbody impulse to its gameobject
//					in the moment it awakes (spawns)
//
///////////////////////////////////////////////////////////////////////////////// 

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]

public class vp_RigidbodyImpulse : MonoBehaviour
{

	public Vector3 RigidbodyForce = new Vector3(0.0f, 5.0f, 0.0f);	// this force will be applied to the rigidbody when spawned
	public bool LocalForce = false;

	public float RigidbodySpin = 0.2f;								// this much random torque will be applied to rigidbody when spawned

	protected Rigidbody m_Rigidbody = null;
	protected Rigidbody Rigidbody
	{
		get
		{
			if (m_Rigidbody == null)
				m_Rigidbody = GetComponent<Rigidbody>(); ;
			return m_Rigidbody;
		}
	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void OnEnable()
	{

		if (Rigidbody == null)
			return;

		if (RigidbodyForce != Vector3.zero)
		{
			if (!LocalForce)
				m_Rigidbody.AddForce(RigidbodyForce, ForceMode.Impulse);
			else
				m_Rigidbody.AddForce(transform.root.TransformDirection(RigidbodyForce), ForceMode.Impulse);
		}
		if (RigidbodySpin != 0.0f)
			m_Rigidbody.AddRelativeTorque(Random.rotation.eulerAngles * (Random.value < 0.5f ? RigidbodySpin : -RigidbodySpin));

	}


}