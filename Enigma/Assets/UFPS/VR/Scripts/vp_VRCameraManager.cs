/////////////////////////////////////////////////////////////////////////////////
//
//	vp_VRCameraManager.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	this component keeps an Oculus VR camera rig attached to the UFPS
//					player controller and enforces headset rotation onto the player
//					pitch and yaw at all times. it also forwards camera shakes (if
//					allowed) and handles input for smooth or snap yaw rotation
//
//					USAGE:
//						1) add a standard 'OVRCameraRig' prefab to the scene.
//							(IMPORTANT: _NOT_ an 'OVRPlayerController')
//						2) add this script to its own gameobject in the scene root
//						3) go to you main vp_FPCamera component's 'Rendering' foldout
//							and set 'Disable VR mode on startup' to ON (checked).
//							(without it, mouse and gamepad will not work in the editor)
//
//					IMPORTANT: when this script wakes up, it will temporarily disable the
//						setting found under:
//						'Edit > Project Settings > Player > Other Settings > Virtual Reality Supported'.
//						however, this setting should always be turned ON in Project Settings
//						by default, or Virtual Reality will not work at all (!). as soon as
//						a vp_VRCameraManager component is active and enabled in the scene,
//						the setting will be turned back on (this is to allow for seamless
//						switching between VR and Destop play in the editor).
//
//					NOTES:
//						- if using a gamepad, go to the Unity main menu > 'UFPS > Input Manager'
//							and make sure that it is set to 'Joystick'
//						- if using keyboard and mouse, go to the Unity main menu > 'UFPS > Input
//							Manager' and make sure that it is set to 'Keyboard and Mouse' (otherwise
//							the snap rotate feature will behave very erratically)
//						- please note that after pressing play in the editor, Unity will not receive
//							proper button input unless the 'Game' window has focus! after pressing
//							play, you must click once in the 'Game' window before you put on your
//							headset, or input won't work. this is sometimes especially confusing
//							when using gamepad (!)
//						- note that 'SnapRotate' uses the input vector of the mouse or left
//							gamepad stick (as opposed to a button). if using it with a mouse
//							and keyboard, make sure to set the rotate interval 
//						- in 'OnEnable', this component strips and modifies the UFPS player
//							hierarchy so it works with its own various logics, and restores
//							everything in 'OnDisable'. this allows vp_VRCameraManager to be
//							toggled on and off at runtime (and the OVRCameraRig with it)
//							allowing you to seamlessly swap between the VR and desktop modes.
//						- this script does not work with - and will disable - the full animated
//							player body, the vp_FPCamera's Unity Camera component and
//							WeaponCamera, along with the vp_SimpleHUD
//						- it will enable any vp_PlayerFootFXHandler found childed directly
//							to the controller
//						- it will disable all AudioListeners and enable the one found under
//							the OVRCameraRig
//						- it will also hide vp_SimpleCrosshair and disable pitch and / or yaw
//							of vp_FPInput (depending on whether 'SnapRotate' is on or off)
//						
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System.Collections;

public class vp_VRCameraManager : MonoBehaviour
{

	public Transform m_CenterEyeAnchor = null;  // the transform located right between the VR eye cameras (i.e. the Oculus 'CenterEyeAnchor'). no need to set this (will be found automatically)

	////////////// 'Procedural Motion' section ////////////////

	[System.Serializable]
	public class ProceduralMotionSection
	{
		// these properties will disable the corresponding features of the vp_FPCamera
		public bool AllowInputBob = true;
		public bool AllowInputRoll = true;
		public bool AllowShakycam = true;
		public bool AllowFallImpacts = true;
		public bool AllowBombShakes = true;
		public bool AllowGroundStomps = true;
		public bool AllowEarthquakes = true;
		public bool AllowControllerSliding = true;
		public bool DisallowAll = false;

#if UNITY_EDITOR
		[vp_HelpBox("", UnityEditor.MessageType.None, null, null, false, vp_PropertyDrawerUtility.Space.Nothing)]
		public float proceduralHelp;
#endif
	}
	public ProceduralMotionSection ProceduralMotion = new ProceduralMotionSection();

