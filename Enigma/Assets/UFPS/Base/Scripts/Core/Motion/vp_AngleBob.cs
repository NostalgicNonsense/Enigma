/////////////////////////////////////////////////////////////////////////////////
//
//	vp_AngleBob.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	this script will rotate its gameobject in a wavy (sinus / bob)
//					motion. NOTE: this script currently can not be used on items
//					that spawn from vp_SpawnPoints
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System.Collections;
using System.Collections.Generic;



public class vp_AngleBob : MonoBehaviour
{

	public Vector3 BobAmp = new Vector3(0.0f, 0.1f, 0.0f);		// wave motion strength
	public Vector3 BobRate = new Vector3(0.0f, 4.0f, 0.0f);		// wave motion speed
	public float YOffset = 0.0f;							// TIP: increase this to avoid ground intersection
	public bool RandomizeBobOffset = false;
	public bool LocalMotion = false;
	public bool FadeToTarget = false;	// NOTE: not available with local motion

	protected Transform m_Transform;
	protected Vector3 m_InitialRotation;
	protected Vector3 m_Offset;


	/// <summary>
	/// 
	/// </summary>
	protected virtual void Awake()
	{
		m_Transform = transform;
		m_InitialRotation = m_Transform.eulerAngles;
	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void OnEnable()
	{

		m_Transform.eulerAngles = m_InitialRotation;
		if (RandomizeBobOffset)
			YOffset = Random.value;

	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void Update()
	{

		if ((BobRate.x != 0.0f) && (BobAmp.x != 0.0f))
			m_Offset.x = vp_MathUtility.Sinus(BobRate.x, BobAmp.x, 0);

		if ((BobRate.y != 0.0f) && (BobAmp.y != 0.0f))
			m_Offset.y = vp_MathUtility.Sinus(BobRate.y, BobAmp.y, 0);

		if ((BobRate.z != 0.0f) && (BobAmp.z != 0.0f))
			m_Offset.z = vp_MathUtility.Sinus(BobRate.z, BobAmp.z, 0);

		if (!LocalMotion)
		{
			if (FadeToTarget)
				m_Transform.rotation = Quaternion.Lerp(m_Transform.rotation, Quaternion.Euler((m_InitialRotation + m_Offset) + (Vector3.up * YOffset)), Time.deltaTime);
			else
				m_Transform.eulerAngles = (m_InitialRotation + m_Offset) + (Vector3.up * YOffset);
		}
		else
		{
			m_Transform.eulerAngles = m_InitialRotation + (Vector3.up * YOffset);
			m_Transform.localEulerAngles += m_Transform.TransformDirection(m_Offset);
		}

	}



}