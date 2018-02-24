/////////////////////////////////////////////////////////////////////////////////
//
//	vp_FPCamera.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	a first person camera class with weapon rendering and animation
//					features. animates the camera transform using springs, bob and
//					perlin noise, in response to user input
//
//					NOTE: this class previously contained a mouselook implementation
//					which has been moved to the 'vp_FPInput' class
//
///////////////////////////////////////////////////////////////////////////////// 

using UnityEngine;
using System.Collections.Generic;
using System.Collections;

[RequireComponent(typeof(Camera))]
[RequireComponent(typeof(AudioListener))]

public class vp_FPCamera : vp_Component
{

	// character controller of the parent gameobject
	public vp_FPController FPController = null;
	
	// NOTE: mouse input variables have been moved to vp_FPInput

	// camera rendering
	public float RenderingFieldOfView = 60.0f;
	public float RenderingZoomDamping = 0.2f;
	protected float m_FinalZoomTime = 0.0f;
	protected float m_ZoomOffset = 0.0f;
	public float ZoomOffset	{	get	{	return m_ZoomOffset;	}	// this can be set by an external script that needs to manipulate zoom
								set	{	m_ZoomOffset = value;	}}

	// camera position
	public Vector3 PositionOffset = new Vector3(0.0f, 1.75f, 0.1f);
	public float PositionGroundLimit = 0.1f;
	public float PositionSpringStiffness = 0.01f;
	public float PositionSpringDamping = 0.25f;
	public float PositionSpring2Stiffness = 0.95f;
	public float PositionSpring2Damping = 0.25f;
	public float PositionKneeling = 0.025f;
	public int PositionKneelingSoftness = 1;
	public float PositionEarthQuakeFactor = 1.0f;
	protected vp_Spring m_PositionSpring = null;		// spring for external forces (falling impact, bob, earthquakes)
	protected vp_Spring m_PositionSpring2 = null;		// 2nd spring for external forces (typically with stiffer spring settings)
	protected bool m_DrawCameraCollisionDebugLine = false;
	protected Vector3 PositionOnDeath = Vector3.zero;	// used for 3rd person death cam

	public Vector3 SpringState
	{
		get
		{
			return (m_PositionSpring.State + m_PositionSpring2.State);
		}
	}

	// camera rotation
	public Vector2 RotationPitchLimit = new Vector2(90.0f, -90.0f);
	public Vector2 RotationYawLimit = new Vector2(-360.0f, 360.0f);
	public float RotationSpringStiffness = 0.01f;
	public float RotationSpringDamping = 0.25f;
	public float RotationKneeling = 0.025f;
	public int RotationKneelingSoftness = 1;
	public float RotationStrafeRoll = 0.01f;
	public float RotationEarthQuakeFactor = 0.0f;
	public Vector3 LookPoint = Vector3.zero;
	protected float m_Pitch = 0.0f;
	protected float m_Yaw = 0.0f;
	protected vp_Spring m_RotationSpring = null;
	protected RaycastHit m_LookPointHit;

	// 3rd person
	public Vector3 Position3rdPersonOffset = new Vector3(0.5f, 0.1f, 0.75f);	// 3rd person camera offset in relation to 1st person offset NOTE: this is disabled by default
	protected float m_Current3rdPersonBlend = 0.0f;								// (0-1) extent to which camera has reached its target offset (during smooth transition to 3rd person mode)
	protected Vector3 m_Final3rdPersonCameraOffset = Vector3.zero;				// only used in 3rd person mode. needed for calculating correct lookat-point in 3rd person

	// camera shake
	public float ShakeSpeed = 0.0f;
	public Vector3 ShakeAmplitude = new Vector3(10, 10, 0);
	protected Vector3 m_Shake = Vector3.zero;

	// camera bob
	public Vector4 BobRate = new Vector4(0.0f, 1.4f, 0.0f, 0.7f);			// TIP: use x for a mech / dino like walk cycle. y should be (x * 2) for a nice classic curve of motion. typical defaults for y are 0.9 (rate) and 0.1 (amp)
	public Vector4 BobAmplitude = new Vector4(0.0f, 0.25f, 0.0f, 0.5f);		// TIP: make x & y negative to invert the curve
	public float BobInputVelocityScale = 1.0f;								
	public float BobMaxInputVelocity = 100;									// TIP: calibrate using 'Debug.Log(Controller.velocity.sqrMagnitude);'
	public bool BobRequireGroundContact = true;
	protected float m_LastBobSpeed = 0.0f;
	protected Vector4 m_CurrentBobAmp = Vector4.zero;
	protected Vector4 m_CurrentBobVal = Vector4.zero;
	protected float m_BobSpeed = 0.0f;

	// camera bob step variables
	public delegate void BobStepDelegate();
	public BobStepDelegate BobStepCallback;
	public float BobStepThreshold = 10.0f;
	protected float m_LastYBob = 0.0f;
	protected bool m_BobWasElevating = false;

