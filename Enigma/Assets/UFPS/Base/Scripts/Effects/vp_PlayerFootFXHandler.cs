/////////////////////////////////////////////////////////////////////////////////
//
//	vp_PlayerFootFXHandler.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	this component handles any and all effects that emanate from the feet
//					of the player, including sounds and particles resulting from footsteps,
//					jumps and fall impacts. it supports three footstep detection modes:
//					'Detect Body Step' (from animation), 'Fixed Time Interval' and 'Detect
//					Camera Bob' (same as classic UFPS). the system delegates all effect
//					spawning to the UFPS surface system by sending it ImpactEvents.
//
//					USAGE:
//						1) assign this component to the 'Body' object of a player
//							hierarchy (typically the child of the main controller that has
//							a 'vp_BodyAnimator' and vp_RagdollHandler' on it, and a body model
//							hierarchy under it).
//						2) under the 'Footsteps' foldout, make sure the 'ImpactEvent' slot
//							has the 'Footstep' ImpactEvent (or another ImpactEvent of your
//							choice). This will make the SurfaceManager spawn the correct fx
//							on different ground surface materials when moving around.
//						3) create two empty gameobjects (one for each foot), child them
//							to the lowest member of the leg hierarchy on each side and
//							place them at the exact middle of the shoe sole (as if the
//							player had stepped in bubble gum). if you don't have a body model,
//							child these to the Controller instead, 50cm apart at ground level.
//						4) assign your new dummy foot gameobjects to the 'LeftFoot' and
//							'RightFoot' slot, respectively.
//						5) if you are using a custom body model and / or custom animations,
//							you may have to tweak the 'Trigger Height' and / or 'Sensitivity'
//							parameters to make footsteps trigger correctly. make sure the
//							Body gameobject (with this component) is selected in the editor to
//							visualize the trigger in the Scene View while tweaking its height.
//						6) under the 'Jumping' and 'Fall Impact' foldouts, assign the 'Jump'
//							and 'FallImpact' ImpactEvents, or some other ImpactEvents of your
//							choice. This will make the SurfaceManager spawn the correct fx
//							on different ground surface materials when jumping and falling.
//
//					NOTES:
//						1) having text presets under the 'States' foldout usually will only
//							apply to the 'Fixed Time Interval' footstep mode, which is for
//							when you have a player without a body model.
//						2) except for triggering fx that may be detectable by a human
//							player in multiplayer, this component does not affect gameplay.
//							for example: fall damage is handled in 'vp_PlayerDamageHandler'.
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System.Collections;
using System.Collections.Generic;



public class vp_PlayerFootFXHandler : vp_Component
{

	// footsteps
	public int Mode = 1;				// 0=DetectBodyStep, 1=DetectCameraBob, 2=FixedTimeInterval
	public vp_ImpactEvent FootstepImpactEvent;
	public GameObject FootLeft;
	public GameObject FootRight;
	public float TriggerHeight = 0.1f;
	public float Sensitivity = 1.25f;
	public float TimeInterval = 0.35f;
	public bool RequireMoveInput = false;
	public bool VerifyGroundContact = false;
	public bool ForceAlwaysAnimate = true;

	public enum FootstepMode
	{
		Bypass,
		DetectBodyStep,
		FixedTimeInterval,
		DetectCameraBob
	}

	// jumping
	public vp_ImpactEvent JumpImpactEvent = null;
	
	// fallimpact
	public vp_ImpactEvent FallImpactEvent = null;
	public float FallImpactThreshold = 0.15f;


	// internal state
	protected AudioSource m_CurrentAudioSource = null;
	protected float m_CurrentLocalTriggerHeight = 0.0f;
	protected Transform m_CurrentFoot = null;
	protected Transform m_OtherFoot = null;
	protected bool m_LeftWasAbove = true;
	protected bool m_RightWasAbove = true;
	protected bool m_FlipFootprint;
	protected float m_NextAllowedFootstepTime = 0.0f;
	protected float m_PrevYaw = 0.0f;
	protected bool m_Wasmoving = false;
	protected RaycastHit m_Hit;
	protected float m_NextAllowedJumpSoundTime = 0.0f;

