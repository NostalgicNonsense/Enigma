/////////////////////////////////////////////////////////////////////////////////
//
//	vp_NameTag.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	an OnGUI-based screen space nametag for any object with a
//					renderer. will fade out if obscured by objects or going out
//					of range
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class vp_NameTag : Photon.MonoBehaviour
{
		
	// data
	public string Text = "Unnamed";							// name that gets rendered to the screen
	protected string m_RefreshedText = "";					// used to detect runtime text changes, for refreshing label size

	// transform & rendering
	public float WorldHeightOffset = 0.3f;	// vertical offset from transform in world coordinates
	public float MaxViewDistance = 1000.0f;	// distance at which nametag will fade away. if zero, view distance is unlimited
	public bool Visible = true;				// if true, TargetAlpha will attempt to be 1, otherwise 0
	public bool VisibleThroughObstacles = false;	// if true, nametag can be seen through walls

	protected float m_Distance = 0.0f;						// current distance from camera to target
	protected Renderer m_Renderer = null;					// first renderer on target transform. used to determine if object is visible
	protected Vector3 m_ScreenPos = Vector3.zero;			// screen space position of nametag
	protected Vector2 m_ScreenSize = new Vector2(100, 25);	// screen space scale of nametag
	protected Rect m_NameRect = new Rect();					// rectangles for GUI drawing
	protected Rect m_OutlineRect = new Rect();
	
	// font
	public Font m_Font;							// NOTE: can not be altered at runtime
	public int m_FontSize = 14;					// size of nametag font
	public bool Outline = false;				// primitive outline (not for mobile use)
	public bool DropShadow = true;				// primitive dropshadow (not for mobile use)
	protected int m_RefreshedFontSize = 0;		// used to detect runtime font changes, for refreshing screen size (useful for development only)
	protected GUIStyle m_NameTagStyle = null;	// NOTE: don't use this directly. instead, use its property below
	public GUIStyle NameTagStyle				// nametag runtime generated GUI style
	{
		get
		{
			if (m_NameTagStyle == null)
			{
				m_NameTagStyle = new GUIStyle("Label");
				m_NameTagStyle.font = m_Font;
				m_NameTagStyle.alignment = TextAnchor.LowerCenter;
				m_NameTagStyle.fontSize = m_FontSize;
			}
			return m_NameTagStyle;
		}
	}

	// colors
	public Color Color = new Color(1, 1, 1, 1);				// main text color
	public Color OutlineColor = new Color(0, 0, 0, 1);		// outline and dropshadow color
	[HideInInspector]
	public float Alpha = 0.0f;					// current main opacity. this can be set remotely to snap alpha
	protected float TargetAlpha = 1.0f;			// opacity that we are fading towards
	protected float OutlineAlpha = 0.0f;		// opacity of the outline (this is kept slightly lower while fading out)
	protected float FadeOutSpeed = 3.0f;		// for when going out of range or getting obscured
	protected float FadeInSpeed = 4.0f;			// for when arriving within range or being revealed


	/// <summary>
	/// fetches the first renderer in this transform, whose visibility
	/// we'll be checking against
	/// </summary>
	void Start()
	{

		m_Renderer = transform.root.GetComponentInChildren<Renderer>();

		PhotonView p = transform.root.GetComponentInChildren<PhotonView>();
		if (p != null)
		{
			Text = vp_MPNetworkPlayer.GetName(p.ownerId);	// TODO: don't set directly (?)
			return;
		}

		// SNIPPET: for use showing itempickup IDs
		//vp_ItemPickup v = transform.root.GetComponentInChildren<vp_ItemPickup>();
		//if (v != null)
		//{
		//    Text = v.ID.ToString();
		//    return;
		//}

	}


	/// <summary>
	/// 
	/// </summary>
	void OnGUI()
	{

		if (!UpdateVisibility())
			return;	// abort rendering when object goes off screen

		if (!UpdateFade())
			return; // abort rendering when alpha goes zero

		// we have stuff to draw, yay!

		RefreshText();

		DrawDropShadow();

		DrawOutline();

		DrawText();


	}


	/// <summary>
	/// recalculates the screen rectangle for the label and refreshes
	/// font size whenever the text or font size changes
	/// </summary>
	void RefreshText()
	{

		if ((Text == m_RefreshedText) && (m_FontSize == m_RefreshedFontSize))
			return;

		if (m_RefreshedFontSize != m_FontSize)
			m_NameTagStyle = null;

		m_ScreenSize = NameTagStyle.CalcSize(new GUIContent(Text));
		//m_NameTagStyle.normal.textColor = m_Color;	// TODO: use if color breaks
		m_OutlineRect = m_NameRect = new Rect(0, 0, m_ScreenSize.x, m_ScreenSize.y);

		m_RefreshedText = Text;
		m_RefreshedFontSize = m_FontSize;

	}


	/// <summary>
	/// detects whether the nametag should be drawn (it might be
	/// off-screen, obscured or beyond the view distance) and
	/// swaps the target alpha between 0 and 1 accordingly.
	/// returns false if rendering should be aborted altogether.
	/// </summary>
	bool UpdateVisibility()
	{

		// NOTE: if object goes off screen we don't fade out but simply kill
		// rendering. however if it goes out of range or gets obscured,
		// we allow rendering while fading out

		TargetAlpha = (Visible ? 1.0f : 0.0f);

		// check if object is on-screen
		if (!vp_3DUtility.OnScreen(Camera.main, m_Renderer, transform.position + (Vector3.up * WorldHeightOffset), out m_ScreenPos))
		{
			TargetAlpha = 0.0f;
			return false;	// nothing to render, abort
		}
		
		// if we have a view distance, check if object is within range
		if ((MaxViewDistance > 0.0f) && !vp_3DUtility.WithinRange(Camera.main.transform.position, transform.position, MaxViewDistance, out m_Distance))
			TargetAlpha = 0.0f;

		// if this nametag can be obscured, validate line of sight
		if (!VisibleThroughObstacles && !vp_3DUtility.InLineOfSight(Camera.main.transform.position, transform,
										(Vector3.up * WorldHeightOffset), vp_Layer.Mask.ExternalBlockers))
			TargetAlpha = 0.0f;

		return true;

	}


	/// <summary>
	/// calculates a new alpha value depending on whether we're fading
	/// in or out. NOTE: if this method returns zero it means the
	/// nametag is invisible and should not be rendered
	/// </summary>
	bool UpdateFade()
	{

		if (TargetAlpha > Alpha)	// fading in
		{
			Alpha = vp_MathUtility.SnapToZero(vp_MathUtility.ReduceDecimals(Mathf.Lerp(Alpha, TargetAlpha, Time.deltaTime * FadeInSpeed)), 0.01f);
			OutlineAlpha = Alpha;
		}
		else if (TargetAlpha < Alpha)	// fading out
		{
			Alpha = vp_MathUtility.SnapToZero(vp_MathUtility.ReduceDecimals(Mathf.Lerp(Alpha, TargetAlpha, Time.deltaTime * FadeOutSpeed)), 0.01f);

			// because of the rather primitive outline solution, fading out does not look
			// good with outline/dropshadow so we calculate a lower alpha value here
			OutlineAlpha = Mathf.Max(0.0f, (Alpha - (1.0f - (Mathf.SmoothStep(0, 1, Alpha)))));
		}

		if (Alpha <= 0.0f)
			return false;

		return true;

	}


	/// <summary>
	/// draws the text
	/// </summary>
	void DrawText()
	{

		Color.a = Alpha;
		GUI.color = Color;
		m_NameRect.x = (m_ScreenPos.x - (m_ScreenSize.x * 0.5f));
		m_NameRect.y = (Screen.height - m_ScreenPos.y) - m_FontSize;
		GUI.Label(m_NameRect, Text, NameTagStyle);
		GUI.color = Color.white;

	}


	/// <summary>
	/// this is a brute-force outline solution and nothing for the
	/// faint-hearted ;). should work with no issues on desktop,
	/// but you may want to disable this in mobile projects
	/// </summary>
	void DrawOutline()
	{

		if (!Outline)
			return;

		OutlineColor.a = OutlineAlpha;
		GUI.color = OutlineColor;

		m_OutlineRect.x = m_NameRect.x - 1;
		m_OutlineRect.y = m_NameRect.y;
		GUI.Label(m_OutlineRect, Text, NameTagStyle);

		m_OutlineRect.y = m_NameRect.y - 1;
		GUI.Label(m_OutlineRect, Text, NameTagStyle);

		m_OutlineRect.x = m_NameRect.x;
		GUI.Label(m_OutlineRect, Text, NameTagStyle);

		m_OutlineRect.x = m_NameRect.x + 1;
		GUI.Label(m_OutlineRect, Text, NameTagStyle);

		m_OutlineRect.y = m_NameRect.y;
		GUI.Label(m_OutlineRect, Text, NameTagStyle);

		m_OutlineRect.y = m_NameRect.y + 1;
		GUI.Label(m_OutlineRect, Text, NameTagStyle);

		m_OutlineRect.x = m_NameRect.x;
		GUI.Label(m_OutlineRect, Text, NameTagStyle);

		m_OutlineRect.x = m_NameRect.x - 1;
		GUI.Label(m_OutlineRect, Text, NameTagStyle);


	}


	/// <summary>
	/// draws a drop shadow. should work with no issues on desktop,
	/// but you may want to disable this in mobile projects
	/// </summary>
	void DrawDropShadow()
	{

		if (!DropShadow)
			return;

		OutlineColor.a = OutlineAlpha * 0.5f;
		GUI.color = OutlineColor;

		m_OutlineRect.x = m_NameRect.x + (Outline ? 2 : 1);
		m_OutlineRect.y = m_NameRect.y + (Outline ? 2 : 1);
		GUI.Label(m_OutlineRect, Text, NameTagStyle);

		m_OutlineRect.x = m_NameRect.x + (Outline ? 2 : 1) - 1;
		m_OutlineRect.y = m_NameRect.y + (Outline ? 2 : 1);
		GUI.Label(m_OutlineRect, Text, NameTagStyle);

	}


}