	// camera collision
	public bool HasCollision = true;										// if true, camera will run its own collision check
	protected Vector3 m_CollisionVector = Vector3.zero;						// holds the direction and distance of a camera collision
	protected Vector3 m_CameraCollisionStartPos = Vector3.zero;
	protected Vector3 m_CameraCollisionEndPos = Vector3.zero;
	protected RaycastHit m_CameraHit;
	public bool DrawCameraCollisionDebugLine { get { return m_DrawCameraCollisionDebugLine; } set { m_DrawCameraCollisionDebugLine = value; } }	// for editor use
	public Vector3 CollisionVector { get { return m_CollisionVector; } }

	// for temporary disabling of specific procedural camera motions at runtime,
	//  bypassesing states (intended to be set directly by VR mode)
	public bool MuteRoll;
	public bool MuteBob;
	public bool MuteShakes;
	public bool MuteEarthquakes;
	public bool MuteBombShakes;
	public bool MuteFallImpacts;
	public bool MuteHeadImpacts;
	public bool MuteGroundStomps;

	// event handler property cast as a playereventhandler
	vp_FPPlayerEventHandler m_Player = null;
	vp_FPPlayerEventHandler Player
	{
		get
		{
			if (m_Player == null)
			{
				if (EventHandler != null)
					m_Player = (vp_FPPlayerEventHandler)EventHandler;
			}
			return m_Player;
		}
	}


	// rigidbody to look at for 3rd person death cam. NOTE: this will return
	// the first rigidbody found under the player hierarchy which is assumed
	// to belong to the root bone. if this is not the case, you may want
	// to make this a public property
	Rigidbody m_FirstRigidbody = null;
	Rigidbody FirstRigidBody
	{
		get
		{
			if (m_FirstRigidbody == null)
			{
				m_FirstRigidbody = Transform.root.GetComponentInChildren<Rigidbody>();
			}
			return m_FirstRigidbody;
		}
	}


	// angle properties

	public Vector2 Angle
	{
		get { return new Vector2(m_Pitch, m_Yaw); }
		set
		{
			Pitch = value.x;
			Yaw = value.y;
		}
	}
	
	public Vector3 Forward
	{
		get { return m_Transform.forward; }
	}
		
	public float Pitch
	{
		// pitch is rotation around the x-vector
		get { return m_Pitch; }
		set
		{
			if (value > 90)
				value -= 360;
			m_Pitch = value;
		}
	}

	public float Yaw
	{
		// yaw is rotation around the y-vector
		get { return m_Yaw; }
		set
		{
			m_Yaw = value;
		}
	}


	public bool DisableVRModeOnStartup = false;


	/// <summary>
	/// in 'Awake' we do things that need to be run once at the
	/// very beginning. NOTE: as of Unity 4, gameobject hierarchy
	/// can not be altered in 'Awake'
	/// </summary>
	protected override void Awake()
	{

		base.Awake();

		FPController = Root.GetComponent<vp_FPController>();

		// run 'SetRotation' with the initial rotation of the camera. this is important
		// when not using the spawnpoint system (or player rotation will snap to zero yaw)
		SetRotation(new Vector2(Transform.eulerAngles.x, Transform.eulerAngles.y));

		// set parent gameobject layer to 'LocalPlayer', so camera can exclude it
		// this also prevents shell casings from colliding with the charactercollider
		Parent.gameObject.layer = vp_Layer.LocalPlayer;

		// TEST: removed for multiplayer. please report if this causes trouble
		//foreach (Transform b in Parent)
		//{
		//	if (b.gameObject.layer != vp_Layer.RemotePlayer)
		//		b.gameObject.layer = vp_Layer.LocalPlayer;
		//}
	
		// main camera initialization
		// render everything except body and weapon
		Camera.cullingMask &= ~((1 << vp_Layer.LocalPlayer) | (1 << vp_Layer.Weapon));
		Camera.depth = 0;

		// weapon camera initialization
		// find a regular Unity Camera component existing in a child
		// gameobject to the FPSCamera's gameobject. if we don't find
		// a weapon cam, that's OK (some games don't have weapons).
		// NOTE: we don't use GetComponentInChildren here because that
		// would return the MainCamera (on this transform)
		Camera weaponCam = null;
		foreach (Transform t in Transform)
		{
			weaponCam = (Camera)t.GetComponent(typeof(Camera));
			if (weaponCam != null)
			{
				weaponCam.transform.localPosition = Vector3.zero;
				weaponCam.transform.localEulerAngles = Vector3.zero;
				weaponCam.clearFlags = CameraClearFlags.Depth;
				weaponCam.cullingMask = (1 << vp_Layer.Weapon);	// only render the weapon
				weaponCam.depth = 1;
				weaponCam.farClipPlane = 100;
				weaponCam.nearClipPlane = 0.01f;
				weaponCam.fieldOfView = 60;
				break;
			}
		}

		// create springs for camera motion

		// --- primary position spring ---
		// this is used for all sorts of positional force acting on the camera
		m_PositionSpring = new vp_Spring(Transform, vp_Spring.UpdateMode.Position, false);
		m_PositionSpring.MinVelocity = 0.0f;
		m_PositionSpring.RestState = PositionOffset;

		// --- secondary position spring ---
		// this is mainly intended for positional force from recoil, stomping and explosions
		m_PositionSpring2 = new vp_Spring(Transform, vp_Spring.UpdateMode.PositionAdditiveLocal, false);
		m_PositionSpring2.MinVelocity = 0.0f;

		// --- rotation spring ---
		// this is used for all sorts of angular force acting on the camera
		m_RotationSpring = new vp_Spring(Transform, vp_Spring.UpdateMode.RotationAdditiveLocal, false);
		m_RotationSpring.MinVelocity = 0.0f;

#if UNITY_EDITOR
#if UNITY_5_3 || UNITY_5_4 || UNITY_5_5 || UNITY_5_6 || UNITY_2017_1
        if (DisableVRModeOnStartup && UnityEngine.VR.VRSettings.enabled) {
            UnityEngine.VR.VRSettings.enabled = false;
        }
#else
        if (DisableVRModeOnStartup && UnityEngine.XR.XRSettings.enabled) {
            UnityEngine.XR.XRSettings.enabled = false;
        }
#endif
#endif

    }


