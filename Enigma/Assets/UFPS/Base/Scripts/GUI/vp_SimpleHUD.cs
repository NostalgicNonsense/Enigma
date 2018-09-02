/////////////////////////////////////////////////////////////////////////////////
//
//	vp_SimpleHUD.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	a primitive HUD displaying health, ammo and total ammo.
//					both health and ammo flashes when low. this is provided
//					for prototyping only and is not written for performance
//
/////////////////////////////////////////////////////////////////////////////////


using UnityEngine;
using System.Collections.Generic;

public class vp_SimpleHUD : MonoBehaviour
{

	public bool ShowHUD = true;

	protected vp_FPPlayerEventHandler m_Player = null;

	// gui
	public Font BigFont;
	public Font SmallFont;
	public Font MessageFont;
	public float BigFontOffset = 69;
	public float SmallFontOffset = 56;
	public Texture Background = null;
	protected Vector2 m_DrawPos = Vector2.zero;
	protected Vector2 m_DrawSize = Vector2.zero;
	protected Rect m_DrawLabelRect = new Rect(0, 0, 0, 0);
	protected Rect m_DrawShadowRect = new Rect(0, 0, 0, 0);
	protected float m_TargetHealthOffset = 0;
	protected float m_CurrentHealthOffset = 0;
	protected float m_TargetAmmoOffset = 200;
	protected float m_CurrentAmmoOffset = 200;
	
	// health
	public Texture2D HealthIcon = null;
	public float HealthMultiplier = 10.0f;
	public Color HealthColor = Color.white;
	public float HealthLowLevel = 2.5f;
	public Color HealthLowColor = new Color(0.75f, 0, 0, 1);
	public AudioClip HealthLowSound = null;
	public float HealthLowSoundInterval = 1.0f;
	protected float m_FormattedHealth = 0.0f;
	protected float m_NextAllowedPlayHealthLowSoundTime = 0.0f;
	protected float m_HealthWidth { get { return ((HealthStyle.CalcSize(new GUIContent(FormattedHealth)).x)); } }
	protected AudioSource m_Audio = null;

	// ammo
	public Color AmmoColor = Color.white;
	public Color AmmoLowColor = new Color(0, 0, 0, 1);

	// message
	protected string m_PickupMessage = "";
	protected Color m_MessageColor = new Color(1.0f, 1.0f, 1.0f, 2);

	// misc colors
	protected Color m_InvisibleColor = new Color(0, 0, 0, 0);
	protected Color m_TranspBlack = new Color(0, 0, 0, 0.5f);
	protected Color m_TranspWhite = new Color(1, 1, 1, 0.5f);

	// ---- styles ---

	protected static GUIStyle m_MessageStyle = null;
	public GUIStyle MessageStyle
	{
		get
		{
			if (m_MessageStyle == null)
			{
				m_MessageStyle = new GUIStyle("Label");
				m_MessageStyle.alignment = TextAnchor.MiddleCenter;
				m_MessageStyle.font = MessageFont;
			}
			return m_MessageStyle;
		}
	}

	protected GUIStyle m_HealthStyle = null;
	public GUIStyle HealthStyle
	{
		get
		{
			if (m_HealthStyle == null)
			{
				m_HealthStyle = new GUIStyle("Label");
				m_HealthStyle.font = BigFont;
				m_HealthStyle.alignment = TextAnchor.MiddleRight;
				m_HealthStyle.fontSize = 28;
				m_HealthStyle.wordWrap = false;
			}
			return m_HealthStyle;
		}
	}
	protected GUIStyle m_AmmoStyle = null;
	public GUIStyle AmmoStyle
	{
		get
		{
			if (m_AmmoStyle == null)
			{
				m_AmmoStyle = new GUIStyle("Label");
				m_AmmoStyle.font = BigFont;
				m_AmmoStyle.alignment = TextAnchor.MiddleRight;
				m_AmmoStyle.fontSize = 28;
				m_AmmoStyle.wordWrap = false;
			}
			return m_AmmoStyle;
		}
	}

	protected GUIStyle m_AmmoStyleSmall = null;
	public GUIStyle AmmoStyleSmall
	{
		get
		{
			if (m_AmmoStyleSmall == null)
			{
				m_AmmoStyleSmall = new GUIStyle("Label");
				m_AmmoStyleSmall.font = SmallFont;
				m_AmmoStyleSmall.alignment = TextAnchor.UpperLeft;
				m_AmmoStyleSmall.fontSize = 15;
				m_AmmoStyleSmall.wordWrap = false;
			}
			return m_AmmoStyleSmall;
		}
	}