	protected float m_ControllerForceDampingBak = 0.0f;
	protected float m_ControllerSlopeSlidinessBak = 0.0f;

	////////////// 'Snap Rotate' section ////////////////

	[System.Serializable]
	public class SnapRotationSection
	{

		public bool SnapRotate = false;				// should the player rotate by fixed degree steps?
		[Range(22.5f, 90)]
		public float StepDegrees = 45.0f;			// step angle in degrees for snap rotation
		public float MinDelay = 0.5f;				// minimum time interval between step rotations

#if UNITY_EDITOR
		[vp_HelpBox("", UnityEditor.MessageType.None, null, null, false, vp_PropertyDrawerUtility.Space.Nothing)]
		public float proceduralHelp;
#endif
	}
	public SnapRotationSection SnapRotation = new SnapRotationSection();


	protected float m_NextAllowedSnapRotateTime = 0.0f;
	protected float m_RotateInputLastFrame = 0.0f;
	protected bool m_SnapTurnLastFrame = true;
	protected Vector3 m_FPCameraOffsetBak = Vector3.zero;
	protected bool m_AppQuitting = false;

	private const float LEFT = -1.0f;
	private const float RIGHT = 1.0f;

#if UNITY_EDITOR
	[vp_HelpBox("• If 'CenterEyeAnchor' is not set, an object with that name will be auto-located in the scene.\n\n• 'Snap Rotate' works best with gamepad.\n\n• Please see the manual (and the detailed script comments of all 'vp_VR' scripts) for setup and usage info.", UnityEditor.MessageType.Info, null, null, false, vp_PropertyDrawerUtility.Space.Nothing)]
	public float helpbox;
#endif

	protected Transform CenterEyeAnchor
	{
		get
		{
			if ((m_CenterEyeAnchor == null) && !m_AppQuitting)
			{
				GameObject g = GameObject.Find("CenterEyeAnchor");
				if (g != null)
					m_CenterEyeAnchor = g.transform;
			}
			return m_CenterEyeAnchor;
		}
	}

	protected vp_FPCamera m_FPCamera = null;
	protected vp_FPCamera FPCamera
	{
		get
		{
			if (m_FPCamera == null)
				m_FPCamera = GameObject.FindObjectOfType<vp_FPCamera>();
			return m_FPCamera;
		}
	}

	protected vp_FPController m_FPController = null;
	protected vp_FPController FPController
	{
		get
		{
			if (m_FPController == null)
				m_FPController = GameObject.FindObjectOfType<vp_FPController>();
			return m_FPController;
		}
	}

	protected vp_FPInput m_FPInput = null;
	protected vp_FPInput FPInput
	{
		get
		{
			if (m_FPInput == null)
				m_FPInput = GameObject.FindObjectOfType<vp_FPInput>();
			return m_FPInput;
		}
	}

	protected Transform m_WeaponCamera = null;
	protected Transform WeaponCamera
	{
		get
		{
			if ((m_WeaponCamera == null) && (FPCamera != null))
				m_WeaponCamera = FPCamera.Transform.Find("WeaponCamera");
			return m_WeaponCamera;
		}
	}

	protected vp_PlayerFootFXHandler m_PlayerFootFXHandler = null;
	protected vp_PlayerFootFXHandler PlayerFootFXHandler
	{
		get
		{
			if ((m_PlayerFootFXHandler == null))
				m_PlayerFootFXHandler = FPPlayer.transform.root.GetComponentInChildren<vp_PlayerFootFXHandler>();
			return m_PlayerFootFXHandler;
		}
	}

	protected vp_WeaponHandler m_WeaponHandler = null;
	protected vp_WeaponHandler WeaponHandler
	{
		get
		{
			if (m_WeaponHandler == null)
				m_WeaponHandler = GameObject.FindObjectOfType<vp_WeaponHandler>();
			return m_WeaponHandler;
		}
	}

	protected vp_FPPlayerEventHandler m_FPPlayer = null;
	protected vp_FPPlayerEventHandler FPPlayer
	{
		get
		{
			if (m_FPPlayer == null)
				m_FPPlayer = GameObject.FindObjectOfType<vp_FPPlayerEventHandler>();
			return m_FPPlayer;
		}
	}

