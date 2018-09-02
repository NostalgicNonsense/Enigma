/////////////////////////////////////////////////////////////////////////////////
//
//	vp_BodyAnimator.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	this script animates a human character model that needs to
//					move around and use guns a lot! it is designed for use with
//					the provided 'UFPSExampleAnimator'. 
//
//					the script assumes an upright player, logically divided into
//					upper and lower body. the upper body is manipulated using a
//					head look logic adapted to the UFPS event system and designed
//					to be used together with the 'vp_3rdPersonWeaponAim' script
//					for hand IK. lower body (legs and feet) rotates independently
//					of upper body (spine, arms and head). for example: the player
//					can look around quite freely without moving its feet.
//
//					PLEASE NOTE: this version of the script is intended for use
//					only on 3RD PERSON players such as multiplayer remote players
//					and AI. there is a separate version of the script,
//					'vp_FPBodyAnimator', which adds functionality for a local,
//					first person player.
//
/////////////////////////////////////////////////////////////////////////////////
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Animator))]

public class vp_BodyAnimator : MonoBehaviour
{

	protected bool m_IsValid = true;
	protected Vector3 m_ValidLookPoint = Vector3.zero;
	protected float m_ValidLookPointForward = 0.0f;
	protected bool HeadPointDirty = true;

	// head look rotation
	public GameObject HeadBone;			// the bone closest to the center of the head should be assigned here. all bones between this and 'LowestSpineBone' will be used for headlook
	public GameObject LowestSpineBone;	// the lowest spine bone above the hip should be assigned here. all bones between this and 'HeadBone' will be used for headlook 
	[Range(0, 90)]
	public float HeadPitchCap = 45.0f;	// how far up and down can the character bend its head. too high values may have the character bend over backwards or put its chin through its chest

	[Range(2, 20)]
	public float HeadPitchSpeed = 7.0f;	// headlook sensitivity to input. higher values look more alert, but stiff. lower values look more natural, or drowsy.

	[Range(0.2f, 20)]
	public float HeadYawSpeed = 2.0f;	// headlook sensitivity to input. higher values look more alert, but stiff. lower values look more natural, or drowsy.

	[Range(0, 1)]
	public float LeaningFactor = 0.25f;	// the higher the leaning factor, the more the character will lean over / lean backwards when looking down / up respectively

	// headlook
	protected List<GameObject> m_HeadLookBones = new List<GameObject>();
	protected List<Vector3> m_ReferenceUpDirs = null;
	protected List<Vector3> m_ReferenceLookDirs = null;
	protected float m_CurrentHeadLookYaw = 0.0f;
	protected float m_CurrentHeadLookPitch = 0.0f;
	protected List<float> m_HeadLookFalloffs = new List<float>();
	protected List<float> m_HeadLookCurrentFalloffs = null;
	protected List<float> m_HeadLookTargetFalloffs = null;
	protected Vector3 m_HeadLookTargetWorldDir;
	protected Vector3 m_HeadLookCurrentWorldDir;
	protected Vector3 m_HeadLookBackup = Vector3.zero;	// work variable
	protected Vector3 m_LookPoint = Vector3.zero;	// work variable

	// lower body rotation
	public float FeetAdjustAngle = 80.0f;			// when the character turns its head sideways above this angle in relation to its feet direction, the body and feet will be adjusted to the look direction
	public float FeetAdjustSpeedStanding = 10.0f;	// the speed of foot / lower body adjustment when character is standing still and looking around
	public float FeetAdjustSpeedMoving = 12.0f;		// the speed of foot / lower body adjustment when character is moving and looking around
	protected float m_PrevBodyYaw = 0.0f;
	protected float m_BodyYaw = 0.0f;
	protected float m_CurrentBodyYawTarget = 0.0f;
	protected float m_LastYaw = 0.0f;

	// movement
	public Vector3 ClimbOffset = Vector3.forward * 0.6f;		// the body position will be offset from the character controller by this amount when climbing ladders. used to move the body closer to the ladder than allowed by the character controller
	public Vector3 ClimbRotationOffset = Vector3.zero;			// the body will be locally rotated by this amount when climbing ladders. used to adapt for animation orientation
	protected float m_CurrentForward = 0.0f;
	protected float m_CurrentStrafe = 0.0f;
	protected float m_CurrentTurn = 0.0f;
	protected float m_CurrentTurnTarget = 0.0f;
	protected float m_MaxWalkSpeed = 1.0f;
	protected float m_MaxRunSpeed = 1.0f;
	protected float m_MaxCrouchSpeed = 1.0f;
	protected bool m_WasMoving = false;

