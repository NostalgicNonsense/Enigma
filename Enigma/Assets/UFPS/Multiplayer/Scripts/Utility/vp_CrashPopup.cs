/////////////////////////////////////////////////////////////////////////////////
//
//	vp_CrashPopup.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	this popup will darken the screen and alert the user at the first
//					sign of instability (defined as any and all exceptions and error
//					logs). as long as this component is present in the scene, the game
//					will halt at every error and players will be given the option of
//					copying error messages to the clipboard, or to quit or keep playing.
//
//					this can be an indispensable tool when running the game in standalone
//					builds where you may not otherwise notice the fact that an exception
//					has occured 'under the hood', resulting in peculiar and hard-to-find
//					bugs.
//
//					NOTES:
//					1) playing after a crash has occured is generally a very bad idea
//						(especially for testers) since the initial crash will sometimes
//						cause additional bugs that would not otherwise have occured and
//						may not really need fixing.
//					2) this script works on EDITOR and STANDALONE platforms.
//						note that EDITOR is disabled by default
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System;
using System.Diagnostics;

public class vp_CrashPopup : MonoBehaviour
{

	// editor pause detection
	protected int m_PauseFrameCounter = 0;
	protected bool m_IsPaused { get { return m_PauseFrameCounter > 0; } }

	// error message / exception
	protected string m_Message = "";
	protected string m_LineInfo = "";

	// gui
	protected Rect m_WindowRect = new Rect(0, 0, 600, 400);
	protected float m_Padding = 20;
	protected Texture2D m_BlackTexture = null;

	// logic
	protected static bool m_ThereHasBeenACrash = false;
	protected bool m_CursorWasForced = false;
	protected bool m_InputWasAllowed = false;
	protected bool m_ChatWasEnabled = false;
	protected float m_NextAllowedShowKeepPlayingButtonTime = 0.0f;
	protected bool m_CanShowKeepPlayingButton = true;

	// applicable environments
	public bool ShowInEditor = false;
	public bool ShowInStandalone = true;
#if UNITY_5_3 || UNITY_5_4 || UNITY_5_5 || UNITY_5_6 || UNITY_2017_1
    public bool ShowInWebplayer = true;
#endif
    public bool ShowOnConsole = true;
	public bool ShowOnMobile = true;

	// --- external components that need to be suppressed ---

	protected vp_FPInput m_FPInput = null;
	protected vp_FPInput FPInput
	{
		get
		{
			if (m_FPInput == null)
				m_FPInput = FindObjectOfType<vp_FPInput>();
			return m_FPInput;
		}
	}

	// TODO: can't reference chat since it's not in Base
	protected vp_MPDemoChat m_Chat = null;
	protected vp_MPDemoChat Chat
	{
		get
		{
			if ((m_Chat == null) && !m_TriedToFindChat)
				m_Chat = FindObjectOfType<vp_MPDemoChat>();
			return m_Chat;
		}
	}
	protected bool m_TriedToFindChat = false;


	/// <summary>
	/// 
	/// </summary>
	protected void OnEnable()
	{

		if (Active)
			Application.logMessageReceived += HandleLog;

		vp_GlobalEvent<bool>.Register("EnableErrorDialog", (bool enable)=> { enabled = enable;  });

	}


	/// <summary>
	/// 
	/// </summary>
	protected void OnDisable()
	{

		if (Active)
			Application.logMessageReceived -= HandleLog;

		vp_GlobalEvent<bool>.Unregister("EnableErrorDialog", (bool enable) => { enabled = enable; });

	}


	/// <summary>
	/// 
	/// </summary>
	protected void Start()
	{

		m_BlackTexture = new Texture2D(1, 1);
		m_BlackTexture.SetPixel(0, 0, new Color(0, 0, 0, 1));

	}
	

	/// <summary>
	/// 
	/// </summary>
	public void HandleLog(string logString, string stackTrace, LogType type)
	{

		if (m_ThereHasBeenACrash)
			return;

		m_CanShowKeepPlayingButton = (Time.time > m_NextAllowedShowKeepPlayingButtonTime);	// don't allow 'keep playing' if unity keeps crashing every frame

		if ((type != LogType.Error) && (type != LogType.Exception))
			return;

		// init window rect with current screen res
		m_WindowRect.x = (Screen.width * 0.5f) - (m_WindowRect.width * 0.5f);
		m_WindowRect.y = (Screen.height * 0.5f) - (m_WindowRect.height * 0.5f);

		// handle editor pause
		m_PauseFrameCounter = 2;
		m_ThereHasBeenACrash = true;

		// store error message
		m_Message = logString;
		m_LineInfo = stackTrace;
		if (!string.IsNullOrEmpty(m_LineInfo))
		{
			m_LineInfo = m_LineInfo.Remove(0, m_LineInfo.LastIndexOf("/") + 1);
#if UNITY_EDITOR
			m_LineInfo = m_LineInfo.Remove(m_LineInfo.LastIndexOf(")"));
#endif
		}

		// suppress external components
		if (FPInput != null)
		{
			m_CursorWasForced = FPInput.MouseCursorForced;
			m_InputWasAllowed = FPInput.AllowGameplayInput;
		}
		if (Chat != null)
			m_ChatWasEnabled = Chat.enabled;

	}


