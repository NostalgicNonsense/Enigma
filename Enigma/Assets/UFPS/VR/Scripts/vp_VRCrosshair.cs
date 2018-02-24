/////////////////////////////////////////////////////////////////////////////////
//
//	vp_VRCrosshair.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	keeps a cursor prefab hovering in front of the FPCamera,
//					but snapped to the surface of any object we're looking at.
//					if we're not looking at an object, relaxes cursor position
//					away to the a distance from the camera.
//
//					this type of cursor is necessary in VR since regular 'gui'
//					crosshairs will not give the proper depth perception
//	
//					the script has two cursor prefabs: one for aiming and one
//					for the UFPS interaction system
//
//					USAGE:
//						1) make sure you have a vp_VRCameraManager in the scene
//						2) add this component to the same gameobject as the
//							vp_VRCameraManager
//						3) make sure to set the prefabs. both should be a quad
//							with normal facing along its Z vector
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;

public class vp_VRCrosshair : MonoBehaviour
{

	// objects
	public Transform m_CenterEyeAnchor = null;					// the transform located right between the VR eye cameras (i.e. the Oculus 'CenterEyeAnchor'). no need to set this (will be found automatically)
	public GameObject CrosshairPrefab = null;					// a quad with a crosshair texture and the normal facing along its Z vector
	public GameObject InteractIconPrefab = null;				// a quad with an interact icon texture and the normal facing along its Z vector
	protected GameObject m_Crosshair = null;
	protected GameObject m_InteractIcon = null;

	// colors
	protected Color m_InteractColor = new Color(1, 1, 1, 0);
	protected Color m_CrosshairColor = new Color(1, 1, 1, 0);
	protected float m_InteractTargetAlpha = 0.0f;
	protected float m_CrosshairTargetAlpha = 0.0f;

	// materials
	protected Material m_CrosshairIconMaterial = null;
	protected Material m_InteractIconMaterial = null;
	protected string m_CrosshairColorPropertyName = "";
	protected string m_InteractIconColorPropertyName = "";

	// state
	[Range(0, 100)]
	public float MaxDistance = 10.0f;				// when aiming at nothing, the cursor will slide to a stop this far away from the camera
	[Range(0.1f, 10)]
	public float RelaxSpeed = 1.5f;					// when aiming at nothing, this is the speed at which the cursor will move to the max distance point
	[Range(0, 10)]
	public float CrosshairMinDistance = 1.5f;		// when closer to the camera than this, the crosshair will fade out
	[Range(0, 10)]
	public float InteractIconMinDistance = 0.5f;	// when closer to the camera than this, the interact icon will fade out
	[Range(0, 1)]
	public float SurfaceOffset = 0.2f;              // when aiming at something, the cursor will stop this far in front of it to avoid clipping / fading into it
	public bool HideOnZoom = true;                  // when zooming (aiming down sights), should the crosshair be hidden?
	public bool CanInteract { get { return m_CanInteract; } }
	protected bool m_CanInteract = true;
	protected float m_CurrentDistance = 0.0f;
	protected float m_LastDistance = 0.0f;
	protected float m_LastSnaptime = 0.0f;
	protected bool m_AppQuitting = false;
	protected float m_LastGrabTime = 0.0f;

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

	protected GameObject Crosshair
	{
		get
		{
			if (m_Crosshair == null)
				if (Cursor == null) { };    // trigger the cursor property;
			return m_Crosshair;
		}
	}

	protected GameObject InteractIcon
	{
		get
		{
			if (m_InteractIcon == null)
				if (Cursor == null) { };    // trigger the cursor property;
			return m_InteractIcon;
		}
	}

	protected Material InteractIconMaterial
	{
		get
		{
			if (m_InteractIconMaterial == null)
			{
				if (InteractIcon != null)
				{
					Renderer r = InteractIcon.GetComponentInChildren<Renderer>();
					if (r != null)
					{
						m_InteractIconMaterial = r.materials[0];
						m_InteractIconColorPropertyName = vp_MaterialUtility.GetColorPropertyName(m_InteractIconMaterial);
					}
				}
			}
			return m_InteractIconMaterial;
		}
	}

