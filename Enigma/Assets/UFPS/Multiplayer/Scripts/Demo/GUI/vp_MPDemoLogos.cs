/////////////////////////////////////////////////////////////////////////////////
//
//	vp_MPDemoLogos.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	displays logo watermarks of the companies involved in bringing
//					you this amazing product =). for demo purposes and not in any
//					way intended for re-use or ease-of-use
//
/////////////////////////////////////////////////////////////////////////////////


using UnityEngine;
using System.Collections;

public class vp_MPDemoLogos : MonoBehaviour
{

	public Texture2D LogoUFPS = null;
	public Texture2D LogoPhoton = null;
	public Texture2D LogoProCore = null;
	public Texture2D LogoGameTextures = null;
	
	private Vector2 m_Pos = Vector2.zero;
	private Vector2 m_Size = Vector2.zero;
	private float m_Margin = 20;

	private Rect m_LogoRect = new Rect(0, 0, 0, 0);
	private Color m_TranspWhite = new Color(1, 1, 1, 0.5f);

		
	/// <summary>
	/// 
	/// </summary>
	private void OnGUI()
	{

		GUI.depth = 0;
		m_Margin = 20;

		// --- UFPS ---
		float y = 0;
		float s = 0.6f; 
		DrawLogo(new Vector2(Screen.width - (LogoUFPS.width * s), y), new Vector2(LogoUFPS.width * s, LogoUFPS.height * s), Style, Color.clear, m_TranspWhite, LogoUFPS);
		float x = Screen.width - (m_Margin * 1) - (LogoUFPS.width * s);

		// --- ProCore ---
		y = 2;
		s = 0.6f;
		DrawLogo(new Vector2(x - (LogoProCore.width * s), y), new Vector2(LogoProCore.width * s, LogoProCore.height * s), Style, Color.clear, m_TranspWhite, LogoProCore);
		x = Screen.width - (m_Margin * 2) - (LogoUFPS.width * s) - (LogoProCore.width * s);

		// --- Photon ---
		y = 0;
		s = 0.5f;
		DrawLogo(new Vector2(x - (LogoPhoton.width * s), y), new Vector2(LogoPhoton.width * s, LogoPhoton.height * s), Style, Color.clear, m_TranspWhite, LogoPhoton);

		// --- GameTextures ---
		x = Screen.width - (m_Margin * 3) - (LogoUFPS.width * 0.6f) - (LogoPhoton.width * 0.5f) - (LogoProCore.width * 0.6f) - 5;
		y = 8;
		s = 0.45f;
		DrawLogo(new Vector2(x - (LogoGameTextures.width * s), y), new Vector2(LogoGameTextures.width * s, LogoGameTextures.height * s), Style, Color.clear, m_TranspWhite, LogoGameTextures);
		
	}


	/// <summary>
	/// 
	/// </summary>
	private void DrawLogo(Vector2 position, Vector2 scale, GUIStyle textStyle, Color textColor, Color bgColor, Texture texture)
	{

		m_LogoRect.x = m_Pos.x = position.x;
		m_LogoRect.y = m_Pos.y = position.y;
		m_LogoRect.width = m_Size.x = scale.x;
		m_LogoRect.height = m_Size.y = scale.y;

		if (bgColor != Color.clear)
		{
			GUI.color = bgColor;
			GUI.DrawTexture(m_LogoRect, texture);
		}


		m_Pos.x += m_Size.x;
		m_Pos.y += m_Size.y;

		GUI.color = Color.white;

	}

	// -------- GUI style --------

	private GUIStyle m_Style = null;
	public GUIStyle Style
	{
		get
		{
			if (m_Style == null)
			{
				m_Style = new GUIStyle("Label");
			}
			return m_Style;
		}
	}

}
