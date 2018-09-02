/////////////////////////////////////////////////////////////////////////////////
//
//	vp_PlayerClimbFixes.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	this script makes ladders work better in multiplayer (pending
//					a possible ladder system re-write). put it on a multiplayer local
//					+ remote player (might also improve singleplayer ladders)
//					NOTES:
//						1) likely to only work on straight ladders, and only tested
//							with the straight, default UFPS ladder prefab
//						2) currently does not work with vp_MPPlayerSpawner's 'Auto Add
//							Component'
//						3) the fixes in this class may eventually be spread out over
//							different classes, or incorporated into a new ladder class
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class vp_PlayerClimbFixes : MonoBehaviour
{

	protected Vector3 m_ForcedRotation = Vector3.zero;
	protected Vector3 m_ClimbablePosition = Vector3.zero;
	protected bool m_IsLocal = false;
	protected float m_MouseSensBak = 0;
	protected float m_LastHealth = 0.0f;
	protected float m_LastYPos = 0.0f;

	// --- properties ---

	protected vp_PlayerEventHandler m_Player = null;
	protected vp_PlayerEventHandler Player
	{
		get
		{
			if (m_Player == null)
				m_Player = transform.root.GetComponentInChildren<vp_PlayerEventHandler>();
			return m_Player;
		}
	}

	protected vp_BodyAnimator m_BodyAnimator = null;
	protected vp_BodyAnimator BodyAnimator
	{
		get
		{
			if (m_BodyAnimator == null)
				m_BodyAnimator = transform.root.GetComponentInChildren<vp_BodyAnimator>();
			return m_BodyAnimator;
		}
	}

	protected Animator m_Animator = null;
	protected Animator Animator
	{
		get
		{
			if (m_Animator == null)
				m_Animator = transform.root.GetComponentInChildren<Animator>();
			return m_Animator;
		}
	}

	protected vp_FPInput m_FPInput = null;
	protected vp_FPInput FPInput
	{
		get
		{
			if (m_FPInput == null)
				m_FPInput = transform.root.GetComponentInChildren<vp_FPInput>();
			return m_FPInput;
		}
	}


	/// <summary>
	/// 
	/// </summary>
	void OnEnable()
	{

		if (Player != null)
			Player.Register(this);

	}


	/// <summary>
	/// 
	/// </summary>
	void OnDisable()
	{

		if (Player != null)
			Player.Unregister(this);
	}


	/// <summary>
	/// 
	/// </summary>
	void Start()
	{

		if (BodyAnimator is vp_FPBodyAnimator)
			m_IsLocal = true;

	}


	/// <summary>
	/// 
	/// </summary>
	void LateUpdate()
	{

		// abort if not climbing
		if (!Player.Climb.Active)
		{
			if (Animator.speed != 1)
				Animator.speed = 1;
			return;
		}

		// if climbing, disallow looking sideways
		if (m_IsLocal && FPInput != null)
			FPInput.MouseLookSensitivity.x = 0.0f;

		// force controller rotation
		m_ForcedRotation.x = Player.transform.root.eulerAngles.x;
		Player.transform.root.eulerAngles = m_ForcedRotation;

		// force body model rotation
		BodyAnimator.transform.localPosition = Vector3.zero + BodyAnimator.ClimbOffset;
		BodyAnimator.transform.localEulerAngles = Vector3.zero + BodyAnimator.ClimbRotationOffset;

		// keep controller distance to climbable (prevents lerping the body through walls)
		Vector3 pos = transform.InverseTransformPoint(m_ClimbablePosition);
		if (pos.z < 1.0f)
			transform.position += transform.forward * -(1.0f - Mathf.Abs(pos.z));

		// slow down animation if moving vertically
		if (Animator != null)
		{
			if (Mathf.Abs(Mathf.Abs(m_LastYPos) - Mathf.Abs(transform.position.y)) < 0.01f)
				Animator.speed *= Mathf.Min(Time.deltaTime * 50.0f, 0.99f);
			else
				Animator.speed = 1;
		}
		m_LastYPos = transform.position.y;

		// force dismount upon damage
		if (m_LastHealth > Player.Health.Get())
		{
			vp_Timer.In(0.1f, delegate()	// allow a slight moment for explosion forces to take effect before messing with the controller
			{
				Player.Climb.TryStop();
			});
		}
		m_LastHealth = Player.Health.Get();

	}


	/// <summary>
	/// 
	/// </summary>
	void OnStart_Climb()
	{

		// store health for dismount-on-damage logic in LateUpdate
		m_LastHealth = Player.Health.Get();

		// back up mouse sensitivity (x is zeroed in LateUpdate)
		if (m_IsLocal && FPInput != null)
			m_MouseSensBak = FPInput.MouseLookSensitivity.x;

		StoreClimbableInfo();

	}


	/// <summary>
	/// 
	/// </summary>
	void StoreClimbableInfo()
	{

		Vector3 rotation = Vector3.zero;
		vp_Climb climbable = null;

		// find the first climbable within a 5 meter radius from player
		Collider[] col = Physics.OverlapSphere(BodyAnimator.transform.position, 5);	// find all colliders
		foreach (Collider c in col)
		{
			if (climbable != null)	// stop looking when we have found one
				continue;
			climbable = c.GetComponentInChildren<vp_Climb>();	// see if collider has a climbable
			if (climbable != null)
				rotation = climbable.transform.parent.eulerAngles;	// found one! store its rotation
		}

		m_ForcedRotation = rotation;
		m_ClimbablePosition = climbable.transform.position;

	}


	/// <summary>
	/// 
	/// </summary>
	void OnStop_Climb()
	{

		// restore mouse sensitivity
		if (m_IsLocal && FPInput != null)
			FPInput.MouseLookSensitivity.x = m_MouseSensBak;

		// snap body position to center of controller
		BodyAnimator.transform.localPosition = Vector3.zero;

		// snap rotation to latest used climb rotation
		Player.Rotation.Set(m_ForcedRotation);
		m_ForcedRotation = Vector3.zero;

	}



}