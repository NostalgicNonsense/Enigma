/////////////////////////////////////////////////////////////////////////////////
//
//	vp_FPBodyAnimator.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	this script animates a human character model that needs to
//					move around and use guns a lot! it is designed for use with
//					the provided 'UFPSExampleAnimator' and can be used in 1st
//					and 3rd person, for local, remote or AI players.
//
//					this is the FIRST PERSON version of the script, intended for
//					use on a local, first person player only (!). it has special
//					logic to replace materials of the head, arms and rest of
//					the body between an invisible-but-shadow-casting material,
//					and their default materials. the script also performs special
//					position adjustment logic to make the body work well with
//					spring-based vp_FPCamera motions without having the camera
//					clipping the local character's body model.
//
//					PLEASE NOTE:
//						1) this script is intended for desktop platforms and
//							is not designed for mobile or VR platforms (!)
//						2) IMPORTANT: in order to use this system as intended, you
//							need to split up the local player's body model so that
//							it has three materials: one for the body, one for the
//							head and one material for the arms. for more info, see
//							this manual chapter:
//							http://bit.ly/1rtfJC6
//						3) for information on the animation features, see the
//							comments in the base class, 'vp_BodyAnimator'
//
/////////////////////////////////////////////////////////////////////////////////
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class vp_FPBodyAnimator : vp_BodyAnimator
{

	// required components
	protected vp_FPController m_FPController = null;
	protected vp_FPCamera m_FPCamera = null;

	// camera
	public Vector3 EyeOffset = new Vector3(0, -0.08f, -0.1f);	// tweak this for the best camera position, esp. when looking down
	protected bool m_WasFirstPersonLastFrame = false;

	protected float m_DefaultCamHeight = 0.0f;

	// lookdown
	public float LookDownZoomFactor = 15.0f;					// can be used to adjust the appearance of the body when looking down
	protected float LookDownForwardOffset = 0.05f;

	// materials
	public bool ShowUnarmedArms = true;							// when active, this will display the body model's arms if no weapon is wielded. NOTE: currently does not work if the UFPS input manager is set to 'Joystick'

	public Material InvisibleMaterial = null;					// this should be set to an invisible, shadow casting material. see the included 'InvisibleShadowCaster' shader & material
	protected Material[] m_FirstPersonMaterials;
	protected Material[] m_FirstPersonWithArmsMaterials;
	protected Material[] m_ThirdPersonMaterials;
	protected Material[] m_InvisiblePersonMaterials;


	// --- properties ---

	public vp_FPCamera FPCamera
	{
		get
		{
			if (m_FPCamera == null)
				m_FPCamera = transform.root.GetComponentInChildren<vp_FPCamera>();
			return m_FPCamera;
		}
	}

	public vp_FPController FPController
	{
		get
		{
			if (m_FPController == null)
				m_FPController = transform.root.GetComponent<vp_FPController>();
			return m_FPController;
		}
	}

	protected float DefaultCamHeight
	{
		get
		{
			if (m_DefaultCamHeight == 0.0f)
			{
				// attempt to fetch Y position from the camera's default state
				if (FPCamera != null && FPCamera.DefaultState != null && FPCamera.DefaultState.Preset != null)
					m_DefaultCamHeight = ((Vector3)FPCamera.DefaultState.Preset.GetFieldValue("PositionOffset")).y;
				else
					m_DefaultCamHeight = 1.75f;		// default on fail
			}
			return m_DefaultCamHeight;
		}
	}
	

	/// <summary>
	/// 
	/// </summary>
	protected override void Awake()
	{

#if UNITY_IOS || UNITY_ANDROID
		Debug.LogError("Error (" + this + ") This script from base UFPS is intended for desktop and not supported on mobile. Are you attempting to use a PC/Mac player prefab on IOS/Android?");
		Component.DestroyImmediate(this);
		return;
#endif

		base.Awake();

		InitMaterials();

		m_WasFirstPersonLastFrame = Player.IsFirstPerson.Get();

		// prevent camera from doing its own collision. (we'll do it
		// from this script's 'UpdateCamera' method instead)
		FPCamera.HasCollision = false;

		Player.IsFirstPerson.Set(true);

	}
	

	/// <summary>
	/// 
	/// </summary>
	protected override void OnEnable()
	{

		base.OnEnable();

		RefreshMaterials();

	}


	/// <summary>
	/// 
	/// </summary>
	protected override void OnDisable()
	{

		base.OnDisable();

	}
	

	/// <summary>
	/// 
	/// </summary>
	protected override void LateUpdate()
	{

		base.LateUpdate();

		if (Time.timeScale == 0.0f)
			return;

		if (Player.IsFirstPerson.Get())
		{

			UpdatePosition();

			UpdateCameraPosition();

			UpdateCameraRotation();

			UpdateCameraCollision();

		}
		else
		{
			FPCamera.TryCameraCollision();
		}

		UpdateFirePosition();

	}


	/// <summary>
	/// swaps out the body model's material array depending on current
	/// gameplay situation, at the end of the frame to give other
	/// logics time to finish (e.g. 'Player.IsFirstPerson.Set')
	/// </summary>
	public void RefreshMaterials()
	{
		StartCoroutine(RefreshMaterialsOnEndOfFrame());
	}


	/// <summary>
	/// swaps out the body model's material array at the end of the
	/// frame. must be called using 'RefreshMaterials'
	/// </summary>
	protected IEnumerator RefreshMaterialsOnEndOfFrame()
	{

		yield return new WaitForEndOfFrame();
				
		if (InvisibleMaterial == null)
		{
			Debug.LogWarning("Warning (" + this + ") No invisible material has been set. Head and arms will look buggy in first person.");
			goto fail;
		}

		if (!Player.IsFirstPerson.Get())
		{
			if (m_ThirdPersonMaterials != null)
				Renderer.materials = m_ThirdPersonMaterials;	// all body parts visible
		}
		else
		{

			if (!Player.Dead.Active && !Player.Climb.Active)		// player is alive and not climbing
			{
				// if we can show unarmed arms, and player is unarmed and not climbing
				if (ShowUnarmedArms
					&& ((Player.CurrentWeaponIndex.Get() < 1)
					&& !Player.Climb.Active)
					&& ((vp_Input.Instance.ControlType != 1))) // 'unarmed arms' don't currently animate well using joystick at low speeds
				{
					if (m_FirstPersonWithArmsMaterials != null)
						Renderer.materials = m_FirstPersonWithArmsMaterials;	// only head is invisible
				}
				// player is armed, climbing, or prohibited to show naked arms :)
				else
				{
					if (m_FirstPersonMaterials != null)
						Renderer.materials = m_FirstPersonMaterials;	// head & arms are invisible
				}
			}
			else						// player is dead ...
			{
				if (m_InvisiblePersonMaterials != null)
					Renderer.materials = m_InvisiblePersonMaterials;	// all bodyparts invisible in order not to clip camera on ragdoll
			}
		}

		fail:
		{}

	}


	/// <summary>
	/// forces model position to bottom center of character controller
	/// and applies user defined camera offset
	/// </summary>
	protected override void UpdatePosition()
	{

		// fix body model position to the charactercontroller
		Transform.position = FPController.SmoothPosition + (FPController.SkinWidth * Vector3.down);

		if (Player.IsFirstPerson.Get() && !Player.Climb.Active)
		{
			// in 1st person, make camera spring physics wear off the more we look
			// at our feet, and have the springs take over the more we look forward
						
			// NOTE: when looking forward, the headless dude's feet will be dangling
			// mid-air, and will clip the ground when jumping. however, these issues
			// are only noticeable in editor scene view and not in 1st person!

			if ((m_HeadLookBones != null) && (m_HeadLookBones.Count > 0))
			{
				Transform.position = Vector3.Lerp(Transform.position,															// blend between model position ...
									Transform.position + (FPCamera.Transform.position - m_HeadLookBones[0].transform.position),	// ... and camera spring position ...
													Mathf.Lerp(1, 0, Mathf.Max(0.0f, ((Player.Rotation.Get().x) / 60.0f))));	// ... by lookdown factor
			}
			else
				Debug.LogWarning("Warning (" + this + ") No headlookbones have been assigned!");

		}
		else
		{
			// in 3rd person, keep the XY position as-is, but zero out Z position
			// or we'll get some heavy stuttering when moving forward / backward
			Transform.localPosition = Vector3.Scale(Transform.localPosition, (Vector3.right + Vector3.up));

		}

		if (Player.Climb.Active)
			Transform.localPosition += ClimbOffset;

	}
	
	
	/// <summary>
	/// performs special position adjustment logic to make the
	/// body work well with spring-based vp_FPCamera motions
	/// without having the camera clipping the local character's
	/// body model.
	/// </summary>
	protected virtual void UpdateCameraPosition()
	{

		// nail camera to neck
		FPCamera.transform.position = m_HeadLookBones[0].transform.position;

		float lookDown = Mathf.Max(0.0f, ((Player.Rotation.Get().x - 45) / 45.0f));
		lookDown = Mathf.SmoothStep(0, 1, lookDown);

		FPCamera.transform.localPosition = new Vector3(
			FPCamera.transform.localPosition.x,
			FPCamera.transform.localPosition.y,
			FPCamera.transform.localPosition.z + lookDown * (Player.Crouch.Active ? 0.0f : LookDownForwardOffset)
			);

		// apply user-adjusted 'eye position' to camera
		FPCamera.Transform.localPosition -= EyeOffset;

		// update camera zoom
		FPCamera.ZoomOffset = (-LookDownZoomFactor * lookDown);
		FPCamera.RefreshZoom();

	}


	/// <summary>
	/// adjusts the camera if attacking with a melee weapon during lookdown, in
	/// order to prevent stabbing ourselves in the gut :)
	/// </summary>
	protected virtual void UpdateCameraRotation()
	{

		if ((Player.CurrentWeaponType.Get() == (int)vp_Weapon.Type.Melee) && Player.Attack.Active)
		{
			if (Player.Rotation.Get().x > 65)
				Player.Rotation.Set(new Vector2(65, Player.Rotation.Get().y));
			return;
		}

	}


	/// <summary>
	/// runs the collision check for the camera and reacts to collisions
	/// by moving the body (but not the controller) away from collision
	/// surfaces by the same distance as the camera
	/// </summary>
	protected virtual void UpdateCameraCollision()
	{

		FPCamera.TryCameraCollision();

		if (FPCamera.CollisionVector != Vector3.zero)
			Transform.position += FPCamera.CollisionVector;

	}


	/// <summary>
	/// 
	/// </summary>
	protected override void UpdateGrounding()
	{
		m_Grounded = FPController.Grounded;
	}


	/// <summary>
	/// (for 3rd person) shows a yellow line indicating the look direction,
	/// and a red ball indicating the current look point
	/// </summary>
	protected override void UpdateDebugInfo()
	{

		if (ShowDebugObjects)
		{
			DebugLookTarget.transform.position = FPCamera.LookPoint; 
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
	/// refreshes shooter fire position last in update, to make sure
	/// it's always in the last known center of 'ProjectileSpawnPoint'
	/// over the duration of the upcoming update
	/// </summary>
	protected void UpdateFirePosition()
	{

		if (WeaponHandler.CurrentShooter == null)
			return;

		if (WeaponHandler.CurrentShooter.ProjectileSpawnPoint == null)
			return;

		WeaponHandler.CurrentShooter.FirePosition = WeaponHandler.CurrentShooter.ProjectileSpawnPoint.transform.position;

	}

	
	/// <summary>
	/// caches the materials on the model on Awake. the materials
	/// are sorted into four arrays to be swapped off and onto the
	/// model at runtime depending on the current gameplay situation.
	/// NOTE: the initialization looks for material names containing
	/// the words "head" and "arm"
	/// </summary>
	protected virtual void InitMaterials()
	{

		if (InvisibleMaterial == null)
		{
			Debug.LogWarning("Warning (" + ") No invisible material has been set.");
			return;
		}

		m_FirstPersonMaterials = new Material[Renderer.materials.Length];
		m_FirstPersonWithArmsMaterials = new Material[Renderer.materials.Length];
		m_ThirdPersonMaterials = new Material[Renderer.materials.Length];
		m_InvisiblePersonMaterials = new Material[Renderer.materials.Length];

		for (int v = 0; v < Renderer.materials.Length; v++)
		{

			// create 4 material arrays from the provided one ...

			// ... one with visible materials on all body parts (for 3rd person)
			m_ThirdPersonMaterials[v] = Renderer.materials[v];

			// ... one with invisible head and arm materials (for classic 1st person)
			if (Renderer.materials[v].name.ToLower().Contains("head") ||
				Renderer.materials[v].name.ToLower().Contains("arm"))
				m_FirstPersonMaterials[v] = InvisibleMaterial;
			else
				m_FirstPersonMaterials[v] = Renderer.materials[v];

			// ... one with an invisible head but visible arms (for unarmed 1st person and VR mods)
			if (Renderer.materials[v].name.ToLower().Contains("head"))
				m_FirstPersonWithArmsMaterials[v] = InvisibleMaterial;
			else
				m_FirstPersonWithArmsMaterials[v] = Renderer.materials[v];

			// ... and one array with all-invisible materials (for ragdolled 1st person)
			m_InvisiblePersonMaterials[v] = InvisibleMaterial;

		}

		RefreshMaterials();
		
	}


	/// <summary>
	/// this is a method rather than a property to allow overriding
	/// </summary>
	protected override bool GetIsMoving()
	{

		return (Vector3.Scale(Player.MotorThrottle.Get(), (Vector3.right + Vector3.forward))).magnitude >
			((vp_Input.Instance.ControlType == 0) ?	// use different sensitivity depending on input hardware
			0.01f	// keyboard (digital)
			:
			0.0f);	// joystick (analog)

	}

	
	/// <summary>
	/// 
	/// </summary>
	protected override Vector3 GetLookPoint()
	{

		return FPCamera.LookPoint;

	}


	/// <summary>
	/// 
	/// </summary>
	protected override Vector3 OnValue_LookPoint
	{
		get
		{
			return GetLookPoint();
		}
	}


	/// <summary>
	/// 
	/// </summary>
	protected override void OnMessage_CameraToggle3rdPerson()
	{

		base.OnMessage_CameraToggle3rdPerson();

		RefreshMaterials();

	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void OnStop_SetWeapon()
	{

		RefreshMaterials();

	}


	/// <summary>
	/// 
	/// </summary>
	protected override void OnStart_Climb()
	{

		base.OnStart_Climb();

		RefreshMaterials();

	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void OnStop_Climb()
	{

		RefreshMaterials();

	}

	
	/// <summary>
	/// 
	/// </summary>
	protected override void OnStart_Dead()
	{

		base.OnStart_Dead();

		RefreshMaterials();
		
	}


}
