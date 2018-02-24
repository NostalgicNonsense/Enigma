/////////////////////////////////////////////////////////////////////////////////
//
//	vp_RagdollHandler.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	this script handles initialization and death + respawn behavior
//					of ragdoll physics on a player body hierarchy. it caches any
//					colliders, rigidbodies and transforms affected, enables the
//					ragdoll OnStart_Death, and restores all bones to their initial
//					positions OnStop_Death. everything is automated and there is
//					only one public property: 'VelocityMultiplier', which makes
//					the momentum of the player controller carry over into velocity
//					on the ragdoll upon death.
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class vp_RagdollHandler : MonoBehaviour
{

	// configurable
	public float CameraFreezeDelay = 2.5f;								// how long for camera to follow ragdoll head upon death. use to minimize jitter
	public float VelocityMultiplier = 30.0f;							// how much player velocity should carry over into velocity on the ragdoll
	public GameObject HeadBone = null;									// required: must have the head bone of the character model assigned

	// internal
	protected float m_TimeOfDeath = 0.0f;
	protected vp_Timer.Handle PostponeTimer = new vp_Timer.Handle();
	protected Vector3 m_HeadRotationCorrection = Vector3.zero;
	protected Vector3 m_CameraFreezeAngle = Vector3.zero;

	// all components potentially involved in the ragdoll process
	protected List<Collider> m_Colliders = null;					// ragdoll colliders
	protected List<Rigidbody> m_Rigidbodies = null;					// ragdoll rigidbodies
	protected List<Transform> m_Transforms = null;					// transforms with the above colliders & rigidbodies
	protected Animator m_Animator = null;							// mecanim animator
	protected vp_BodyAnimator m_BodyAnimator = null;				// UFPS body animator
	protected vp_PlayerEventHandler m_Player = null;				// player event handler
	protected vp_FPCamera m_FPCamera = null;						// UFPS vpFPCamera, NOTE: only used on local player
	protected vp_Controller m_Controller = null;				// the vp_FPController, vp_CapsuleController or vp_CharacterController
	
	// dictionaries storing the local position and rotation of
	// every involved transform (body part) in its initial state
	protected Dictionary<Transform, Quaternion> TransformRotations = new Dictionary<Transform, Quaternion>();
	protected Dictionary<Transform, Vector3> TransformPositions = new Dictionary<Transform, Vector3>();


#if UNITY_EDITOR
	[vp_HelpBox("'VelocityMultiplier' makes the momentum of the player controller carry over into velocity on the ragdoll.", UnityEditor.MessageType.None, typeof(vp_RagdollHandler), null, false, vp_PropertyDrawerUtility.Space.Nothing)]
	public float helpbox;
#endif

	// work variables
	protected Quaternion m_Rot;
	protected Vector3 m_Pos;

	bool m_TriedToFetchPlayer = false;

	// --- lazy initialization properties, for stability ---

	protected vp_PlayerEventHandler Player
	{
		get
		{
			if ((m_Player == null) && !m_TriedToFetchPlayer)
			{
				m_Player = transform.root.GetComponentInChildren<vp_PlayerEventHandler>();
				m_TriedToFetchPlayer = true;
			}
			return m_Player;
		}
	}

	public vp_FPCamera FPCamera
	{
		get
		{
			if (m_FPCamera == null)
				m_FPCamera = transform.root.GetComponentInChildren<vp_FPCamera>();
			return m_FPCamera;
		}
	}

	protected vp_Controller Controller
	{
		get
		{
			if (m_Controller == null)
				m_Controller = transform.root.GetComponentInChildren<vp_Controller>();
			return m_Controller;
		}
	}

	protected List<Collider> Colliders
	{
		get
		{
			if (m_Colliders == null)
			{
				m_Colliders = new List<Collider>();
				foreach (Collider c in GetComponentsInChildren<Collider>())
				{
					if (c.gameObject.layer != vp_Layer.RemotePlayer)
						m_Colliders.Add(c);
				}
			}
			return m_Colliders;
		}
	}

	protected List<Rigidbody> Rigidbodies
	{
		get
		{
			if (m_Rigidbodies == null)
				m_Rigidbodies = new List<Rigidbody>(GetComponentsInChildren<Rigidbody>());
			return m_Rigidbodies;
		}
	}

	protected List<Transform> Transforms
	{
		get
		{
			if (m_Transforms == null)
			{
				m_Transforms = new List<Transform>();
				foreach (Rigidbody r in Rigidbodies)
				{
					m_Transforms.Add(r.transform);
				}
			}
			return m_Transforms;
		}
	}

	protected Animator Animator
	{
		get
		{
			if (m_Animator == null)
				m_Animator = GetComponent<Animator>();
			return m_Animator;
		}
	}

	protected vp_BodyAnimator BodyAnimator
	{
		get
		{
			if (m_BodyAnimator == null)
				m_BodyAnimator = GetComponent<vp_BodyAnimator>();
			return m_BodyAnimator;
		}
	}

	
	/// <summary>
	/// 
	/// </summary>
	protected virtual void Awake()
	{

#if UNITY_IOS || UNITY_ANDROID
		Debug.LogError("Error (" + this + ") This script from base UFPS is intended for desktop and not supported on mobile. Are you attempting to use a PC/Mac player prefab on IOS/Android?");
		Component.DestroyImmediate(this);
		return;
#endif

		// verify that we have all the required components
		// NOTE: CharacterController and vp_FPCamera are optional and only
		// pertain to local player
		if (((Colliders == null) || (Colliders.Count == 0)) ||
			((Rigidbodies == null) || (Rigidbodies.Count == 0)) ||
			((Transforms == null) || (Transforms.Count == 0)) ||
			(Animator == null) ||
			(BodyAnimator == null))
		{
			Debug.LogError("Error (" + this + ") Could not be initialized. Please make sure hierarchy has ragdoll colliders, Animator and vp_BodyAnimator." );
			this.enabled = false;
			return;
		}

		//SaveStartPose();	// why is this not referenced? evaluate!

	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void Start()
	{

		// disable the ragdoll by default or we'll behave very oddly
		SetRagdoll(false);

	}
	

	/// <summary>
	/// stores the initial local position and rotation of every
	/// rigidbody under the body hierarchy
	/// </summary>
	protected virtual void SaveStartPose()
	{

		foreach (Transform t in Transforms)
			{
				if (!TransformRotations.ContainsKey(t))
					TransformRotations.Add(t.transform, t.localRotation);
				if (!TransformPositions.ContainsKey(t))
					TransformPositions.Add(t.transform, t.localPosition);
			}
		
	}

	
	/// <summary>
	/// resets the initial local position and rotation of every
	/// rigidbody under the body hierarchy
	/// </summary>
	protected virtual void RestoreStartPose()
	{

		foreach (Transform t in Transforms)
		{
			if (TransformRotations.TryGetValue(t, out m_Rot))
				t.localRotation = m_Rot;
			if (TransformPositions.TryGetValue(t, out m_Pos))
				t.localPosition = m_Pos;
		}

	}

	
	/// <summary>
	/// registers this component with the event handler (if any)
	/// </summary>
	protected virtual void OnEnable()
	{
		if (Player != null)
			Player.Register(this);
	}


	/// <summary>
	/// unregisters this component from the event handler (if any)
	/// </summary>
	protected virtual void OnDisable()
	{
		if (Player != null)
			Player.Unregister(this);
	}


	/// <summary>
	/// 
	/// </summary>
	void Update()
	{
		
		// SNIPPET: uncomment the below code to instantly ragdoll the player
		// when the 'T' button is pressed. good for testing ragdoll behavior.
		// TIP: don't forget to re-comment this when done. it can be quite
		// confusing when chatting in multiplayer =)
		/*
		if (Input.GetKeyUp(KeyCode.T) && (Player.GetType() == typeof(vp_FPPlayerEventHandler)))
		{
			vp_PlayerDamageHandler d = Player.GetComponentInChildren<vp_PlayerDamageHandler>();
			if (d != null)
			{
				d.Damage(Player.MaxHealth.Get());
				vp_PainHUD p = Player.GetComponentInChildren<vp_PainHUD>();
				if (p != null)
					p.enabled = false;
			}
		}
		*/

	}


	/// <summary>
	/// 
	/// </summary>
	void LateUpdate()
	{

		UpdateDeathCamera();

	}


	/// <summary>
	/// this method syncs any present 1st person camera with
	/// the position and angle of the ragdoll head
	/// </summary>
	protected virtual void UpdateDeathCamera()
	{

		if (Player == null)
			return;

		if (!Player.Dead.Active)
			return;

		if (HeadBone == null)
			return;

		if (!Player.IsFirstPerson.Get())
			return;

		// lock camera position to head bone
		FPCamera.Transform.position = HeadBone.transform.position;
		m_HeadRotationCorrection = HeadBone.transform.localEulerAngles;

		// up until the freeze time, sync camera rotation to ragdoll
		// head rotation
		
		if ((Time.time - m_TimeOfDeath) < CameraFreezeDelay)
		{
			FPCamera.Transform.localEulerAngles = m_CameraFreezeAngle =
			new Vector3(
				// NOTE: here we brutally flip some axes since our ragdoll
				// did not have the same local space as the camera. these
				// flips may not suite your body model, in which case you'll
				// need to tweak the below
					-m_HeadRotationCorrection.z,
					-m_HeadRotationCorrection.x,
					m_HeadRotationCorrection.y
			);
		}
		else
			FPCamera.Transform.localEulerAngles = m_CameraFreezeAngle;

	}


	/// <summary>
	/// this method is used to set ragdoll physics either on (default)
	/// or off. when toggled, any conflicting components will be either
	/// disabled or enabled
	/// </summary>
	public virtual void SetRagdoll(bool enabled = true)
	{
		
		// --- postpone ragdolling if necessary ---

		// NOTE: unfortunately, enabling a fast falling ragdoll might provoke a
		// unity culling bug where the mesh goes permanently invisible to other
		// players due to ragdoll falling 'out of bounds'. to prevent this we
		// postpone ragdolling in 0.1 second steps until reliably grounded
		if (vp_Gameplay.IsMultiplayer && enabled)
		{

			// allow ragdolling of dead players only. this cancels any postponed
			// ragdolling when player has come back alive
			if (!Player.Dead.Active)
				return;

			// only allow one 'postponement' at a time
			PostponeTimer.Cancel();

			if(!Animator.GetBool("IsGrounded"))	// fetching grounding from the Animator is more reliable for (lerped) remote players
			{
				//Debug.Log("Not grounded -> POSTPONING ragdolling");
				vp_Timer.In(0.1f, () => SetRagdoll(true), PostponeTimer);
				return;
			}
			//else Debug.Log("Grounded -> RAGDOLLING");

		}

		// --- toggle components that conflict with ragdoll physics ---

		if(Animator != null)
			Animator.enabled = !enabled;
		if(BodyAnimator != null)
			BodyAnimator.enabled = !enabled;
		if(Controller != null)
			Controller.EnableCollider(!enabled);

		// --- disable / enable rigidbodies and colliders ---

		foreach (Rigidbody r in Rigidbodies)
		{
			r.isKinematic = !enabled;
			if (enabled)
				r.AddForce(Player.Velocity.Get() * VelocityMultiplier);		// pass on momentum of controller into the rigidbodies
		}
		
		foreach (Collider c in Colliders)
		{
			c.enabled = enabled;
		}

		// if disabling, restore the initial state of all the rigidbodies
		if (!enabled)
			RestoreStartPose();

	}


	/// <summary>
	/// this triggers when the player dies, enabling ragdoll physics
	/// </summary>
	protected virtual void OnStart_Dead()
	{

		m_TimeOfDeath = Time.time;	// store time of death for death camera (if any)

		// ragdoll! but not until next frame for more reliable 'grounded' state
		vp_Timer.In(0, () => { SetRagdoll(true); });

	}


	/// <summary>
	/// this triggers when the player respawns, disabling ragdoll
	/// physics and restoring all limbs to their initial state
	/// </summary>
	protected virtual void OnStop_Dead()
	{

		SetRagdoll(false);

		Player.OutOfControl.Stop();

	}


}