	protected Material CrosshairIconMaterial
	{
		get
		{
			if (m_CrosshairIconMaterial == null)
			{
				if (Crosshair != null)
				{
					Renderer r = Crosshair.GetComponentInChildren<Renderer>();
					if (r != null)
					{
						m_CrosshairIconMaterial = r.materials[0];
						m_CrosshairColorPropertyName = vp_MaterialUtility.GetColorPropertyName(m_InteractIconMaterial);
					}
				}
			}
			return m_CrosshairIconMaterial;
		}
	}

	protected GameObject m_Cursor = null;
	protected GameObject Cursor
	{
		get
		{
			if (m_Cursor == null)
			{
				m_Cursor = new GameObject("VRCursor");
				if (m_Crosshair == null)
				{
					if (CrosshairPrefab != null)
						m_Crosshair = GameObject.Instantiate(CrosshairPrefab);
					else
						m_Crosshair = vp_3DUtility.DebugBall();
					m_Crosshair.transform.parent = m_Cursor.transform;
					m_Crosshair.transform.localPosition = Vector3.zero;
					m_Crosshair.transform.localEulerAngles = Vector3.zero;
				}
				if (m_InteractIcon == null)
				{
					if (InteractIconPrefab != null)
						m_InteractIcon = GameObject.Instantiate(InteractIconPrefab);
					else
						m_InteractIcon = vp_3DUtility.DebugBall();
					m_InteractIcon.transform.parent = m_Cursor.transform;
					m_InteractIcon.transform.localPosition = Vector3.zero;
					m_InteractIcon.transform.localEulerAngles = Vector3.zero;
				}
			}
			return m_Cursor;

		}
	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void Start()
	{

		if (FPPlayer == null)
		{
			Debug.LogError("Error (" + this + ") This component requires a vp_FPPlayer component to be present in the scene (disabling self).");
			enabled = false;
			return;
		}

		if (VRCamera == null)
		{
			Debug.LogError("Error (" + this + ") This component must sit on the same transform as a vp_VRCameraManager component (disabling self).");
			enabled = false;
			return;
		}

		if (CenterEyeAnchor == null)
		{
			Debug.LogError("Error (" + this + ") 'CenterEyeAnchor' is not assigned. IMPORTANT: Make sure to drag an 'OVRCameraRig', into the scene from 'OVR/Prefabs', and 'CenterEyeAnchor' will be set automatically.");
			enabled = false;
			return;
		}

		m_InteractColor = new Color(1,1, 1, 1);
		m_InteractTargetAlpha = 1.0f;

	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void LateUpdate()
	{
		
		UpdateCursorPosition();

		UpdateCursorAlpha();

	}


	/// <summary>
	/// locks cursor position and rotation to in front of the FPCamera,
	/// but snapped to the surface of the object we're looking at (if any).
	/// if not looking at an object, relaxes cursor position back to the
	/// max distance from the camera
	/// </summary>
	protected virtual void UpdateCursorPosition()
	{

		// snap position and rotation of the cursor to that of the FPCamera
		Cursor.transform.rotation = CenterEyeAnchor.transform.rotation;
		Cursor.transform.position = CenterEyeAnchor.transform.position;								  

		// back up this frame's distance
		m_LastDistance = m_CurrentDistance;

		m_CanInteract = false;

		// raycast to see if we're looking at something in the scene
		RaycastHit hit;
		if (Physics.Linecast(
			CenterEyeAnchor.transform.position,
			(CenterEyeAnchor.transform.position + (CenterEyeAnchor.transform.forward * MaxDistance)),
			out hit,
			vp_Layer.Mask.ExternalBlockers)
			)
		{
			// raycast hit something: set distance to slightly in front of it
			m_CurrentDistance = (Mathf.Min(MaxDistance, hit.distance)) - Mathf.Abs(SurfaceOffset);
			if (	hit.transform.GetComponent<vp_Grab>()
				||	hit.transform.GetComponent<vp_ItemGrab>()
				||	hit.transform.GetComponent<vp_PlatformSwitch>()
				)
				m_CanInteract = true;
		}
		else
		{
			// raycast didn't hit anything: smoothly relax back to standard distance
			m_CurrentDistance = Mathf.Lerp(m_CurrentDistance, MaxDistance, Time.deltaTime * RelaxSpeed);
			m_CanInteract = false;
		}

		// move cursor ahead onto the target object
		Cursor.transform.position += (Cursor.transform.forward * m_CurrentDistance);

	}



	/// <summary>
	/// fades in the currently active cursor (crosshair or interact icon),
	/// and insta-hides the inactive one
	/// </summary>
	protected virtual void UpdateCursorAlpha()
	{

		// when snap rotating or teleporting, insta-hide the cursor
		if ((Time.time - m_LastSnaptime) < 0.1f)
		{
			m_CrosshairColor.a = 0;
			m_InteractColor.a = 0;
		}

		// if we can't interact with anything, fade in the crosshair and insta-hide
		// the interact icon
		if (!FPPlayer.CanInteract.Get() || !m_CanInteract)
		{
			m_InteractTargetAlpha = 0.0f;
			if ((WeaponHandler.CurrentWeapon == null)
				|| (m_CurrentDistance < CrosshairMinDistance
				|| (HideOnZoom && FPPlayer.Zoom.Active)
				)
				)
				m_CrosshairTargetAlpha = 0.0f;
			else if (m_InteractColor.a < 0.1f)
				m_CrosshairTargetAlpha = 1.0f;
			m_InteractColor.a = 0;
		}
		else
		{
			// we can interact with something, so fade in the interact icon and
			// insta-hide the crosshair icon
			m_CrosshairTargetAlpha = 0.0f;
			if (FPPlayer.Interactable.Get() != null)    // we are grabbing (holding) something
			{
				m_InteractTargetAlpha = 0.0f;
				m_LastGrabTime = Time.time;
			}
			else if (Time.time < (m_LastGrabTime + 1.0f))
			{
				m_InteractTargetAlpha = 0.0f;	// wait one second until showing interact icon again after tossing an object
			}
			else
			{
				// we are not grabbing (holding) something but standing close enough to interact
				if (m_CurrentDistance < InteractIconMinDistance)
					m_InteractTargetAlpha = 0.05f;  // extremely close to object so fade out, but show the interact icon very slightly to show we can still interact
				else if (m_CrosshairColor.a < 0.1f)
					m_InteractTargetAlpha = 1.0f;	// within a normal grabbing distance
			}

			m_CrosshairColor.a = 0;

		}

		// handle fading of the cursor objects to their current target alpha
		m_InteractColor.a = Mathf.Lerp(m_InteractColor.a, m_InteractTargetAlpha, Time.deltaTime * 10);
		InteractIconMaterial.SetColor(m_InteractIconColorPropertyName, m_InteractColor);
		if ((m_CrosshairTargetAlpha == 0) && Mathf.Abs(m_LastDistance - m_CurrentDistance) > 1)
			m_CrosshairColor.a = 0;
		m_CrosshairColor.a = Mathf.Lerp(m_CrosshairColor.a, m_CrosshairTargetAlpha, Time.deltaTime * 10);
		CrosshairIconMaterial.SetColor(m_CrosshairColorPropertyName, m_CrosshairColor);

	}


	/// <summary>
	/// 
	/// </summary>
	public virtual void OnCameraSnap()
	{

		if (!enabled)
			return;
		m_LastSnaptime = Time.time;

	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void OnApplicationQuit()
	{
		m_AppQuitting = true;
	}


}