	// runtime
	protected System.Action m_UpdateFunc = null;		// will be either: UpdateRealStep, UpdateFakeStepWithFoot or UpdateFakeStepWithoutFoot
	protected bool IsMoving { get { return (Player.InputMoveVector.Get() != Vector2.zero); } }
	protected bool IsRotating { get { return (Mathf.Abs(Mathf.DeltaAngle(m_PrevYaw, transform.eulerAngles.y)) > 45.0f); } }
	protected float CurrentTriggerHeight		// manipulates and returns the current trigger height for the 'Detect Body Step' mode
	{
		get
		{
			if (!IsMoving)	// if stopped, max out trigger to avoid 'tapdancing' when looking around
				m_CurrentLocalTriggerHeight = 1.0f;
			else if (!m_Wasmoving)								// when starting moving, drop trigger height to zero ...
				m_CurrentLocalTriggerHeight = 0.0f;

			// ... and gradually elevate trigger to user defined height while moving,
			// otherwise - due to animation blending from idle to moving - feet will
			// initially be situated too low for the trigger and the first one or two
			// footsteps won't be audible
			m_CurrentLocalTriggerHeight = Mathf.Lerp(m_CurrentLocalTriggerHeight, 1, Time.deltaTime * Mathf.Max((2.0f - Sensitivity), 0.1f));
			m_Wasmoving = Player.InputMoveVector.Get().magnitude > 0.1f;
			return (transform.position.y + (TriggerHeight * m_CurrentLocalTriggerHeight));
		}
	}

	// debug settings

#if UNITY_EDITOR
	public bool PauseOnEveryStep
	{
		get	{	return m_PauseOnEveryStep;		}
		set	{	m_PauseOnEveryStep = value;		}
	}
	protected bool m_PauseOnEveryStep = false;
#endif

	public bool MuteLocalFootsteps
	{
		get
		{
			if(Application.isPlaying)
				return m_MuteLocalFootsteps && Player.IsLocal.Get();
			else
				return m_MuteLocalFootsteps;
		}
		set	{	m_MuteLocalFootsteps = value;	}
	}
	protected bool m_Mute = false;
	
	// editor

	[SerializeField]
	protected bool m_MuteLocalFootsteps = false;
	protected Color m_GizmoColor = new Color(1.0f, 1.0f, 1.0f, 0.4f);
	protected Color m_SelectedGizmoColor = new Color32(160, 255, 100, 100);
	protected Vector3 GizmoScale = new Vector3(1.5f, 0.01f, 1.5f);

	// auto-instantiated properties

	vp_PlayerEventHandler m_Player = null;
	public vp_PlayerEventHandler Player
	{
		get
		{
			if (m_Player == null)
			{
				m_Player = transform.root.GetComponent<vp_PlayerEventHandler>();
				if ((m_Player == null) && Application.isPlaying)
				{
					Debug.LogError("Error (" + this + ") This component requires a vp_PlayerEventHandler. Disabling self!");
					this.enabled = false;
				}
			}
			return m_Player;
		}
	}

	Animator m_Animator = null;
	public Animator Animator
	{
		get
		{
			if (m_Animator == null)
			{
				m_Animator = GetComponent<Animator>();
			}
			return m_Animator;
		}
	}
	
	protected vp_FPCamera m_FPCamera = null;
	public vp_FPCamera FPCamera
	{
		get
		{
			if ((m_FPCamera == null) && !m_CachedCamera)
			{
				m_FPCamera = transform.root.GetComponentInChildren<vp_FPCamera>();
				m_CachedCamera = true;
			}
			return m_FPCamera;
		}
	}
	protected bool m_CachedCamera = false;