	protected vp_FPBodyAnimator m_BodyAnimator;
	protected vp_FPBodyAnimator BodyAnimator
	{
		get
		{
			if ((m_BodyAnimator == null) && (FPPlayer != null))
				m_BodyAnimator = FPPlayer.transform.root.GetComponentInChildren<vp_FPBodyAnimator>();
			return m_BodyAnimator;
		}
	}


	protected vp_PlayerFootFXHandler m_VRFootFXHandler = null;
	protected vp_PlayerFootFXHandler VRFootFXHandler
	{
		get
		{
			if ((m_VRFootFXHandler == null) && (FPPlayer != null))
				m_VRFootFXHandler = FPPlayer.GetComponent<vp_PlayerFootFXHandler>();
			return m_VRFootFXHandler;
		}
	}


	protected AudioListener m_AudioListener = null;
	protected AudioListener VRAudioListener
	{
		get
		{
			if (m_AudioListener == null)
				m_AudioListener = GetComponentInChildren<AudioListener>();
			return m_AudioListener;
		}
	}


	protected vp_SimpleCrosshair m_DesktopCrosshair = null;
	protected vp_SimpleCrosshair DesktopCrosshair
	{
		get
		{
			if (m_DesktopCrosshair == null)
				m_DesktopCrosshair = GameObject.FindObjectOfType<vp_SimpleCrosshair>();
			return m_DesktopCrosshair;
		}
	}


	protected vp_SimpleHUD m_DesktopHUD = null;
	protected vp_SimpleHUD DesktopHUD
	{
		get
		{
			if (m_DesktopHUD == null)
				m_DesktopHUD = GameObject.FindObjectOfType<vp_SimpleHUD>();
			return m_DesktopHUD;
		}
	}


	/// <summary>
	/// 
	/// </summary>
	void Awake()
	{

		// abort if the Unity version is too old
		string version = Application.unityVersion.Remove(5);	// get unity version string with just the 3 first numbers
		version = version.Replace(".", "");						// remove the dots
		int v = System.Int32.Parse(version);                    // convert the string to an int
		if (v < 534)											// see if Unity is too old
			StartCoroutine(ErrorDisableOnEndOfFrame("Error: Running UFPS in VR mode requires at least Unity version 5.3.4. Your version of Unity is: " + Application.unityVersion + "."));

	}


