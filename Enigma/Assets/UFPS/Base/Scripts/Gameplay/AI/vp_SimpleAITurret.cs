/////////////////////////////////////////////////////////////////////////////////
//
//	vp_SimpleAITurret.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	just a very quick test to verify that vp_Shooter can be used
//					independently of the FPPlayer
//
/////////////////////////////////////////////////////////////////////////////////
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(vp_Shooter))]

public class vp_SimpleAITurret : MonoBehaviour
{

	public float ViewRange = 10.0f;
	public float AimSpeed = 50.0f;
	public float WakeInterval = 2.0f;
	public float FireAngle = 10.0f;

	protected vp_Shooter m_Shooter = null;
	protected Transform m_Transform = null;
	protected Transform m_Target = null;
	protected Collider m_TargetCollider = null;
	protected vp_Timer.Handle m_Timer = new vp_Timer.Handle();


	/// <summary>
	/// 
	/// </summary>
	protected virtual void Start()
	{
		m_Shooter = GetComponent<vp_Shooter>();
		m_Transform = transform;
	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void Update()
	{

		// turn on and off with 'UpdateInterval'
		if (!m_Timer.Active)
		{
			vp_Timer.In(WakeInterval, delegate()
			{
				if (m_Target == null)
					m_Target = ScanForLocalPlayer();
				else
				{
					m_Target = null;
					m_TargetCollider = null;
				}
			}, m_Timer);
		}

		if (m_Target != null)
			AttackTarget();

	}


	/// <summary>
	/// scans the area for the local player and returns its
	/// transform if present, and if we have line-of-sight
	/// </summary>
	protected virtual Transform ScanForLocalPlayer()
	{

		Collider[] colliders = Physics.OverlapSphere(m_Transform.position, ViewRange, (1 << vp_Layer.LocalPlayer));
		foreach (Collider hit in colliders)
		{

			// found the local player within range, now see if we have line-of-sight
			RaycastHit blocker;
			Physics.Linecast(m_Transform.position, hit.transform.position + Vector3.up, out blocker, vp_Layer.Mask.BulletBlockers);

			// skip if raycast hit an object that wasn't the intended target
			if (blocker.collider != null && blocker.collider != hit)
				continue;

			// we have line of sight to the local player! return its transform
			return hit.transform;

		}

		return null;

	}


	/// <summary>
	/// smoothly aims at target while firing the shooter
	/// </summary>
	protected virtual void AttackTarget()
	{

		// smoothly aim at target
		if (m_TargetCollider == null)
			m_TargetCollider = m_Target.GetComponent<Collider>();
		Vector3 dir;
		if (m_TargetCollider != null)
			dir = (m_TargetCollider.bounds.center - m_Transform.position);
		else
			dir = (m_Target.transform.position - m_Transform.position);
		m_Transform.rotation = Quaternion.RotateTowards(m_Transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * AimSpeed);

		// fire the shooter
		if(Mathf.Abs(vp_3DUtility.LookAtAngleHorizontal(m_Transform.position, m_Transform.forward, m_Target.position)) < FireAngle)
			m_Shooter.TryFire();

	}


}
