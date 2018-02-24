/////////////////////////////////////////////////////////////////////////////////
//
//	vp_VRTeleporter.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	this is an implementation of a popular VR locomotion model by
//					which you point to the ground and press a button to go to new
//					positions. this significantly reduces nausea due to mismatch
//					between the human visual and vestibular systems and works great
//					for casual games, slow paced games and puzzle games.
//
//					TIP: this is best used with a gamepad and the 'Snap Rotate' setting
//						of the vp_VRCameraManager enabled
//
//					USAGE:
//						1) make sure you have a vp_VRCameraManager in the scene
//						2) add this component to the same gameobject as the
//							vp_VRCameraManager
//						3) make sure to set the 'CursorPrefab'. this should be a quad with
//							the normal facing along its Z vector
//						4) go to the Unity main menu > 'UFPS > Input Manager' and make sure
//							that you have 'Teleport' bound to a button that is not bound
//							to anything else. TIP: 'Joystick Button 0' is highly recommended if
//							using a gamepad. if so, also make sure that the input manager's
//							'Control Type' is set to 'Joystick'
//						5) by default, the 'DirectionPointer' is set to the user's head (the
//							Oculus 'CenterEyeAnchor') meaning you must look at the ground where
//							you want to go. however, 'DirectionPointer' can be set to any scene
//							object, for example: a hand-held motion tracker (if present).
//
//					NOTES:
//						- please note that after pressing play in the editor, Unity will not receive
//							proper button input unless the 'Game' window has focus! after pressing
//							play, you must click once in the 'Game' window before you put on your
//							headset, or input won't work. this is sometimes especially confusing
//							when using gamepad (!)
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;

public class vp_VRTeleporter : MonoBehaviour
{

	public Transform DirectionPointer = null;	// the object that points to the ground to pick a destination point. if not set, the transform of the vp_FPCamera will be set. however, this could be set to any object (TIP: such as an Oculus Touch controller).
	public GameObject CursorPrefab = null;		// this object will be spawned once, snapped to the destination point and (optionally) made to face the camera at all times
	public bool CursorFacesCamera = true;		// if true, the cursor gameobject will be rotated to face the camera at all times. set this to false if you want to put a vp_Spin script on the cursor
	[Range(0, 100)]
	public float MaxTeleportDistance = 5;		// the maximum distance the player will be able to teleport from the current position. the cursor will be visible within this radius unless objects are blocking the way
	[Range(0.0f, 1.0f)]
	public float MinTeleportInterval = 0.0f;	// the minimum delay between teleports
	[Range(0, 100)]
	public float FallImpactDistance = 2;        // if teleporting to a surface lower than the current position, fall impact will be applied beyond this vertical teleport distance, in relation to the distance
	public bool AllowJumping = false;			// if false (default), the player will be prevented from jumping at all times even if the jump button is bound

	// cursor
	protected float m_CursorTargetAlpha = 1.0f;
	protected Material m_CursorMaterial = null;
	protected string m_ColorPropertyName = "";
	protected GameObject m_Cursor = null;

	// state
	protected bool m_AppQuitting = false;
	protected float m_NextAllowedTeleportTime = 0.0f;
	protected float m_FPControllerAccelerationBak = 0.0f;
	protected float m_LastCameraSnapTime = 0.0f;
	protected Vector3 m_TargetDestination = Vector3.zero;
	private Vector3 NO_DESTINATION = new Vector3(-99999, -99999, -99999);

#if UNITY_EDITOR
	[vp_HelpBox("• If 'DirectionPointer' is not set, it will automatically be set to the camera.\n\n• Please see the manual and script comments for detailed setup and usage info.", UnityEditor.MessageType.Info, null, null, false, vp_PropertyDrawerUtility.Space.Nothing)]
	public float helpbox;
#endif