    /// <summary>
    /// 
    /// </summary>
    protected override void OnEnable()
	{
		base.OnEnable();
		vp_TargetEvent<float>.Register(m_Root, "CameraBombShake", OnMessage_CameraBombShake);
		vp_TargetEvent<float>.Register(m_Root, "CameraGroundStomp", OnMessage_CameraGroundStomp);
	}


	/// <summary>
	/// 
	/// </summary>
	protected override void OnDisable()
	{
		base.OnDisable();
		vp_TargetEvent<float>.Unregister(m_Root, "CameraBombShake", OnMessage_CameraBombShake);
		vp_TargetEvent<float>.Unregister(m_Root, "CameraGroundStomp", OnMessage_CameraGroundStomp);
	}


	/// <summary>
	/// in 'Start' we do things that need to be run once at the
	/// beginning, but potentially depend on all other scripts
	/// first having run their 'Awake' calls.
	/// NOTE: don't do anything here that depends on activity
	/// in other 'Start' calls
	/// </summary>
	protected override void Start()
	{

		base.Start();

		Refresh();

		// snap the camera to its start values when first activated
		SnapSprings();
		SnapZoom();

	}

	
	/// <summary>
	/// in 'Init' we do things that must be run once at the
	/// beginning, but only after all other components have
	/// run their 'Start' calls. this method is called once
	/// by the vp_Component base class in its first 'Update'
	/// </summary>
	protected override void Init()
	{

		base.Init();
		
	}


	/// <summary>
	/// 
	/// </summary>
	protected override void Update()
	{

		base.Update();

		if (Time.timeScale == 0.0f)
		    return;

		UpdateInput();
		
	}


	/// <summary>
	/// 
	/// </summary>
	protected override void FixedUpdate()
	{

		base.FixedUpdate();

		if (Time.timeScale == 0.0f)
			return;

		UpdateZoom();

		UpdateSwaying();

		UpdateBob();

		UpdateEarthQuake();

		UpdateShakes();

		UpdateSprings();

	}


	/// <summary>
	/// actual rotation of the player model and camera is performed in
	/// LateUpdate, since by then all game logic should be finished
	/// </summary>
	protected override void LateUpdate()
	{

		base.LateUpdate();

		if (Time.timeScale == 0.0f)
			return;

		// fetch the FPSController's SmoothPosition. this reduces jitter
		// by moving the camera at arbitrary update intervals while
		// controller and springs move at the fixed update interval
		m_Transform.position = FPController.SmoothPosition;

		// apply current spring offsets
		if (Player.IsFirstPerson.Get())
			m_Transform.localPosition += (m_PositionSpring.State + m_PositionSpring2.State);
		else
			m_Transform.localPosition +=	(m_PositionSpring.State +
											(Vector3.Scale(m_PositionSpring2.State, Vector3.up)));	// don't shake camera sideways in third person

		// prevent camera from intersecting objects
		TryCameraCollision();

		// rotate the parent gameobject (i.e. player model)
		// NOTE: this rotation does not pitch the player model, it only applies yaw
		Quaternion xQuaternion = Quaternion.AngleAxis(m_Yaw, Vector3.up);
		Quaternion yQuaternion = Quaternion.AngleAxis(0, Vector3.left);
		Parent.rotation =
			vp_MathUtility.NaNSafeQuaternion((xQuaternion * yQuaternion), Parent.rotation);

		// pitch and yaw the camera
		yQuaternion = Quaternion.AngleAxis(-m_Pitch, Vector3.left);
		Transform.rotation =
			vp_MathUtility.NaNSafeQuaternion((xQuaternion * yQuaternion), Transform.rotation);

		// roll the camera
		Transform.localEulerAngles +=
			vp_MathUtility.NaNSafeVector3(Vector3.forward * m_RotationSpring.State.z);

		// third person
		Update3rdPerson();

	}


