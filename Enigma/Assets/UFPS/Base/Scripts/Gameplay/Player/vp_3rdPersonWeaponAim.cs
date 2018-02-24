/////////////////////////////////////////////////////////////////////////////////
//
//	vp_3rdPersonWeaponAim.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	this script can be added on a 3rd person weapon object to make the
//					character's main hand & weapon aim more accurately towards the
//					cursor. it also imposes recoil from the corresponding vp_FPWeapon
//					onto the 3rd person hand & weapon.
//
/////////////////////////////////////////////////////////////////////////////////
using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class vp_3rdPersonWeaponAim : MonoBehaviour
{

	public GameObject Hand = null;

#if UNITY_EDITOR
	[vp_HelpBox("If left empty, 'Hand' will fallback to the nearest ancestor with 'Hand' in its name, or just the parent.", UnityEditor.MessageType.None, typeof(vp_3rdPersonWeaponAim), null, false, vp_PropertyDrawerUtility.Space.Nothing)]
	public float helpbox2;
#endif
	
	[Range(0.0f, 360.0f)]
	public float AngleAdjustX = 0.0f;
	[Range(0.0f, 360.0f)]
	public float AngleAdjustY = 0.0f;
	[Range(0.0f, 360.0f)]
	public float AngleAdjustZ = 0.0f;

#if UNITY_EDITOR
	[vp_HelpBox("Use these sliders to tweak the character's main hand & weapon to aim more accurately towards the cursor. TIP: Start the game and enable 'Keep Aiming' first.", UnityEditor.MessageType.None, typeof(vp_3rdPersonWeaponAim), null, false, vp_PropertyDrawerUtility.Space.Nothing)]
	public float helpbox3;
#endif

	[Range(0.0f, 5.0f)]
	public float RecoilFactorX = 1.0f;
	[Range(0.0f, 5.0f)]
	public float RecoilFactorY = 1.0f;
	[Range(0.0f, 5.0f)]
	public float RecoilFactorZ = 1.0f;

#if UNITY_EDITOR
	[vp_HelpBox("Adjust to what extent recoil from the vp_FPWeapon will affect the 3rd person hand & weapon.", UnityEditor.MessageType.None, typeof(vp_3rdPersonWeaponAim), null, false, vp_PropertyDrawerUtility.Space.Nothing)]
	public float helpbox4;
#endif

#if UNITY_EDITOR
	public bool KeepAiming = false;
	[vp_HelpBox("Enable while editing at runtime to make the character keep pointing the gun forwards.", UnityEditor.MessageType.None, typeof(vp_3rdPersonWeaponAim), null, false, vp_PropertyDrawerUtility.Space.Nothing)]
	public float helpbox5;
#endif

#if UNITY_EDITOR
	[vp_HelpBox("PLEASE NOTE:\n\n• This gameobject should be assigned to the 'Rendering -> 3rd Person Weapon' slot of a vp_Weapon or vp_FPWeapon component.\n\n• This script has a purely cosmetical function and does not affect gameplay in any way (!). To change gameplay-accuracy, look for the 'Spread' parameter on the 1st person weapon's shooter component.", UnityEditor.MessageType.Info, typeof(vp_3rdPersonWeaponAim), null, false, vp_PropertyDrawerUtility.Space.Nothing)]
	public float helpbox;
#endif

	protected Quaternion m_DefaultRotation;
	protected Vector3 m_ReferenceUpDir;
	protected Vector3 m_ReferenceLookDir;
	protected Quaternion m_HandBoneRotDif;
	protected Vector3 m_WorldDir = Vector3.zero;

	// --- properties ---

	protected Transform m_Transform = null;
	public Transform Transform
	{
		get
		{
			if (m_Transform == null)
				m_Transform = transform;
			return m_Transform;
		}
	}
	
	protected vp_PlayerEventHandler m_Player = null;
	public vp_PlayerEventHandler Player
	{
		get
		{
			if (m_Player == null)
				m_Player = (vp_PlayerEventHandler)Root.GetComponentInChildren(typeof(vp_PlayerEventHandler));
			return m_Player;
		}
	}
	
	vp_WeaponHandler m_WeaponHandler = null;
	public vp_WeaponHandler WeaponHandler
	{
		get
		{
			if (m_WeaponHandler == null)
				m_WeaponHandler = (vp_WeaponHandler)Root.GetComponentInChildren(typeof(vp_WeaponHandler));
			return m_WeaponHandler;
		}
	}
	
	protected Animator m_Animator;
	protected Animator Animator
	{
		get
		{
			if (m_Animator == null)
				m_Animator = Root.GetComponentInChildren<Animator>();
			return m_Animator;
		}
	}

	Transform m_Root = null;
	Transform Root
	{
		get
		{
			if (m_Root == null)
				m_Root = Transform.root;
			return m_Root;
		}
	}
	
	Transform m_LowerArmObj = null;
	Transform LowerArmObj
	{
		get
		{
			if (m_LowerArmObj == null)
				m_LowerArmObj = HandObj.parent;
			return m_LowerArmObj;
		}
	}
	
	Transform m_HandObj = null;
	Transform HandObj
	{
		get
		{
			if (m_HandObj == null)
			{
				if (Hand != null)
					m_HandObj = Hand.transform;
				else
				{
					m_HandObj = vp_Utility.GetTransformByNameInAncestors(Transform, "hand", true, true);
					if ((m_HandObj == null) && Transform.parent != null)
						m_HandObj = Transform.parent;
					if (m_HandObj != null)
						Hand = m_HandObj.gameObject;
				}
			}
			return m_HandObj;
		}
	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void OnEnable()
	{

		if (Player != null)
			Player.Register(this);

	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void OnDisable()
	{

		if (Player != null)
			Player.Unregister(this);

	}
	

	/// <summary>
	/// 
	/// </summary>
	protected virtual void Awake()
	{

#if UNITY_IOS || UNITY_ANDROID
		Debug.LogError("Error ("+this+") This script from base UFPS is intended for desktop and not supported on mobile. Are you attempting to use a PC/Mac player prefab on IOS/Android?");
		Component.DestroyImmediate(this);
		return;
#endif

#if UNITY_EDITOR
		KeepAiming = false;
#endif
		m_DefaultRotation = Transform.localRotation;

		// store reference up- and forward directions
		if ((LowerArmObj == null) || (HandObj == null))
		{
			Debug.LogError("Hierarchy Error (" + this + ") This script should be placed on a 3rd person weapon gameobject childed to a hand bone in a rigged character.");
			this.enabled = false;
			return;
		}

		Quaternion parentRotInv = Quaternion.Inverse(LowerArmObj.rotation);
		m_ReferenceLookDir = (parentRotInv * Root.rotation * Vector3.forward);
		m_ReferenceUpDir = (parentRotInv * Root.rotation * Vector3.up);
		
		// save difference between initial rotation and world rotation
		Quaternion currentRot = HandObj.rotation;
		HandObj.rotation = Root.rotation;
		Quaternion targetRot = HandObj.rotation;
		HandObj.rotation = currentRot;
		m_HandBoneRotDif = Quaternion.Inverse(targetRot) * currentRot;

	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void LateUpdate()
	{

		if (Time.timeScale == 0.0f)
			return;

		UpdateAiming();

	}

	
	/// <summary>
	/// 
	/// </summary>
	protected virtual void UpdateAiming()
	{

		if (Animator == null)
			return;

		if (!(Animator.GetBool("IsAttacking") || Animator.GetBool("IsZooming"))
			|| (Animator.GetBool("IsReloading") || Animator.GetBool("IsOutOfControl"))
			|| Player.CurrentWeaponIndex.Get() == 0)
		{
			Transform.localRotation = m_DefaultRotation;
			return;
		}

		Quaternion gunRotBak = Transform.rotation;
		if(Player.IsLocal.Get())
			Transform.rotation = Quaternion.LookRotation((WeaponHandler.CurrentWeapon.Weapon3rdPersonModel.transform.position - Player.LookPoint.Get()).normalized); // ensure weapon is pointed correctly for local player shadow
		else
			Transform.rotation = Quaternion.LookRotation(Player.AimDirection.Get());	// ensure weapon is pointed correctly for remote players
		m_WorldDir = Transform.forward;
		Transform.rotation = gunRotBak;

		// rotate HandObj in scene-compatible world space
		HandObj.rotation =
			vp_3DUtility.GetBoneLookRotationInWorldSpace(
			HandObj.rotation,
			LowerArmObj.rotation,	// parent transform of the hand (ideally a lower arm bone)
			m_WorldDir,
			1,
			m_ReferenceUpDir,
			m_ReferenceLookDir,
			m_HandBoneRotDif
			);

		// apply angle adjustment and recoil
		HandObj.Rotate(Transform.forward, AngleAdjustZ + WeaponHandler.CurrentWeapon.Recoil.z * RecoilFactorZ, Space.World);
		HandObj.Rotate(Transform.up, AngleAdjustY + WeaponHandler.CurrentWeapon.Recoil.y * RecoilFactorY, Space.World);
		HandObj.Rotate(Transform.right, AngleAdjustX + WeaponHandler.CurrentWeapon.Recoil.x * RecoilFactorX, Space.World);

	}


	/// <summary>
	/// 
	/// </summary>
#if UNITY_EDITOR
	bool CanStop_Zoom()
	{
		if(KeepAiming)
			return false;
		return true;
	}
#endif


}
