/////////////////////////////////////////////////////////////////////////////////
//
//	vp_DMDemoScoreBoard.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	a basic, classic team deathmatch scoreboard. can be used for
//					debugging purposes (you can enable any player stat in the list).
//					provided for demo purposes only
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;


public class vp_DMDemoScoreBoard : Photon.MonoBehaviour
{

	public Font Font;							// NOTE: can not be altered at runtime
	public int TextFontSize = 14;		
	public int CaptionFontSize = 25;
	public int TeamNameFontSize = 35;
	public int TeamScoreFontSize = 50;	
	public Texture Background = null;

	protected float m_NameColumnWidth = 150;
	protected float m_Margin = 20;
	protected float m_Padding = 10;

	protected bool m_StatNamesChecked = false;
	protected Rect labelRect = new Rect(0, 0, 0, 0);
	protected Rect shadowRect = new Rect(0, 0, 0, 0);
	protected Rect m_BGRect;

	protected Vector2 m_Pos = Vector2.zero;
	protected Vector2 m_Size = Vector2.zero;
	
	protected Color m_CaptionBGColor = new Color(0, 0, 0, 0.5f);
	protected Color m_TranspBlack = new Color(0, 0, 0, 0.5f);
	protected Color m_TranspWhite = new Color(1, 1, 1, 0.5f);
	protected Color m_TranspCyan = new Color(0, 0.8f, 0.8f, 0.5f);
	protected Color m_TranspBlackLine = new Color(0, 0, 0, 0.15f);
	protected Color m_TranspWhiteLine = new Color(1, 1, 1, 0.15f);
	protected Color m_CurrentRowColor;

	protected enum CaptionScoreSetting
	{
		None,
		Left,
		Right
	}

	// --- properties ---

	protected static bool m_ShowScore = false;
	public static bool ShowScore
	{
		get
		{
			return m_ShowScore;
		}
		set
		{
			m_ShowScore = value;
			if (Chat != null)
				Chat.Visible = !ShowScore;
			if(Crosshair != null)
				Crosshair.enabled = !ShowScore;
			if(HUD != null)
				HUD.ShowHUD = !ShowScore;
		}
	}

	static vp_MPDemoChat m_Chat = null;
	static vp_MPDemoChat Chat
	{
		get
		{
			if (m_Chat == null)
				m_Chat = Component.FindObjectOfType<vp_MPDemoChat>();
			return m_Chat;
		}
	}

	static vp_SimpleHUD m_HUD = null;
	static vp_SimpleHUD HUD
	{
		get
		{
			if (m_HUD == null)
				m_HUD = Component.FindObjectOfType<vp_SimpleHUD>();
			return m_HUD;
		}
	}

	static vp_SimpleCrosshair m_Crosshair = null;
	static vp_SimpleCrosshair Crosshair
	{
		get
		{
			if (m_Crosshair == null)
				m_Crosshair = Component.FindObjectOfType<vp_SimpleCrosshair>();
			return m_Crosshair;
		}
	}

	// NOTE: all of the stats in the 'VisibleStatNames' list must be present in
	// 'vp_MPLocalPlayer.Instance.Stats' except 'Ping' which is stored in Photon's
	// custom player prefs (as opposed to the UFPSMP player state)
	public List<string> m_VisibleStatNames = new List<string>(new string[]	{ "Ping", "Score", "Frags", "Deaths", "Shots"	});
	protected List<string> VisibleStatNames
	{
		get
		{
			if (!m_StatNamesChecked)
			{
				if (m_VisibleStatNames == null)
					m_VisibleStatNames = new List<string>();

				if (m_VisibleStatNames.Count > 0)
				{
					if (vp_MPLocalPlayer.Instance != null)
					{
						for (int v = m_VisibleStatNames.Count - 1; v > -1; v--)
						{
							if ((m_VisibleStatNames[v] != "Ping") && !vp_MPLocalPlayer.Instance.Stats.Names.Contains(m_VisibleStatNames[v]))
								m_VisibleStatNames.Remove(m_VisibleStatNames[v]);
						}
					}
				}
				m_StatNamesChecked = true;
			}
			return m_VisibleStatNames;
		}
	}

	
	/// <summary>
	/// 
	/// </summary>
	void Update()
	{

		UpdateInput();

	}