	/// <summary>
	/// 
	/// </summary>
	void Update3rdPerson()
	{

		if (Position3rdPersonOffset == Vector3.zero)	// this system is disabled by default
			return;

		if (PositionOnDeath != Vector3.zero)
		{
			Transform.position = PositionOnDeath;
			if (FirstRigidBody != null)
				Transform.LookAt(FirstRigidBody.transform.position + Vector3.up);
			else
				Transform.LookAt(Root.position + Vector3.up);
			return;
		}

		if (Player.IsFirstPerson.Get())
		{
			m_Final3rdPersonCameraOffset = Vector3.zero;
			m_Current3rdPersonBlend = 0.0f;
			LookPoint = GetLookPoint();
			return;
		}

		m_Current3rdPersonBlend = Mathf.Lerp(m_Current3rdPersonBlend, 1.0f, Time.deltaTime);

		m_Final3rdPersonCameraOffset = Transform.position;
		
		// brute force way of preventing camera to clip the player's head
		if (Transform.localPosition.z > -0.2f)
		{
			Transform.localPosition = new Vector3(
				Transform.localPosition.x,
				Transform.localPosition.y,
				-0.2f
				);
		}

		// apply 3rd person offset
		Vector3 offset = Transform.position;
		offset += m_Transform.right * Position3rdPersonOffset.x;
		offset += m_Transform.up * Position3rdPersonOffset.y;
		offset += m_Transform.forward * Position3rdPersonOffset.z;
		Transform.position = Vector3.Lerp(Transform.position, offset, m_Current3rdPersonBlend);

		m_Final3rdPersonCameraOffset -= Transform.position;

		TryCameraCollision();

		LookPoint = GetLookPoint();

	}


	/// <summary>
	/// prevents the camera from intersecting other objects by
	/// raycasting from the controller to the camera and blocking
	/// the camera on the first object hit
	/// </summary>
	public virtual void TryCameraCollision()
	{

		if (!HasCollision)
			return;

		// start position is the center of the character controller
		// and height of the camera PositionOffset. this will detect
		// objects between the camera and controller even if the
		// camera PositionOffset is far from the controller

		m_CameraCollisionStartPos = FPController.Transform.TransformPoint(0, PositionOffset.y, 0)
			- (m_Player.IsFirstPerson.Get() ? Vector3.zero : (FPController.Transform.position - FPController.SmoothPosition));	// this alleviates stuttering to some extent in third person
		
		// end position is the current camera position plus we'll move it
		// back the distance of our Controller.radius in order to reduce
		// camera clipping issues very close to walls
		// TIP: for solving such issues, you can also try reducing the
		// main camera's near clipping plane 
		m_CameraCollisionEndPos = Transform.position + (Transform.position - m_CameraCollisionStartPos).normalized * FPController.CharacterController.radius;
		m_CollisionVector = Vector3.zero;
		if (Physics.Linecast(m_CameraCollisionStartPos, m_CameraCollisionEndPos, out m_CameraHit, vp_Layer.Mask.ExternalBlockers))
		{
			if (!m_CameraHit.collider.isTrigger)
			{
				Transform.position = m_CameraHit.point - (m_CameraHit.point - m_CameraCollisionStartPos).normalized * FPController.CharacterController.radius;
				m_CollisionVector = (m_CameraHit.point - m_CameraCollisionEndPos);
			}
		}

#if UNITY_EDITOR
		// draw a camera intersection debug line in the scene view. this is
		// enabled by the vp_FPCameraEditor class when the camera position
		// spring foldout is open
		if (m_DrawCameraCollisionDebugLine)
			Debug.DrawLine(m_CameraCollisionStartPos, m_CameraCollisionEndPos, (m_CameraHit.collider == null) ? Color.yellow : Color.red);
#endif

		// also, prevent the camera from ever going below the player's
		// feet (not even when up in the air)
		if (Transform.localPosition.y < PositionGroundLimit)
			Transform.localPosition = new Vector3(Transform.localPosition.x,
											PositionGroundLimit, Transform.localPosition.z);

	}


	/// <summary>
	/// pushes the camera position spring along the 'force' vector
	/// for one frame. For external use.
	/// </summary>
	public virtual void AddForce(Vector3 force)
	{
		m_PositionSpring.AddForce(force);
	}


	/// <summary>
	/// pushes the camera position spring along the 'force' vector
	/// for one frame. For external use.
	/// </summary>
	public virtual void AddForce(float x, float y, float z)
	{
		AddForce(new Vector3(x, y, z));
	}


	/// <summary>
	/// pushes the 2nd camera position spring along the 'force'
	/// vector for one frame. for external use.
	/// </summary>
	public virtual void AddForce2(Vector3 force)
	{
		m_PositionSpring2.AddForce(force);
	}

	/// <summary>
	/// pushes the 2nd camera position spring along the 'force'
	/// vector for one frame. for external use.
	/// </summary>
	public void AddForce2(float x, float y, float z)
	{
		AddForce2(new Vector3(x, y, z));
	}