	protected vp_FPController m_FPController = null;
	public vp_FPController FPController
	{
		get
		{
			if ((m_FPController == null) && !m_CachedController)
			{
				m_FPController = transform.root.GetComponentInChildren<vp_FPController>();
				m_CachedController = true;
			}
			return m_FPController;
		}
	}
	protected bool m_CachedController = false;


	// NOTE: this will cache the audiosource hierarchically closest to
	// each foot as belonging to that foot. if the feet have no audio
	// sources, the audiosource of the main charactercontroller will be
	// automatically used for both feet.

	AudioSource m_AudioLeft = null;
	AudioSource AudioLeft
	{
		get
		{
			if (m_AudioLeft == null)
			{
				Transform t = ((FootLeft == null) ? transform : FootLeft.transform);
				while ((t != null) && (m_AudioLeft == null))
				{
					m_AudioLeft = t.GetComponent<AudioSource>();
					t = t.parent;
				}
			}
			return m_AudioLeft;
		}
	}


	AudioSource m_AudioRight = null;
	AudioSource AudioRight
	{
		get
		{
			if (m_AudioRight == null)
			{
				Transform t = ((FootRight == null) ? transform : FootRight.transform);
				while ((t != null) && (m_AudioRight == null))
				{
					m_AudioRight = t.GetComponent<AudioSource>();
					t = t.parent;
				}
			}
			return m_AudioRight;
		}
	}


	AudioSource m_JumpFallAudio = null;
	protected AudioSource JumpFallAudio
	{
		get
		{
			if (m_JumpFallAudio == null)
			{
				m_JumpFallAudio = transform.root.gameObject.AddComponent<AudioSource>();
				// copy some parameters from the main audio source
				// TIP: add more parameters to copy here as needed
				m_JumpFallAudio.rolloffMode = AudioLeft.rolloffMode;
				m_JumpFallAudio.minDistance = AudioLeft.minDistance;
				m_JumpFallAudio.maxDistance = AudioLeft.maxDistance;
				m_JumpFallAudio.spatialBlend = AudioLeft.spatialBlend;
				m_JumpFallAudio.volume = AudioLeft.volume;
				m_JumpFallAudio.spread = AudioLeft.spread;
			}
			return m_JumpFallAudio;
		}
	}

	/// <summary>
	/// overridden to skip caching a lot of stuff we don't need
	/// </summary>
	protected override void Awake()
	{

		StateManager.SetState("Default", enabled);

	}


	/// <summary>
	/// 
	/// </summary>
	protected override void Start()
	{

		base.Start();

		Refresh();

	}