	/// <summary>
	/// 
	/// </summary>
	void UpdateInput()
	{

		if (vp_MPMaster.Phase != vp_MPMaster.GamePhase.Playing)
			return;

		ShowScore = (vp_Input.GetButton("ScoreBoard"));

	}


	/// <summary>
	/// 
	/// </summary>
	protected float GetColumnWidth(float tableWidth)
	{
		return ((tableWidth - m_NameColumnWidth) / VisibleStatNames.Count);
	}


	/// <summary>
	/// 
	/// </summary>
	void DrawLabel(string text, Vector2 position, Vector2 scale, GUIStyle textStyle, Color textColor, Color bgColor, bool dropShadow = false)
	{

		if (scale.x == 0)
			scale.x = textStyle.CalcSize(new GUIContent(text)).x;
		if (scale.y == 0)
			scale.y = textStyle.CalcSize(new GUIContent(text)).y;

		labelRect.x = m_Pos.x = position.x;
		labelRect.y = m_Pos.y = position.y;
		labelRect.width = m_Size.x = scale.x;
		labelRect.height = m_Size.y = scale.y;
		
		if (bgColor != Color.clear)
		{
			GUI.color = bgColor;
			GUI.DrawTexture(labelRect, Background);
		}


		if (dropShadow)
		{
			GUI.color = Color.Lerp(bgColor, Color.black, 0.5f);
			shadowRect = labelRect;
			shadowRect.x += scale.y * 0.1f;
			shadowRect.y += scale.y * 0.1f;
			GUI.Label(shadowRect, text, textStyle);
		}

		GUI.color = textColor;
		GUI.Label(labelRect, text, textStyle);
		GUI.color = Color.white;

		m_Pos.x += m_Size.x;
		m_Pos.y += m_Size.y;

	}


	/// <summary>
	/// 
	/// </summary>
	void DrawLabel(string text, Vector2 pos)
	{
		DrawLabel(text, pos, Vector2.zero, TextStyle, Color.white, Color.clear);
	}


	/// <summary>
	/// 
	/// </summary>
	void DrawTeam(Vector2 position, Vector2 scale, vp_MPTeam team)
	{

		Color col = (team != null ? team.Color * (Color.white * 0.35f) : Color.white);
		col.a = 0.75f;
		col.a = 0.75f;

		if ((team != null) && vp_MPTeamManager.Exists && vp_MPTeamManager.TeamCount > 1)
		{
			if (vp_MathUtility.IsOdd(team.Number))
			{
				TeamNameStyle.alignment = TextAnchor.MiddleLeft;
				TeamScoreStyle.alignment = TextAnchor.MiddleRight;
			}
			else
			{
				TeamNameStyle.alignment = TextAnchor.MiddleRight;
				TeamScoreStyle.alignment = TextAnchor.MiddleLeft;
			}
			DrawLabel(team.Name.ToUpper(), position, scale, TeamNameStyle, Color.white, col, true);
			if(team is vp_DMTeam)
				DrawLabel((team as vp_DMTeam).Score.ToString(), position, scale, TeamScoreStyle, Color.white, Color.clear, true);
			scale.y = m_Size.y;
			m_Pos.y -= m_Padding;
		}

		m_Pos.x = position.x;
		m_Size.y = Screen.height - m_Pos.y - m_Margin;
		DrawLabel("", m_Pos, m_Size, TextStyle, Color.clear, m_TranspBlack);
		m_Pos.y = position.y + scale.y;
		DrawTopRow(new Vector2(position.x, m_Pos.y), scale);
		m_CurrentRowColor = m_TranspBlack;

		foreach (vp_MPNetworkPlayer p in vp_MPNetworkPlayer.Players.Values)
		{
			if (p == null)
				continue;
			if ((team == null) || p.TeamNumber == team.Number)
			{
				DrawPlayerRow(p, m_Pos + (Vector2.up * m_Size.y), new Vector2(scale.x, m_Size.y));
			}
		}

	}


