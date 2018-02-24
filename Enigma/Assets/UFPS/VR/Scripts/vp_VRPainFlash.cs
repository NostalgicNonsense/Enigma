/////////////////////////////////////////////////////////////////////////////////
//
//	vp_VRPainFlash.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	this script piggybacks off the regular UFPS vp_PainHUD to detect
//					when the intensity of incoming damage (and death) and updates a
//					full screen fx plane in front of the camera with pain flashes and
//					blood spatter. the vp_PainHUD is prevented from drawing GUI, and
//					its realtime color values are copied and used by this script instead.
//
//					USAGE:
//						1) make sure you have a vp_VRCameraManager in the scene
//						2) add this component to the same gameobject as the
//							vp_VRCameraManager
//						3) make sure to set the 'PlanePrefab'. it should be a quad with
//							the normal facing along its Z vector
//						4) tweak 'Distance' until the plane precisely covers all the
//							edges of the visible view in VR. TIP: the scalar dimension
//							of the plane should be roughly 16:10
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;

public class vp_VRPainFlash : MonoBehaviour
{

	public GameObject PlanePrefab = null;           // a quad with the normal facing along its Z vector. gets texture assigned automatically from the other public properties
	public Texture PainTexture = null;              // should be a white texture that fades to zero alpha in the middle. if not set, will be acquired from the vp_PainHUD instance
	public Texture DeathTexture = null;             // should be a texture with blood splatter in it. if not set, will be acquired from the vp_PainHUD instance
	[Range(0.15f, 1.0f)]
	public float Distance = 0.15f;					// the distance of the plane from the camera (enforced at runtime)
	protected GameObject m_Plane = null;

	protected Material m_Material = null;
	protected string m_ColorPropertyName = "";

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


	protected vp_PainHUD m_PainHUD = null;
	protected vp_PainHUD PainHUD
	{
		get
		{
			if (m_PainHUD == null)
				m_PainHUD = GameObject.FindObjectOfType<vp_PainHUD>();
			return m_PainHUD;
		}
	}

	protected Material Material
	{
		get
		{
			if (m_Material == null)
			{
				if (Plane != null)
				{
					if (Plane != null)
					{
						Renderer r = Plane.GetComponentInChildren<Renderer>();
						if (r != null)
						{
							m_Material = r.materials[0];
							m_ColorPropertyName = vp_MaterialUtility.GetColorPropertyName(m_Material);
						}
					}
				}
			}
			return m_Material;
		}
	}

	protected GameObject Plane
	{
		get
		{

			if ((m_Plane == null) && (PlanePrefab != null))
			{
				m_Plane = GameObject.Instantiate(PlanePrefab);
				m_Plane.name = "VRPainFlash";
				m_Plane.transform.parent = m_Plane.transform;
				m_Plane.transform.localPosition = Vector3.zero;
				m_Plane.transform.localEulerAngles = Vector3.zero;
			}

			return m_Plane;

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

		if (PainHUD == null)
		{
			Debug.LogError("Error (" + this + ") This component requires a vp_PainHUD to be present in the scene (disabling self).");
			enabled = false;
			return;
		}

		if (PainTexture == null)
			PainTexture = PainHUD.PainTexture;

		if (DeathTexture == null)
			DeathTexture = PainHUD.DeathTexture;

	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void LateUpdate()
	{

		UpdatePosition();

		UpdateAlpha();

		UpdateTexture();

	}


	/// <summary>
	/// keeps the plane prefab at a fixed distance in front of the camera
	/// </summary>
	protected virtual void UpdatePosition()
	{

		Plane.transform.rotation = FPCamera.transform.rotation;
		Plane.transform.position = FPCamera.transform.position + (FPCamera.transform.forward * Distance);

	}


	/// <summary>
	/// gets current color of the painhud script and applies it to our plane.
	/// also, caps color intensity and toggles plane on and off depending on
	/// alpha
	/// </summary>
	protected virtual void UpdateAlpha()
	{

		// deactivate the plane if essentially invisible
		if (PainHUD.PainColor.a < 0.01f)
		{
			if (vp_Utility.IsActive(m_Plane))
				vp_Utility.Activate(m_Plane, false);
			return;
		}

		// activate the plane if visible
		if (!vp_Utility.IsActive(m_Plane))
			vp_Utility.Activate(m_Plane);

		// get color from painhud script
		Color col = PainHUD.PainColor;
		if (col.a > 2.0f)
			col.a = 2.0f;	// avoid excessive color intensity on high damage
		else if (FPPlayer.Dead.Active)
			col.a = 1.0f;

		// apply color to the plane
		Material.SetColor(m_ColorPropertyName, col);

	}


	/// <summary>
	/// if player is dead, assigns the pain hud's death texture to the plane,
	/// otherwise, uses the pain hud's pain texture
	/// </summary>
	protected virtual void UpdateTexture()
	{

		if (FPPlayer.Dead.Active && (DeathTexture != null))
		{
			if (Material.mainTexture != DeathTexture)
				Material.SetTexture("_MainTex", DeathTexture);
		}
		else if (PainTexture!= null && (Material != null))
		{
			if (Material.mainTexture != PainTexture)
				Material.SetTexture("_MainTex", PainTexture);
		}

	}


}

