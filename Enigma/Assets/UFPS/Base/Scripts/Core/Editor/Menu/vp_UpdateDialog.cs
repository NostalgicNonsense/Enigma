/////////////////////////////////////////////////////////////////////////////////
//
//	vp_UpdateDialog.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	a simple wizard to guide users through the asset update process
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class vp_UpdateDialog : EditorWindow
{

	// GUI
	private static Vector2 m_DialogSize = new Vector2(354, 146);
	public static Texture2D m_UFPSIcon = (Texture2D)Resources.Load("Icons/UFPS32x32", typeof(Texture2D));
	public static Texture2D m_InfoIcon = (Texture2D)Resources.Load("Icons/Info32x32", typeof(Texture2D));
	public Texture2D m_Icon = null;
	enum Mode
	{
		ShowVersions,
		AskPortal,
		AssetStoreInfoMode,
		OpsiveComInfoMode
	}
	Mode m_Mode = Mode.ShowVersions;
	private static GUIStyle m_AssetNameStyle = null;

	// remote info
	private static WWW m_InfoFile;
	private static string m_ReleaseNotesPath = "";
	private static string m_AssetStoreID = "";
	private static string m_AssetNameShort = "";
	private static string m_AssetNameFormal = "";
	private static string m_LatestVersion = null;
	private static string m_SpecialNote = null;
	private static bool m_InfoReady = false;

	// local info
	private static string m_AssetCode = "";
	private static string m_LocalVersion = "";


	/// <summary>
	/// 
	/// </summary>
	public static void Create(string assetCode, string localVersion)
	{

		m_InfoReady = false;
		m_LatestVersion = null;

		m_AssetCode = assetCode;
		m_LocalVersion = localVersion;

		vp_UpdateDialog window = (vp_UpdateDialog)EditorWindow.GetWindow(typeof(vp_UpdateDialog), true);

		window.titleContent.text = "Check for Updates";
		window.minSize = new Vector2(m_DialogSize.x, m_DialogSize.y);
		window.maxSize = new Vector2(m_DialogSize.x, m_DialogSize.y);
		window.position = new Rect(
			(Screen.currentResolution.width / 2) - (m_DialogSize.x / 2),
			(Screen.currentResolution.height / 2) - (m_DialogSize.y / 2),
			m_DialogSize.x,
			m_DialogSize.y);
		window.Show();

		window.m_Icon = m_UFPSIcon;

		m_ReleaseNotesPath = "http://www.opsive.com/assets/UFPS/hub/assets/" + m_AssetCode + "/releasenotes";

		m_InfoFile = new WWW("http://www.opsive.com/assets/UFPS/content/assets/" + m_AssetCode + "/info.txt");
		
	}
		

	/// <summary>
	/// 
	/// </summary>
	private void Update()
	{
				
		if ((m_InfoFile != null) && m_InfoFile.isDone && string.IsNullOrEmpty(m_InfoFile.error))
		{
			if (!m_InfoReady)
				ExtractInfo();
		}
		else if (!(m_InfoFile != null && !m_InfoFile.isDone) && string.IsNullOrEmpty(m_LatestVersion))
			ShowErrorLoadingDialog();

	}


	/// <summary>
	/// 
	/// </summary>
	private void ExtractInfo()
	{

		string[] splitLines = m_InfoFile.text.Split('\n');

		if(splitLines.Length < 4)
		{
			ShowErrorLoadingDialog();
			return;
		}

		m_AssetNameFormal = splitLines[0].Trim();
		m_AssetNameShort = splitLines[1].Trim();
		m_AssetStoreID = splitLines[2].Trim();
		m_LatestVersion = splitLines[3].Trim();
		if (splitLines.Length > 4)
		{
			m_SpecialNote = splitLines[4].Trim();
			if (m_SpecialNote.Contains("//"))
				m_SpecialNote = m_SpecialNote.Remove(m_SpecialNote.LastIndexOf("//"));
		}
		
		m_InfoReady = true;

		this.Repaint();

	}


	/// <summary>
	/// 
	/// </summary>
	private void OnGUI()
	{

		GUILayout.BeginHorizontal();
		GUILayout.Space(50);

		if (m_Icon != null)
			GUI.DrawTexture(new Rect(10, 10, 32, 32), m_Icon);

		switch (m_Mode)
		{

			case Mode.ShowVersions: NormalMode(); break;
			case Mode.AskPortal: PortalMode(); break;
			case Mode.AssetStoreInfoMode: AssetStoreInfoMode(); break;
			case Mode.OpsiveComInfoMode: OpsiveComInfoMode(); break;

		}

		GUILayout.Space(50);
		GUILayout.EndHorizontal();

	}


	/// <summary>
	/// 
	/// </summary>
	private void ShowErrorLoadingDialog()
	{

		m_LatestVersion = "fail";
		vp_MessageBox.Create(vp_MessageBox.Mode.OK, "Error", "Failed to fetch asset info. " + ((m_InfoFile != null) ? m_InfoFile.error : ""));
		this.Close();

	}


	/// <summary>
	/// 
	/// </summary>
	void NormalMode()
	{

		if (!string.IsNullOrEmpty(m_LatestVersion))
		{

			GUILayout.BeginVertical();

			if (m_LatestVersion != m_LocalVersion)
			{

				GUILayout.Label("There is a new " + m_AssetNameShort  + " version!");
				GUILayout.Label("(Yours: " + m_LocalVersion + "  ->  Latest: " + m_LatestVersion + ")");
				GUILayout.Space(10);
				if (GUILayout.Button("             View the Release Notes.", vp_EditorGUIUtility.LinkStyle))
					Application.OpenURL(m_ReleaseNotesPath);
				GUILayout.Space(10);
				GUILayout.BeginHorizontal();
				if (GUILayout.Button("\nUpdate Now\n"))
				{
					if (!string.IsNullOrEmpty(m_SpecialNote))
						ShowSpecialNoteDialog();
					else
						m_Mode = Mode.AskPortal;
				}
				if (GUILayout.Button("\nLater\n"))
					this.Close();
				GUILayout.EndHorizontal();
			}
			else
			{
				GUILayout.Label("Your " + m_AssetNameShort + " version is up to date.");
				GUILayout.Label("Latest version: " + m_LatestVersion);
				GUILayout.Space(15);
				if (GUILayout.Button("            View the Release Notes.", vp_EditorGUIUtility.LinkStyle))
					Application.OpenURL(m_ReleaseNotesPath);
				GUILayout.Space(10);
				GUILayout.BeginHorizontal();
				GUILayout.Space(40);
				if (GUILayout.Button("\nReimport\n"))
				{
					if (!string.IsNullOrEmpty(m_SpecialNote))
						ShowSpecialNoteDialog();
					else
						m_Mode = Mode.AskPortal;
				}
				if (GUILayout.Button("\nOK\n"))
					this.Close();
				GUILayout.Space(40);
				GUILayout.EndHorizontal();
			}

			GUILayout.EndVertical();

		}
		else
			GUILayout.Label("Hold on ...");


	}


	/// <summary>
	/// 
	/// </summary>
	void ShowSpecialNoteDialog()
	{
		string s = m_SpecialNote;
		m_SpecialNote = null;

		vp_MessageBox.Create(vp_MessageBox.Mode.YesNo, "A special note regarding " + m_AssetNameShort + " v." + m_LatestVersion.ToString(), "PLEASE NOTE: " + s + " Do you want to abort and view the release notes first?", delegate(vp_MessageBox.Answer answer)
		{
			if (answer == vp_MessageBox.Answer.Yes)
			{
				Application.OpenURL(m_ReleaseNotesPath);
				this.Close();
			}
			else if (answer == vp_MessageBox.Answer.No)
				m_Mode = Mode.AskPortal;
		});

	}


	/// <summary>
	/// 
	/// </summary>
	void ShowBackupDialog()
	{

	}
	bool haveShownImportantNote = false;

	/// <summary>
	/// 
	/// </summary>
	void PortalMode()
	{

		if (!haveShownImportantNote)
		{
			haveShownImportantNote = true;
			vp_MessageBox.Create(vp_MessageBox.Mode.YesNo, "IMPORTANT NOTE", "Before reimporting, please make a BACKUP of your project. Note that if you have modified any scripts, YOUR CHANGES WILL BE OVERWRITTEN. Are you sure?", delegate(vp_MessageBox.Answer answer)
			{
				if (answer == vp_MessageBox.Answer.No)
					m_Mode = Mode.ShowVersions;
			});
		}

		this.titleContent.text = "Choose Portal";

		GUILayout.BeginVertical();
		
		GUILayout.Space(10);
		GUILayout.Label("Where did you purchase the asset?");
		GUILayout.Space(40);
		GUILayout.BeginHorizontal();
		if (GUILayout.Button("\nUnity Asset Store\n"))
			m_Mode = Mode.AssetStoreInfoMode;
		if (GUILayout.Button("\nopsive.com\n"))
			m_Mode = Mode.OpsiveComInfoMode;
		GUILayout.EndHorizontal();

		GUILayout.EndVertical();

	}


	/// <summary>
	/// 
	/// </summary>
	void AssetStoreInfoMode()
	{

		m_Icon = m_InfoIcon;

		this.titleContent.text = "Update Instructions";
		this.minSize = new Vector2(m_DialogSize.x * 1.1f, m_DialogSize.y * 1.5f);
		this.maxSize = new Vector2(m_DialogSize.x * 1.1f, m_DialogSize.y * 1.5f);
		this.position = new Rect(
			(Screen.currentResolution.width / 2) - ((m_DialogSize.x * 1.1f) / 2),
			(Screen.currentResolution.height / 2) - ((m_DialogSize.y * 1.5f) / 2),
			m_DialogSize.x * 1.1f,
			m_DialogSize.y * 1.5f);
		
		GUILayout.BeginHorizontal();
		GUILayout.BeginVertical();
		GUILayout.Space(10);
		GUILayout.TextArea("Please follow these steps to update:\n", vp_EditorGUIUtility.LabelWrapStyle);
		GUILayout.TextArea("1) Click the button below to open the Asset Store page for \"" + m_AssetNameFormal + "\"\n", vp_EditorGUIUtility.LabelWrapStyle);
		GUILayout.TextArea("2) Make sure you're logged in (top right corner)\n\n3) Click on the blue \"Update/Import\" button", vp_EditorGUIUtility.LabelWrapStyle);
		GUILayout.Space(20);
		GUILayout.BeginHorizontal();
		if (GUILayout.Button("\nDo it now!\n"))
		{
			UnityEditorInternal.AssetStore.Open(((m_AssetStoreID != "1115") ? "content/" : "publisher/") + m_AssetStoreID);
			this.Close();
		}
		if (GUILayout.Button("\nLater\n"))
			this.Close();
		GUILayout.EndHorizontal();
		GUILayout.EndVertical();
		GUILayout.EndHorizontal();
	}


	/// <summary>
	/// 
	/// </summary>
	void OpsiveComInfoMode()
	{

		m_Icon = m_InfoIcon;

		this.titleContent.text = "Update Instructions";
		this.minSize = new Vector2(m_DialogSize.x * 1.1f, m_DialogSize.y * 1.5f);
		this.maxSize = new Vector2(m_DialogSize.x * 1.1f, m_DialogSize.y * 1.5f);
		this.position = new Rect(
			(Screen.currentResolution.width / 2) - ((m_DialogSize.x * 1.1f) / 2),
			(Screen.currentResolution.height / 2) - ((m_DialogSize.y * 1.5f) / 2),
			m_DialogSize.x * 1.1f,
			m_DialogSize.y * 1.5f);

		GUILayout.BeginHorizontal();
		GUILayout.BeginVertical();
		GUILayout.Space(10);
		GUILayout.TextArea("Upon purchase, you received an email from Opsive Support with the subject line:", vp_EditorGUIUtility.LabelWrapStyle);
		GUILayout.Space(10);
		GUILayout.TextArea("Your " + m_AssetNameFormal + " File Download", AssetNameStyle);
		GUILayout.Space(10);
		GUILayout.TextArea("Locate this email and click the link to download the latest version. It is highly recommended to make a backup of this link for future updates.", vp_EditorGUIUtility.LabelWrapStyle);
		GUILayout.Space(20);
		GUILayout.BeginHorizontal();
		if (GUILayout.Button("\nOK\n"))
			this.Close();
		GUILayout.EndHorizontal();
		GUILayout.EndVertical();
		GUILayout.EndHorizontal();
	}


	/// <summary>
	/// 
	/// </summary>
	public static GUIStyle AssetNameStyle
	{
		get
		{
			if (m_AssetNameStyle == null)
			{
				m_AssetNameStyle = new GUIStyle("Label");
				m_AssetNameStyle.wordWrap = true;
				m_AssetNameStyle.fontStyle = FontStyle.Bold;
				m_AssetNameStyle.fontSize = 9;
			}
			return m_AssetNameStyle;
		}
	}


}