	/// <summary>
	/// 
	/// </summary>
	void DrawTopRow(Vector2 position, Vector2 scale)
	{

		m_Pos = position;
		m_Pos.x += scale.x - m_Padding - GetColumnWidth(scale.x);

		foreach (string s in VisibleStatNames)
		{
			DrawStatLabel(s, m_Pos, scale);
		}

		DrawStatLabel("Name", position + (Vector2.right * m_Padding), scale);

		m_Pos.x = position.x;
		m_Size.y = 20;

	}


	/// <summary>
	/// 
	/// </summary>
	void DrawPlayerRow(vp_MPNetworkPlayer p, Vector2 position, Vector2 scale)
	{

		m_CurrentRowColor = ((m_CurrentRowColor == m_TranspWhiteLine) ? m_TranspBlackLine : m_TranspWhiteLine);
		if (p.photonView.owner == PhotonNetwork.player)
			m_CurrentRowColor = m_TranspCyan;
		DrawLabel(vp_MPNetworkPlayer.GetName(p.photonView.ownerId), position, scale, PlayerTextStyle, Color.white, m_CurrentRowColor);
		
		m_Pos = position;
		m_Pos.x += scale.x - m_Padding - GetColumnWidth(scale.x);

		foreach (string s in VisibleStatNames)
		{

			if (s == "Ping")
			{
				DrawStatLabel(p.Ping.ToString(), m_Pos, scale);
				continue;
			}

			object stat = p.Stats.Get(s);
			string statOut = stat.ToString();
			DrawStatLabel(statOut.ToString(), m_Pos, scale);
	
		}


		m_Pos.x = position.x;
		m_Size.y = 20;

	}
	
	
	/// <summary>
	/// 
	/// </summary>
	void DrawStatLabel(string statName, Vector2 position, Vector2 scale)
	{
		
		DrawLabel(statName, position);
		m_Pos = position;
		m_Pos.x -= GetColumnWidth(scale.x);

	}
	

	/// <summary>
	/// NOTE: scoreboard does not access game state for data, but
	/// instead fetches it from vp_MPNetworkPlayer properties
	/// </summary>
	void OnGUI()
	{

		if (!ShowScore)
			return;

		m_Pos = Vector2.zero;
		m_Size = Vector2.zero;

		DrawCaption();

		DrawTeams();
		
	}


	/// <summary>
	/// 
	/// </summary>
	void DrawTeams()
	{

		Vector2 tPos = new Vector2(m_Margin, m_Pos.y);
		if (Screen.width > 1200)
			tPos.x += (Screen.width - 1200) * 0.5f;
		Vector2 tSize = m_Size;

		if (vp_MPTeamManager.Exists && vp_MPTeamManager.TeamCount > 1)
		{
			foreach (vp_MPTeam t in vp_MPTeamManager.Instance.Teams)
			{
				m_Size.x =
					((Mathf.Min(Screen.width, 1200)) - (m_Margin * 2))
					/ (vp_MPTeamManager.TeamCount - (vp_MPTeamManager.TeamCount < 3 ? 0 : 1))
					- (vp_MPTeamManager.TeamCount < 3 ? 0 : (m_Padding * 0.5f))
					;
				m_Size.y = 0;
				tSize = m_Size;
				if (t.Number > 0)
				{
					DrawTeam(tPos, tSize, t);
					tPos += tSize + ((Vector2.right * (m_Padding * 0.5f)) * 2);
				}
			}
		}
		else 
		{
			m_Size.x = ((Mathf.Min(Screen.width, 1200)) - (m_Margin * 2));
			m_Size.y = 0;
			tSize = m_Size;
			DrawTeam(tPos, tSize, (vp_MPTeamManager.Exists ? vp_MPTeamManager.Instance.Teams[0] : null));
			tPos += tSize + ((Vector2.right * (m_Padding * 0.5f)) * 2);
		}

	}
		

