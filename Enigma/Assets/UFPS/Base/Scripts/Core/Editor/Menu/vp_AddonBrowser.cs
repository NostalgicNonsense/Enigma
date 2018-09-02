/////////////////////////////////////////////////////////////////////////////////
//
//	vp_AddonBrowser.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	a window that loads a list of UFPS add-ons from opsive.com and
//					presents them as a list of icons that launch the Asset Store
//					page for the respective product when clicked
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class vp_AddonBrowser : EditorWindow
{

	// GUI
	private static Vector2 m_DialogSize = new Vector2(563, 350);
	private GUIStyle m_IconLabelStyle = null;
	private Vector2 m_ScrollPos = Vector2.zero;

	// web
	private static WWW m_ProductFile;

	// product icons
	private static bool m_IconsReady = false;
	private static bool m_ProductsReady = false;
	private int m_ProductCount = 0;
	private List<ProductIcon> m_Icons = new List<ProductIcon>();
	private delegate void OpenFunc();


	/// <summary>
	/// 
	/// </summary>
	private class ProductIcon
	{

		public string Name = "";
		public string IconPath = "";
		public Texture2D Icon = null;
		public string Id = "";
		public OpenFunc Open = null;
		public WWW www = null;
		public float Alpha = 0.0f;
		public DateTime FadeInTime = DateTime.Now;
		public bool DoneLoading = false;

		/// <summary>
		/// 
		/// </summary>
		public ProductIcon(string name, string id)
		{
			Name = name;
			Id = id;
			IconPath = "http://legacy.opsive.com/assets/UFPS/content/assets/ufps/editor/addonicons/" + Id + ".png";
			www = new WWW(IconPath);
			Open = delegate() {
                if (!name.Contains("Opsive")) {
                    UnityEditorInternal.AssetStore.Open("content/" + Id);
                } else {
                    Application.OpenURL("https://www.assetstore.unity3d.com/#!/publisher/" + Id);
                }
            };
		}

	}


	/// <summary>
	/// 
	/// </summary>
	public static void Create()
	{

		vp_AddonBrowser window = (vp_AddonBrowser)EditorWindow.GetWindow(typeof(vp_AddonBrowser), true);

		window.titleContent.text = "Boost your game with these awesome add-on packages!";
		window.minSize = new Vector2(m_DialogSize.x, m_DialogSize.y);
		window.maxSize = new Vector2(m_DialogSize.x, Screen.currentResolution.height);
		window.position = new Rect(
			(Screen.currentResolution.width / 2) - (m_DialogSize.x / 2),
			(Screen.currentResolution.height / 2) - (m_DialogSize.y / 2),
			m_DialogSize.x,
			m_DialogSize.y);
		window.Show();

		m_ProductsReady = false;
		m_IconsReady = false;
		m_ProductFile = new WWW("http://legacy.opsive.com/assets/UFPS/content/assets/ufps/editor/addons.txt");

	}


	/// <summary>
	/// 
	/// </summary>
	private void Update()
	{

		if ((m_ProductFile != null) && m_ProductFile.isDone && string.IsNullOrEmpty(m_ProductFile.error))
		{
			
			if (!m_ProductsReady)
				ExtractProducts();

			if (!m_IconsReady)
				ScheduleIconFadeIns();
			else if (m_Icons.Count == 0)
				ShowErrorLoadingDialog();

			FadeIcons();
		
		}
		else if(!(m_ProductFile != null && !m_ProductFile.isDone) && !m_ProductsReady)
			ShowErrorLoadingDialog();

	}


	/// <summary>
	/// 
	/// </summary>
	private void FadeIcons()
	{

		bool repaint = false;

		foreach (ProductIcon p in m_Icons)
		{
			if (p.DoneLoading && p.Alpha < 1.0f && DateTime.Now > p.FadeInTime)
			{
				p.Alpha += 0.01f;
				repaint = true;
			}
		}
		
		if(repaint)
			Repaint();

	}


	/// <summary>
	/// 
	/// </summary>
	private void OnGUI()
	{

		m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos);

		if (m_ProductsReady)
		{

			int columns = 4;
			int iconsLeft = m_ProductCount;
			int slotsLeft = ((iconsLeft % columns) == 0) ? iconsLeft : ((iconsLeft / columns) + 1) * columns;
			int currentColumn = 0;

			while (slotsLeft > 0)
			{
				try
				{

					if (currentColumn == 0)
						EditorGUILayout.BeginHorizontal();

					if (iconsLeft > 0 && m_Icons[iconsLeft - 1].www.isDone)
						DrawIcon(m_Icons[iconsLeft - 1]);
					else
						DrawEmptySpace();

					currentColumn++;
					slotsLeft--;
					iconsLeft--;

					if (currentColumn == columns)
					{
						EditorGUILayout.EndHorizontal();
						currentColumn = 0;
					}

				}
				catch
				{
					slotsLeft = 0;
				}
			}

			GUI.color = Color.white;
		}

		EditorGUILayout.EndScrollView();
		
	}


	/// <summary>
	/// 
	/// </summary>
	private void DrawIcon(ProductIcon icon)
	{

		EditorGUILayout.BeginVertical();
		GUI.color = new Color(1.0f, 1.0f, 1.0f, icon.Alpha);
		if (GUILayout.Button(new GUIContent(icon.Icon, (!icon.Name.Contains("Opsive") ? "Click for info, screens & videos" : "View all of our Unity products")), "Label"))
			icon.Open();
		GUILayout.Label(icon.Name, IconLabelStyle);
		GUI.color = Color.white;
		EditorGUILayout.EndVertical();

	}


	/// <summary>
	/// 
	/// </summary>
	private void DrawEmptySpace()
	{

		EditorGUILayout.BeginVertical();
		GUILayout.Label("", IconLabelStyle);	// using this style forces the correct width of 128
		EditorGUILayout.EndVertical();

	}


	/// <summary>
	/// 
	/// </summary>
	private void ExtractProducts()
	{

		string[] splitLines = m_ProductFile.text.Split('\n');
		m_ProductCount = 0;
		for (int v = splitLines.Length - 1; v > -1; v--)
		{
			if (string.IsNullOrEmpty(splitLines[v]) || !splitLines[v].Contains(","))
				continue;

			m_ProductCount++;

			int start = splitLines[v].LastIndexOf(',') + 1;
			int length = splitLines[v].Length - start;
			string name = splitLines[v].Substring(start, length);
			string number = splitLines[v].Substring(0, splitLines[v].LastIndexOf(','));
			m_Icons.Add(new ProductIcon(name, number));
		}

		m_ProductsReady = true;

	}


	/// <summary>
	/// 
	/// </summary>
	private void ScheduleIconFadeIns()
	{

		m_IconsReady = true;
		for (int v = m_Icons.Count-1; v > -1; v--)
		{
			if (m_Icons[v].www.isDone)
			{
				if(!string.IsNullOrEmpty(m_Icons[v].www.error))
				{
					m_Icons.Remove(m_Icons[v]);
					m_ProductCount--;
				}
				else if(!m_Icons[v].DoneLoading)
				{
					m_Icons[v].Icon = m_Icons[v].www.texture;
					m_Icons[v].DoneLoading = true;
					m_Icons[v].FadeInTime = DateTime.Now + new TimeSpan(0, 0, 0, 0, ((m_Icons.Count - 1) - v) * (125 + (int)(UnityEngine.Random.value * 125)));
				}
			}
			else
				m_IconsReady = false;

		}

	}


	/// <summary>
	/// 
	/// </summary>
	private void ShowErrorLoadingDialog()
	{

		m_ProductsReady = true;
		vp_MessageBox.Create(vp_MessageBox.Mode.OK, "Error", "Failed to load add-on list. " + ((m_ProductFile != null) ? m_ProductFile.error : ""));
		this.Close(); // NOTE: must close vp_AddonBrowser here or this dialog may be spawned every frame

	}


	/// <summary>
	/// 
	/// </summary>
	private GUIStyle IconLabelStyle
	{
		get
		{
			if (m_IconLabelStyle == null)
			{
				m_IconLabelStyle = new GUIStyle("Label");
				m_IconLabelStyle.fontSize = 9;
				m_IconLabelStyle.alignment = TextAnchor.UpperCenter;
				m_IconLabelStyle.contentOffset = new Vector2(0, -6);
				m_IconLabelStyle.fixedWidth = 128;
			}
			return m_IconLabelStyle;
		}
	}


}

