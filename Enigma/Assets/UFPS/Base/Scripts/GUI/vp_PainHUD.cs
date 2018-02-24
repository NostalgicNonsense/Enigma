/////////////////////////////////////////////////////////////////////////////////
//
//	vp_PainHUD.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	a pain hud featuring directional arrows to help locating the
//					sources of incoming damage, a pain effect with color + motion
//					blur that gets more intense with the amount of damage inflicted,
//					and a random blood spatter effect that triggers upon death
//
//					REQUIREMENTS FOR PAIN BLUR:
//						1) will only work on Unity Pro! to enable the blur effect, search
//							this script for all instances of "// BLUR" and UNCOMMENT the
//							code that follows
//						2) this script will automatically try to find the first standard
//							unity 'MotionBlur' full screen effect that it finds under the
//							main camera hierarchy. make sure such a component exists, and
//							that it is DISABLED by default to avoid having constant blur
//						3) it is best to assign the motion blur component to the WEAPON
//							CAMERA, or it won't affect the local weapon
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System.Collections.Generic;

public class vp_PainHUD : MonoBehaviour
{

	/// <summary>
	/// describes an object that recently inflicted damage on the
	/// player. used to track direction and fade out arrows
	/// </summary>
	protected class Inflictor
	{
		public Transform Transform = null;
		public float DamageTime = 0.0f;
		public Inflictor(Transform transform, float damageTime)
		{
			Transform = transform;
			DamageTime = damageTime;
		}
	}

	// list of current inflictors
	protected List<Inflictor> m_Inflictors = new List<Inflictor>();

	public Texture PainTexture = null;
	public Texture DeathTexture = null;
	public Texture ArrowTexture = null;

	public float PainIntensity = 0.2f;		// strength of the blur and red pain color along the edges of the screen

	[Range(0.01f, 0.5f)]
	public float ArrowScale = 0.083f;			// scale of the pain arrows
	public float ArrowAngleOffset = -135;		// rotation offset depending on the arrow art -135 degrees is for the default arrow art
	public float ArrowVisibleDuration = 1.5f;	// how long it takes for an arrow to fade out post damage
	public float ArrowShakeDuration = 0.125f;	// duration of the small shake imposed on the arrows for every incoming damage event

	protected float m_LastInflictorTime = 0.0f;	// last moment of incoming damage used for the arrow shake
	protected vp_DamageInfo.DamageType m_LatestIncomingDamageType = vp_DamageInfo.DamageType.Unknown;	// used for varying effects according to damage type

	protected Color m_PainColor = new Color(0.8f, 0, 0, 0);
	protected Color m_ArrowColor = new Color(0.8f, 0, 0, 0);		
	protected Color m_FlashInvisibleColor = new Color(1, 0, 0, 0);	// the pain flash will fade out to this color
	protected Color m_SplatColor = new Color(1, 1, 1, 0);	// lightness of the current blood spatter upon death
	protected Rect m_SplatRect;					// gui rectangle for blood spatter

	private vp_FPPlayerEventHandler m_Player = null;

	protected bool m_RenderGUI = true;
	public bool UseOnGUI
	{
		get
		{
			return m_RenderGUI;
		}
		set
		{
			m_RenderGUI = value;
		}
	}

	public Color PainColor
	{
		get
		{
			return m_PainColor;
		}
	}

	// BLUR: uncomment to enable on Unity Pro
	//protected MotionBlur m_PainBlur = null;


	/// <summary>
	///
	/// </summary>
	protected virtual void Awake()
	{

		m_Player = transform.GetComponent<vp_FPPlayerEventHandler>();

		// BLUR: uncomment to enable on Unity Pro
		//m_PainBlur = (MotionBlur)Camera.main.GetComponentInChildren(typeof(MotionBlur));
		//if (m_PainBlur != null)
		//{
		//	m_PainBlur.enabled = false;
		//	m_PainBlur.extraBlur = true;
		//	m_PainBlur.blurAmount = 0.0f;
		//}

	}


	/// <summary>
	/// registers this component with the event handler (if any)
	/// </summary>
	protected virtual void OnEnable()
	{

		if (m_Player != null)
			m_Player.Register(this);

	}


	/// <summary>
	/// unregisters this component from the event handler (if any)
	/// </summary>
	protected virtual void OnDisable()
	{

		if (m_Player != null)
			m_Player.Unregister(this);

	}
	

	/// <summary>
	/// 
	/// </summary>
	protected virtual void OnGUI()
	{

		UpdatePainFlash();

		UpdateInflictorArrows();

		UpdateDeathTexture();

	}


	/// <summary>
	/// draws a fullscreen pain flash + blur when damaged
	/// </summary>
	protected virtual void UpdatePainFlash()
	{

		if (m_PainColor.a < 0.01f)
		{
			m_PainColor.a = 0.0f;

			// BLUR: uncomment to enable on Unity Pro
			//if (m_PainBlur != null)
			//	m_PainBlur.enabled = false;	// don't keep fullscreen blur effect enabled unnecessarily

			return;
		}

		m_PainColor = Color.Lerp(m_PainColor, m_FlashInvisibleColor, Time.deltaTime * 0.4f);
		if (UseOnGUI)
		{
			GUI.color = m_PainColor;

			if (PainTexture != null)
				GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), PainTexture);