	/// <summary>
	/// 
	/// </summary>
	public bool Active
	{

		get
		{
			if ((Application.isEditor && !ShowInEditor)
#if UNITY_5_3 || UNITY_5_4 || UNITY_5_5 || UNITY_5_6 || UNITY_2017_1
                || (Application.isWebPlayer && !ShowInWebplayer)
#endif
                || (Application.isMobilePlatform && !ShowOnMobile)
				|| (Application.isConsolePlatform && !ShowOnConsole))
				return false;

			if ((!Application.isEditor)
#if UNITY_5_3 || UNITY_5_4 || UNITY_5_5 || UNITY_5_6 || UNITY_2017_1
                && (!Application.isWebPlayer)
#endif
                && (!Application.isMobilePlatform)
				&& (!Application.isConsolePlatform)
				&& !ShowInStandalone)
				return false;

			return true;
		}

	}


	/// <summary>
	/// 
	/// </summary>
	protected void Update()
	{

		// this is needed to detect the editor pause state from outside an editor class
		if (m_IsPaused)
			m_PauseFrameCounter--;

	}


	/// <summary>
	/// 
	/// </summary>
	protected void OnGUI()
	{

		GUI.depth = 100;
		if (!m_ThereHasBeenACrash)
			return;

		GUI.color = Color.black;
		GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), m_BlackTexture);
		GUI.color = Color.white;
		m_WindowRect = GUI.Window(0, m_WindowRect, WindowFunc, "BOOM!");

		if (FPInput != null)
		{
			FPInput.MouseCursorForced = true;
			FPInput.AllowGameplayInput = false;
		}
		if (Chat != null)
			Chat.enabled = false;

	}


	/// <summary>
	/// 
	/// </summary>
	protected void WindowFunc(int windowID)
	{

		string errorMessage = m_Message + (!string.IsNullOrEmpty(m_LineInfo) ? " --> " + m_LineInfo : "");

		GUI.Label(new Rect(m_Padding, m_Padding, m_WindowRect.width - (m_Padding * 2), m_WindowRect.height - (m_Padding * 2) + 10),
			"There has been a CRASH and the game is running in a BAD STATE!\n\n" +
			"Message:\n" + errorMessage);

		float x = m_Padding;
		float buttonwidth = ((m_WindowRect.width - (m_Padding)) / 3);

#if UNITY_EDITOR
		if (m_IsPaused)
		{
			GUI.Label(new Rect(x, m_WindowRect.height - (m_Padding * 3) - 10, m_WindowRect.width, 20), "(Unpause Editor to activate buttons)");
			GUI.color = new Color(1, 1, 1, 0.7f);
			GUI.enabled = false;
		}
#endif

		// --- button: 'Copy to Clipboard' ---
		if (DrawButton(x, buttonwidth, "Copy to Clipboard"))
			CopyToClipboard(errorMessage);
		x += buttonwidth;

		// --- button: 'Keep Playing' ---
		if (m_CanShowKeepPlayingButton)
		{
			if (DrawButton(x, buttonwidth * 1.3f, "Keep Playing (not recommended)"))
			{
				m_NextAllowedShowKeepPlayingButtonTime = Time.time + 0.1f;	// any crash dialog spawning within this timeframe will be unable to keep playing
				Reset();
			}
		}
		else
			DrawLabel(x, buttonwidth * 1.3f, "Unable to keep playing.");

		x += buttonwidth * 1.3f;

		// --- button: 'Quit' ---
		if (DrawButton(x, buttonwidth * 0.7f, "Quit"))
			vp_Gameplay.Quit();

#if UNITY_EDITOR
		GUI.color = Color.white;
		GUI.enabled = true;
#endif

	}


	/// <summary>
	/// 
	/// </summary>
	protected void CopyToClipboard(string s)
	{

		TextEditor te = new TextEditor();
        te.text = s;
        te.SelectAll();
		te.Copy();
	
	}


	/// <summary>
	/// 
	/// </summary>
	public void Reset()
	{

		m_ThereHasBeenACrash = false;
		
		if (FPInput != null)
		{
			FPInput.AllowGameplayInput = true; // m_InputWasAllowed		// ('true' probably works better in most cases)
			FPInput.MouseCursorForced = m_CursorWasForced;
		}

		if (Chat != null)
			Chat.enabled = true; // m_ChatWasEnabled		// ('true' probably works better in most cases)

		m_TriedToFindChat = false;

	}



	/// <summary>
	/// 
	/// </summary>
	protected bool DrawButton(float x, float buttonWidth, string caption)
	{
		return GUI.Button(new Rect(x, m_WindowRect.height - (m_Padding * 2), buttonWidth - m_Padding, 20), caption);
	}


	/// <summary>
	/// 
	/// </summary>
	protected void DrawLabel(float x, float buttonWidth, string caption)
	{
		GUIStyle style = new GUIStyle("Label");
		style.alignment = TextAnchor.MiddleCenter;
		GUI.Label(new Rect(x, m_WindowRect.height - (m_Padding * 2), buttonWidth - m_Padding, 20), caption, style);
	}


}