	/// <summary>
	/// 
	/// </summary>
	void DrawTime()
	{
		float bx = m_Pos.x;
		string s = ((vp_MPMaster.Phase == vp_MPMaster.GamePhase.Playing) ?
			"Time Left: " :
			"Next game starts in: ");
		m_Pos.x = 630 - TextStyle.CalcSize(new GUIContent(s)).x;
		DrawLabel(s + GetFormattedTime(vp_MPClock.TimeLeft)
			//+ " / " + GetFormattedTime(vp_MasterClient.GameDuration)			// SNIPPET: uncomment to also show total game duration
		, m_Pos);
		m_Pos.y += 30;
		m_Pos.x = bx;
	}


	/// <summary>
	/// 
	/// </summary>
	void DrawPlayers()
	{
		
		DrawLabel("Player", m_Pos);

		m_Pos.x = 200;

		foreach (string s in VisibleStatNames)
		{
			DrawLabel(s, m_Pos);
			m_Pos.x += 100;
		}

		m_Pos.x = 200;
		m_Pos.y += 30;

		foreach (int playerID in vp_MPNetworkPlayer.IDs)	// TODO: must be sorted (see old property above)
		{

			DrawLabel(vp_MPNetworkPlayer.GetName(playerID), new Vector2(100, m_Pos.y));

			foreach (string s in VisibleStatNames)
			{

				object stat = vp_MPNetworkPlayer.Get(playerID).Stats.Get(s);
				string statOut = stat.ToString();


				DrawLabel(statOut.ToString(), m_Pos);
				m_Pos.x += 100;
			}
			m_Pos.x = 200;
			m_Pos.y += 20;
		}

	}


	/// <summary>
	/// 
	/// </summary>
	void DrawCaption()
	{

		m_Pos.y = m_Margin;
		m_Size.y = 0;

		if(vp_MPMaster.Phase == vp_MPMaster.GamePhase.BetweenGames)
		{
			m_Pos.x = m_Margin;
			if (Screen.width > 1200)
				m_Pos.x += (Screen.width - 1200) * 0.5f;
			m_Size.x = 250;
			DrawLabel("GAME OVER", m_Pos, m_Size, CaptionStyle, Color.yellow, m_CaptionBGColor, true);
			m_Pos.x = m_Margin;
			if (Screen.width > 1200)
				m_Pos.x += (Screen.width - 1200) * 0.5f;
			m_Size.x = 250;
			DrawLabel("Next game in: " + GetFormattedTime(vp_MPClock.TimeLeft), m_Pos, m_Size, CenteredTextStyle, Color.white, m_CaptionBGColor);
		}
		else
		{
			m_Pos.x = m_Margin;
			if (Screen.width > 1200)
				m_Pos.x += (Screen.width - 1200) * 0.5f;
			m_Size.x = 100;
			DrawLabel(GetFormattedTime(vp_MPClock.TimeLeft), m_Pos, m_Size, CaptionStyle, Color.white, m_CaptionBGColor, true);
		}

		m_Pos.y += m_Margin;

	}
	

	/// <summary>
	/// TODO: move to timeutility?
	/// </summary>
	string GetFormattedTime(float t)
	{

		t = Mathf.Max(0, t);
		int hours = ((int)t) / 3600;
		int minutes = (((int)t) - (hours * 3600)) / 60;
		int seconds = ((int)t) % 60;
		string s = "";
		s += (hours > 0) ? hours.ToString() + ":" : "";
		s += ((minutes > 0) ? (minutes < 10) ? "0" + minutes + ":" : minutes + ":" : "");
		s += (seconds < 10) ? "0" + seconds : seconds.ToString();
		return s;

	}


