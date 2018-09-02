/////////////////////////////////////////////////////////////////////////////////
//
//	vp_Bob.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	this script will move its gameobject in a wavy (sinus / bob)
//					motion. NOTE: this script currently can not be used on items
//					that spawn from vp_SpawnPoints
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System.Collections;
using System.Collections.Generic;



public class vp_Bob : MonoBehaviour
{

	public Vector3 BobAmp = new Vector3(0.0f, 0.1f, 0.0f);		// wave motion strength
	public Vector3 BobRate = new Vector3(0.0f, 4.0f, 0.0f);		// wave motion speed
	public float BobOffset = 0.0f;								// bob offset (can be used to make pickups in a row move independently)
	public float GroundOffset = 0.0f;							// TIP: increase this to avoid ground intersection
	public bool RandomizeBobOffset = false;
	public bool LocalMotion = false;
	public bool SmoothGroundOffset = false;						// if true, the ground offset will be zero OnEnable, then applied slowly

	protected Transform m_Transform;
	protected Vector3 m_Offset;
	protected float m_CurrentGroundOffset = 0.0f;
	private const float GROUND_OFFSET_LERP_SPEED = 2.0f;

	[HideInInspector]
	public Vector3 InitialPosition = Vector3.zero;


	/// <summary>
	/// 
	/// </summary>
	protected virtual void Awake()
	{

		m_Transform = transform;
		InitialPosition = m_Transform.position;

	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void OnEnable()
	{

		m_CurrentGroundOffset = 0.0f;
		if (RandomizeBobOffset)
			BobOffset = Random.value;

	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void Update()
	{

		if (SmoothGroundOffset)
			m_CurrentGroundOffset = Mathf.Lerp(m_CurrentGroundOffset, GroundOffset, Time.deltaTime * GROUND_OFFSET_LERP_SPEED);
		else
			m_CurrentGroundOffset = GroundOffset;

		if ((BobRate.x != 0.0f) && (BobAmp.x != 0.0f))
			m_Offset.x = vp_MathUtility.Sinus(BobRate.x, BobAmp.x, BobOffset);

		if ((BobRate.y != 0.0f) && (BobAmp.y != 0.0f))
			m_Offset.y = vp_MathUtility.Sinus(BobRate.y, BobAmp.y, BobOffset);

		if ((BobRate.z != 0.0f) && (BobAmp.z != 0.0f))
			m_Offset.z = vp_MathUtility.Sinus(BobRate.z, BobAmp.z, BobOffset);

		if (!LocalMotion)
			m_Transform.position = (InitialPosition + m_Offset) + (Vector3.up * m_CurrentGroundOffset);
		else
		{
			m_Transform.position = InitialPosition + (Vector3.up * m_CurrentGroundOffset);
			m_Transform.localPosition += m_Transform.TransformDirection(m_Offset);
		}

	}


}