	/// <summary>
	/// sets up component functionality depending on the current mode.
	/// called on startup, and by the editor when manipulating params
	/// </summary>
	public override void Refresh()
	{

		// clear all footstep delegates

		m_UpdateFunc = null;

		if (FPCamera != null)
		{
			FPCamera.BobStepCallback -= UpdateFakeStepWithFoot;
			FPCamera.BobStepCallback -= UpdateFakeStepWithoutFoot;
		}

		if(((AudioLeft == null) && (AudioRight == null))
			|| ((AudioLeft != null) && (AudioRight == null) && !AudioLeft.enabled)
			|| ((AudioLeft == null) && (AudioRight != null) && !AudioRight.enabled)
			|| ((AudioLeft != null) && (AudioRight != null) && !AudioLeft.enabled && !AudioRight.enabled)
			)
			Debug.LogWarning("Warning (" + this + ") An enabled AudioSource is required for footstep sounds.");

		// reassign delegates depending on mode
		switch (Mode)
		{
			case (int)FootstepMode.Bypass:
				m_UpdateFunc = null;
				break;
			case (int)FootstepMode.DetectBodyStep:
				m_UpdateFunc = UpdateRealStep;
				if (ForceAlwaysAnimate && Application.isPlaying && (Animator != null))
					Animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
				break;
			case (int)FootstepMode.FixedTimeInterval:
				if ((FootLeft != null) && (FootRight != null))
					m_UpdateFunc = UpdateFakeStepWithFoot;
				else
				{
					m_UpdateFunc = UpdateFakeStepWithoutFoot;
					m_CurrentAudioSource = ((AudioLeft != null) ? AudioLeft : AudioRight);
					m_CurrentFoot = transform.root;
				}
				break;
			case (int)FootstepMode.DetectCameraBob:
				if (FPCamera != null)
				{
					if ((FPCamera.BobRate.y == 0.0f) && (FPCamera.BobAmplitude.y == 0.0f))
						Debug.LogWarning("Warning (" + this + ") FootstepMode is 'DetectCameraBob' but this vp_FPCamera has no Y axis bob.");
					if ((FootLeft != null) && (FootRight != null))
						FPCamera.BobStepCallback += UpdateFakeStepWithFoot;
					else
					{
						FPCamera.BobStepCallback += UpdateFakeStepWithoutFoot;
						m_CurrentAudioSource = ((AudioLeft != null) ? AudioLeft : AudioRight);
						m_CurrentFoot = transform.root;
					}
				}
				break;
		}

		// enforce having feet in same model at runtime (in case user copied component from another body hierarchy)
		if (Application.isPlaying)
		{
			if (FootLeft != null)
			{
				if (FootLeft.transform.root != Root)
				{
					Debug.LogWarning("Warning (" + this + ") LeftFoot is in another gameobject hierarchy (removing it). Did you copy component values from another body model?");
					FootLeft = null;
				}
			}

			if (FootRight != null)
			{
				if (FootRight.transform.root != Root)
				{
					Debug.LogWarning("Warning (" + this + ") RightFoot is in another gameobject hierarchy (removing it). Did you copy component values from another body model?");
					FootRight = null;
				}
			}
		}


	}


	/// <summary>
	/// 
	/// </summary>
	protected override void Update()
	{

		base.Update();

		// skip if mode is 'Bypass' or 'DetectCameraBob'
		if (m_UpdateFunc == null)
			return;

		// in interval mode, do nothing until allowed
		if ((Mode == (int)FootstepMode.FixedTimeInterval) && (Time.time < m_NextAllowedFootstepTime))
			return;

		// some special cases where footsteps should not trigger
		if (!IsMoving)
		{

			// when not moving and movement is required
			if (RequireMoveInput)
				return;

			// when neither moving nor rotating in fixed interval mode
			if (!IsRotating)
			{
				if (Mode == (int)FootstepMode.FixedTimeInterval)
					return;
			}
			// when standing still but rotating on a platform (rotating platforms make the player rotate)
			else if (Player.Platform.Get() != null)
					return;

		}

		// execute the update function determined by 'Refresh'. this delegate will be either
		// 'UpdateRealStep', 'UpdateFakeStepWithFoot' or 'UpdateFakeStepWithoutFoot'
		m_UpdateFunc();

	}
	

	/// <summary>
	/// places footsteps under actual foot nodes as they touch the ground
	/// </summary>
	protected virtual void UpdateRealStep()
	{

		if ((FootLeft == null) || (FootRight == null))
			return;

		m_CurrentFoot = FootLeft.transform;
		m_OtherFoot = FootRight.transform;
		m_FlipFootprint = false;
		m_CurrentAudioSource = AudioLeft;
		TryTriggerStep(ref m_LeftWasAbove);

		m_CurrentFoot = FootRight.transform;
		m_OtherFoot = FootLeft.transform;
		m_FlipFootprint = true;
		m_CurrentAudioSource = AudioRight;
		TryTriggerStep(ref m_RightWasAbove);

	}