	protected vp_VRCameraManager m_VRCameraManager = null;
	protected vp_VRCameraManager VRCamera
	{
		get
		{
			if (m_VRCameraManager == null)
				m_VRCameraManager = transform.GetComponent<vp_VRCameraManager>();
			return m_VRCameraManager;
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

	protected vp_VRCrosshair m_VRCrosshair = null;
	protected vp_VRCrosshair VRCrosshair
	{
		get
		{
			if (m_VRCrosshair == null)
				m_VRCrosshair = GameObject.FindObjectOfType<vp_VRCrosshair>();
			return m_VRCrosshair;
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

	protected Material CursorMaterial
	{
		get
		{
			if (m_CursorMaterial == null)
			{
				if (m_Cursor != null)
				{
					Renderer r = m_Cursor.GetComponentInChildren<Renderer>();
					if (r != null)
					{
						m_CursorMaterial = r.materials[0];
						m_ColorPropertyName = vp_MaterialUtility.GetColorPropertyName(m_CursorMaterial);
					}
				}
			}
			return m_CursorMaterial;
		}
	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void OnEnable()
	{

		if (FPController != null)
			m_FPControllerAccelerationBak = FPController.MotorAcceleration;

		FPPlayer.Register(this);

	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void OnDisable()
	{

		if (m_AppQuitting)
			return;

		if ((m_FPControllerAccelerationBak != 0.0f) && (FPController != null))
			FPController.MotorAcceleration = m_FPControllerAccelerationBak;

		FPPlayer.Unregister(this);

	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void Start()
	{

		if (FPCamera == null)
		{
			Debug.LogError("Error (" + this + ") This component requires a vp_FPCamera to be present in the scene (disabling self).");
			enabled = false;
			return;
		}

		if (FPController == null)
		{
			Debug.LogError("Error (" + this + ") This component requires a vp_FPController to be present in the scene (disabling self).");
			enabled = false;
			return;
		}

		if (FPPlayer == null)
		{
			Debug.LogError("Error (" + this + ") This component requires a vp_FPPlayer to be present in the scene (disabling self).");
			enabled = false;
			return;
		}

		if (VRCamera == null)
		{
			Debug.LogError("Error (" + this + ") This component must sit on the same transform as a vp_VRCameraManager component (disabling self).");
			enabled = false;
			return;
		}

		if ((DirectionPointer == null) && (FPCamera != null))
			DirectionPointer = FPCamera.transform;

		if (CursorPrefab != null)
		{
			m_Cursor = (GameObject)vp_Utility.Instantiate(CursorPrefab);
			vp_Utility.Activate(m_Cursor, false);
		}

	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void LateUpdate()
	{

		UpdateForcedValues();

		UpdateTeleportation();

		UpdateCursorAlpha();

	}


	/// <summary>
	/// prevents regular movement by the vp_FPController
	/// </summary>
	protected virtual void UpdateForcedValues()
	{

		if (FPController != null)
			FPController.MotorAcceleration = 0.0f;

	}


	/// <summary>
	/// tries to detect a valid teleportation destination every frame. if found,
	/// makes the teleportation cursor visible. if not, fades it out. if the
	/// user presses the teleport button when the cursor is visible, instantly
	/// teleports the player to the chosen destination
	/// </summary>
	void UpdateTeleportation()
	{

		// try to fetch a valid destination
		m_TargetDestination = GetDestination();

		// move cursor and teleport if possible
		if ((m_TargetDestination != NO_DESTINATION)
			&& (Time.time > m_NextAllowedTeleportTime))
		{

			m_CursorTargetAlpha = 1.0f;

			// we have a destination and teleportation is allowed
			if (m_Cursor != null)
			{
				// enable cursor
				if (!vp_Utility.IsActive(m_Cursor.gameObject))
					vp_Utility.Activate(m_Cursor.gameObject);
				// position and rotate cursor
				m_Cursor.transform.position = m_TargetDestination;
				if (CursorFacesCamera)
					m_Cursor.transform.rotation = FPCamera.transform.rotation;
			}

			// teleport (if user has pressed the button)
			if (vp_Input.GetButtonDown("Teleport"))
				TeleportTo(m_TargetDestination);

		}

	}


	/// <summary>
	/// fades cursor color to the current target alpha
	/// </summary>
	protected virtual void UpdateCursorAlpha()
	{

		// make cursor invisible if we can't teleport
		if (m_TargetDestination == NO_DESTINATION)
		{
			SetZeroAlpha();
			if (vp_Utility.IsActive(m_Cursor.gameObject))
				vp_Utility.Activate(m_Cursor.gameObject, false);
		}

		// don't fade alpha back up if we've just camera snapped or teleported
		if ((Time.time - m_LastCameraSnapTime) < 0.1f)
			return;

		// continuously keep fading alpha back up from zero
		if (CursorMaterial == null)
			return;
		Color col = CursorMaterial.GetColor(m_ColorPropertyName);
		CursorMaterial.SetColor(m_ColorPropertyName, Color.Lerp(col, new Color(1, 1, 1, m_CursorTargetAlpha), Time.deltaTime * 5.0f));

	}
	

	/// <summary>
	/// performs a number of tests to see if the player can currently teleport
	/// and whether the pointer object is currently pointing at a position that
	/// can be teleported to. if so, returns that position. if not, returns
	/// 'NO_DESTINATION'
	/// </summary>
	protected virtual Vector3 GetDestination()
	{

		// abort if player is dead
		if (FPPlayer.Dead.Active)
			return NO_DESTINATION;

		// abort if player is interacting
		if (FPPlayer.Interactable.Get() != null)
			return NO_DESTINATION;

		// abort if player can interact
		if (VRCrosshair != null && VRCrosshair.CanInteract)
		{
			m_NextAllowedTeleportTime = (Time.time + MinTeleportInterval);
			return NO_DESTINATION;
		}

		// get where we're pointed at
		RaycastHit hit;
		Physics.Linecast(
				DirectionPointer.position,
				((DirectionPointer.position) + (DirectionPointer.forward * 100)),
				out hit,
				~(	(1 << vp_Layer.LocalPlayer) | (1 << vp_Layer.Debris) | (1 << vp_Layer.IgnoreRaycast) |
					(1 << vp_Layer.IgnoreBullets) | (1 << vp_Layer.Trigger) | (1 << vp_Layer.Water) | (1 << vp_Layer.Pickup)));

		// abort if pointing at nothing
		if (hit.collider == null)
			return NO_DESTINATION;

		// abort if pointing at a trigger
		if (hit.collider.isTrigger)
			return NO_DESTINATION;

		// abort if too steep
		float steepness = Vector3.Angle(Vector3.up, hit.normal);
		if (steepness > FPPlayer.GetComponent<CharacterController>().slopeLimit)
			return NO_DESTINATION;

		// abort if too far
		if (Vector3.Distance(vp_3DUtility.HorizontalVector(FPPlayer.transform.position), vp_3DUtility.HorizontalVector(hit.point)) > MaxTeleportDistance)
			return NO_DESTINATION;

		// abort if path is blocked
		float distance = Mathf.Max(1.0f, Vector3.Distance(vp_3DUtility.HorizontalVector(FPPlayer.transform.position), vp_3DUtility.HorizontalVector(hit.point)));
		Vector3 point1 = FPPlayer.transform.position + (Vector3.up * (FPPlayer.Height.Get() * 0.5f));
		Vector3 point2 = point1 + Vector3.up * (FPPlayer.Height.Get() - (FPPlayer.Radius.Get() * 2));
		if (Physics.CapsuleCast(
			point1,
			point2,
			FPPlayer.Radius.Get(),
			FPController.transform.forward,
			distance,
			vp_Layer.Mask.PhysicsBlockers))
		{
			if (!(hit.collider is TerrainCollider)
				&& (hit.transform.gameObject.layer != vp_Layer.Pickup)
				&& (!hit.collider.isTrigger)
				)   // non-steep terrain, triggers and pickups are always passable
				return NO_DESTINATION;
		}

		return hit.point;

	}


	/// <summary>
	/// teleports to the chosen destination, and takes care of footstep fx,
	/// fall damage, and teleport interval
	/// </summary>
	protected virtual void TeleportTo(Vector3 destination)
	{

		if (!enabled)
			return;

		// teleport to the chosen destination
		Vector3 prevPos = FPPlayer.Position.Get();
		FPPlayer.Position.Set(destination);
		FPPlayer.Stop.Send();

		// trigger a footstep effect
		if (VRFootFXHandler != null)
			VRFootFXHandler.TryStep();

		// trigger a fall impact if far enough down
		float fallDistance = (prevPos.y - (FPPlayer.Position.Get().y));
		if (fallDistance > FallImpactDistance)
			FPPlayer.FallImpact.Send(fallDistance * 0.05f);		// NOTE: this is just an approximation of the default UFPS fall damage sensitivity

		// regulate minimum teleport interval
		m_NextAllowedTeleportTime = (Time.time + MinTeleportInterval);

		// notify external components that care about the camera snapping
		// to a new destination in a single frame
		SendMessage("OnCameraSnap", SendMessageOptions.DontRequireReceiver);

	}


	/// <summary>
	/// makes the cursor instantly invisible
	/// </summary>
	protected virtual void SetZeroAlpha()
	{

		if (CursorMaterial == null)
			return;

		CursorMaterial.SetColor(m_ColorPropertyName, new Color(1, 1, 1, 0));

	}


	/// <summary>
	/// 
	/// </summary>
	public virtual void OnCameraSnap()
	{

		if (!enabled)
			return;

		m_LastCameraSnapTime = Time.time;

		SetZeroAlpha();

		m_CursorTargetAlpha = ((GetDestination() == NO_DESTINATION) ? 0.0f : 1.0f);

	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void OnApplicationQuit()
	{
		m_AppQuitting = true;
	}


	/// <summary>
	/// 
	/// </summary>
	bool CanStart_Jump()
	{
		return AllowJumping;
	}


}