	// grounding
	protected RaycastHit m_GroundHit;
	protected bool m_Grounded = true;

	// timers	
	protected vp_Timer.Handle m_AttackDoneTimer = new vp_Timer.Handle();
	protected float m_NextAllowedUpdateTurnTargetTime = 0;

	// constants
	protected const float TURNMODIFIER = 0.2f;
	protected const float CROUCHTURNMODIFIER = 100.0f;
	protected const float MOVEMODIFIER = 100.0f;

	// debug
	public bool ShowDebugObjects = false;	// when on, this toggle will show yellow lines indicating the look vector, and a red ball indicating the current look target

	// --- hash IDs ---

	// floats
	protected int ForwardAmount;
	protected int PitchAmount;
	protected int StrafeAmount;
	protected int TurnAmount;
	protected int VerticalMoveAmount;

	// booleans
	protected int IsAttacking;
	protected int IsClimbing;
	protected int IsCrouching;
	protected int IsGrounded;
	protected int IsMoving;
	protected int IsOutOfControl;
	protected int IsReloading;
	protected int IsRunning;
	protected int IsSettingWeapon;
	protected int IsZooming;
	protected int IsFirstPerson;

	// triggers
	protected int StartClimb;
	protected int StartOutOfControl;
	protected int StartReload;

	// enum indices
	protected int WeaponGripIndex;
	protected int WeaponTypeIndex;


	// --- properties ---

	protected vp_WeaponHandler m_WeaponHandler = null;
	protected vp_WeaponHandler WeaponHandler
	{
		get
		{
			if (m_WeaponHandler == null)
				m_WeaponHandler = (vp_WeaponHandler)transform.root.GetComponentInChildren(typeof(vp_WeaponHandler));
			return m_WeaponHandler;
		}
	}

	protected Transform m_Transform = null;
	protected Transform Transform
	{
		get
		{
			if (m_Transform == null)
				m_Transform = transform;
			return m_Transform;
		}
	}

	protected vp_PlayerEventHandler m_Player = null;
	protected vp_PlayerEventHandler Player
	{
		get
		{
			if (m_Player == null)
				m_Player = (vp_PlayerEventHandler)transform.root.GetComponentInChildren(typeof(vp_PlayerEventHandler));
			return m_Player;
		}
	}

	protected SkinnedMeshRenderer m_Renderer = null;
	protected SkinnedMeshRenderer Renderer
	{
		get
		{
			if (m_Renderer == null)
				m_Renderer = transform.root.GetComponentInChildren<SkinnedMeshRenderer>();
			return m_Renderer;
		}
	}

	protected Animator m_Animator;
	protected Animator Animator
	{
		get
		{
			if (m_Animator == null)
				m_Animator = GetComponent<Animator>();
			return m_Animator;
		}
	}

	protected Vector3 m_LocalVelocity
	{
		get
		{

			return vp_MathUtility.SnapToZero(
				Transform.root.InverseTransformDirection(Player.Velocity.Get())
				/ m_MaxSpeed
				);

		}
	}

	protected float m_MaxSpeed
	{
		get
		{
			if (Player.Run.Active)
				return m_MaxRunSpeed;
			if (Player.Crouch.Active)
				return m_MaxCrouchSpeed;
			return m_MaxWalkSpeed;
		}
	}

	protected GameObject m_HeadPoint = null;
	protected GameObject HeadPoint
	{
		get
		{
			if (m_HeadPoint == null)
			{
				m_HeadPoint = new GameObject("HeadPoint");
				m_HeadPoint.transform.parent = m_HeadLookBones[0].transform;
				m_HeadPoint.transform.localPosition = Vector3.zero;
				HeadPoint.transform.eulerAngles = Player.Rotation.Get();
			}
			return m_HeadPoint;
		}
	}

	protected GameObject m_DebugLookTarget;
	protected GameObject DebugLookTarget
	{
		get
		{
			if (m_DebugLookTarget == null)
				m_DebugLookTarget = vp_3DUtility.DebugBall();
			return m_DebugLookTarget;
		}
	}