			GUI.color = Color.white;

		}
		// BLUR: uncomment to enable on Unity Pro
		//if (m_PainBlur != null)
		//	m_PainBlur.blurAmount = m_PainColor.a;
		
	}

	
	/// <summary>
	/// draws a direction arrow for every current inflictor of damage
	/// </summary>
	protected virtual void UpdateInflictorArrows()
	{

		if (ArrowTexture == null)
			return;

		for (int v = m_Inflictors.Count - 1; v > -1; v--)
		{

			// remove inflictors that have been destroyed or made inactive
			if ((m_Inflictors[v] == null)
				|| (m_Inflictors[v].Transform == null)
				|| (!vp_Utility.IsActive(m_Inflictors[v].Transform.gameObject)))
			{
				m_Inflictors.Remove(m_Inflictors[v]);
				continue;
			}

			// fade out arrow
			m_ArrowColor.a = (ArrowVisibleDuration - (Time.time - m_Inflictors[v].DamageTime)) / ArrowVisibleDuration;

			// skip any invisible arrows
			if (m_ArrowColor.a < 0.0f)
				continue;

			// get horizontal direction of damage inflictor
			Vector2 pos = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
			float rot = vp_3DUtility.LookAtAngleHorizontal(
				transform.position,
				transform.forward,
				m_Inflictors[v].Transform.position)
				+ ArrowAngleOffset
				+ ((m_Inflictors[v].Transform != transform) ? 0 : 90);	// if damaging self, point straight down
			float scale = (Screen.width * ArrowScale);

			// shake arrows on each instance of incoming damage
			// NOTE: to make the arrows shake individually, you can instead use
			// '(Time.time - m_Inflictors[v].DamageTime)', but it won't look
			// quite as slick ;)
			float push = (ArrowShakeDuration - (Time.time - m_LastInflictorTime)) / ArrowShakeDuration;
			push = Mathf.Lerp(0, 1, push);
			scale += ((Screen.width / 100) * push);

			if (UseOnGUI)
			{

				// rotate and draw arrow
				Matrix4x4 matrixBackup = GUI.matrix;
				GUIUtility.RotateAroundPivot(rot, pos);
				GUI.color = m_ArrowColor;
				GUI.DrawTexture(new Rect(pos.x, pos.y, scale, scale), ArrowTexture);
				GUI.matrix = matrixBackup;
			}

		}

	}


	/// <summary>
	/// draws a blood spatter image while player is dead. the color,
	/// scaling and position of the image is unique for each death
	/// and gets set in OnStart_Dead
	/// </summary>
	protected virtual void UpdateDeathTexture()
	{

		if (DeathTexture == null)
			return;

		if(!m_Player.Dead.Active)
			return;

		if (m_SplatColor.a == 0.0f)
			return;

		if (UseOnGUI)
		{

			GUI.color = m_SplatColor;
			GUI.DrawTexture(m_SplatRect, DeathTexture);
		}

	}


	/// <summary>
	/// picks up an incoming HUD damage flash message and composes
	/// the data needed to draw the various effects
	/// </summary>
	protected virtual void OnMessage_HUDDamageFlash(vp_DamageInfo damageInfo)
	{

		if (damageInfo == null || damageInfo.Damage == 0.0f)
		{
			m_PainColor.a = 0.0f;
			m_SplatColor.a = 0.0f; 
			return;
		}

		m_LatestIncomingDamageType = damageInfo.Type;

		m_PainColor.a += (damageInfo.Damage * PainIntensity);

		if (damageInfo.Source != null)
		{
			m_LastInflictorTime = Time.time;
			bool create = true;
			// update damage time for existing inflictors and see if we
			// need to create a new inflictor
			foreach (Inflictor i in m_Inflictors)
			{
				if (i.Transform == damageInfo.Source.transform)
				{
					i.DamageTime = Time.time;
					create = false;
				}
			}
			if (create)
				m_Inflictors.Add(new Inflictor(damageInfo.Source, Time.time));
		}

		// BLUR: uncomment to enable on Unity Pro
		//if (m_PainBlur != null)
		//	m_PainBlur.enabled = true;	// activate the pain blur component (if any)

	}


	/// <summary>
	/// generates random values for drawing a fairly unique blood splatter
	/// effect on the screen while the player is dead. gets refreshed once
	/// every time the player dies
	/// </summary>
	protected virtual void OnStart_Dead()
	{

		// don't show blood spatter for falling damage
		if (m_LatestIncomingDamageType == vp_DamageInfo.DamageType.Fall)
		{
			m_SplatColor.a = 0.0f;
			return;
		}

		// variate texture brightness. darker looks gorier
		float col = (Random.value * 0.6f) + 0.4f;
		m_SplatColor = new Color(col, col, col, 1);

		// decide how big the droplets should appear
		float zoom = 
			(Random.value < 0.5f) ?
			(Screen.width / Random.Range(5, 10)) :	// more detailed "high velocity" spatter
			 (Screen.width / Random.Range(4, 7));	// big and smudgy spatter

		// set up screen rect
		m_SplatRect = new Rect(
			Random.Range(-zoom, 0),
			Random.Range(-zoom, 0),
			Screen.width + zoom,
			Screen.height + zoom);

		if (Random.value < 0.5f)	// flip texture horizontally half of the time
		{
			m_SplatRect.x = Screen.width - m_SplatRect.x;
			m_SplatRect.width = -m_SplatRect.width;
		}

		if (Random.value < 0.125f)	// small chance of flipping texture upside down with lower brightness
		{
			col *= 0.5f;
			m_SplatColor = new Color(col, col, col, 1);
			m_SplatRect.y = Screen.height - m_SplatRect.y;
			m_SplatRect.height = -m_SplatRect.height;
		}

	}


	/// <summary>
	/// clears pain fx when player comes back to life
	/// </summary>
	protected virtual void OnStop_Dead()
	{

		m_PainColor.a = 0.0f;

		for (int v = m_Inflictors.Count - 1; v > -1; v--)
		{
			m_Inflictors[v].DamageTime = 0.0f;
		}

	}


}