	/// <summary>
	/// 
	/// </summary>
	void OnJoinedRoom()
	{

		vp_Timer.In(0.99f, delegate()
		{
			vp_MPDebug.Log("Press TAB for SCOREBOARD");
		});

	}

	
	// -------- GUI styles --------

	public GUIStyle TextStyle
	{
		get
		{
			if (m_TextStyle == null)
			{
				m_TextStyle = new GUIStyle("Label");
				m_TextStyle.font = Font;
				m_TextStyle.alignment = TextAnchor.UpperLeft;
				m_TextStyle.fontSize = TextFontSize;
				m_TextStyle.wordWrap = false;
			}
			return m_TextStyle;
		}
	}
	protected GUIStyle m_TextStyle = null;

	public GUIStyle PlayerTextStyle
	{
		get
		{
			if (m_PlayerTextStyle == null)
			{
				m_PlayerTextStyle = new GUIStyle("Label");
				m_PlayerTextStyle.font = Font;
				m_PlayerTextStyle.alignment = TextAnchor.MiddleLeft;
				m_PlayerTextStyle.fontSize = TextFontSize;
				m_PlayerTextStyle.wordWrap = false;
				m_PlayerTextStyle.padding.left = 5;
			}
			return m_PlayerTextStyle;
		}
	}
	protected GUIStyle m_PlayerTextStyle = null;

	public GUIStyle CenteredTextStyle
	{
		get
		{
			if (m_CenteredTextStyle == null)
			{
				m_CenteredTextStyle = new GUIStyle("Label");
				m_CenteredTextStyle.font = Font;
				m_CenteredTextStyle.fontSize = TextFontSize;
				m_CenteredTextStyle.wordWrap = false;
				m_CenteredTextStyle.alignment = TextAnchor.MiddleCenter;
			}
			return m_CenteredTextStyle;
		}
	}
	protected GUIStyle m_CenteredTextStyle = null;

	public GUIStyle CaptionStyle
	{
		get
		{
			if (m_CaptionStyle == null)
			{
				m_CaptionStyle = new GUIStyle("Label");
				m_CaptionStyle.font = Font;
				m_CaptionStyle.alignment = TextAnchor.MiddleCenter;
				m_CaptionStyle.fontSize = CaptionFontSize;
				m_CaptionStyle.wordWrap = false;
				m_CaptionStyle.padding.top = 10;
			}
			return m_CaptionStyle;
		}
	}
	protected GUIStyle m_CaptionStyle = null;

	public GUIStyle TeamNameStyle
	{
		get
		{
			if (m_TeamNameStyle == null)
			{
				m_TeamNameStyle = new GUIStyle("Label");
				m_TeamNameStyle.font = Font;
				m_TeamNameStyle.alignment = TextAnchor.MiddleLeft;
				m_TeamNameStyle.fontSize = TeamNameFontSize;
				m_TeamNameStyle.wordWrap = false;
				m_TeamNameStyle.padding.left = 10;
				m_TeamNameStyle.padding.right = 10;
				m_TeamNameStyle.padding.top = 10;
			}
			return m_TeamNameStyle;
		}
	}
	protected GUIStyle m_TeamNameStyle = null;

	public GUIStyle TeamScoreStyle
	{
		get
		{
			if (m_TeamScoreStyle == null)
			{
				m_TeamScoreStyle = new GUIStyle("Label");
				m_TeamScoreStyle.font = Font;
				m_TeamScoreStyle.alignment = TextAnchor.MiddleLeft;
				m_TeamScoreStyle.fontSize = TeamScoreFontSize;
				m_TeamScoreStyle.wordWrap = false;
				m_TeamScoreStyle.padding.left = 10;
				m_TeamScoreStyle.padding.right = 10;
			}
			return m_TeamScoreStyle;
		}
	}
	protected GUIStyle m_TeamScoreStyle = null;


}