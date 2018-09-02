/////////////////////////////////////////////////////////////////////////////////
//
//	vp_CapsuleController.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	adds UFPS state & preset system functionality to a unity
//					capsulecollider and implements crouching logic. intended use
//					is for multiplayer or AI, to avoid having many heavy controllers
//					(like 'vp_FPController') in the same scene.
//
//					NOTE: this class is used by the UFPS multiplayer starter kit for
//					remote-controlled players. it currently has no motor, and must be
//					extended if you want to use it with AI
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(CapsuleCollider))]
[RequireComponent(typeof(Rigidbody))]

public class vp_CapsuleController : vp_Controller
{

	protected CapsuleCollider m_CapsuleCollider = null;
	protected CapsuleCollider CapsuleCollider
	{
		get
		{
			if (m_CapsuleCollider == null)
			{
				m_CapsuleCollider = Collider as CapsuleCollider;
				if (m_CapsuleCollider != null && m_CapsuleCollider.isTrigger)
					m_CapsuleCollider = null;
			}
			return m_CapsuleCollider;
		}
	}


	/// <summary>
	/// initializes collider dimension variables for crouching and standing up
	/// </summary>
	protected override void InitCollider()
	{

		// NOTES:
		// 1) by default, collider width is half the height, with pivot at the feet
		// 2) don't change radius in-game (it may cause missed wall collisions)
		// 3) controller height can never be smaller than radius

		m_NormalHeight = CapsuleCollider.height;
		CapsuleCollider.center = m_NormalCenter = (m_NormalHeight * (Vector3.up * 0.5f));
		CapsuleCollider.radius = m_NormalHeight * DEFAULT_RADIUS_MULTIPLIER;
		m_CrouchHeight = m_NormalHeight * PhysicsCrouchHeightModifier;
		m_CrouchCenter = m_NormalCenter * PhysicsCrouchHeightModifier;

		Collider.transform.localPosition = Vector3.zero;

	}


	/// <summary>
	/// refresh collider dimension variables depending on whether we're
	/// crouching or standing up
	/// </summary>
	protected override void RefreshCollider()
	{

		if (Player.Crouch.Active && !(MotorFreeFly && !Grounded))	// crouching & not flying
		{
			CapsuleCollider.height = m_NormalHeight * PhysicsCrouchHeightModifier;
			CapsuleCollider.center = m_NormalCenter * PhysicsCrouchHeightModifier;
		}
		else	// standing up (whether flying or not)
		{
			CapsuleCollider.height = m_NormalHeight;
			CapsuleCollider.center = m_NormalCenter;
		}

	}


	/// <summary>
	/// enables or disables the collider (CapsuleCollider)
	/// </summary>
	public override void EnableCollider(bool isEnabled = true)
	{

		if (CapsuleCollider != null)
			CapsuleCollider.enabled = isEnabled;

	}


	/// <summary>
	/// returns the current collider radius
	/// </summary>
	protected override float OnValue_Radius
	{
		get	{	return CapsuleCollider.radius;	}
	}


	/// <summary>
	/// returns the current collider height
	/// </summary>
	protected override float OnValue_Height
	{
		get	{	return CapsuleCollider.height;	}
	}




}