	/// <summary>
	/// places footsteps under actual foot nodes according to a timed
	/// interval OR camera bob step detection
	/// </summary>
	protected virtual void UpdateFakeStepWithFoot()
	{

		if ((FootLeft == null) || (FootRight == null))
			return;

		if (m_FlipFootprint)
		{
			m_CurrentAudioSource = AudioLeft;
			m_CurrentFoot = FootLeft.transform;
		}
		else
		{
			m_CurrentAudioSource = AudioRight;
			m_CurrentFoot = FootRight.transform;
		}

		m_FlipFootprint = !m_FlipFootprint;

		TryStep();

	}


	/// <summary>
	/// places footsteps on the ground in the center of the charactercontroller
	/// according to a timed interval OR camera bob step detection
	/// </summary>
	protected virtual void UpdateFakeStepWithoutFoot()
	{

		// NOTE: when this mode is activated the following properties are
		// set by the 'Refresh' method: m_CurrentAudioSource, m_CurrentFoot

		if (m_CurrentFoot == null)
			return;

		m_FlipFootprint = !m_FlipFootprint;

		TryStep();

	}


	/// <summary>
	/// tries to place a footstep with the current foot node using
	/// the current trigger height
	/// </summary>
	protected virtual void TryTriggerStep(ref bool wasAbove)
	{

		// only detect step if foot is below the trigger
		if ((m_CurrentFoot != null) && (m_CurrentFoot.transform.position.y > CurrentTriggerHeight))
		{
			wasAbove = true;
			return;
		}

		// only detect step if other foot is in the air (or there will be 'shuffling'
		// footsteps, placed too early)
		if ((m_OtherFoot != null) && (m_OtherFoot.transform.position.y < CurrentTriggerHeight))
			return;

		if (wasAbove)
			TryStep();

		wasAbove = false;

	}


	/// <summary>
	/// places a footstep if allowed by the current player state + a raycast
	/// </summary>
	public virtual void TryStep()
	{

		if (Player == null)
			return;

		if (Player.Dead.Active)
			return;

		if (!Player.Grounded.Get())
			return;

		if (Player.OutOfControl.Active)
			return;

		if (Mode == (int)FootstepMode.DetectCameraBob)
			if (!IsMoving && Player.Platform.Get() != null)
				return;

		if (IsMoving
			&& (FPController != null)
			&& (FPController.MotorAcceleration == 0.0f)
			)
			return;

		if (Physics.Raycast(new Ray(m_CurrentFoot.transform.position + (Vector3.up * 0.5f), Vector3.down), out m_Hit, 1.0f, vp_Layer.Mask.ExternalBlockers))
			DoStep();

	}
	
	
	/// <summary>
	/// places a footstep effect on the ground using the foot, raycast
	/// hit, footprint flip, audiosource and other settings assigned by the
	/// previously executed methods. also, updates next allowed footstep
	/// time for the timed interval mode, and yaw for the 'IsRotating'
	/// property
	/// </summary>
	protected virtual void DoStep()
	{

		// mute is a debug feature (for tweaking footstep ranges in multiplayer)
		if (m_Mute)
			return;

		// --- play effects using the surface system ---

		// first retrieve the surface type, so see if it supports footprints.

		if (FootstepImpactEvent != null)
		{

			vp_SurfaceType surface = vp_SurfaceManager.GetSurfaceType(m_Hit);
			//Debug.Log("surface: " + surface);

			if (!IsMoving || ((surface == null) || !surface.CanHaveFootprints()))
			{

				// non-footrint surface: place a simple effect supporting just audio and particles
				vp_SurfaceManager.SpawnEffect(m_Hit, FootstepImpactEvent, surface, m_CurrentAudioSource, false);

				// NOTES:
				// 1) if surface is null, a fallback surface will be used
				// 2) checking for 'IsMoving' prevents spawning hundreds of footprint decals under
				// a player who may be just standing still mouselooking

			}
			else
			{

				// footprint surface: place the effect with direction, movement and footprint flip info
				vp_SurfaceManager.SpawnFootprintEffect(m_Hit, surface, FootstepImpactEvent,
				m_CurrentFoot.transform.forward, m_FlipFootprint, VerifyGroundContact, m_CurrentAudioSource);

			}
		}

		// --- update misc variables for next frame ---

		// for timed interval mode
		if (Mode == (int)FootstepMode.FixedTimeInterval)
		{

			if (!IsMoving && IsRotating)
			{
				m_NextAllowedFootstepTime = Time.time + (TimeInterval * Random.Range(1.0f, 2.0f));	// make foosteps a bit irregular standing still and rotating
			}
			else
			{
				m_NextAllowedFootstepTime = Time.time + (TimeInterval / Mathf.Max(0.2f, Player.InputMoveVector.Get().magnitude));	// dividing by input move vector allows slowing down the footsteps using an analog controller
			}
		}

		// for 'IsRotating' property
		m_PrevYaw = transform.eulerAngles.y;

		// if debugging with the pause feature, pause the editor now
#if UNITY_EDITOR
		if (PauseOnEveryStep)
		{
			UnityEditor.EditorApplication.isPaused = true;
			Debug.Log("Debug (" + this + ") " + (m_FlipFootprint ? " Right " : " Left ") + " footstep triggered @ " + Time.time);
		}
#endif

	}

	
	/// <summary>
	/// 
	/// </summary>
	public virtual void OnDrawGizmosSelected()
	{

		if (!enabled)
			return;

		if (Mode != (int)FootstepMode.DetectBodyStep)
			return;

		Gizmos.matrix = transform.localToWorldMatrix;
		Gizmos.color = m_SelectedGizmoColor;
		Gizmos.DrawCube((Vector3.up * (TriggerHeight * (Application.isPlaying ? m_CurrentLocalTriggerHeight : 1.0f))), GizmoScale);
		Gizmos.color = new Color(0f, 0f, 0f, 1f);
		Gizmos.DrawLine(Vector3.zero, Vector3.forward);

	}


