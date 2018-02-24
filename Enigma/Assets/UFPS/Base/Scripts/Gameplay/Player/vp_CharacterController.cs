/////////////////////////////////////////////////////////////////////////////////
//
//	vp_CharacterController.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	this script complements a unity charactercontroller with UFPS
//					state and event system functionality, along with resize logic
//					for the various states
//
//					NOTE: you may not want to use this as-is. for a 1st person
//					player there is the extended script 'vp_FPController' which
//					has a tweakable motor and lots more functionality. for a remote
//					player or AI you probably instead want 'vp_CapsuleController'
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(CharacterController))]

public class vp_CharacterController : vp_Controller
{


	private CharacterController m_CharacterController = null;
	public CharacterController CharacterController
	{
		get
		{
			if (m_CharacterController == null)
				m_CharacterController = gameObject.GetComponent<CharacterController>();
			return m_CharacterController;
		}
	}


	/// <summary>
	/// sets up various collider dimension variables for dynamic crouch
	/// logic, depending on whether the collider is a capsule or a character
	/// controller
	/// </summary>
	protected override void InitCollider()
	{

		// NOTES:
		// 1) by default, collider width is half the height, with pivot at the feet
		// 2) don't change radius in-game (it may cause missed wall collisions)
		// 3) controller height can never be smaller than radius

		m_NormalHeight = CharacterController.height;
		CharacterController.center = m_NormalCenter = (m_NormalHeight * (Vector3.up * 0.5f));
		CharacterController.radius = m_NormalHeight * DEFAULT_RADIUS_MULTIPLIER;
		m_CrouchHeight = m_NormalHeight * PhysicsCrouchHeightModifier;
		m_CrouchCenter = m_NormalCenter * PhysicsCrouchHeightModifier;

		//Collider.transform.localPosition = Vector3.zero;

	}


	/// <summary>
	/// updates charactercontroller and physics trigger sizes
	/// depending on player Crouch activity
	/// </summary>
	protected override void RefreshCollider()
	{

		if (Player.Crouch.Active && !(MotorFreeFly && !Grounded))	// crouching & not flying
		{
			CharacterController.height = m_NormalHeight * PhysicsCrouchHeightModifier;
			CharacterController.center = m_NormalCenter * PhysicsCrouchHeightModifier;
		}
		else	// standing up (whether flying or not)
		{
			CharacterController.height = m_NormalHeight;
			CharacterController.center = m_NormalCenter;
		}

	}


	/// <summary>
	/// returns the current step offset
	/// </summary>
	protected virtual float OnValue_StepOffset
	{
		get { return CharacterController.stepOffset; }
	}


	/// <summary>
	/// returns the current slopeLimit
	/// </summary>
	protected virtual float OnValue_SlopeLimit
	{
		get { return CharacterController.slopeLimit; }
	}


	/// <summary>
	/// moves the controller by 'direction'. the controller will be affected
	/// by gravity, constrained by collisions and slide along colliders
	/// </summary>
	protected virtual void OnMessage_Move(Vector3 direction)
	{
		if (CharacterController.enabled)
			CharacterController.Move(direction);
	}


	/// <summary>
	/// returns the current collider radius
	/// </summary>
	protected override float OnValue_Radius
	{
		get	{	return CharacterController.radius;	}
	}



	/// <summary>
	/// returns the current collider height
	/// </summary>
	protected override float OnValue_Height
	{
		get { return CharacterController.height; }
	}



	
}







