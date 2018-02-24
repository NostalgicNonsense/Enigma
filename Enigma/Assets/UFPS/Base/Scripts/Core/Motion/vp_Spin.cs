/////////////////////////////////////////////////////////////////////////////////
//
//	vp_Spin.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
// http://www.opsive.com
//
//	description:	this component will make its gameobject spin continuously
//					around a set vector / speed
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class vp_Spin : MonoBehaviour
{

	public Vector3 RotationSpeed = new Vector3(0, 90, 0);
	public bool Accelerate = false;
	public float Acceleration = 1.0f;
	protected Transform m_Transform;
	protected float m_CurrentRotationSpeed = 0.0f;

#if UNITY_EDITOR
		[vp_HelpBox("This script rotates a gameobject at a constant speed. The 'Rotation Speed' vector determines torque around each axis.", UnityEditor.MessageType.None, typeof(vp_Spin), null, true)]
		public float helpbox;
		[vp_Separator]
		public float separator;
#endif


	/// <summary>
	/// 
	/// </summary>
	protected virtual void Start()
	{
		m_Transform = transform;
	}


	/// <summary>
	/// 
	/// </summary>
	void OnEnable()
	{
		m_CurrentRotationSpeed = 0.0f;
	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void Update()
	{

		if (Accelerate)
			m_CurrentRotationSpeed = Mathf.Lerp(m_CurrentRotationSpeed, 1.0f, Time.deltaTime * Acceleration);
		else
			m_CurrentRotationSpeed = 1.0f;

		m_Transform.Rotate((RotationSpeed * m_CurrentRotationSpeed) * Time.deltaTime);

	}


}