	protected GameObject m_DebugLookArrow;
	protected GameObject DebugLookArrow
	{
		get
		{
			if (m_DebugLookArrow == null)
			{
				m_DebugLookArrow = vp_3DUtility.DebugPointer();
				m_DebugLookArrow.transform.parent = HeadPoint.transform;
				m_DebugLookArrow.transform.localPosition = Vector3.zero;
				m_DebugLookArrow.transform.localRotation = Quaternion.identity;
				return m_DebugLookArrow;
			}
			return m_DebugLookArrow;
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

		if (!IsValidSetup())
			return;

		InitHashIDs();

		InitHeadLook();

		InitMaxSpeeds();
		
		//Player.IsFirstPerson.Set(false);	// TEMP: checking to see if commenting this out causes trouble

	}



	/// <summary>
	/// 
	/// </summary>
	protected virtual void LateUpdate()
	{

		if (Time.timeScale == 0.0f)
			return;

		if (!m_IsValid)
		{
			this.enabled = false;
			return;
		}

		UpdatePosition();

		UpdateGrounding();

		UpdateBody();

		UpdateSpine();

		UpdateAnimationSpeeds();

		UpdateAnimator();

		UpdateDebugInfo();

		UpdateHeadPoint();

	}

	
	/// <summary>
	/// stores an interpretation of player mouselook and WASD input
	/// </summary>
	protected virtual void UpdateAnimationSpeeds()
	{

		// --- turn animation speed ---

		if (Time.time > m_NextAllowedUpdateTurnTargetTime)
		{
			m_CurrentTurnTarget = ((Mathf.DeltaAngle(m_PrevBodyYaw, m_BodyYaw)) * (Player.Crouch.Active ? CROUCHTURNMODIFIER : TURNMODIFIER));
			m_NextAllowedUpdateTurnTargetTime = Time.time + 0.1f;
		}

		if (Player.Platform.Get() == null  || !Player.IsLocal.Get())
		{
			m_CurrentTurn = Mathf.Lerp(m_CurrentTurn, m_CurrentTurnTarget, Time.deltaTime);
			if (Mathf.Round(Transform.root.eulerAngles.y) == Mathf.Round(m_LastYaw))
				m_CurrentTurn *= 0.6f;
			m_LastYaw = Transform.root.eulerAngles.y;
			m_CurrentTurn = vp_MathUtility.SnapToZero(m_CurrentTurn);
		}
		else
			m_CurrentTurn = 0.0f;	// turn logic does not work very well locally on platforms


		// --- forward motion animation speed ---

		m_CurrentForward = Mathf.Lerp(m_CurrentForward, m_LocalVelocity.z, Time.deltaTime * MOVEMODIFIER);
		if(vp_Input.Instance.ControlType == 0)	// only do this if using keyboard, not joystick
			m_CurrentForward = Mathf.Abs(m_CurrentForward) > 0.03f ? m_CurrentForward : 0.0f;

		// --- strafe animation speed ---
		if (Player.Crouch.Active)
		{
			if (Mathf.Abs(GetStrafeDirection()) < Mathf.Abs(m_CurrentTurn))
				m_CurrentStrafe = Mathf.Lerp(m_CurrentStrafe, m_CurrentTurn, Time.deltaTime * 5);
			else
				m_CurrentStrafe = Mathf.Lerp(m_CurrentStrafe, GetStrafeDirection(), Time.deltaTime * 5);
		}
		else
			m_CurrentStrafe = Mathf.Lerp(m_CurrentStrafe, GetStrafeDirection(), Time.deltaTime * 5);

		if (vp_Input.Instance.ControlType == 0)	// only do this if using keyboard, not gamepad
			m_CurrentStrafe = Mathf.Abs(m_CurrentStrafe) > 0.03f ? m_CurrentStrafe : 0.0f;

	}


	/// <summary>
	/// returns a value indicating the strafe direction. on analog
	/// input hardware, returns a float indicating the strafe speed
	/// </summary>
	protected virtual float GetStrafeDirection()
	{

		// if using keyboard, return a binary value
		if (vp_Input.Instance.ControlType == 0)
		{
			if (Player.InputMoveVector.Get().x < 0.0f)
				return -1.0f;
			else if (Player.InputMoveVector.Get().x > 0.0f)
				return 1.0f;
		}

		// if using gamepad, return an analog value
		return Player.InputMoveVector.Get().x;

	}


	/// <summary>
	/// updates variables on the mecanim animator object
	/// </summary>
	protected virtual void UpdateAnimator()
	{

		// --- booleans used to transition between blend states ---
		// TODO: these should be moved to event callbacks on the next optimization run

		Animator.SetBool(IsRunning, Player.Run.Active && GetIsMoving());
		Animator.SetBool(IsCrouching, Player.Crouch.Active);
		Animator.SetInteger(WeaponTypeIndex, Player.CurrentWeaponType.Get());
		Animator.SetInteger(WeaponGripIndex, Player.CurrentWeaponGrip.Get());
		Animator.SetBool(IsSettingWeapon, Player.SetWeapon.Active);
		Animator.SetBool(IsReloading, Player.Reload.Active);
		Animator.SetBool(IsOutOfControl, Player.OutOfControl.Active);
		Animator.SetBool(IsClimbing, Player.Climb.Active);
		Animator.SetBool(IsZooming, Player.Zoom.Active);
		Animator.SetBool(IsGrounded, m_Grounded);
		Animator.SetBool(IsMoving, GetIsMoving());
		Animator.SetBool(IsFirstPerson, Player.IsFirstPerson.Get());

		// --- floats used inside blend states to blend between animations ---

		Animator.SetFloat(TurnAmount, m_CurrentTurn);
		Animator.SetFloat(ForwardAmount, m_CurrentForward);
		Animator.SetFloat(StrafeAmount, m_CurrentStrafe);
		Animator.SetFloat(PitchAmount, (-Player.Rotation.Get().x) / 90.0f);

		if (m_Grounded)
			Animator.SetFloat(VerticalMoveAmount, 0.0f);
		else
		{
			if (Player.Velocity.Get().y < 0.0f)
				Animator.SetFloat(VerticalMoveAmount, Mathf.Lerp(Animator.GetFloat(VerticalMoveAmount), -1.0f, Time.deltaTime * 3));
			else
				Animator.SetFloat(VerticalMoveAmount, Player.MotorThrottle.Get().y * 10.0f);
		}

	}


	/// <summary>
	/// (for 3rd person) shows a yellow line indicating the look direction,
	/// and a red ball indicating the current look point
	/// </summary>
	protected virtual void UpdateDebugInfo()
	{

		if (ShowDebugObjects)
		{
			DebugLookTarget.transform.position = m_HeadLookBones[0].transform.position
				+ (HeadPoint.transform.forward * 1000);
			DebugLookArrow.transform.LookAt(DebugLookTarget.transform.position);
			if (!vp_Utility.IsActive(m_DebugLookTarget))
				vp_Utility.Activate(m_DebugLookTarget);
			if (!vp_Utility.IsActive(m_DebugLookArrow))
				vp_Utility.Activate(m_DebugLookArrow);
		}
		else
		{
			if (m_DebugLookTarget != null)
				vp_Utility.Activate(m_DebugLookTarget, false);
			if (m_DebugLookArrow != null)
				vp_Utility.Activate(m_DebugLookArrow, false);
		}

	}


	/// <summary>
	/// maintains proper headlook orientation
	/// </summary>
	protected virtual void UpdateHeadPoint()
	{

		if (!HeadPointDirty)
			return;

		HeadPoint.transform.eulerAngles = Player.Rotation.Get();
		HeadPointDirty = false;

	}


	/// <summary>
	/// adjusts the position of the player body when climbing ladders
	/// </summary>
	protected virtual void UpdatePosition()
	{

		if (Player.IsFirstPerson.Get())
			return;

		if (Player.Climb.Active)
			Transform.localPosition += ClimbOffset;

	}


	/// <summary>
	/// maintains and animates rotation of the lower body
	/// </summary>
	protected virtual void UpdateBody()
	{

		// blend rotation towards target yaw, if active
		m_PrevBodyYaw = m_BodyYaw;
		m_BodyYaw = Mathf.LerpAngle(m_BodyYaw, m_CurrentBodyYawTarget, Time.deltaTime * ((Player.Velocity.Get().magnitude > 0.1f) ? FeetAdjustSpeedMoving : FeetAdjustSpeedStanding));
		m_BodyYaw = m_BodyYaw < -360.0f ? m_BodyYaw += 360.0f : m_BodyYaw;
		m_BodyYaw = m_BodyYaw > 360.0f ? m_BodyYaw -= 360.0f : m_BodyYaw;
		
		Transform.eulerAngles = m_BodyYaw * Vector3.up;

		// calculate head yaw in relation to body
		m_CurrentHeadLookYaw = Mathf.DeltaAngle(Player.Rotation.Get().y, Transform.eulerAngles.y);

		// force-rotate bodyYaw if it twists more than 90 degrees away from
		// controller yaw to the left or right
		if (Mathf.Max(0, m_CurrentHeadLookYaw - 90) > 0)		// left
		{
			Transform.eulerAngles = (Vector3.up * (Transform.root.eulerAngles.y + 90));
			m_BodyYaw = m_CurrentBodyYawTarget = Transform.eulerAngles.y;
		}
		else if (Mathf.Min(0, m_CurrentHeadLookYaw - (-90)) < 0)		// right
		{
			Transform.eulerAngles = (Vector3.up * (Transform.root.eulerAngles.y - 90));
			m_BodyYaw = m_CurrentBodyYawTarget = Transform.eulerAngles.y;
		}

		// detect when yaw and input rotation diverges because of
		// 360 degree snap, and fix it or character will twist
		float dif = (Player.Rotation.Get().y - m_BodyYaw);
		if (Mathf.Abs(dif) > 180)
		{
			if (m_BodyYaw > 0.0f)
			{
				m_BodyYaw -= 360;
				m_PrevBodyYaw -= 360;
			}
			else if (m_BodyYaw < 0.0f)
			{
				m_BodyYaw += 360;
				m_PrevBodyYaw += 360;
			}
		}

		// make lower body smoothly face forward in certain cases
		if ((m_CurrentHeadLookYaw > FeetAdjustAngle)			//  
			|| (m_CurrentHeadLookYaw < -FeetAdjustAngle)		// when turning around to the left
			|| (Player.Velocity.Get().magnitude > 0.1f)			// when moving around in any way
			|| (Player.Crouch.Active && (Player.Attack.Active || Player.Zoom.Active)))	// when crouching and aiming (keeps guns without dynamic
																						// aiming pointed in roughly the right direction)
		{
			m_CurrentBodyYawTarget =
				Mathf.LerpAngle(m_CurrentBodyYawTarget, Transform.root.eulerAngles.y, 0.1f);
		}

	}


	/// <summary>
	/// mecanim ik and lookat logic is unity pro only. this method
	/// implements a headlook logic that allows us to manipulate the
	/// spine in different ways depending on circumstances for a very
	/// lifelike appearance
	/// </summary>
	protected virtual void UpdateSpine()
	{

		if (Player.Climb.Active)
			return;

		// NOTE: the underlying headlook 3D math was adapted from the Unity
		// 'HeadLookController' example by Rune Skovbo Johansen.

		// set head pitch and yaw with bone falloff
		for (int v = 0; v < m_HeadLookBones.Count; v++)
		{

			// --- yaw logic ---

			// we rotate the head and spine in three different ways, depending
			// on what the character is up to:

			//2) if in first person, or attacking, or zooming,
			//invert headlookfalloff so shoulders always face the look angle
			if (((Player.IsFirstPerson.Get()
				// TIP: uncomment this to allow the camera to look at your shoulders in 1st person
				// when standing still and unarmed (but beware: stiffer, and glitchy in crouch mode)
				 //&& ((Player.CurrentWeaponType.Get() > 0) || Player.Crouch.Active)
				)
				|| Animator.GetBool(IsAttacking)
				|| Animator.GetBool(IsZooming))
				&& !Animator.GetBool(IsCrouching)	// always focus headlook on neck while crouching
				)
				m_HeadLookTargetFalloffs[v] = m_HeadLookFalloffs[(m_HeadLookFalloffs.Count - 1) - v];

			//3) if standing still and relaxing in third person, let head rotate freely
			//(as in free of the shoulders) for a less stiff, more life-like appearance
			else
				m_HeadLookTargetFalloffs[v] = m_HeadLookFalloffs[v];

			// if was moving and stopped, snap to target falloff
			if (m_WasMoving && !(Animator.GetBool(IsMoving)))
				m_HeadLookCurrentFalloffs[v] = m_HeadLookTargetFalloffs[v];

			// lerp bones toward the new target angle
			m_HeadLookCurrentFalloffs[v] = Mathf.SmoothStep(m_HeadLookCurrentFalloffs[v], Mathf.LerpAngle(m_HeadLookCurrentFalloffs[v], m_HeadLookTargetFalloffs[v], Time.deltaTime * 10.0f), Time.deltaTime * 20);

			// multiply world yaw with bone falloff. we use a different,
			// stiffer pattern for the model in 1st person, and a more limber
			// pattern in 3rd person
			if (Player.IsFirstPerson.Get())
			{
				m_HeadLookTargetWorldDir = (GetLookPoint() - (m_HeadLookBones[0].transform.position));
				m_HeadLookCurrentWorldDir = Vector3.Slerp(
					m_HeadLookTargetWorldDir,
					vp_3DUtility.HorizontalVector(m_HeadLookTargetWorldDir),
					(m_HeadLookCurrentFalloffs[v] / m_HeadLookFalloffs[0])	// div by largest value to get into a 0-1 range
					);
			}
			else
			{

				// make sure the lookpoint is not behind us (this can happen in local
				// 3rd person if the camera is pointed at the ground behind the feet
				// of the player). if it's behind, push it forward accordingly so the
				// player never looks or aims over its shoulder unnaturaly
				m_ValidLookPoint = GetLookPoint();
				m_ValidLookPointForward = Transform.InverseTransformDirection((m_ValidLookPoint - (m_HeadLookBones[0].transform.position))).z;
				if (m_ValidLookPointForward < 0.0f)
					m_ValidLookPoint += Transform.forward * -m_ValidLookPointForward;

				// multiply world yaw with bone falloff
				m_HeadLookTargetWorldDir = Vector3.Slerp(m_HeadLookTargetWorldDir, (m_ValidLookPoint - (m_HeadLookBones[0].transform.position)), Time.deltaTime * HeadYawSpeed);
				m_HeadLookCurrentWorldDir = Vector3.Slerp(
					m_HeadLookCurrentWorldDir,
					vp_3DUtility.HorizontalVector(m_HeadLookTargetWorldDir),
					(m_HeadLookCurrentFalloffs[v] / m_HeadLookFalloffs[0])	// div by largest value to get into a 0-1 range
					);

			}

			// perform yaw headlook for this bone in the correct world direction
			// regardless of the bone's inherent 3d space
			m_HeadLookBones[v].transform.rotation =
				vp_3DUtility.GetBoneLookRotationInWorldSpace(
				m_HeadLookBones[v].transform.rotation,
				m_HeadLookBones[m_HeadLookBones.Count - 1].transform.parent.rotation,
				m_HeadLookCurrentWorldDir,
				m_HeadLookCurrentFalloffs[v],
				m_ReferenceLookDirs[v],
				m_ReferenceUpDirs[v],
				Quaternion.identity
				);

			// perform damped pitch headlook if in 3rd person
			if (!Player.IsFirstPerson.Get())
			{
				m_CurrentHeadLookPitch = Mathf.SmoothStep(m_CurrentHeadLookPitch, Mathf.Clamp(Player.Rotation.Get().x, -HeadPitchCap, HeadPitchCap), Time.deltaTime * HeadPitchSpeed);
				m_HeadLookBones[v].transform.Rotate(
					HeadPoint.transform.right,
					m_CurrentHeadLookPitch
					* Mathf.Lerp(m_HeadLookFalloffs[v], m_HeadLookCurrentFalloffs[v], LeaningFactor),
					Space.World);
			}

		}

		m_WasMoving = Animator.GetBool(IsMoving);

	}


	/// <summary>
	/// this is a method rather than a property to allow overriding
	/// </summary>
	protected virtual bool GetIsMoving()
	{
		return (Vector3.Scale(Player.Velocity.Get(), (Vector3.right + Vector3.forward))).magnitude > 0.01f;
	}
	

	/// <summary>
	/// returns the first look vector intersection with a solid object
	/// </summary>
	protected virtual Vector3 GetLookPoint()
	{

		m_HeadLookBackup = HeadPoint.transform.eulerAngles;
		HeadPoint.transform.eulerAngles = vp_MathUtility.NaNSafeVector3(Player.Rotation.Get());
		m_LookPoint = HeadPoint.transform.position
			+ (HeadPoint.transform.forward * 1000);
		HeadPoint.transform.eulerAngles = vp_MathUtility.NaNSafeVector3(m_HeadLookBackup);

		return m_LookPoint;

	}


	/// <summary>
	/// initializes the falloff variables used to get headlook to
	/// gradually affect bones by distance to the head or lowest
	/// spine bone
	/// </summary>
	protected virtual List<float> CalculateBoneFalloffs(List<GameObject> boneList)
	{

		List<float> boneFalloffs = new List<float>();

		float factor = 0.0f;

		for (int v = boneList.Count - 1; v > -1; v--)
		{
			if (boneList[v] == null)
				boneList.RemoveAt(v);
			else
			{
				float f = Mathf.Lerp(0, 1, (v + 1) / ((float)boneList.Count));
				boneFalloffs.Add(f * f * f);
				factor += f * f * f;
			}

		}

		if (boneList.Count == 0)
			return boneFalloffs;

		for (int v = 0; v < boneFalloffs.Count; v++)
		{
			boneFalloffs[v] *= (1 / (factor));
		}

		return boneFalloffs;

	}


	/// <summary>
	/// stores the relative rotations of the headlook bones and the
	/// world at startup. necessary to support characer rigs with
	/// arbitrary bone rotation spaces
	/// </summary>
	protected virtual void StoreReferenceDirections()
	{

		for (int v = 0; v < m_HeadLookBones.Count; v++)
		{
			Quaternion parentRotInv = Quaternion.Inverse(m_HeadLookBones[m_HeadLookBones.Count - 1].transform.parent.rotation);
			m_ReferenceLookDirs.Add(parentRotInv * Transform.rotation * Vector3.forward);
			m_ReferenceUpDirs.Add(parentRotInv * Transform.rotation * Vector3.up);
		}

	}


	/// <summary>
	/// calculates grounding similarly to the vp_FPController class.
	/// exists here so we can have a player body without a controller
	/// </summary>
	protected virtual void UpdateGrounding()
	{
		// TODO: now supported in vp_Controller, remove?
		Physics.SphereCast(
			new Ray(Transform.position + Vector3.up * 0.5f, Vector3.down),
			0.4f,
			out m_GroundHit,
			1.0f,
			vp_Layer.Mask.ExternalBlockers);
		m_Grounded = (m_GroundHit.collider != null);

	}


	/// <summary>
	/// implements a delay between attacking and relaxing the
	/// gun in 3rd person
	/// </summary>
	protected virtual void RefreshWeaponStates()
	{

		if (WeaponHandler == null)
			return;

		if (WeaponHandler.CurrentWeapon == null)
			return;

		WeaponHandler.CurrentWeapon.SetState("Attack", Player.Attack.Active);
		WeaponHandler.CurrentWeapon.SetState("Zoom", Player.Zoom.Active);

	}

	
	/// <summary>
	/// calculates the maximum running speeds of this controller
	/// </summary>
	protected virtual void InitMaxSpeeds()
	{

		if(Player.IsLocal.Get())
		{
			// get max speed of first vp_FPController that we can find under the ancestor
			vp_FPController controller = Transform.root.GetComponentInChildren<vp_FPController>();
			m_MaxWalkSpeed = controller.CalculateMaxSpeed();
			m_MaxRunSpeed = controller.CalculateMaxSpeed("Run");
			m_MaxCrouchSpeed = controller.CalculateMaxSpeed("Crouch");
			//Debug.Log("m_MaxWalkSpeed: " + m_MaxWalkSpeed + ", m_MaxRunSpeed: " + m_MaxRunSpeed + ", m_MaxCrouchSpeed: " + m_MaxCrouchSpeed);
		}
		else
		{
			// TEMP: hardcoded for remote players
			m_MaxWalkSpeed = 3.999999f;
			m_MaxRunSpeed = 10.08f;
			m_MaxCrouchSpeed = 1.44f;
		}

	}
	

	/// <summary>
	/// 
	/// </summary>
	protected virtual void InitHashIDs()
	{

		// floats
		ForwardAmount = Animator.StringToHash("Forward");
		PitchAmount = Animator.StringToHash("Pitch");
		StrafeAmount = Animator.StringToHash("Strafe");
		TurnAmount = Animator.StringToHash("Turn");
		VerticalMoveAmount = Animator.StringToHash("VerticalMove");

		// booleans
		IsAttacking = Animator.StringToHash("IsAttacking");
		IsClimbing = Animator.StringToHash("IsClimbing");
		IsCrouching = Animator.StringToHash("IsCrouching");
		IsGrounded = Animator.StringToHash("IsGrounded");
		IsMoving = Animator.StringToHash("IsMoving");
		IsOutOfControl = Animator.StringToHash("IsOutOfControl");
		IsReloading = Animator.StringToHash("IsReloading");
		IsRunning = Animator.StringToHash("IsRunning");
		IsSettingWeapon = Animator.StringToHash("IsSettingWeapon");
		IsZooming = Animator.StringToHash("IsZooming");
		IsFirstPerson = Animator.StringToHash("IsFirstPerson");

		// triggers
		StartClimb = Animator.StringToHash("StartClimb");
		StartOutOfControl = Animator.StringToHash("StartOutOfControl");
		StartReload = Animator.StringToHash("StartReload");

		// enum indices
		WeaponGripIndex = Animator.StringToHash("WeaponGrip");
		WeaponTypeIndex = Animator.StringToHash("WeaponType");

	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void InitHeadLook()
	{

		if (!m_IsValid)
			return;

		m_HeadLookBones.Clear();

		GameObject current = HeadBone;
		while (current != LowestSpineBone.transform.parent.gameObject)
		{
			m_HeadLookBones.Add(current);
			current = current.transform.parent.gameObject;
		}

		m_ReferenceUpDirs = new List<Vector3>();
		m_ReferenceLookDirs = new List<Vector3>();

		m_HeadLookFalloffs = CalculateBoneFalloffs(m_HeadLookBones);
		m_HeadLookCurrentFalloffs = new List<float>(m_HeadLookFalloffs);
		m_HeadLookTargetFalloffs = new List<float>(m_HeadLookFalloffs);

		StoreReferenceDirections();

	}


	/// <summary>
	/// makes sure the bodyanimator has been set up properly and
	/// reports an error and disables the component if not
	/// </summary>
	protected virtual bool IsValidSetup()
	{

		if (HeadBone == null)
		{
			Debug.LogError("Error (" + this + ") No gameobject has been assigned for 'HeadBone'.");
			goto abort;
		}

		if (LowestSpineBone == null)
		{
			Debug.LogError("Error (" + this + ") No gameobject has been assigned for 'LowestSpineBone'.");
			goto abort;
		}

		if (!vp_Utility.IsDescendant(HeadBone.transform, transform.root))
		{
			NotInSameHierarchyError(HeadBone);
			goto abort;
		}

		if (!vp_Utility.IsDescendant(LowestSpineBone.transform, transform.root))
		{
			NotInSameHierarchyError(LowestSpineBone);
			goto abort;
		}

		if (!vp_Utility.IsDescendant(HeadBone.transform, LowestSpineBone.transform))
		{
			Debug.LogError("Error (" + this + ") 'HeadBone' must be a child or descendant of 'LowestSpineBone'.");
			goto abort;
		}

		return true;

	abort:

		m_IsValid = false;
		this.enabled = false;
		return false;

	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void NotInSameHierarchyError(GameObject o)
	{
		Debug.LogError("Error '" + o + "' can not be used as a bone for  " + this + " because it is not part of the same hierarchy.");
	}


	/// <summary>
	/// gets the world Y rotation of the lower body
	/// </summary>
	protected virtual float OnValue_BodyYaw
	{
		get
		{
			return Transform.eulerAngles.y;	// return world yaw
		}
		set
		{
			m_BodyYaw = value;
		}
	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void OnStart_Attack()
	{

		// TEST: time body animation better to throwing weapon projectile spawn
		if (Player.CurrentWeaponType.Get() == (int)vp_Weapon.Type.Thrown)
		{
			if (WeaponHandler.CurrentShooter != null)
			{
				vp_Timer.In(WeaponHandler.CurrentShooter.ProjectileSpawnDelay * 0.7f, () =>
					{
						if ((this != null) && (Animator != null))
						{
							m_AttackDoneTimer.Cancel();
							Animator.SetBool(IsAttacking, true);
							OnStop_Attack();
						}
					});
			}
		}
		else
		// ---
		Animator.SetBool(IsAttacking, true);

	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void OnStop_Attack()
	{

		// for 'RefreshWeaponStates'
		vp_Timer.In(0.5f, delegate()
		{
			if ((this != null) && (Animator != null))
			{
				Animator.SetBool(IsAttacking, false);
				RefreshWeaponStates();
			}
		}, m_AttackDoneTimer);

	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void OnStop_Zoom()
	{

		// for 'RefreshWeaponStates'
		vp_Timer.In(0.5f, delegate()
		{
			if((Player != null) && (Player.Attack != null))
			{
				if (!Player.Attack.Active)
				{
					if(Animator != null)
						Animator.SetBool(IsAttacking, false);
				}
				RefreshWeaponStates();
			}
		}, m_AttackDoneTimer);

	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void OnStart_Reload()
	{
		Animator.SetTrigger(StartReload);
	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void OnStart_OutOfControl()
	{
		Animator.SetTrigger(StartOutOfControl);
	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void OnStart_Climb()
	{

		Animator.SetTrigger(StartClimb);

	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void OnStart_Dead()
	{

		// for 'RefreshWeaponStates'
		if (m_AttackDoneTimer.Active)
			m_AttackDoneTimer.Execute();

	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void OnStop_Dead()
	{

		// for 'UpdateHeadPoint'
		HeadPointDirty = true;

	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void OnMessage_CameraToggle3rdPerson()
	{

		// for 'UpdateSpine'
		m_WasMoving = !m_WasMoving;

		// for 'UpdateHeadPoint'
		HeadPointDirty = true;

	}


	/// <summary>
	/// returns the direction between the player model's head and the
	/// look point. NOTE: _not_ necessarily the direction between the
	/// camera and the look point
	/// </summary>
	protected virtual Vector3 OnValue_HeadLookDirection
	{
		get
		{

			return (Player.LookPoint.Get() - HeadPoint.transform.position).normalized;

		}

	}


	/// <summary>
	/// returns the contact point on the surface of the first physical
	/// object that the camera looks at
	/// </summary>
	protected virtual Vector3 OnValue_LookPoint
	{
		get
		{
			return GetLookPoint();
		}
	}

}