	/// <summary>
	/// twists the camera around its z vector for one frame
	/// </summary>
	public virtual void AddRollForce(float force)
	{
		m_RotationSpring.AddForce(Vector3.forward * force);
	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void UpdateInput()
	{

		if (Player.Dead.Active)
			return;

		if (Player.InputSmoothLook.Get() == Vector2.zero)
			return;

		// modify pitch and yaw with mouselook
		m_Yaw += Player.InputSmoothLook.Get().x;
		m_Pitch += Player.InputSmoothLook.Get().y;

		// clamp angles
		m_Yaw = m_Yaw < -360.0f ? m_Yaw += 360.0f : m_Yaw;
		m_Yaw = m_Yaw > 360.0f ? m_Yaw -= 360.0f : m_Yaw;
		m_Yaw = Mathf.Clamp(m_Yaw, RotationYawLimit.x, RotationYawLimit.y);
		m_Pitch = m_Pitch < -360.0f ? m_Pitch += 360.0f : m_Pitch;
		m_Pitch = m_Pitch > 360.0f ? m_Pitch -= 360.0f : m_Pitch;
		m_Pitch = Mathf.Clamp(m_Pitch, -RotationPitchLimit.x, -RotationPitchLimit.y);
		
	}
	
	
	/// <summary>
	/// interpolates to the target FOV value
	/// </summary>
	protected virtual void UpdateZoom()
	{

		if (m_FinalZoomTime <= Time.time)
			return;

		RenderingZoomDamping = Mathf.Max(RenderingZoomDamping, 0.01f);
		float zoom = 1.0f - ((m_FinalZoomTime - Time.time) / RenderingZoomDamping);
		Camera.fieldOfView = Mathf.SmoothStep(Camera.fieldOfView, RenderingFieldOfView + ZoomOffset, zoom);

	}


	/// <summary>
	/// 
	/// </summary>
	public void RefreshZoom()
	{
		float zoom = 1.0f - ((m_FinalZoomTime - Time.time) / RenderingZoomDamping);
		Camera.fieldOfView = Mathf.SmoothStep(Camera.fieldOfView, RenderingFieldOfView + ZoomOffset, zoom);
	}


	/// <summary>
	/// interpolates to the target FOV using 'RenderingZoomDamping'
	/// as interval
	/// </summary>
	public virtual void Zoom()
	{

		m_FinalZoomTime = Time.time + RenderingZoomDamping;

	}


	/// <summary>
	/// instantly sets camera to the target FOV
	/// </summary>
	public virtual void SnapZoom()
	{

		Camera.fieldOfView = RenderingFieldOfView + ZoomOffset;

	}

	
	/// <summary>
	/// updates the procedural shaking of the camera.
	/// NOTE: x and y shakes are applied to the actual controls.
	/// if you increase the shakes, the result will be a drunken
	/// / sick / drugged movement experience. this can also be used
	/// for things like sniper breathing since it affects aiming
	/// </summary>
	protected virtual void UpdateShakes()
	{

		if (MuteShakes)
			return;

		// apply camera shakes
		if (ShakeSpeed != 0.0f)
		{
			m_Yaw -= m_Shake.y;			// subtract shake from last frame or camera will drift
			m_Pitch -= m_Shake.x;
			m_Shake = Vector3.Scale(vp_SmoothRandom.GetVector3Centered(ShakeSpeed), ShakeAmplitude);
			m_Yaw += m_Shake.y;			// apply new shake
			m_Pitch += m_Shake.x;
			m_RotationSpring.AddForce(Vector3.forward * m_Shake.z * Time.timeScale);
		}
	
	}


	/// <summary>
	/// speed should be the magnitude speed of the character
	/// controller. if controller has no ground contact, '0.0f'
	/// should be passed and the bob will fade to a halt
	/// </summary>
	protected virtual void UpdateBob()
	{

		if (MuteBob)
			return;

		if (BobAmplitude == Vector4.zero || BobRate == Vector4.zero)
			return;

		if (!Player.IsFirstPerson.Get())
			return;

		m_BobSpeed = ((BobRequireGroundContact && !FPController.Grounded) ? 0.0f : FPController.CharacterController.velocity.sqrMagnitude);

		// scale and limit input velocity
		m_BobSpeed = Mathf.Min(m_BobSpeed * BobInputVelocityScale, BobMaxInputVelocity);

		// reduce number of decimals to avoid floating point imprecision bugs
		m_BobSpeed = Mathf.Round(m_BobSpeed * 1000.0f) / 1000.0f;

		// if speed is zero, this means we should just fade out the last stored
		// speed value. NOTE: it's important to clamp it to the current max input
		// velocity since the preset may have changed since last bob!
		if (m_BobSpeed == 0)
			m_BobSpeed = Mathf.Min((m_LastBobSpeed * 0.93f), BobMaxInputVelocity);

		m_CurrentBobAmp.y = (m_BobSpeed * (BobAmplitude.y * -0.0001f));
		m_CurrentBobVal.y = (Mathf.Cos(Time.time * (BobRate.y * 10.0f))) * m_CurrentBobAmp.y;
		m_CurrentBobVal.y = vp_MathUtility.SnapToZero(m_CurrentBobVal.y, 0.0003f);

		m_CurrentBobAmp.x = (m_BobSpeed * (BobAmplitude.x * 0.0001f));
		m_CurrentBobVal.x = (Mathf.Cos(Time.time * (BobRate.x * 10.0f))) * m_CurrentBobAmp.x;
		m_CurrentBobVal.x = vp_MathUtility.SnapToZero(m_CurrentBobVal.x, 0.0003f);

		m_CurrentBobAmp.z = (m_BobSpeed * (BobAmplitude.z * 0.0001f));
		m_CurrentBobVal.z = (Mathf.Cos(Time.time * (BobRate.z * 10.0f))) * m_CurrentBobAmp.z;
		m_CurrentBobVal.z = vp_MathUtility.SnapToZero(m_CurrentBobVal.z, 0.0003f);

		m_CurrentBobAmp.w = (m_BobSpeed * (BobAmplitude.w * 0.0001f));
		m_CurrentBobVal.w = (Mathf.Cos(Time.time * (BobRate.w * 10.0f))) * m_CurrentBobAmp.w;
		m_CurrentBobVal.w = vp_MathUtility.SnapToZero(m_CurrentBobVal.w, 0.0003f);

		m_PositionSpring.AddForce((Vector3)m_CurrentBobVal * Time.timeScale);

		AddRollForce(m_CurrentBobVal.w * Time.timeScale);

		m_LastBobSpeed = m_BobSpeed;

		DetectBobStep(m_BobSpeed, m_CurrentBobVal.y);
		
	}


	/// <summary>
	/// the bob step callback is triggered when the vertical
	/// camera bob reaches its bottom value (provided that the
	/// speed is higher than the bob step threshold). this can
	/// be used for various footstep sounds and behaviors.
	/// </summary>
	protected virtual void DetectBobStep(float speed, float yBob)
	{

		if (BobStepCallback == null)
			return;

		if (speed < BobStepThreshold)
			return;

		if (yBob > 0)
			return;

		if ((m_LastYBob < yBob) && !m_BobWasElevating)
		{
			//Debug.Log(yBob + " <------------------");
			BobStepCallback();
		}

		m_BobWasElevating = (m_LastYBob < yBob);
		m_LastYBob = yBob;

	}


	/// <summary>
	/// applies swaying forces to the camera in response to user
	/// input and character controller motion.
	/// </summary>
	protected virtual void UpdateSwaying()
	{

		if (MuteRoll)
			return;

		Vector3 localVelocity = Transform.InverseTransformDirection(FPController.CharacterController.velocity * 0.016f) * Time.timeScale;
		AddRollForce(localVelocity.x * RotationStrafeRoll);

	}


	/// <summary>
	/// shakes the camera according to any global earthquake
	/// detectable via the event handler
	/// </summary>
	protected virtual void UpdateEarthQuake()
	{

		if (MuteEarthquakes)
			return;

		if (Player == null)
			return;

		if (!Player.CameraEarthQuake.Active)
			return;
				
		// apply horizontal move to the camera spring.
		// NOTE: this will only shake the camera, though it will give
		// the appearance of pushing the player around.

		// the vertical move has a 30% chance of occuring each frame.
		// when it does, it alternates between positive and negative.
		// this produces sharp shakes with nice spring smoothness inbetween.
		if (m_PositionSpring.State.y >= m_PositionSpring.RestState.y)
		{
			Vector3 earthQuakeForce = Player.CameraEarthQuakeForce.Get();
			earthQuakeForce.y = -earthQuakeForce.y;
			Player.CameraEarthQuakeForce.Set(earthQuakeForce);
		}

		m_PositionSpring.AddForce(Player.CameraEarthQuakeForce.Get() * PositionEarthQuakeFactor);

		// apply earthquake roll force on the camera rotation spring
		m_RotationSpring.AddForce(Vector3.forward * (-Player.CameraEarthQuakeForce.Get().x * 2) * RotationEarthQuakeFactor);

	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void UpdateSprings()
	{
	
		m_PositionSpring.FixedUpdate();
		m_PositionSpring2.FixedUpdate();
		m_RotationSpring.FixedUpdate();
	
	}

	
	/// <summary>
	/// shakes the camera according to the defined bomb forces
	/// </summary>
	public virtual void DoBomb(Vector3 positionForce, float minRollForce, float maxRollForce)
	{

		if (MuteBombShakes)
			return;

		AddForce2(positionForce);

		float roll = Random.Range(minRollForce, maxRollForce);
		if (Random.value > 0.5f)
			roll = -roll;
		AddRollForce(roll);


	}


	/// <summary>
	/// this method is called to reset various camera settings,
	/// typically after creating or loading a camera
	/// </summary>
	public override void Refresh()
	{

		if (!Application.isPlaying)
			return;
		if (m_PositionSpring != null)
		{
			m_PositionSpring.Stiffness =
				new Vector3(PositionSpringStiffness, PositionSpringStiffness, PositionSpringStiffness);
			m_PositionSpring.Damping = Vector3.one -
				new Vector3(PositionSpringDamping, PositionSpringDamping, PositionSpringDamping);

			m_PositionSpring.MinState.y = PositionGroundLimit;
			m_PositionSpring.RestState = PositionOffset;

		}

		if (m_PositionSpring2 != null)
		{
			m_PositionSpring2.Stiffness =
			new Vector3(PositionSpring2Stiffness, PositionSpring2Stiffness, PositionSpring2Stiffness);
			m_PositionSpring2.Damping = Vector3.one -
				new Vector3(PositionSpring2Damping, PositionSpring2Damping, PositionSpring2Damping);

			m_PositionSpring2.MinState.y = (-PositionOffset.y) + PositionGroundLimit;
			// we don't force a position offset for position spring 2
		}

		if (m_RotationSpring != null)
		{
			m_RotationSpring.Stiffness =
			new Vector3(RotationSpringStiffness, RotationSpringStiffness, RotationSpringStiffness);
			m_RotationSpring.Damping = Vector3.one -
				new Vector3(RotationSpringDamping, RotationSpringDamping, RotationSpringDamping);
		}

		Zoom();

	}


	/// <summary>
	/// resets all the springs to their default positions, i.e.
	/// for when loading a new camera or switching a weapon
	/// </summary>
	public virtual void SnapSprings()
	{

		if (m_PositionSpring != null)
		{
			m_PositionSpring.RestState = PositionOffset;
			m_PositionSpring.State = PositionOffset;
			m_PositionSpring.Stop(true);
		}

		if (m_PositionSpring2 != null)
		{
			m_PositionSpring2.RestState = Vector3.zero;
			m_PositionSpring2.State = Vector3.zero;
			m_PositionSpring2.Stop(true);
		}

		if (m_RotationSpring != null)
		{
			m_RotationSpring.RestState = Vector3.zero;
			m_RotationSpring.State = Vector3.zero;
			m_RotationSpring.Stop(true);
		}

	}



	/// <summary>
	/// stops all the springs
	/// </summary>
	public virtual void StopSprings()
	{

		if (m_PositionSpring != null)
			m_PositionSpring.Stop(true);

		if (m_PositionSpring2 != null)
			m_PositionSpring2.Stop(true);

		if (m_RotationSpring != null)
			m_RotationSpring.Stop(true);

		m_BobSpeed = 0.0f;
		m_LastBobSpeed = 0.0f;

	}


	/// <summary>
	/// stops the springs and zoom
	/// </summary>
	public virtual void Stop()
	{
		SnapSprings();
		SnapZoom();
		Refresh();
	}


	/// <summary>
	/// sets camera rotation and optionally snaps springs and zoom to a halt
	/// </summary>
	public virtual void SetRotation(Vector2 eulerAngles, bool stopZoomAndSprings)	// TEMP: don't use optional params: keep below overrides for mobile add-on compatibility
	{

		Angle = eulerAngles;

		if (stopZoomAndSprings)
			Stop();

	}


	/// <summary>
	/// sets camera rotation and snaps springs and zoom to a halt
	/// </summary>
	public virtual void SetRotation(Vector2 eulerAngles)
	{

		Angle = eulerAngles;
		Stop();

	}


	/// <summary>
	/// sets camera rotation and optionally snaps springs and zoom to a halt
	/// NOTE: provided only for mobile add-on backwards compatibility
	/// </summary>
	public virtual void SetRotation(Vector2 eulerAngles, bool stopZoomAndSprings, bool obsolete)
	{

		SetRotation(eulerAngles, stopZoomAndSprings);

	}



	/// <summary>
	/// returns the world point that the player is looking at
	/// </summary>
	public Vector3 GetLookPoint()
	{

		// 3RD PERSON and looking at a solid object
		if (!Player.IsFirstPerson.Get())
		{

			// raycast to see if we hit an external blocker
			if (Physics.Linecast(
				Transform.position,	// aim source: position of camera taking 3rd person camera pos into account
				((Transform.position) + (Transform.forward * 1000)),		// max aim range: 1000 meters
				out m_LookPointHit,
				vp_Layer.Mask.ExternalBlockers) && !m_LookPointHit.collider.isTrigger &&	// only aim at non-local player solids
				(Root.InverseTransformPoint(m_LookPointHit.point).z > 0.0f)	// don't aim at stuff between camera & local player
				)
			{
				return m_LookPointHit.point;
			}

		}

		// 1ST PERSON or 3rd person and looking into empty space
		return ((Transform.position) + (Transform.forward * 1000));

	}



	/// <summary>
	/// returns the world transform that the player is looking at. if 'layerMask'
	/// is not provided, then 'vp_Layer.Mask.ExternalBlockers' will be used. 
	/// </summary>
	public Transform GetLookTransform(float maxRange, int layerMask = -1)
	{

		if (layerMask == -1)
			layerMask = vp_Layer.Mask.ExternalBlockers;

		// raycast to see if we hit an external blocker
		if (Physics.Linecast(
			Transform.position,	// aim source: position of camera taking 3rd person camera pos into account
			((Transform.position) + (Transform.forward * maxRange)),		// max aim range
			out m_LookPointHit,
			layerMask) && !m_LookPointHit.collider.isTrigger &&	// only aim at non-local player solids
			(Root.InverseTransformPoint(m_LookPointHit.point).z > 0.0f)	// don't aim at stuff between camera & local player
			)
			return m_LookPointHit.transform;

		// looking at nothing
		return null;

	}
	

	/// <summary>
	/// returns the contact point on the surface of the first physical
	/// object that the camera looks at
	/// </summary>
	public virtual Vector3 OnValue_LookPoint
	{
		get
		{
			return LookPoint;
		}
	}


	/// <summary>
	/// returns the direction between the camera position and the
	/// look point. NOTE: _not_ the direction between the player
	/// model's head and the look point
	/// </summary>
	protected virtual Vector3 OnValue_CameraLookDirection
	{
		get
		{
			return (Player.LookPoint.Get() - Transform.position).normalized;
		}
	}


	/// <summary>
	/// applies various forces to the camera and weapon springs
	/// in response to falling impact.
	/// </summary>
	protected virtual void OnMessage_FallImpact(float impact)
	{

		if (MuteFallImpacts)
			return;

		impact = (float)Mathf.Abs((float)impact * 55.0f);
		// ('55' is for preset backwards compatibility)

		float posImpact = (float)impact * PositionKneeling;
		float rotImpact = (float)impact * RotationKneeling;

		// smooth step the impacts to make the springs react more subtly
		// from short falls, and more aggressively from longer falls
		posImpact = Mathf.SmoothStep(0, 1, posImpact);
		rotImpact = Mathf.SmoothStep(0, 1, rotImpact);
		rotImpact = Mathf.SmoothStep(0, 1, rotImpact);

		// apply impact to camera position spring
		if (m_PositionSpring != null)
			m_PositionSpring.AddSoftForce(Vector3.down * posImpact, PositionKneelingSoftness);

		// apply impact to camera rotation spring
		if (m_RotationSpring != null)
		{
			float roll = Random.value > 0.5f ? (rotImpact * 2) : -(rotImpact * 2);
			m_RotationSpring.AddSoftForce(Vector3.forward * roll, RotationKneelingSoftness);
		}

	}


	/// <summary>
	/// applies a force to the camera rotation spring (intended
	/// for when the controller bumps into objects above it)
	/// </summary>
	protected virtual void OnMessage_HeadImpact(float impact)
	{

		if (MuteHeadImpacts)
			return;

		if ((m_RotationSpring != null) && (Mathf.Abs(m_RotationSpring.State.z) < 30.0f))
		{

			// apply impact to camera rotation spring
			m_RotationSpring.AddForce(Vector3.forward * (impact * 20.0f) * Time.timeScale);

		}

	}


	/// <summary>
	/// makes the ground shake as if a large dinosaur or mech is
	/// approaching. great for bosses!
	/// </summary>
	protected virtual void OnMessage_CameraGroundStomp(float impact)
	{

		if (MuteGroundStomps)
			return;

		AddForce2(new Vector3(0.0f, -1.0f, 0.0f) * impact);

	}


	/// <summary>
	/// makes the ground shake as if a bomb has gone off nearby
	/// </summary>
	protected virtual void OnMessage_CameraBombShake(float impact)
	{

		DoBomb((new Vector3(1.0f, -10.0f, 1.0f) * impact),
			1,
			2);

	}

	
	/// <summary>
	/// this callback is triggered right after the 'Zoom' activity
	/// has been approved for activation. it prevents the player
	/// from running while zooming
	/// </summary>
	protected virtual void OnStart_Zoom()
	{

		if (Player == null)
			return;

		Player.Run.Stop();

	}


	/// <summary>
	/// adds a condition (a rule set) that must be met for the
	/// event handler 'Run' activity to successfully activate.
	/// NOTE: other scripts may have added conditions to this
	/// activity aswell
	/// </summary>
	protected virtual bool CanStart_Run()
	{

		if (Player == null)
			return true;

		// can't start running while zooming
		if (Player.Zoom.Active)
			return false;

		return true;

	}


	/// <summary>
	/// gets or sets the rotation of the camera
	/// </summary>
	protected virtual Vector2 OnValue_Rotation
	{
		get
		{
			return Angle;
		}
		set
		{
			Angle = value;
		}
	}


	/// <summary>
	/// snaps the camera springs and zoom to a halt
	/// </summary>
	protected virtual void OnMessage_Stop()
	{
		Stop();
	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void OnStart_Dead()
	{

		if (Player.IsFirstPerson.Get())
			return;
		
		PositionOnDeath = Transform.position - m_Final3rdPersonCameraOffset;

	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void OnStop_Dead()
	{

		if (Player.IsFirstPerson.Get())
			return;

		PositionOnDeath = Vector3.zero;
		m_Current3rdPersonBlend = 0.0f;

	}


	/// <summary>
	/// makes the playereventhandler 'IsLocal' value event always
	/// return true, since if this player has a first person camera
	/// it's very reliably a local player. otherwise, it defaults to
	/// false
	/// </summary>
	protected virtual bool OnValue_IsLocal
	{
		get
		{
			return true;
		}
	}


	/// <summary>
	/// toggles camera view between 1st and 3rd person mode
	/// </summary>
	protected virtual void OnMessage_CameraToggle3rdPerson()
	{

		m_Player.IsFirstPerson.Set(!m_Player.IsFirstPerson.Get());

	}

}

	