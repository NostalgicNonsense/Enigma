/////////////////////////////////////////////////////////////////////////////////
//
//	vp_FPController.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	a first person controller class with a configurable motor and
//					and many tweakable physics parameters for interaction with
//					incoming forces, rigidbodies and external surfaces.
//					optionally adds a collision trigger for interaction with the
//					vp_MovingPlatform system.
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class vp_FPController : vp_CharacterController
{
	
	// general
	protected Vector3 m_FixedPosition = Vector3.zero;		// exact position. updates at a fixed interval and is used for gameplay
	protected Vector3 m_SmoothPosition = Vector3.zero;		// smooth position. updates as often as possible and is only used for the camera
	public Vector3 SmoothPosition { get { return m_SmoothPosition; } }	// a version of the controller position calculated in 'Update' to get smooth camera motion
	public Vector3 Velocity { get { return CharacterController.velocity; } }
	protected bool m_IsFirstPerson = true;

	// collision
	public bool HeadContact { get { return m_HeadContact; } }
	public Vector3 GroundNormal { get { return m_GroundHit.normal; } }
	public float GroundAngle { get { return Vector3.Angle(m_GroundHit.normal, Vector3.up); } }
	protected bool m_HeadContact = false;
	protected RaycastHit m_CeilingHit;					// contains info about any ceilings we may have bumped into
	protected RaycastHit m_WallHit;						// contains info about any horizontal blockers we may have collided with

	// physics trigger
	protected CapsuleCollider m_TriggerCollider = null;		// trigger collider for incoming objects to detect us
	public bool PhysicsHasCollisionTrigger = true;			// whether to automatically generate a child object with a trigger on startup
	protected GameObject m_Trigger = null;					// trigger gameobject for detection of incoming objects

	// surface identifier
	protected vp_SurfaceIdentifier m_SurfaceIdentifier = null;
	protected vp_SurfaceIdentifier SurfaceIdentifier
	{
		get
		{
			if ((m_SurfaceIdentifier == null))
				m_SurfaceIdentifier = GetComponent<vp_SurfaceIdentifier>();
			return m_SurfaceIdentifier;
		}
	}

	// motor
	public float MotorAcceleration = 0.18f;
	public float MotorDamping = 0.17f;
	public float MotorBackwardsSpeed = 0.65f;
	public float MotorAirSpeed = 0.35f;
	public float MotorSlopeSpeedUp = 1.0f;
	public float MotorSlopeSpeedDown = 1.0f;
	protected Vector3 m_MoveDirection = Vector3.zero;
	protected float m_SlopeFactor = 1.0f;
	protected Vector3 m_MotorThrottle = Vector3.zero;
	protected float m_MotorAirSpeedModifier = 1.0f;
	protected float m_CurrentAntiBumpOffset = 0.0f;

	// jump
	public float MotorJumpForce = 0.18f;
	public float MotorJumpForceDamping = 0.08f;
	public float MotorJumpForceHold = 0.003f;
	public float MotorJumpForceHoldDamping = 0.5f;
	protected int m_MotorJumpForceHoldSkipFrames = 0;
	protected float m_MotorJumpForceAcc = 0.0f;
	protected bool m_MotorJumpDone = true;

	// physics
	public float PhysicsForceDamping = 0.05f;			// damping of external forces
	public float PhysicsSlopeSlideLimit = 30.0f;		// steepness in angles above which controller will start to slide
	public float PhysicsSlopeSlidiness = 0.15f;			// slidiness of the surface that we're standing on. will be additive if steeper than CharacterController.slopeLimit
	public float PhysicsWallBounce = 0.0f;				// how much to bounce off walls
	public float PhysicsWallFriction = 0.0f;
	protected Vector3 m_ExternalForce = Vector3.zero;	// current velocity from external forces (explosion knockback, jump pads, rocket packs)
	protected Vector3[] m_SmoothForceFrame = new Vector3[120];
	protected bool m_Slide = false;						// are sliding on a steep surface without moving?
	protected bool m_SlideFast = false;					// have we accumulated a quick speed from standing on a slope above 'slopeLimit'
	protected float m_SlideFallSpeed = 0.0f;			// fall speed resulting from sliding fast into free fall
	protected float m_OnSteepGroundSince = 0.0f;		// the point in time at which we started standing on a slope above 'slopeLimit'. used to calculate slide speed accumulation
	protected float m_SlopeSlideSpeed = 0.0f;			// current velocity from sliding
	protected Vector3 m_PredictedPos = Vector3.zero;
	protected Vector3 m_PrevDir = Vector3.zero;
	protected Vector3 m_NewDir = Vector3.zero;
	protected float m_ForceImpact = 0.0f;
	protected float m_ForceMultiplier = 0.0f;
	protected Vector3 CapsuleBottom = Vector3.zero;
	protected Vector3 CapsuleTop = Vector3.zero;


	/// <summary>
	/// 
	/// </summary>
	protected override void OnEnable()
	{

		base.OnEnable();

		vp_TargetEvent<Vector3>.Register(m_Transform, "ForceImpact", AddForce);

	}


	/// <summary>
	/// 
	/// </summary>
	protected override void OnDisable()
	{

		base.OnDisable();

		vp_TargetEvent<Vector3>.Unregister(m_Root, "ForceImpact", AddForce);

	}


	/// <summary>
	/// 
	/// </summary>
	protected override void Start()
	{

		base.Start();

		SetPosition(Transform.position);	// this will initialize some position variables

		// if set, automagically sets up a trigger for interacting with
		// incoming rigidbodies
		if (PhysicsHasCollisionTrigger)
		{

			m_Trigger = new GameObject("Trigger");
			m_Trigger.transform.parent = m_Transform;
			m_Trigger.layer = vp_Layer.LocalPlayer;
			m_Trigger.transform.localPosition = Vector3.zero;

			m_TriggerCollider = m_Trigger.AddComponent<CapsuleCollider>();
			m_TriggerCollider.isTrigger = true;
			m_TriggerCollider.radius = CharacterController.radius + SkinWidth;
			m_TriggerCollider.height = CharacterController.height + (SkinWidth * 2.0f);
			m_TriggerCollider.center = CharacterController.center;

			m_Trigger.gameObject.AddComponent<vp_DamageTransfer>();

			// if we have a SurfaceIdentifier, copy it along with its values onto the trigger.
			// this will make the trigger emit the same fx as the controller when hit by bullets
			if (SurfaceIdentifier != null)
			{
				vp_Timer.In(0.05f, ()=>	// wait atleast one frame for this to take effect properly
				{
					vp_SurfaceIdentifier triggerSurfaceIdentifier = m_Trigger.gameObject.AddComponent<vp_SurfaceIdentifier>();
					triggerSurfaceIdentifier.SurfaceType = SurfaceIdentifier.SurfaceType;
					triggerSurfaceIdentifier.AllowDecals = SurfaceIdentifier.AllowDecals;
				});
			}

		}

	}


	/// <summary>
	/// updates charactercontroller and physics trigger sizes
	/// depending on player Crouch activity
	/// </summary>
	protected override void RefreshCollider()
	{

		base.RefreshCollider();

		// update physics trigger size
		if (m_TriggerCollider != null)
		{
			m_TriggerCollider.radius = CharacterController.radius + SkinWidth;
			m_TriggerCollider.height = CharacterController.height + (SkinWidth * 2.0f);
			m_TriggerCollider.center = CharacterController.center;
		}

	}


	/// <summary>
	/// enables or disables the collider
	/// </summary>
	public override void EnableCollider(bool isEnabled = true)
	{

		if (CharacterController != null)
			CharacterController.enabled = isEnabled;

	}


	/// <summary>
	/// 
	/// </summary>
	protected override void Update()
	{

		base.Update();

		// simulate high-precision movement for smoothest possible camera motion
		SmoothMove();

		// TIP: uncomment either of these lines to debug print the
		// speed of the character controller
		//Debug.Log(Velocity.magnitude);		// speed in meters per second
		//Debug.Log(Controller.Velocity.sqrMagnitude);	// speed as used by the camera bob

	}


	/// <summary>
	/// 
	/// </summary>
	protected override void FixedUpdate()
	{

		if (Time.timeScale == 0.0f)
			return;

		// convert user input to motor throttle
		UpdateMotor();

		// apply motion generated by tapping or holding the jump button
		UpdateJump();

		// handle external forces like gravity, explosion shockwaves or wind
		UpdateForces();

		// apply sliding in slopes
		UpdateSliding();

		// detect when player falls, slides or gets pushed out of control
		UpdateOutOfControl();
		
		// update controller position based on current motor- & external forces
		FixedMove();

		// respond to environment collisions that may have happened during the move
		UpdateCollisions();

		// move and rotate player along with rigidbodies & moving platforms
		UpdatePlatformMove();

		// store final position and velocity for next frame's physics calculations
		UpdateVelocity();

	}


	/// <summary>
	/// simulates velocity acceleration and damping in the cardinal
	/// directions
	/// </summary>
	protected virtual void UpdateMotor()
	{

		if (!MotorFreeFly)
			UpdateThrottleWalk();
		else
			UpdateThrottleFree();

		// snap super-small values to zero to avoid floating point issues
		m_MotorThrottle = vp_MathUtility.SnapToZero(m_MotorThrottle);

	}


	/// <summary>
	/// throttle logic for moving a grounded controller, taking ground
	/// slope and air speed into consideration
	/// </summary>
	protected virtual void UpdateThrottleWalk()
	{

		// if on the ground, make movement speed dependent on ground slope
		UpdateSlopeFactor();

		// update air speed modifier
		// (at 1.0, this will completely prevent the controller from altering
		// its trajectory while in the air, and will disable motor damping)
		m_MotorAirSpeedModifier = (m_Grounded ? 1.0f : MotorAirSpeed);

		// convert horizontal input to forces in the motor
		m_MotorThrottle +=
            ((Player.InputMoveVector.Get().y > 0) ? Player.InputMoveVector.Get().y : // if moving forward or sideways: use normal speed
			(Player.InputMoveVector.Get().y * MotorBackwardsSpeed))		// if moving backwards: apply backwards-modifier
			* (Transform.TransformDirection(
			Vector3.forward *
			(MotorAcceleration * 0.1f) *
			m_MotorAirSpeedModifier) *
			m_SlopeFactor);
		m_MotorThrottle += Player.InputMoveVector.Get().x * (Transform.TransformDirection(
			Vector3.right *
			(MotorAcceleration * 0.1f) *
			m_MotorAirSpeedModifier) *
			m_SlopeFactor);

		// dampen motor force
		m_MotorThrottle.x /= (1.0f + (MotorDamping * m_MotorAirSpeedModifier * Time.timeScale));
		m_MotorThrottle.z /= (1.0f + (MotorDamping * m_MotorAirSpeedModifier * Time.timeScale));
	}


	/// <summary>
	/// throttle logic for moving a flying controller in an arbitrary
	/// direction based on player (camera) forward vector. this can be
	/// used for spectator cams, zero gravity, swimming underwater,
	/// jetpacks and superhero style flying
	/// </summary>
	protected virtual void UpdateThrottleFree()
	{

		// convert input to forces in the motor
		m_MotorThrottle += Player.InputMoveVector.Get().y * (Transform.TransformDirection(
			Transform.InverseTransformDirection(((vp_FPPlayerEventHandler)Player).CameraLookDirection.Get()) *
			(MotorAcceleration * 0.1f)));
		m_MotorThrottle += Player.InputMoveVector.Get().x * (Transform.TransformDirection(
			Vector3.right *
			(MotorAcceleration * 0.1f)));

		// dampen motor force
		m_MotorThrottle.x /= (1.0f + (MotorDamping * Time.timeScale));
		m_MotorThrottle.z /= (1.0f + (MotorDamping * Time.timeScale));

	}


	/// <summary>
	/// handles all jump logic, including impulse jumping, continuous
	/// jumping, vertical movement during free fly mode and stopping
	/// the controller on ceiling contact
	/// </summary>
	protected virtual void UpdateJump()
	{

		// abort all jumping activity for 1 second if head touches a ceiling
		if (m_HeadContact)
			Player.Jump.Stop(1.0f);

		if (!MotorFreeFly)
			UpdateJumpForceWalk();
		else
			UpdateJumpForceFree();

		// apply accumulated 'hold jump' force
		m_MotorThrottle.y += m_MotorJumpForceAcc * Time.timeScale;

		// dampen forces
		m_MotorJumpForceAcc /= (1.0f + (MotorJumpForceHoldDamping * Time.timeScale));
		m_MotorThrottle.y /= (1.0f + (MotorJumpForceDamping * Time.timeScale));

	}


	/// <summary>
	/// performs jump logic for a ground walking controller
	/// </summary>
	protected virtual void UpdateJumpForceWalk()
	{

		if (Player.Jump.Active)
		{
			if (!m_Grounded)
			{
				// accumulate 'hold jump' force if the jump button is still being held
				// down 2 fixed frames after the impulse jump
				if (m_MotorJumpForceHoldSkipFrames > 2)
				{
					// but only if jump button hasn't been released on the way down
					if (!(Player.Velocity.Get().y < 0.0f))
						m_MotorJumpForceAcc += MotorJumpForceHold;
				}
				else
					m_MotorJumpForceHoldSkipFrames++;
			}
		}

	}


	/// <summary>
	/// performs vertical movement logic for a free flying controller,
	/// going straight up or down while the jump or crouch activities
	/// are active, respectively
	/// </summary>
	protected virtual void UpdateJumpForceFree()
	{

		if (Player.Jump.Active && Player.Crouch.Active)
			return;

		if (Player.Jump.Active)
			m_MotorJumpForceAcc += MotorJumpForceHold;
		else if (Player.Crouch.Active)
		{

			m_MotorJumpForceAcc -= MotorJumpForceHold;

			// trigger crouch collision update on ground contact
			if (Grounded && CharacterController.height == m_NormalHeight)
			{
				CharacterController.height = m_CrouchHeight;
				CharacterController.center = m_CrouchCenter;
			}

		}

	}


	/// <summary>
	/// updates the controller according to a simple physics
	/// simulation including gravity and smooth external forces
	/// </summary>
	protected override void UpdateForces()
	{

		base.UpdateForces();

		// apply smooth force (forces applied over several frames)
		if (m_SmoothForceFrame[0] != Vector3.zero)
		{
			AddForceInternal(m_SmoothForceFrame[0]);
			for (int v = 0; v < 120; v++)
			{
				m_SmoothForceFrame[v] = (v < 119) ? m_SmoothForceFrame[v + 1] : Vector3.zero;
				if (m_SmoothForceFrame[v] == Vector3.zero)
					break;
			}
		}

		// dampen external forces
		m_ExternalForce /= (1.0f + (PhysicsForceDamping * vp_TimeUtility.AdjustedTimeScale));

	}


	/// <summary>
	/// simulates sliding in slopes. the controller may slide at
	/// a constant or accumulated rate. see the docs for available
	/// parameters
	/// </summary>
	protected virtual void UpdateSliding()
	{

		bool wasSlidingFast = m_SlideFast;
		bool wasSliding = m_Slide;

		// --- handle slope sliding ---
		// TIP: alter 'PhysicsSlopeSlidiness' and 'SlopeSlideLimit' in realtime
		// using the state manager, depending on the current ground surface
		m_Slide = false;
		if (!m_Grounded)
		{
			m_OnSteepGroundSince = 0.0f;
			m_SlideFast = false;
		}
		// start sliding if ground is steep enough in angles
		else if (GroundAngle > PhysicsSlopeSlideLimit)
		{
			m_Slide = true;

			// if ground angle is within slopelimit, slide at a constant speed
			if (GroundAngle <= Player.SlopeLimit.Get())
			{
				m_SlopeSlideSpeed = Mathf.Max(m_SlopeSlideSpeed, (PhysicsSlopeSlidiness * 0.01f));
				m_OnSteepGroundSince = 0.0f;
				m_SlideFast = false;
				// apply slope speed damping (and snap to zero if miniscule, to avoid
				// floating point errors)
				m_SlopeSlideSpeed = (Mathf.Abs(m_SlopeSlideSpeed) < 0.0001f) ? 0.0f :
					(m_SlopeSlideSpeed / (1.0f + (0.05f * vp_TimeUtility.AdjustedTimeScale)));
			}
			else	// if steeper than slopelimit, slide with accumulating slide speed
			{
				if ((m_SlopeSlideSpeed) > 0.01f)
					m_SlideFast = true;
				if (m_OnSteepGroundSince == 0.0f)
					m_OnSteepGroundSince = Time.time;
				m_SlopeSlideSpeed += (((PhysicsSlopeSlidiness * 0.01f) * ((Time.time - m_OnSteepGroundSince) * 0.125f)) * vp_TimeUtility.AdjustedTimeScale);
				m_SlopeSlideSpeed = Mathf.Max((PhysicsSlopeSlidiness * 0.01f), m_SlopeSlideSpeed);	// keep minimum slidiness
			}

			// add horizontal force in the slope direction, multiplied by slidiness
			AddForce(Vector3.Cross(Vector3.Cross(GroundNormal, Vector3.down), GroundNormal) *
				m_SlopeSlideSpeed * vp_TimeUtility.AdjustedTimeScale);

		}
		else
		{
			m_OnSteepGroundSince = 0.0f;
			m_SlideFast = false;
			m_SlopeSlideSpeed = 0.0f;
		}

		// if player is moving by its own, external components should not
		// consider it slow-sliding. this is intended for retaining movement
		// fx (like weapon bob) on less slidy surfaces
		if (m_MotorThrottle != Vector3.zero)
			m_Slide = false;

		// handle fast sliding into free fall
		if (m_SlideFast)
			m_SlideFallSpeed = Transform.position.y;	// store y to calculate difference next frame
		else if (wasSlidingFast && !Grounded)
			m_FallSpeed = Transform.position.y - m_SlideFallSpeed;	// lost grounding while sliding fast: kickstart gravity at slide fall speed

		// detect whether the slide variables have changed, and broadcast
		// messages so external components can update accordingly

		if (wasSliding != m_Slide)
			Player.SetState("Slide", m_Slide);

	}


	/// <summary>
	/// this method starts an 'out of control' activity whenever the player
	/// is pushed around by external forces or starts sliding very fast.
	/// this is currently intended just for triggering animations on the
	/// character body model, however it could also be used to prevent other
	/// activities from starting when the player is out of control. example:
	/// (in 'CanStart_Attack') if(Player.OutOfControl.Active) return false;
	/// </summary>
	void UpdateOutOfControl()
	{

		if ((m_ExternalForce.magnitude > 0.2f) ||		// TODO: make 0.2 a constant
			(m_FallSpeed < -0.2f) ||	// TODO: make 0.2 a constant
				(m_SlideFast == true))
			Player.OutOfControl.Start();
		else if (Player.OutOfControl.Active)
				Player.OutOfControl.Stop();

	}


	/// <summary>
	/// combines motor and external forces into a move direction
	/// and sets the resulting controller position
	/// </summary>
	protected override void FixedMove()
	{

		// --- apply forces ---
		m_MoveDirection = Vector3.zero;
		m_MoveDirection += m_ExternalForce;
		m_MoveDirection += m_MotorThrottle;
		m_MoveDirection.y += m_FallSpeed;

		// --- apply anti-bump offset ---
		// this pushes the controller towards the ground to prevent the character
		// from "bumpety-bumping" when walking down slopes or stairs. the strength
		// of this effect is determined by the character controller's 'Step Offset'
		m_CurrentAntiBumpOffset = 0.0f;
		if (m_Grounded && m_MotorThrottle.y <= 0.001f)
		{
			m_CurrentAntiBumpOffset = Mathf.Max(Player.StepOffset.Get(), Vector3.Scale(m_MoveDirection, (Vector3.one - Vector3.up)).magnitude);
			m_MoveDirection += m_CurrentAntiBumpOffset * Vector3.down;
		}

		// --- predict move result ---
		// do some prediction in order to detect blocking and deflect forces on collision
		m_PredictedPos = Transform.position + vp_MathUtility.NaNSafeVector3(m_MoveDirection * Delta * Time.timeScale);

		// --- move the charactercontroller ---

		// ride along with movable objects
		if (m_Platform != null && PositionOnPlatform != Vector3.zero)
				Player.Move.Send(vp_MathUtility.NaNSafeVector3(m_Platform.TransformPoint(PositionOnPlatform) -
																		m_Transform.position));

        // move on our own
		Player.Move.Send(vp_MathUtility.NaNSafeVector3(m_MoveDirection * Delta * Time.timeScale));

		// while there is an active death event, block movement input
		if (Player.Dead.Active)
		{
			Player.InputMoveVector.Set(Vector2.zero);
			return;
		}

		// --- store ground info ---
		StoreGroundInfo();

		// --- store head contact info ---
		// spherecast upwards for some info on the surface touching the top of the collider, if any
		if (!m_Grounded && (Player.Velocity.Get().y > 0.0f))
		{
			Physics.SphereCast(new Ray(Transform.position, Vector3.up),
										Player.Radius.Get(), out m_CeilingHit,
										Player.Height.Get() - (Player.Radius.Get() - SkinWidth) + 0.01f,
										vp_Layer.Mask.ExternalBlockers);
			m_HeadContact = (m_CeilingHit.collider != null);
		}
		else
			m_HeadContact = false;

		// --- handle loss of grounding ---
		if ((m_GroundHitTransform == null) && (m_LastGroundHitTransform != null))
		{
			
			// if we lost contact with a moving object, inherit its speed
			// then forget about it
			if (m_Platform != null && PositionOnPlatform != Vector3.zero)
			{
				AddForce(m_Platform.position - m_LastPlatformPos);
				m_Platform = null;
			}

			// undo anti-bump offset to make the fall smoother
			if (m_CurrentAntiBumpOffset != 0.0f)
			{
				Player.Move.Send(vp_MathUtility.NaNSafeVector3(m_CurrentAntiBumpOffset * Vector3.up) * Delta * Time.timeScale);
				m_PredictedPos += vp_MathUtility.NaNSafeVector3(m_CurrentAntiBumpOffset * Vector3.up) * Delta * Time.timeScale;
				m_MoveDirection += m_CurrentAntiBumpOffset * Vector3.up;
			}

		}


	}
	

	/// <summary>
	/// since the controller is moved in FixedUpdate and the
	/// camera in Update there will be noticeable camera jitter.
	/// this method simulates the controller move in Update and
	/// stores the smooth position for the camera to read
	/// </summary>
	protected virtual void SmoothMove()
	{

		if (Time.timeScale == 0.0f)
			return;

		// restore last smoothpos
		m_FixedPosition = Transform.position;	// backup fixedpos
		Transform.position = m_SmoothPosition;

		// move controller to get the smooth position
		Player.Move.Send(vp_MathUtility.NaNSafeVector3((m_MoveDirection * Delta * Time.timeScale)));
		m_SmoothPosition = Transform.position;
		Transform.position = m_FixedPosition;	// restore fixedpos

		// reset smoothpos in these cases
		if ((Vector3.Distance(Transform.position, m_SmoothPosition) > Player.Radius.Get())	// smoothpos deviates too much
			|| (m_Platform != null) && ((m_LastPlatformPos != m_Platform.position)))		// we're on a platform thas is moving (causes jitter)
			m_SmoothPosition = Transform.position;

		// lerp smoothpos back to fixedpos slowly over time
		m_SmoothPosition = Vector3.Lerp(m_SmoothPosition, Transform.position, Time.deltaTime);

	}


	/// <summary>
	/// updates controller motion according to detected collisions
	/// against objects below, above and around the controller
	/// </summary>
	protected override void UpdateCollisions()
	{

		base.UpdateCollisions();

		if (m_OnNewGround)
		{

			// deflect the controller sideways under some circumstances
			if (m_WasFalling)
			{

				DeflectDownForce();

				// sync camera y pos
				m_SmoothPosition.y = Transform.position.y;

				// reset all the jump variables
				m_MotorThrottle.y = 0.0f;
				m_MotorJumpForceAcc = 0.0f;
				m_MotorJumpForceHoldSkipFrames = 0;
			}
			// detect and store moving platforms	// TODO: should be in base class for AI
			if (m_GroundHit.collider.gameObject.layer == vp_Layer.MovingPlatform)
			{
				m_Platform = m_GroundHitTransform;
				m_LastPlatformAngle = m_Platform.eulerAngles.y;
			}
			else
				m_Platform = null;

		}


		// --- respond to ceiling collision ---
		// deflect forces that push the controller upward, in order to prevent
		// getting stuck in ceilings
		if ((m_PredictedPos.y > Transform.position.y) && (m_ExternalForce.y > 0 || m_MotorThrottle.y > 0))
			DeflectUpForce();

		// --- respond to wall collision ---
		// if the controller didn't end up at the predicted position, some
		// external object has blocked its way, so deflect the movement forces
		// to avoid getting stuck at walls
		if ((m_PredictedPos.x != Transform.position.x) ||
			(m_PredictedPos.z != Transform.position.z) &&
			(m_ExternalForce != Vector3.zero))
			DeflectHorizontalForce();

	}
	

	/// <summary>
	/// this method calculates a controller velocity multiplier
	/// depending on ground slope. at 'MotorSlopeSpeed' 1.0,
	/// velocity in slopes will be kept roughly the same as on
	/// flat ground. values lower or higher than 1 will make the
	/// controller slow down / speed up, depending on whether
	/// we're moving uphill or downhill
	/// </summary>
	protected virtual void UpdateSlopeFactor()
	{

		if (!m_Grounded)
		{
			m_SlopeFactor = 1.0f;
			return;
		}

		// determine if we're moving uphill or downhill
		m_SlopeFactor = 1 + (1.0f - (Vector3.Angle(m_GroundHit.normal, m_MotorThrottle) / 90.0f));

		if (Mathf.Abs(1 - m_SlopeFactor) < 0.01f)
			m_SlopeFactor = 1.0f;		// standing still or moving on flat ground, or moving perpendicular to a slope
		else if (m_SlopeFactor > 1.0f)
		{
			// moving downhill
			if (MotorSlopeSpeedDown == 1.0f)
			{
				// 1.0 means 'no change' so we'll alter the value to get
				// roughly the same velocity as if ground was flat
				m_SlopeFactor = 1.0f / m_SlopeFactor;
				m_SlopeFactor *= 1.2f;
			}
			else
				m_SlopeFactor *= MotorSlopeSpeedDown;	// apply user defined multiplier
		}
		else
		{
			// moving uphill
			if (MotorSlopeSpeedUp == 1.0f)
			{
				// 1.0 means 'no change' so we'll alter the value to get
				// roughly the same velocity as if ground was flat
				m_SlopeFactor *= 1.2f;
			}
			else
				m_SlopeFactor *= MotorSlopeSpeedUp;	// apply user defined multiplier

			// kill motor if moving into a slope steeper than 'slopeLimit'. this serves
			// to prevent exploits with being able to walk up steep surfaces and walls
			m_SlopeFactor = (GroundAngle > Player.SlopeLimit.Get()) ? 0.0f : m_SlopeFactor;

		}

	}
	

	/// <summary>
	/// moves and rotates the controller while standing on top a movable
	/// object such as a rigidbody or a moving platform. NOTE: any moving
	/// platforms must be in the'MovingPlatform' layer!
	/// </summary>
	protected override void UpdatePlatformMove()
	{

		base.UpdatePlatformMove();

		// sync smooth position to fixed position while on a movable
		if (m_Platform != null)
			m_SmoothPosition = Transform.position;

	}

	
	/// <summary>
	/// 
	/// </summary>
	protected override void UpdatePlatformRotation()	// needed for smooth remote player movement on platforms
	{

		if (m_Platform == null)
			return;

		base.UpdatePlatformRotation();

	}
	

	/// <summary>
	/// sets the position and smooth position of the Controller
	/// </summary>
	public override void SetPosition(Vector3 position)
	{

		base.SetPosition(position);
		m_SmoothPosition = position;

	}
	

	/// <summary>
	/// adds external force to the controller, such as explosion
	/// knockback, wind or jump pads
	/// </summary>
	protected virtual void AddForceInternal(Vector3 force)
    {
        m_ExternalForce += force;
	}


	/// <summary>
	/// adds external force to the controller, such as explosion
	/// knockback, wind or jump pads
	/// </summary>
	public virtual void AddForce(float x, float y, float z)
	{
		AddForce(new Vector3(x, y, z));
	}


	/// <summary>
	/// adds external velocity to the controller in one frame
	/// </summary>
	public virtual void AddForce(Vector3 force)
	{

		if (Time.timeScale >= 1.0f)
			AddForceInternal(force);
		else
			AddSoftForce(force, 1);

	}


	/// <summary>
	/// adds a force distributed over up to 120 fixed frames
	/// </summary>
	public virtual void AddSoftForce(Vector3 force, float frames)
	{

		force /= Time.timeScale;

		frames = Mathf.Clamp(frames, 1, 120);

		AddForceInternal(force / frames);

		for (int v = 0; v < (Mathf.RoundToInt(frames) - 1); v++)
		{
			m_SmoothForceFrame[v] += (force / frames);
		}

	}


	/// <summary>
	/// clears any soft forces currently buffered on this controller
	/// </summary>
	public virtual void StopSoftForce()
	{

		for (int v = 0; v < 120; v++)
		{
			if (m_SmoothForceFrame[v] == Vector3.zero)
				break;
			m_SmoothForceFrame[v] = Vector3.zero;
		}

	}


	/// <summary>
	/// completely stops the character controller in one frame
	/// </summary>
	public override void Stop()
	{

		base.Stop();

		m_MotorThrottle = Vector3.zero;
		m_MotorJumpDone = true;
		m_MotorJumpForceAcc = 0.0f;
		m_ExternalForce = Vector3.zero;
		StopSoftForce();
		m_SmoothPosition = Transform.position;
		
	}


	/// <summary>
	/// typically we don't deflect downward forces since there is
	/// always a ground collision imposed by gravity (and it would
	/// be annoying from a controls perspective). this deals with
	/// the couple of cases when deflection does happen
	/// </summary>
	public virtual void DeflectDownForce()
	{
		
		// if we land on a surface tilted above the slide limit, convert
		// fall speed into slide speed on impact
		if (GroundAngle > PhysicsSlopeSlideLimit)
		{
			m_SlopeSlideSpeed = m_FallImpact * (0.25f * Time.timeScale);
		}

		// deflect away from nearly vertical surfaces. this serves to make
		// falling along walls smoother, and to prevent the controller
		// from getting stuck on vertical walls when falling into them
		if (GroundAngle > 85)
		{
			m_MotorThrottle += (vp_3DUtility.HorizontalVector((GroundNormal * m_FallImpact)));
			m_Grounded = false;
		}

	}


	/// <summary>
	/// this method deflects the controller away from ceilings
	/// in response to collisions resulting from upward forces
	/// such as jumping, explosions or jump pads. wall friction
	/// is applied in the collision
	/// </summary>
	protected virtual void DeflectUpForce()
	{

		if (!m_HeadContact)
			return;

		// convert the vertical force into horizontal force, deflecting the
		// controller away from any tilted ceilings (a perfectly horizontal
		// ceiling simply kills the vertical force). also, store impact force
		m_NewDir = Vector3.Cross(Vector3.Cross(m_CeilingHit.normal, Vector3.up), m_CeilingHit.normal);
		m_ForceImpact = (m_MotorThrottle.y + m_ExternalForce.y);
		Vector3 newForce = m_NewDir * (m_MotorThrottle.y + m_ExternalForce.y) * (1.0f - PhysicsWallFriction);
		m_ForceImpact = m_ForceImpact - newForce.magnitude;
		AddForce(newForce * Time.timeScale);
		m_MotorThrottle.y = 0.0f;
		m_ExternalForce.y = 0.0f;
		m_FallSpeed = 0.0f;

		// transmit headbump for other components to perform effects. make the
		// impact positive or negative depending on whether the ceiling we
		// bumped into repelled us to the left or right. if any other direction
		// (forward / backward / none) then pick randomly
		m_NewDir.x = (Transform.InverseTransformDirection(m_NewDir).x);

		Player.HeadImpact.Send(((m_NewDir.x < 0.0f) || (m_NewDir.x == 0.0f && (Random.value < 0.5f))) ? -m_ForceImpact : m_ForceImpact);

	}


	/// <summary>
	/// this method is called when the controller collides with
	/// something while moving horizontally. it calculates a new
	/// movement direction based on the impact normal and the new
	/// position decided by the physics engine, and deflects the
	/// controller's current horizontal force along the new vector.
	/// wall friction and bouncing are also applied
	/// </summary>
	protected virtual void DeflectHorizontalForce()
	{
		// flatten positions (this is 2d) and get our direction at point of impact
		m_PredictedPos.y = Transform.position.y;
		m_PrevPosition.y = Transform.position.y;
		m_PrevDir = (m_PredictedPos - m_PrevPosition).normalized;

		// get the origins of the controller capsule's spheres at prev position
		CapsuleBottom = m_PrevPosition + Vector3.up * (Player.Radius.Get());
		CapsuleTop = CapsuleBottom + Vector3.up * (Player.Height.Get() - (Player.Radius.Get() * 2));

		// capsule cast from the previous position to the predicted position to find
		// the exact impact point. this capsule cast does not include the skin width
		// (it's not really needed plus we don't want ground collisions)
		if (!(Physics.CapsuleCast(CapsuleBottom, CapsuleTop, Player.Radius.Get(), m_PrevDir,
			out m_WallHit, Vector3.Distance(m_PrevPosition, m_PredictedPos), vp_Layer.Mask.ExternalBlockers)))
			return;

		// the force will be deflected perpendicular to the impact normal, and to the
		// left or right depending on whether the previous position is to our left or
		// right when looking back at the impact point from the current position
		m_NewDir = Vector3.Cross(m_WallHit.normal, Vector3.up).normalized;
		if ((Vector3.Dot(Vector3.Cross((m_WallHit.point - Transform.position),
			(m_PrevPosition - Transform.position)), Vector3.up)) > 0.0f)
			m_NewDir = -m_NewDir;

		// calculate how the current force gets absorbed depending on angle of impact.
		// if we hit a wall head-on, almost all force will be absorbed, but if we
		// barely glance it, force will be almost unaltered (depending on friction)
		m_ForceMultiplier = Mathf.Abs(Vector3.Dot(m_PrevDir, m_NewDir)) * (1.0f - (PhysicsWallFriction));

		// if the controller has wall bounciness, apply it
		if (PhysicsWallBounce > 0.0f)
		{
			m_NewDir = Vector3.Lerp(m_NewDir, Vector3.Reflect(m_PrevDir, m_WallHit.normal), PhysicsWallBounce);
			m_ForceMultiplier = Mathf.Lerp(m_ForceMultiplier, 1.0f, (PhysicsWallBounce * (1.0f - (PhysicsWallFriction))));
		}

		// deflect current force and report the impact
		m_ForceImpact = 0.0f;
		float yBak = m_ExternalForce.y;
		m_ExternalForce.y = 0.0f;
		m_ForceImpact = m_ExternalForce.magnitude;
		m_ExternalForce = m_NewDir * m_ExternalForce.magnitude * m_ForceMultiplier;
		m_ForceImpact = m_ForceImpact - m_ExternalForce.magnitude;
		for (int v = 0; v < 120; v++)
		{
			if (m_SmoothForceFrame[v] == Vector3.zero)
				break;
			m_SmoothForceFrame[v] = m_SmoothForceFrame[v].magnitude * m_NewDir * m_ForceMultiplier;
		}
		m_ExternalForce.y = yBak;

		// TIP: the force that was absorbed by the bodies during the impact can be used for
		// things like damage, so an event could be sent here with the amount of absorbed force

	}

	
	/// <summary>
	/// returns the maximum speed of the controller's "Default" state
	/// (default) or an optional state. the return value represents the
	/// speed in meters per second that would build up if the controller
	/// was	to move full throttle for 5 (default) seconds on an even
	/// surface. NOTE: you may not want to simulate this every frame
	/// </summary>
	public float CalculateMaxSpeed(string stateName = "Default", float accelDuration = 5.0f)
	{

		// if we're getting a state other than "Default", first make sure
		// the state exists
		if (stateName != "Default")
		{
			bool foundState = false;
			foreach (vp_State s in States)
			{
				if (s.Name == stateName)
					foundState = true;
			}

			if (!foundState)
			{
				Debug.LogError("Error (" + this + ") Controller has no such state: '" + stateName + "'.");
				return 0.0f;
			}
		}

		// backup the current 'enabled' status of all the states
		Dictionary<vp_State, bool> statesBackup = new Dictionary<vp_State, bool>();
		foreach (vp_State s in States)
		{
			statesBackup.Add(s, s.Enabled);
			s.Enabled = false;
		}

		// reset state manager so only the default state is active
		StateManager.Reset();

		// enable the user passed state (if any)
		if (stateName != "Default")
			SetState(stateName, true);

		// ok, here's where the magic happens! simulate accelerating the
		// controller in an arbitrary direction for 'accelDuration' seconds
		float speed = 0.0f;
		float seconds = 5.0f;
		for (int v = 0; v < 60 * seconds; v++)
		{
			speed += (MotorAcceleration * 0.1f) * 60.0f;
			speed /= (1.0f + MotorDamping);
		}

		// got the resulting speed, now clean up after ourselves
		foreach (vp_State s in States)
		{
			bool enabled;
			statesBackup.TryGetValue(s, out enabled);
			s.Enabled = enabled;
		}

		return speed;

	}


	/// <summary>
	/// simple solution for pushing rigid bodies. the push force
	/// of the FPSController is used to determine how much we
	/// can affect the other object, and we don't affect fast
	/// falling objects.
	/// </summary>
	protected virtual void OnControllerColliderHit(ControllerColliderHit hit)
	{
		//Debug.Log(vp_Gameplay.isMultiplayer);

		// early-out if the object is static or debris

		if (hit.gameObject.isStatic)
			return;

		if (hit.gameObject.layer == vp_Layer.Debris)
			return;

		// try to find a rigidbody on the object
		Rigidbody body = hit.collider.attachedRigidbody;

		// abort if there was no rigidbody
		if (body == null)
			return;

		// abort if the rigidbody is kinematic and this is a singleplayer
		// (or multiplayer master) scene
		if (vp_Gameplay.IsMaster && body.isKinematic)
			return;

		// abort if we can't push yet
		if (Time.time < m_NextAllowedPushTime)
			return;
		m_NextAllowedPushTime = Time.time + PhysicsPushInterval;

		// in multiplayer, don't manipulate anything directly, just send a push
		// message to the rigidbody with the movedirection and point of impact.
		// a multiplayer script can pick this up and ask the master to push the
		// rigidbody over the network, as allowed
		if (vp_Gameplay.IsMultiplayer)
			vp_TargetEvent<Vector3, Vector3>.Send(body, "Push", hit.moveDirection, hit.point);
		else
			PushRigidbody(body, hit.moveDirection, hit.point);	// in singleplayer, go ahead and push the rigidbody

	}


	/// <summary>
	/// adds a condition (a rule set) that must be met for the
	/// event handler 'Jump' activity to successfully activate.
	/// NOTE: other scripts may have added conditions to this
	/// activity aswell
	/// </summary>
	protected virtual bool CanStart_Jump()
	{

		// always allowed to move vertically in free fly mode
		if (MotorFreeFly)
			return true;

		// can't jump without ground contact
		if (!m_Grounded)
			return false;

		// can't jump until the previous jump has stopped
		if (!m_MotorJumpDone)
			return false;

		// can't bunny-hop up steep surfaces
		if (GroundAngle > Player.SlopeLimit.Get())
			return false;

		// passed the test!
		return true;

	}


	/// <summary>
	/// adds a condition (a rule set) that must be met for the
	/// event handler 'Run' activity to successfully activate.
	/// NOTE: other scripts may have added conditions to this
	/// activity aswell
	/// </summary>
	protected virtual bool CanStart_Run()
	{

		// can't start running while crouching
		if (Player.Crouch.Active)
			return false;

		return true;

	}


	/// <summary>
	/// this callback is triggered right after the 'Jump' activity
	/// has been approved for activation
	/// </summary>
	protected virtual void OnStart_Jump()
	{

		m_MotorJumpDone = false;

		// disable impulse jump if we have no grounding in free fly mode 
		if (MotorFreeFly && !Grounded)
			return;

		// perform impulse jump
		m_MotorThrottle.y = (MotorJumpForce / Time.timeScale);

		// sync camera y pos
		m_SmoothPosition.y = Transform.position.y;

	}


	/// <summary>
	/// this callback is triggered when the 'Jump' activity deactivates
	/// </summary>
	protected virtual void OnStop_Jump()
	{

		m_MotorJumpDone = true;

	}


	/// <summary>
	/// adds a condition (a rule set) that must be met for the
	/// event handler 'Crouch' activity to successfully deactivate.
	/// NOTE: other scripts may have added conditions to this
	/// activity aswell
	/// </summary>
	protected virtual bool CanStop_Crouch()
	{

		// can't stop crouching if there is a blocking object above us
		if (Physics.SphereCast(new Ray(Transform.position, Vector3.up),
				Player.Radius.Get(),
				(m_NormalHeight - Player.Radius.Get() + 0.01f),
				vp_Layer.Mask.ExternalBlockers))
		{

			// regulate stop test interval to reduce amount of sphere casts
			Player.Crouch.NextAllowedStopTime = Time.time + 1.0f;

			// found a low ceiling above us - abort getting up
			return false;

		}

		// nothing above us - okay to get up!
		return true;

	}


	/// <summary>
	/// adds external force to the controller
	/// </summary>
	protected virtual void OnMessage_ForceImpact(Vector3 force)
	{
		AddForce(force);
	}


	/// <summary>
	/// gets or sets the current motor throttle
	/// </summary>
	protected virtual Vector3 OnValue_MotorThrottle
	{
		get { return m_MotorThrottle; }
		set { m_MotorThrottle = value; }
	}


	/// <summary>
	/// returns true if the current jump has ended, false if not
	/// </summary>
	protected virtual bool OnValue_MotorJumpDone
	{
		get { return m_MotorJumpDone; }
	}

	
	/// <summary>
	/// always returns true if the player is in 1st person mode,
	/// and false in 3rd person
	/// </summary>
	protected virtual bool OnValue_IsFirstPerson
	{

		get
		{
			return m_IsFirstPerson;
		}
		set
		{
			m_IsFirstPerson = value;
		}

	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void OnStart_Dead()
	{
		m_Platform = null;
	}


	/// <summary>
	/// TEMP: (test)
	/// </summary>
	protected virtual void OnStop_Dead()
	{
		Player.OutOfControl.Stop();
	}


}