	/// <summary>
	/// 
	/// </summary>
	void Start()
	{

		if (FPCamera == null)
		{
			Debug.LogError("Error (" + this + ") This component requires a vp_FPCamera component to be present in the scene (disabling self).");
			ErrorDisable();
			return;
		}
		
		if (FPPlayer == null)
		{
			Debug.LogError("Error (" + this + ") This component requires a vp_FPPlayer component to be present in the scene (disabling self).");
			ErrorDisable();
			return;
		}

		if ((transform.root == FPPlayer.transform)
			|| (transform.root == CenterEyeAnchor.transform)
			)
		{
			Debug.LogError("Error (" + this + ") This component must not be part of the player controller OR the head tracker hierarchies (disabling self). TIP: Assign it to its own GameObject.");
			ErrorDisable();
			return;
		}

	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void OnEnable()
	{


		vp_Gameplay.IsVR = true;

#if UNITY_5_3 || UNITY_5_4 || UNITY_5_5 || UNITY_5_6 || UNITY_2017_1
        UnityEngine.VR.VRSettings.enabled = true;
#else
        UnityEngine.XR.XRSettings.enabled = true;
#endif

        TryEnsureSingleAudioListener();

		if (CenterEyeAnchor == null)
		{
			Debug.LogError("Error (" + this + ") 'CenterEyeAnchor' is not assigned. IMPORTANT: Make sure to drag an 'OVRCameraRig', into the scene from 'OVR/Prefabs', and 'CenterEyeAnchor' will be set automatically.");
			ErrorDisable();
			return;
		}

		StartCoroutine(ModifyPlayerHierarchy());  // wait until end of frame so weapon system has time to cache the weapon camera

		if (!vp_Utility.IsActive(CenterEyeAnchor.root.gameObject))
			vp_Utility.Activate(CenterEyeAnchor.root.gameObject);

		// rotate OVR camera rig according to FPPlayer when VR mode is enabled
		if(FPPlayer != null)
			CenterEyeAnchor.root.eulerAngles = (Vector3.up * FPPlayer.Rotation.Get().y);

	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void OnDisable()
	{

		vp_Gameplay.IsVR = false;

#if UNITY_5_3 || UNITY_5_4 || UNITY_5_5 || UNITY_5_6 || UNITY_2017_1
        UnityEngine.VR.VRSettings.enabled = false;
#else
        UnityEngine.XR.XRSettings.enabled = false;
#endif
        RestorePlayerHierarchy();

		if (!m_AppQuitting)
		{
			if (vp_Utility.IsActive(CenterEyeAnchor.root.gameObject))
				vp_Utility.Activate(CenterEyeAnchor.root.gameObject, false);
		}

	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void LateUpdate()
	{

		UpdateForcedValues();

		UpdatePosition();

		UpdateRotation();

		UpdateSnapRotation();

	}


	/// <summary>
	/// forces the vp_FPCamera to mute some of its procedural motion settings.
	/// </summary>
	protected virtual void UpdateForcedValues()
	{

		// prevent vp_FPInput from processing pitch, and yaw if snap rotate is on
		if (FPInput != null)
		{
			FPInput.MouseLookMutePitch = true;
			if (SnapRotation.SnapRotate)
				FPInput.MouseLookMuteYaw = true;
		}

		if (!ProceduralMotion.AllowControllerSliding)
		{
			FPController.PhysicsForceDamping = 1.0f;
			FPController.PhysicsSlopeSlidiness = 0.0f;
		}

		if (ProceduralMotion.DisallowAll)
		{
			ProceduralMotion.AllowInputBob = false;
			ProceduralMotion.AllowEarthquakes = false;
			ProceduralMotion.AllowShakycam = false;
			ProceduralMotion.AllowInputRoll = false;
			ProceduralMotion.AllowFallImpacts = false;
			ProceduralMotion.AllowBombShakes = false;
			ProceduralMotion.AllowGroundStomps = false;
			ProceduralMotion.AllowControllerSliding = false;
			FPController.PhysicsForceDamping = 1.0f;
			FPController.PhysicsSlopeSlidiness = 0.0f;
			return; // all camera motion will be ignored in 'UpdatePosition' anyway
		}

		FPCamera.MuteBob = !ProceduralMotion.AllowInputBob;
		FPCamera.MuteEarthquakes = !ProceduralMotion.AllowEarthquakes;
		FPCamera.MuteShakes = !ProceduralMotion.AllowShakycam;
		FPCamera.MuteRoll = !ProceduralMotion.AllowInputRoll;
		FPCamera.MuteFallImpacts = !ProceduralMotion.AllowFallImpacts;
		FPCamera.MuteBombShakes = !ProceduralMotion.AllowBombShakes;
		FPCamera.MuteGroundStomps = !ProceduralMotion.AllowGroundStomps;

	}


	/// <summary>
	/// keeps the headset rig attached to the UFPS player controller and adjusts
	/// its position for camera shakes (if allowed)
	/// </summary>
	protected virtual void UpdatePosition()
	{

		// prevent any FPCamera states from using horizontal position offset (for proper head tracking)
		FPCamera.PositionOffset = (Vector3.up * FPCamera.PositionOffset.y);

		// stick headset rig XZ pos to the FPPlayer's feet at all times (to make it follow the UFPS player
		// around horizontally)
		CenterEyeAnchor.root.position = FPController.SmoothPosition;

		// stick headset rig Y pos to the FPCamera Y pos at all times for the correct head height
			// NOTE: you may want to set 'FPCamera.PositionOffset.y' to the actual real world player
			// height, as reported by the user in the VR platform's settings (UFPS default is 1.75m).
		if (!ProceduralMotion.DisallowAll)
			CenterEyeAnchor.root.position += FPCamera.SpringState;   // NOTE: using 'FPCamera.transform.localPosition' here would result in bad head tracking
		else
			CenterEyeAnchor.root.position += (Vector3.up * FPCamera.PositionOffset.y);
		
		// stick FPCamera right back to the headset rig (or player would be able to walk away from
		// the controller and weapons)
		FPCamera.transform.position = CenterEyeAnchor.position;

	}


	/// <summary>
	/// handles smooth rotation (unless snap rotation is enabled) and forces
	/// headset yaw onto the camera pitch and controller yaw at all times
	/// </summary>
	protected virtual void UpdateRotation()
	{

		// when using regular smooth yaw, add yaw rotation directly to the headset
		// rig, causing the player controller to rotate in 'UpdateRotation'
		if (!SnapRotation.SnapRotate)
			CenterEyeAnchor.root.eulerAngles += (Vector3.up * FPPlayer.InputSmoothLook.Get().x);

		// apply headset Yaw to the FPController, and Pitch to the FPCamera
		// (overriding all other rotation inputs)
		FPPlayer.Rotation.Set(CenterEyeAnchor.eulerAngles);

	}


	/// <summary>
	/// analyzes gamepad or mouse input to trigger snap rotation (if enabled)
	/// </summary>
	protected virtual void UpdateSnapRotation()
	{

		HandleRuntimeSnapToggle();

		if (!SnapRotation.SnapRotate)
			return;

		// gamepads can only rotate again once the stick has returned to center.
		// 'MinDelay' is ignored if tapping quickly, but imposed when holding
		// stick to left/right
		if ((vp_Input.Instance.ControlType == 1)	// 'Joystick'
			&& (Mathf.Abs(Input.GetAxisRaw("Mouse X")) < 0.1f)
			&& (Mathf.Abs(m_RotateInputLastFrame) >= 0.1f))
			m_NextAllowedSnapRotateTime = Time.time;

		if (Time.time < m_NextAllowedSnapRotateTime)
			return;

		// mice can rotate ony when allowed by the rotate delay
		if (vp_Input.Instance.ControlType == 0)	// 'Mouse and Keyboard'
			m_NextAllowedSnapRotateTime = Time.time + SnapRotation.MinDelay;

		// snap rotate left or right depending on input vector
		if (Input.GetAxisRaw("Mouse X") < -0.1f)
			DoSnapRotate(LEFT);
		else if (Input.GetAxisRaw("Mouse X") > 0.1f)
			DoSnapRotate(RIGHT);

		// SNIPPET: to bind snap rotation to a button instead of an axis, add
		// the button names 'TurnLeft' and 'TurnRight' to the input manager and
		// do the following instead of the above:
			//if (vp_Input.GetButtonDown("TurnLeft"))
			//	DoSnapRotate(LEFT);
			//else if (vp_Input.GetButtonDown("TurnRight"))
			//	DoSnapRotate(RIGHT);

		// remember this frame's rotate input for the gamepad logic above
		m_RotateInputLastFrame = Input.GetAxisRaw("Mouse X");
		
	}


	/// <summary>
	/// rotates the whole player body and OVR camera rig a set number of degrees
	/// to the left or right in one frame, and sends a Unity Message 'OnCameraSnap'.
	/// 'dir' must be either '-1' (LEFT) or '1' (RIGHT)
	/// </summary>
	protected virtual void DoSnapRotate(float dir)
	{

		if ((dir != LEFT) && (dir != RIGHT))
			return;

		Transform t = CenterEyeAnchor.root;
		t.eulerAngles = new Vector3(t.eulerAngles.x, t.eulerAngles.y + (SnapRotation.StepDegrees * dir), t.eulerAngles.z);	// rotate OVR camera rig
		FPPlayer.Rotation.Set(new Vector2(FPPlayer.Rotation.Get().x, t.eulerAngles.y + (SnapRotation.StepDegrees * dir)));	// rotate UFPS player hierarchy
		m_NextAllowedSnapRotateTime = (Time.time + SnapRotation.MinDelay);    // enforce minimum snap rotate interval
		SendMessage("OnCameraSnap", SendMessageOptions.DontRequireReceiver); 	// notify external components
		if (VRFootFXHandler != null)	// try to play a footstep sound
			VRFootFXHandler.TryStep();

	}


	/// <summary>
	/// toggles vp_FPInput's ability to process mouse yaw when snap rotation is
	/// toggled at runtime (typically only in the editor during development)
	/// </summary>
	protected virtual void HandleRuntimeSnapToggle()
	{

		if (FPInput == null)
			return;

		if (SnapRotation.SnapRotate && !m_SnapTurnLastFrame)
			FPInput.MouseLookMuteYaw = true;
		else if (!SnapRotation.SnapRotate && m_SnapTurnLastFrame)
			FPInput.MouseLookMuteYaw = false;

		m_SnapTurnLastFrame = SnapRotation.SnapRotate;

	}


	/// <summary>
	/// strips and modifies the UFPS player hierarchy so it works with the various
	/// 'vp_VRCameraManager' logics. called from this script's 'OnEnable'
	/// </summary>
	protected virtual IEnumerator ModifyPlayerHierarchy()
	{

		yield return new WaitForEndOfFrame();

		if (m_AppQuitting)
			yield return null;

		// disable the Unity camera on the FPCamera gameobject because the
		// OVRCameraRig has its own cameras
		if ((FPCamera != null) && (FPCamera.Camera != null))
			FPCamera.Camera.enabled = false;

		if (FPController != null)
		{
			m_ControllerForceDampingBak = FPController.PhysicsForceDamping;
			m_ControllerSlopeSlidinessBak = FPController.PhysicsSlopeSlidiness;
		}

		// disable weapon camera because it does not apply / work in VR
		if (WeaponCamera != null)
		{
			vp_Utility.Activate(WeaponCamera.gameObject, false);
			FPCamera.SnapSprings();
			FPCamera.SnapZoom();
			if (WeaponHandler.CurrentWeapon != null)
				WeaponHandler.CurrentWeapon.ResetState();
			FPCamera.Refresh();
		}

		// disable local player body because it does not have advanced enough IK
		// for VR (at least not until UFPS 2)
		// NOTE: this will also disable any vp_PlayerFootFXHandler placed on the body object
		if (BodyAnimator != null)
			vp_Utility.Activate(BodyAnimator.gameObject, false);

		// enable the VR foot fx handler (if any - placed directly on the controller object)
		if (VRFootFXHandler != null)
			VRFootFXHandler.enabled = true;

		// disable all audio listeners on the player and enable our own
		if (VRAudioListener != null)
		{
			VRAudioListener.enabled = true;
		}
		AudioListener[] desktopAudioListeners = FPPlayer.GetComponentsInChildren<AudioListener>(true);
		foreach (AudioListener a in desktopAudioListeners)
		{
			a.enabled = false;
		}

		// disable the UFPS HUD (if any)
		if (DesktopHUD != null)
			DesktopHUD.enabled = false;

		// hide the UFPS crosshair
		if (DesktopCrosshair != null)
			DesktopCrosshair.Hide = true;

	}


	/// <summary>
	/// tries to ensure there is only a single audio listener in the scene for startup,
	/// to avoid unnecessary warnings being spammed to the console (the appropriate
	/// audio listener will be enabled later)
	/// </summary>
	protected virtual void TryEnsureSingleAudioListener()
	{

#if UNITY_5_3_OR_NEWER
	
		AudioListener[] listeners = GameObject.FindObjectsOfType<AudioListener>();
		foreach (AudioListener a in listeners)
		{
			a.enabled = false;
		}
		AudioListener b;
		if (CenterEyeAnchor != null)
		{
			b = CenterEyeAnchor.GetComponentInChildren<AudioListener>(true);
			if (b != null)
				b.enabled = true;
		}
		else
		{
			b = FPCamera.GetComponentInChildren<AudioListener>(true);
			if (b != null)
				b.enabled = true;
		}

#else
		if (FindObjectsOfType<AudioListener>().Length > 1)
		{
			if (VRAudioListener != null)
				VRAudioListener.enabled = false;
			FindObjectOfType<AudioListener>().enabled = true;
		}
		else if (FindObjectsOfType<AudioListener>().Length < 1)
			gameObject.AddComponent<AudioListener>();
#endif

	}


	/// <summary>
	/// completely restores the UFPS player hierarchy for desktop mode play. called
	/// from this script's 'OnDisable'
	/// </summary>
	protected virtual void RestorePlayerHierarchy()
	{

		// prevent nullrefs if scene is being cleared
		if (m_AppQuitting)
			return;

		// don't do anything if the hierarchy hasn't been modified
		if (CenterEyeAnchor == null)
			return;

		// restore FPCamera
		if (FPCamera != null)
		{
			FPCamera.PositionOffset = m_FPCameraOffsetBak;

			// reenable Unity camera component
			if (FPCamera.Camera != null)
			{
				FPCamera.Camera.enabled = true;
				FPCamera.Refresh();
			}

		}

		// reactivate weapon camera (if any)
		if (WeaponCamera != null)
		{
			vp_Utility.Activate(WeaponCamera.gameObject, true);
			if (FPCamera != null)
			{
				FPCamera.SnapSprings();
				FPCamera.SnapZoom();
			}
			if (WeaponHandler != null)
			{
				if (WeaponHandler.CurrentWeapon != null)
					WeaponHandler.CurrentWeapon.ResetState();
			}
			if(FPCamera != null)
				FPCamera.Refresh();
		}

		// reactivate player body
		// NOTE: if player body has a foot fx handler, this should reenable it
		if (BodyAnimator != null)
			vp_Utility.Activate(BodyAnimator.gameObject, true);

		// disable our own foot fx handler
		if (VRFootFXHandler != null)
			VRFootFXHandler.enabled = false;

		// reenable desktop audio listener
		if (FPPlayer != null)
		{
			AudioListener[] desktopAudioListeners = FPPlayer.GetComponentsInChildren<AudioListener>(true);
			bool done = false;
			foreach (AudioListener a in desktopAudioListeners)
			{
				if (!done)  // only enable a single audio listener
				{
					a.enabled = true;
					done = true;
				}
			}
		}

		// disable our own audio listener
		if (VRAudioListener != null)
			VRAudioListener.enabled = false;

		// reenable the UFPS HUD (if any)
		if (DesktopHUD != null)
			DesktopHUD.enabled = true;

		// unhide the UFPS crosshair
		if (DesktopCrosshair != null)
			DesktopCrosshair.Hide = false;

		// reallow vp_FPInput from processing pitch and yaw
		if (FPInput != null)
		{
			FPInput.MouseLookMutePitch = false;
			FPInput.MouseLookMuteYaw = false;
		}

		// reenable all camera motion settings
		if (FPCamera != null)
		{
			FPCamera.MuteBob = false;
			FPCamera.MuteEarthquakes = false;
			FPCamera.MuteShakes = false;
			FPCamera.MuteRoll = false;
			FPCamera.MuteFallImpacts = false;
			FPCamera.MuteBombShakes = false;
			FPCamera.MuteGroundStomps = false;
		}

		if (FPController != null)
		{
			FPController.PhysicsForceDamping = m_ControllerForceDampingBak;
			FPController.PhysicsSlopeSlidiness = m_ControllerSlopeSlidinessBak;
		}

		if(FPPlayer != null)
			FPPlayer.RefreshActivityStates();

	}


	/// <summary>
	/// disables all audiolisteners but one to prevent annoying log spam from
	/// Unity, then disables the component
	/// </summary>
	void ErrorDisable()
	{

		TryEnsureSingleAudioListener();

		enabled = false;

	}


	/// <summary>
	/// disables the component in such a way that the error message (hopefully)
	/// is displayed last in the console to avoid confusion from other potential
	/// error messages. this version of the method is reserved for the most
	/// serious error: old unity version
	/// </summary>
	protected virtual IEnumerator ErrorDisableOnEndOfFrame(string message)
	{

		yield return new WaitForEndOfFrame();

		TryEnsureSingleAudioListener();
		Debug.LogError(message);
		enabled = false;

	}

		
	/// <summary>
	/// 
	/// </summary>
	protected virtual void OnApplicationQuit()
	{
		m_AppQuitting = true;
	}


}