	/// <summary>
	/// DEBUG: for tweaking multiplayer footstep sound range with two
	/// side-by side standalone executables. when enabled, footstep fx
	/// will only trigger if the window is NOT focused. toggle between
	/// the windows a couple of times for this setting to take effect
	/// in both windows. TIP: sound range is determined by the
	/// AudioSource's 'Max Distance' setting
	/// </summary>
	protected virtual void OnApplicationFocus(bool focusStatus)
	{

		if (!MuteLocalFootsteps)
			return;

		m_Mute = focusStatus;

	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void OnMessage_FallImpact(float impact)
	{

		if (impact < FallImpactThreshold)
			return;

		RaycastHit hit;
		if (!Physics.Raycast(new Ray(transform.position + (Vector3.up * 0.5f), Vector3.down), out hit, 1.0f, vp_Layer.Mask.ExternalBlockers))
			return;

		// use a special audio source since the player frequently gets injured on
		// falling, which may trigger other audio clips on the main audio source
		JumpFallAudio.Stop();	// kill any ongoing jump sounds (can't fall and jump at the same time)
		vp_SurfaceManager.SpawnEffect(hit, FallImpactEvent, JumpFallAudio);
		m_NextAllowedJumpSoundTime = Time.time + 0.5f;	// prevent jump sounds from cutting off fallimpact sound in case player is bunny-hopping

	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void OnStart_Jump()
	{

		// prevent jump sounds from cutting off fallimpact sound in case player is bunny-hopping
		if (Time.time < m_NextAllowedJumpSoundTime)
			return;

		RaycastHit hit;
		if (!Physics.Raycast(new Ray(transform.position + (Vector3.up * 0.5f), Vector3.down), out hit, 1.0f, vp_Layer.Mask.ExternalBlockers))
			return;

		vp_SurfaceManager.SpawnEffect(hit, JumpImpactEvent, JumpFallAudio);

	}


}