	/// <summary>
	///
	/// </summary>
	protected virtual void Awake()
	{

		m_Player = transform.GetComponent<vp_FPPlayerEventHandler>();
		m_Audio = m_Player.transform.GetComponent<AudioSource>();

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
	/// returns a useful string representation of the player health value
	/// in percent
	/// </summary>
	string FormattedHealth
	{

		get
		{
			m_FormattedHealth = (m_Player.Health.Get() * HealthMultiplier);
			if(m_FormattedHealth < 1.0f)
				m_FormattedHealth = (m_Player.Dead.Active ? Mathf.Min(m_FormattedHealth, 0.0f) : 1.0f);
			if (m_Player.Dead.Active && m_FormattedHealth > 0.0f)
				m_FormattedHealth = 0.0f;
			return ((int)m_FormattedHealth).ToString();
		}

	}


	/// <summary>
	/// 
	/// </summary>
	void Update()
	{

		// update smooth movement values for sliding in and out of screen
		m_CurrentAmmoOffset = Mathf.SmoothStep(m_CurrentAmmoOffset, m_TargetAmmoOffset, Time.deltaTime * 10);
		m_CurrentHealthOffset = Mathf.SmoothStep(m_CurrentHealthOffset, m_TargetHealthOffset, Time.deltaTime * 10);

		// move ammo out of view if no weapon is wielded
		if ((m_Player.CurrentWeaponIndex.Get() == 0)
			|| (m_Player.CurrentWeaponType.Get() == (int)vp_Weapon.Type.Melee))
			m_TargetAmmoOffset = 200;
		else
			m_TargetAmmoOffset = 10;

		// make health black on death
		if (m_Player.Dead.Active)
			HealthColor = Color.black;

		// if health is low, fade color up and down and play a warning sound
		else if ((m_Player.Health.Get() < HealthLowLevel))
		{
			HealthColor = Color.Lerp(Color.white, HealthLowColor, (vp_MathUtility.Sinus(6.0f, 0.1f, 0.0f) * 5) + 0.5f);
			if ((HealthLowSound != null) && (Time.time >= m_NextAllowedPlayHealthLowSoundTime))
			{
				m_NextAllowedPlayHealthLowSoundTime = Time.time + HealthLowSoundInterval;
				m_Audio.pitch = 1.0f;
				m_Audio.PlayOneShot(HealthLowSound);
			}
		}
		else
			HealthColor = Color.white;	// health is not low, draw it normally

		// if ammo is low, fade color up and down
		if ((m_Player.CurrentWeaponAmmoCount.Get() < 1) && (m_Player.CurrentWeaponType.Get() != (int)vp_Weapon.Type.Thrown))
			AmmoColor = Color.Lerp(Color.white, AmmoLowColor, (vp_MathUtility.Sinus(8.0f, 0.1f, 0.0f) * 5) + 0.5f);
		else
			AmmoColor = Color.white;	// ammo is not low, draw it normally

	}


	/// <summary>
	/// this draws a primitive HUD and also renders the current
	/// message, fading out in the middle of the screen
	/// </summary>
	protected virtual void OnGUI()
	{

		if (!ShowHUD)
			return;
		
		DrawHealth();

		DrawAmmo();

		DrawText();

	}


	/// <summary>
	/// displays a simple 'Health' HUD
	/// </summary>
	void DrawHealth()
	{

		DrawLabel("", new Vector2(m_CurrentHealthOffset, Screen.height - 68), new Vector2(80 + m_HealthWidth, 52), AmmoStyle, Color.white, m_TranspBlack, null);	// background
		if (HealthIcon != null)
			DrawLabel("", new Vector2(m_CurrentHealthOffset + 10, Screen.height - 58), new Vector2(32, 32), AmmoStyle, Color.white, HealthColor, HealthIcon);			// icon
		DrawLabel(FormattedHealth, new Vector2(m_CurrentHealthOffset - 18 - (45 - m_HealthWidth), Screen.height - BigFontOffset), new Vector2(110, 60), HealthStyle, HealthColor, Color.clear, null);	// value
		DrawLabel("%", new Vector2(m_CurrentHealthOffset + 50 + m_HealthWidth, Screen.height - SmallFontOffset), new Vector2(110, 60), AmmoStyleSmall, HealthColor, Color.clear, null);	// percentage mark
		GUI.color = Color.white;

	}

	
	/// <summary>
	/// displays a simple 'Ammo' HUD
	/// </summary>
	void DrawAmmo()
	{

		if ((m_Player.CurrentWeaponType.Get() == (int)vp_Weapon.Type.Thrown))
		{
			DrawLabel("", new Vector2(m_CurrentAmmoOffset + Screen.width - 93 - (AmmoStyle.CalcSize(new GUIContent(m_Player.CurrentWeaponAmmoCount.Get().ToString())).x), Screen.height - 68), new Vector2(200, 52), AmmoStyle, AmmoColor, m_TranspBlack, null);	// background
			if (m_Player.CurrentAmmoIcon.Get() != null)
				DrawLabel("", new Vector2(m_CurrentAmmoOffset + Screen.width - 83 - (AmmoStyle.CalcSize(new GUIContent(m_Player.CurrentWeaponAmmoCount.Get().ToString())).x), Screen.height - 58), new Vector2(32, 32), AmmoStyle, Color.white, AmmoColor, m_Player.CurrentAmmoIcon.Get());	// icon
			DrawLabel((m_Player.CurrentWeaponAmmoCount.Get() + m_Player.CurrentWeaponClipCount.Get()).ToString(), new Vector2(m_CurrentAmmoOffset + Screen.width - 145, Screen.height - BigFontOffset), new Vector2(110, 60), AmmoStyle, AmmoColor, Color.clear, null);		// value
		}
		else
		{
			DrawLabel("", new Vector2(m_CurrentAmmoOffset + Screen.width - 115 - (AmmoStyle.CalcSize(new GUIContent(m_Player.CurrentWeaponAmmoCount.Get().ToString())).x), Screen.height - 68), new Vector2(200, 52), AmmoStyle, AmmoColor, m_TranspBlack, null);	// background
			if (m_Player.CurrentAmmoIcon.Get() != null)
				DrawLabel("", new Vector2(m_CurrentAmmoOffset + Screen.width - 105 - (AmmoStyle.CalcSize(new GUIContent(m_Player.CurrentWeaponAmmoCount.Get().ToString())).x), Screen.height - 58), new Vector2(32, 32), AmmoStyle, Color.white, AmmoColor, m_Player.CurrentAmmoIcon.Get());	// icon
			DrawLabel(m_Player.CurrentWeaponAmmoCount.Get().ToString(), new Vector2(m_CurrentAmmoOffset + Screen.width - 177, Screen.height - BigFontOffset), new Vector2(110, 60), AmmoStyle, AmmoColor, Color.clear, null);		// value
			DrawLabel("/ " + m_Player.CurrentWeaponClipCount.Get().ToString(), new Vector2((m_CurrentAmmoOffset + Screen.width - 60), Screen.height - SmallFontOffset), new Vector2(110, 60), AmmoStyleSmall, AmmoColor, Color.clear, null);		// total ammo count
		}

	}


	/// <summary>
	/// shows a message in the middle of the screen and fades it out
	/// </summary>
	void DrawText()
	{

		if (m_PickupMessage == null)
			return;

		if(m_MessageColor.a < 0.01f)
			return;

		m_MessageColor = Color.Lerp(m_MessageColor, m_InvisibleColor, Time.deltaTime * 0.4f);
		GUI.color = m_MessageColor;
		GUI.Box(new Rect(200, 150, Screen.width - 400, Screen.height - 400), m_PickupMessage, MessageStyle);
		GUI.color = Color.white;

	}


	/// <summary>
	/// updates the HUD message text and makes it fully visible
	/// </summary>
	protected virtual void OnMessage_HUDText(string message)
	{

		m_MessageColor = Color.white;
		m_PickupMessage = (string)message;

	}


	/// <summary>
	/// a simple standard method for drawing labels with text and / or textures
	/// </summary>
	void DrawLabel(string text, Vector2 position, Vector2 scale, GUIStyle textStyle, Color textColor, Color bgColor, Texture texture)
	{

		if (texture == null)
			texture = Background;

		if (scale.x == 0)
			scale.x = textStyle.CalcSize(new GUIContent(text)).x;
		if (scale.y == 0)
			scale.y = textStyle.CalcSize(new GUIContent(text)).y;

		m_DrawLabelRect.x = m_DrawPos.x = position.x;
		m_DrawLabelRect.y = m_DrawPos.y = position.y;
		m_DrawLabelRect.width = m_DrawSize.x = scale.x;
		m_DrawLabelRect.height = m_DrawSize.y = scale.y;

		if (bgColor != Color.clear)
		{
			GUI.color = bgColor;
			if (texture != null)
				GUI.DrawTexture(m_DrawLabelRect, texture);
		}

		GUI.color = textColor;
		GUI.Label(m_DrawLabelRect, text, textStyle);
		GUI.color = Color.white;

		m_DrawPos.x += m_DrawSize.x;
		m_DrawPos.y += m_DrawSize.y;

	}


	/// <summary>
	/// moves ammo counter temporarily out of view when changing weapons
	/// </summary>
	void OnStart_SetWeapon()
	{
		m_TargetAmmoOffset = 200;
	}


	/// <summary>
	/// moves ammo counter back in view after changing weapons
	/// </summary>
	void OnStop_SetWeapon()
	{

		m_TargetAmmoOffset = 10;

	}


	/// <summary>
	/// reset health counter upon respawn
	/// </summary>
	void OnStop_Dead()
	{

		m_CurrentHealthOffset = -200;
		m_TargetHealthOffset = 0;
		HealthColor = Color.white;

	}


}

