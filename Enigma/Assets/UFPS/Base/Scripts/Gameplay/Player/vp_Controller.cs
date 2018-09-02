/////////////////////////////////////////////////////////////////////////////////
//
//	vp_Controller.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	abstract base class for player controllers in UFPS. contains a
//					basic common feature set for local, remote and AI players. has no
//					movement logic and can not be used on its own. must be extended
//					into a new script with special logic for that type of controller.
//					see 'vp_FPController' and 'vp_CapsuleController' for examples.
//
/////////////////////////////////////////////////////////////////////////////////


using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public abstract class vp_Controller : vp_Component
{

	// ground collision
	public bool Grounded { get { return m_Grounded; } }
	public Transform GroundTransform { get { return m_GroundHitTransform; } }	// current transform of the collider we're standing on
	protected bool m_Grounded = false;
	protected RaycastHit m_GroundHit;					// contains info about the ground we're standing on, if any
	protected Transform m_LastGroundHitTransform;		// ground hit from last frame: used to detect ground collision changes
	protected Transform m_GroundHitTransform;			// ground hit from last frame: used to detect ground collision changes
	protected float m_FallStartHeight = NOFALL;			// used for calculating fall impact
	protected float m_FallImpact = 0.0f;
	protected bool m_OnNewGround = false;
	protected bool m_WasFalling = false;

	// ground surface
	protected vp_SurfaceIdentifier m_CurrentGroundSurface = null;	// has info on the current SurfaceType we're standing on
	public Texture m_CurrentGroundTexture = null;		// current ground texture we're standing on
	public Texture m_CurrentTerrainTexture = null;		// current terrain texture we're standing on
	
	// gravity
	public float PhysicsGravityModifier = 0.2f;			// affects fall speed
	protected float m_FallSpeed = 0.0f;					// determines how quickly the controller falls in the world
	protected const float PHYSICS_GRAVITY_MODIFIER_INTERNAL = 0.002f;	// retained for backwards compatibility

	// motor
	public bool MotorFreeFly = false;

	// physics
	public float PhysicsPushForce = 5.0f;				// mass for pushing around rigidbodies
	public PushForceMode PhysicsPushMode = PushForceMode.Simplified;	// should pushing an object directly control its velocity (simplified) or apply accumulating kinetic force to it (realistic)
	public float PhysicsPushInterval = 0.1f;			// minimum delay between each rigidbody push
	public float PhysicsCrouchHeightModifier = 0.5f;	// how much to downscale the controller when crouching
	protected Vector3 m_Velocity = Vector3.zero;			// velocity calculated in same way as unity's character controller
	protected Vector3 m_PrevPosition = Vector3.zero;	// position on end of each fixed timestep
	protected Vector3 m_PrevVelocity = Vector3.zero;	// used for calculating velocity, and detecting the start of a fall 
	protected float m_NextAllowedPushTime;
	public float SkinWidth { get { return CHARACTER_CONTROLLER_SKINWIDTH; } }
	public enum PushForceMode
	{
		Simplified,		// pushing an object will directly control its velocity. this is the default mode
						// and 'classic UFPS mode' (prior to 1.5.0). it makes for smoother gameplay but
						// never applies vertical or point force

		Kinetic			// applies force to a point on a rigidbody allowing energy to accumulate. gives
						// more 'realistic' physics but may be trickier to balance for smooth gameplay
	}
	
	// moving platforms
	[HideInInspector]
	public Vector3 PositionOnPlatform = Vector3.zero;		// local position in relation to the movable object we're currently standing on
	[System.Obsolete("Please use 'PositionOnPlatform' instead.")]
	public Vector3 m_PositionOnPlatform
	{
		get	{	return PositionOnPlatform;	}
		set	{	PositionOnPlatform = value;	}
	}

	protected Transform m_Platform = null;					// current rigidbody or object in the 'MovableObject' layer that we are standing on
	protected float m_LastPlatformAngle;					// used for rotating controller along with movable object
	protected Vector3 m_LastPlatformPos = Vector3.zero;		// used for calculating inherited speed upon platform dismount
	protected float m_MovingPlatformBodyYawDif = 0.0f;		// used to make the lower body rotate correctly while on rotating platforms

	// crouching
	protected float m_NormalHeight = 0.0f;				// height of the player controller when not crouching (stored from the character controller in Start)
	protected Vector3 m_NormalCenter = Vector3.zero;	// forced to half of the controller height (for crouching logic)
	protected float m_CrouchHeight = 0.0f;				// height of the player controller when crouching (calculated in Start)
	protected Vector3 m_CrouchCenter = Vector3.zero;	// will be half of the crouch height, but no smaller than the crouch radius

	// constants (for backwards compatibility and special cases)
	protected const float KINETIC_PUSHFORCE_MULTIPLIER = 15.0f;		// makes 'kinetic' push force roughly similar to 'simplified' when pushing a 1x1 m cube with mass 1
	protected const float CHARACTER_CONTROLLER_SKINWIDTH = 0.08f;	// NOTE: should be kept the same as the Unity CharacterController's 'Skin Width' parameter, which is unfortunately not exposed to script
	protected const float DEFAULT_RADIUS_MULTIPLIER = 0.25f;		// forces width of controller capsule to a percentage of its height
	protected const float FALL_IMPACT_MULTIPLIER = 0.075f;			// for backwards compatibility (pre 1.5.0)
	protected const float NOFALL = -99999;							// when fall height is set to this value it means no fall impact will be reported


	// event handler property cast as a playereventhandler
	private vp_PlayerEventHandler m_Player = null;
	protected vp_PlayerEventHandler Player
	{
		get
		{
			if (m_Player == null)
			{
				if (EventHandler != null)
					m_Player = (vp_PlayerEventHandler)EventHandler;
				if (m_Player == null)
					Debug.LogError("Error (" + this + ") This component requires a " + ((this is vp_FPController) ? "vp_FPPlayerEventHandler" : "vp_PlayerEventHandler") + " component!");
			}
			return m_Player;
		}
	}



	/// <summary>
	/// 
	/// </summary>
	protected override void Awake()
	{

		base.Awake();

		InitCollider();

	}


	/// <summary>
	/// 
	/// </summary>
	protected override void Start()
	{

		base.Start();

		RefreshCollider();

	}


	/// <summary>
	/// 
	/// </summary>
	protected override void Update()
	{

		base.Update();

		// platform rotation is done in Update rather than FixedUpdate for
		// smooth remote player movement on platforms in multiplayer
		UpdatePlatformRotation();

	}


	/// <summary>
	/// 
	/// </summary>
	protected override void FixedUpdate()
	{

		if (Time.timeScale == 0.0f)
			return;

		// updates external forces like gravity
		UpdateForces();

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
	/// moves and rotates the controller while standing on top a movable
	/// object such as a rigidbody or a moving platform. NOTE: any moving
	/// platforms must be in the'MovableObject' layer or player slides off
	/// </summary>
	protected virtual void UpdatePlatformMove()
	{

		if (m_Platform == null)
			return;

		// calculate the controller's local position in relation to movable object
		// NOTE: if this method is disabled in 'FixedUpdate', 'PositionOnPlatform'
		// will remain zero, disabling platform logic in other methods also
		PositionOnPlatform = m_Platform.InverseTransformPoint(m_Transform.position);

		// store movement delta for calculating inherited velocity on dismount
		m_LastPlatformPos = m_Platform.position;

	}



	/// <summary>
	/// makes a controller rotate correctly with the current platform
	/// that it's standing on (if any)
	/// </summary>
	protected virtual void UpdatePlatformRotation()
	{

		if (m_Platform == null)
			return;

		// TODO: currently player feet will rotate with platforms locally,
		// however due to jitter issues this is not done for remote players

		// store any difference in yaw between our lower body (the controller)
		// and our head (the camera)
		if(Player.IsLocal.Get())
			m_MovingPlatformBodyYawDif = Mathf.Lerp(m_MovingPlatformBodyYawDif, Mathf.DeltaAngle(Player.Rotation.Get().y, Player.BodyYaw.Get()), Time.deltaTime * 1);

		// rotate camera yaw in sync with the movable
		Player.Rotation.Set(new Vector2(Player.Rotation.Get().x, Player.Rotation.Get().y -
			Mathf.DeltaAngle(m_Platform.eulerAngles.y, m_LastPlatformAngle)));
		m_LastPlatformAngle = m_Platform.eulerAngles.y;

		// restore difference in lower body and head yaw by rotating lower body only
		if (Player.IsLocal.Get())
			Player.BodyYaw.Set(Player.BodyYaw.Get() - m_MovingPlatformBodyYawDif);

	}

	
	/// <summary>
	/// stores final position and velocity for next frame's physics
	/// calculations
	/// </summary>
	protected virtual void UpdateVelocity()
	{

		m_PrevVelocity = m_Velocity;
		m_Velocity = (transform.position - m_PrevPosition) / Time.deltaTime;
		m_PrevPosition = Transform.position;

	}


	/// <summary>
	/// override this to completely stop the controller in one frame
	/// IMPORTANT: remember to call this base method too
	/// </summary>
	public virtual void Stop()
	{

		Player.Move.Send(Vector3.zero);
		Player.InputMoveVector.Set(Vector2.zero);
		m_FallSpeed = 0.0f;
		m_FallStartHeight = NOFALL;

	}


	/// <summary>
	/// this method should be overridden to initialize dynamic collider dimension
	/// variables for various states as needed, depending on whether the collider
	/// is a capsule collider, character controller or other type of collider
	/// </summary>
	protected virtual void InitCollider()
	{
	}


	/// <summary>
	/// this method should be overridden to refresh collider dimension variables
	/// depending on various states as needed
	/// </summary>
	protected virtual void RefreshCollider()
	{
	}


	/// <summary>
	/// this method should be overridden to enable or disable the collider, whether
	/// a capsule collider, character controller or other type of collider
	/// </summary>
	public virtual void EnableCollider(bool enabled)
	{
	}


	/// <summary>
	/// performs a sphere cast (as wide as the character) from ~knees to ground, and
	/// saves hit info in the 'm_GroundHit' variable. this gives access to lots of
	/// data on the object directly below us, object transform, ground angle etc.
	/// </summary>
	protected virtual void StoreGroundInfo()
	{

		// store ground hit for detecting fall impact and loss of grounding
		// in next frame
		m_LastGroundHitTransform = m_GroundHitTransform;

		m_Grounded = false;
		m_GroundHitTransform = null;
		// spherecast to just below feet to see if we're grounded - and if so store ground info
		if (
		Physics.SphereCast(new Ray(Transform.position + Vector3.up * (Player.Radius.Get()), Vector3.down),
									(Player.Radius.Get()), out m_GroundHit, (CHARACTER_CONTROLLER_SKINWIDTH + 0.1f),
									vp_Layer.Mask.ExternalBlockers))
		{

			m_GroundHitTransform = m_GroundHit.transform;
			// SNIPPET: use this if spherecast somehow returns the non-collider parent of your platform
			//if (m_GroundHitTransform.collider == null)
			//{
			//	Collider c = m_GroundHitTransform.GetComponentInChildren<Collider>();
			//	m_GroundHitTransform = c.transform;
			//}

			m_Grounded = true;
			
			//Debug.Log(m_GroundHitTransform);

		}

		// detect walking OFF AN EDGE into a fall (for fall impact)
		if ((m_Velocity.y < 0) && (m_GroundHitTransform == null)
			&& (m_LastGroundHitTransform != null)
			&& !Player.Jump.Active)
			SetFallHeight(Transform.position.y);
		
		return;

	}
	
	
	/// <summary>
	/// sets the value used for fall impact calculation
	/// according to certain criteria
	/// </summary>
	protected void SetFallHeight(float height)
	{

		// we can only track one fall at a time
		if (m_FallStartHeight != NOFALL)
			return;

		// can't set fall height if grounded
		if (m_Grounded || m_GroundHitTransform != null)
			return;

		m_FallStartHeight = height;

	}


	/// <summary>
	/// handles gravity. override to add additional forces like explosion
	/// shockwaves or wind
	/// </summary>
	protected virtual void UpdateForces()
	{

		// store ground for detecting fall impact and loss of grounding this frame
		m_LastGroundHitTransform = m_GroundHitTransform;

		// accumulate gravity
		if (m_Grounded && (m_FallSpeed <= 0.0f))
		// when not falling, stick controller to the ground by a small, fixed gravity
		{
			m_FallSpeed = (Physics.gravity.y * (PhysicsGravityModifier * PHYSICS_GRAVITY_MODIFIER_INTERNAL) * vp_TimeUtility.AdjustedTimeScale);
			return;
		}
		else
		{

			m_FallSpeed += (Physics.gravity.y * (PhysicsGravityModifier * PHYSICS_GRAVITY_MODIFIER_INTERNAL) * vp_TimeUtility.AdjustedTimeScale);

			// detect starting to fall MID-JUMP (for fall impact)
			if ((m_Velocity.y < 0) && (m_PrevVelocity.y >= 0.0f))
				SetFallHeight(Transform.position.y);

		}

	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void FixedMove()
	{

		StoreGroundInfo();

	}


	/// <summary>
	/// returns current fall distance, calculated as the altitude
	/// where fall began minus current altitude 
	/// </summary>
	float FallDistance
	{
		get
		{
			return ((m_FallStartHeight != NOFALL) ?	// only report positive fall distance if we have stored a fall height
					Mathf.Max(0.0f, (m_FallStartHeight - Transform.position.y)) : 0);
		}
	}


	/// <summary>
	/// updates controller motion according to detected collisions
	/// against objects below, above and around the controller
	/// </summary>
	protected virtual void UpdateCollisions()
	{

		// if climbing, abort any ongoing fall
		if (Player.Climb.Active)
			m_FallStartHeight = NOFALL;

		m_FallImpact = 0.0f;
		m_OnNewGround = false;
		m_WasFalling = false;

		// respond to ground collision
		if ((m_GroundHitTransform != null)
		 && (m_GroundHitTransform != m_LastGroundHitTransform))
		{

			// just standing on a new surface (important to detect even if
			// not falling, e.g. if walking onto a moving platform)
			m_OnNewGround = true;

			// if we were falling, transmit fall impact to the player. fall impact
			// is based on the distance from the impact position to the start point
			// of a mid-jump fall (detected in 'UpdateForces') or off-an-edge fall
			// (detected in 'StoreGroundInfo')
			if (m_LastGroundHitTransform == null)
			{
				m_WasFalling = true;
				if ((m_FallStartHeight > Transform.position.y) && m_Grounded)
				{
					m_FallImpact = (MotorFreeFly ? 0.0f : FallDistance * FALL_IMPACT_MULTIPLIER);
					Player.FallImpact.Send(m_FallImpact);
					//Debug.Log("DISTANCE: " + FallDistance);
				}
			}
			m_FallStartHeight = NOFALL;

		}

	}


	/// <summary>
	/// 
	/// </summary>
	protected override void LateUpdate()
	{

		base.LateUpdate();
	
	}


	/// <summary>
	/// sets the position of the Controller
	/// </summary>
	public virtual void SetPosition(Vector3 position)
	{

		Transform.position = position;
		m_PrevPosition = position;
		// must zero out 'm_PrevVelocity.y' at beginning of next frame in case
		// we're teleporting into free fall, or fall impact detection will break
		vp_Timer.In(0, () => { m_PrevVelocity = vp_3DUtility.HorizontalVector(m_PrevVelocity); });

	}


	/// <summary>
	/// makes this controller push a rigidbody using force or fixed velocity
	/// depending on the supplied 'PushForceMode'. in the 'Simplified' mode
	/// the controller will directly set the velocity of the rigidbody.
	/// in the 'Kinetic' mode force will be applied to the rigidbody at point
	/// of contact, and will accumulate. NOTE: 'point' will only have effect
	/// in 'Kinetic' mode.
	/// </summary>
	public void PushRigidbody(Rigidbody rigidbody, Vector3 moveDirection, PushForceMode pushForcemode, Vector3 point)
	{

		if (PhysicsPushForce == 0.0f)
			return;

		// riding and pushing platforms at the same time does not work
		// with simplified push force
		if ((rigidbody.gameObject.layer == vp_Layer.MovingPlatform) && (pushForcemode == PushForceMode.Simplified))
			return;

		switch (pushForcemode)
		{
			case PushForceMode.Simplified:
				rigidbody.velocity = (vp_3DUtility.HorizontalVector((new Vector3(moveDirection.x, 0, moveDirection.z)).normalized) * (PhysicsPushForce / rigidbody.mass));
				break;
			case PushForceMode.Kinetic:
				// if collision occurs beside (neither above nor below) the player we
				// will only apply horizontal force. this makes pushing stuff around
				// much smoother and easier
				if (Vector3.Distance(vp_3DUtility.HorizontalVector(Transform.position), vp_3DUtility.HorizontalVector(point)) > Player.Radius.Get())
				{
					rigidbody.AddForceAtPosition(vp_3DUtility.HorizontalVector(moveDirection) * (PhysicsPushForce * KINETIC_PUSHFORCE_MULTIPLIER), point);
					// DEBUG: uncomment this to visualize horizontal push RPCs as green balls
					//GameObject o1 = vp_3DUtility.DebugBall();
					//o1.renderer.material.color = Color.green;
					//o1.transform.position = point;
				}
				else
				{
					// if collision occured above or below the player we will apply force
					// along the unmodified collision vector. this makes for more realistic
					// physics when walking on top of stuff or bumping your head into it
					rigidbody.AddForceAtPosition(moveDirection * (PhysicsPushForce * KINETIC_PUSHFORCE_MULTIPLIER), point);
					// DEBUG: uncomment this to visualize vertical push RPCs as red balls
					//GameObject o2 = vp_3DUtility.DebugBall();
					//o2.transform.position = point;
				}
				break;
		}

	}


	/// <summary>
	/// pushes a rigidbody using the supplied 'PushForceMode' (regardless
	/// of the controller's own mode). if mode is 'Kinetic', the point of
	/// impact will automatically be the rigidbody's closest point on bounds
	/// to the center of the controller collider
	/// </summary>
	public void PushRigidbody(Rigidbody rigidbody, Vector3 moveDirection, PushForceMode pushForceMode)
	{

		PushRigidbody(rigidbody, moveDirection, pushForceMode,
			(pushForceMode == PushForceMode.Simplified ?
			Vector3.zero :
			rigidbody.ClosestPointOnBounds(Collider.bounds.center)));
				
	}


	/// <summary>
	/// pushes a rigidbody using the controller's own 'PhysicsPushMode'.
	/// if mode is 'Kinetic', the point of impact will automatically be 
	/// the rigidbody's closest point on bounds to the center of the
	/// controller collider
	/// </summary>
	public void PushRigidbody(Rigidbody rigidbody, Vector3 moveDirection)
	{
		PushRigidbody(rigidbody, moveDirection, this.PhysicsPushMode,
			(this.PhysicsPushMode == PushForceMode.Simplified ?
			Vector3.zero :
			rigidbody.ClosestPointOnBounds(Collider.bounds.center)));
	}
	

	/// <summary>
	/// pushes a rigidbody using the controller's own 'PhysicsPushMode',
	/// at the supplied point of impact. NOTE: 'point' will only have
	/// effect in 'Kinetic' mode.
	/// </summary>
	public void PushRigidbody(Rigidbody rigidbody, Vector3 moveDirection, Vector3 point)
	{
		PushRigidbody(rigidbody, moveDirection, this.PhysicsPushMode, point);
	}


	/// <summary>
	/// stops the controller in one frame, killing all forces
	/// acting upon it
	/// </summary>
	protected virtual void OnMessage_Stop()
	{
		Stop();
	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void OnStart_Crouch()
	{

		// force-stop the run activity
		Player.Run.Stop();

		// modify collider size
		RefreshCollider();

	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void OnStop_Crouch()
	{
		// modify collider size
		RefreshCollider();
	}


	/// <summary>
	/// returns transform of current platform we're standing on
	/// </summary>
	protected virtual Transform OnValue_Platform
	{
		get { return m_Platform; }
		set { m_Platform = value; }
	}


	/// <summary>
	/// gets or sets the current controller position in relation to the
	/// current platform. only works if 'm_Platform' is set
	/// </summary>
	protected virtual Vector3 OnValue_PositionOnPlatform
	{
		get { return ((m_Platform == null) ? Vector3.zero : PositionOnPlatform); }
		set { PositionOnPlatform = ((m_Platform == null) ? Vector3.zero : value); }
	}


	/// <summary>
	/// gets or sets the world position of the controller
	/// </summary>
	protected virtual Vector3 OnValue_Position
	{
		get { return Transform.position; }
		set { SetPosition(value); }
	}
	

	/// <summary>
	/// gets or sets the current motor falling speed directly
	/// </summary>
	protected virtual float OnValue_FallSpeed
	{
		get { return m_FallSpeed; }
		set { m_FallSpeed = value; }
	}


	/// <summary>
	/// gets or sets the current controller world velocity. the velocity
	/// returned is calculated in the same way as that of unity's
	/// charactercontroller. NOTE: setting this value won't have
	/// any effect on player position, it will only affect internal
	/// states such as animation speeds. to forcibly move the player,
	/// use Player.ForceImpact.Send() or Player.InputMoveVector.Set()
	/// </summary>
	protected virtual Vector3 OnValue_Velocity
	{
		get { return m_Velocity;	}
		set	{ m_Velocity = value;	}
	}


	/// <summary>
	/// this method must be overridden to get/set collider radius
	/// in a manner compatible with the current type of collider
	/// </summary>
	protected abstract float OnValue_Radius { get; }


	/// <summary>
	/// this method must be overridden to get/set collider height
	/// in a manner compatible with the current type of collider
	/// </summary>
	protected abstract float OnValue_Height { get; }


	/// <summary>
	/// returns whether the controller is grounded
	/// </summary>
	protected virtual bool OnValue_Grounded
	{
		get { return m_Grounded; }
	}


}







