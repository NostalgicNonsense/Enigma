/////////////////////////////////////////////////////////////////////////////////
//
//	vp_InputWindow.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	window for mapping controls
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using UnityEditor;

public class vp_InputWindow : EditorWindow
{

	// target component
	public vp_Input m_Component;

	public static vp_InputWindow m_Window = null;
    public static Texture2D m_Icon;

	// foldouts
	public static bool m_ButtonsFoldout = true;
	public static bool m_AxisFoldout = true;
	
	protected static GUIStyle m_HeaderStyle = null;
	protected static GUIStyle m_SmallButtonStyle = null;
	protected Vector2 m_ScrollPosition = Vector2.zero;
	protected static string[] m_ControlTypes = new string[2]{ "Keyboard And Mouse", "Joystick" };

	static public Texture2D blankTexture	{	get	{		return EditorGUIUtility.whiteTexture;	}	}


	/// <summary>
	/// 
	/// </summary>
	public static void Init()
	{
        m_Icon = (Texture2D)Resources.Load("Icons/UFPS32x32", typeof(Texture2D));
        // Get existing open window or if none, make a new one:
        m_Window = (vp_InputWindow)EditorWindow.GetWindow(typeof(vp_InputWindow), false, "UFPS InputMgr");
	}


	/// <summary>
	/// 
	/// </summary>
	void OnFocus()
    {
    
    	// if application is not playing, find a vp_Input instance in resources
		GameObject go = Resources.Load("Input/vp_Input") as GameObject;
		
		// if not found create it
		if(go == null)
		{
#if UNITY_EDITOR
			vp_Input.CreateMissingInputPrefab(vp_Input.PrefabPath, vp_Input.FolderPath);
			go = Resources.Load("Input/vp_Input") as GameObject;
#endif
			if (go == null)
			{
				Debug.LogError("Error (" + this + ") Failed to create input manager.");
				return;
			}

		}
		
		m_Component = go.GetComponent<vp_Input>();
		if(m_Component == null)
			m_Component = go.AddComponent<vp_Input>();
			
		m_Component.SetupDefaults();
    		
		m_Component.SetDirty(true);
    
    }
    

    /// <summary>
	/// 
	/// </summary>
	public void OnGUI()
	{

		GUI.color = Color.white;

		GUILayout.Space(10);
		GUILayout.BeginHorizontal();
		GUILayout.Space(50);
		if(m_Icon != null)
			GUI.DrawTexture(new Rect(10, 10, 32, 32), m_Icon);
		EditorGUILayout.LabelField("UFPS Input Manager", HeaderStyleSelected, GUILayout.Height(20), GUILayout.MinWidth(200));
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();
		
		GUILayout.Space(30);
		
		GUILayout.BeginHorizontal();
		GUILayout.Space(10);
		m_Component.ControlType = (int)EditorGUILayout.Popup("Control Type", m_Component.ControlType, m_ControlTypes);
		GUILayout.Space(10);
		GUILayout.EndHorizontal();

		DrawSeparator();

		m_ScrollPosition = GUILayout.BeginScrollView(m_ScrollPosition);
		GUILayout.Space(10);

		try
		{
			DoButtonsFoldout();
			DoAxisFoldout();
			DoUnityAxis();
		}
		catch
		{
		}

		GUILayout.EndScrollView();

		// update
		if (GUI.changed)
		{

			EditorUtility.SetDirty(m_Component);

		}

	}
	

	/// <summary>
	/// 
	/// </summary>
	public virtual void DoButtonsFoldout()
	{
		
		GUILayout.BeginHorizontal(GUILayout.MaxWidth(210));
		m_ButtonsFoldout = EditorGUILayout.Foldout(m_ButtonsFoldout, m_ButtonsFoldout && m_Component.ButtonKeys.Count > 0 ? "Button Name" : "Buttons");
		if(m_ButtonsFoldout && m_Component.ButtonKeys.Count > 0)
		{
			GUILayout.Space(80);
			EditorGUILayout.LabelField("Key");
		}
		GUILayout.EndHorizontal();

		if (m_Component.ButtonValues.Count >= m_Component.ButtonValues2.Count)
			m_Component.CreateMissingSecondaryButtons();

		if (m_ButtonsFoldout)
		{
			if (m_Component.ButtonKeys != null)
			{
			
				GUILayout.Space(10);
			
				for (int i = 0; i < m_Component.ButtonKeys.Count; ++i)
				{
	
					GUILayout.BeginHorizontal();
					GUILayout.Space(20);
					
					m_Component.ButtonKeys[i] = EditorGUILayout.TextField(m_Component.ButtonKeys[i], GUILayout.MaxWidth(100), GUILayout.MinWidth(100));
					GUILayout.Space(10);

					// primary bindings
					m_Component.ButtonValues[i] = (KeyCode)EditorGUILayout.EnumPopup(m_Component.ButtonValues[i]);
					m_Component.Buttons[m_Component.ButtonKeys[i]] = (KeyCode)m_Component.ButtonValues[i];

					// secondary bindings
					m_Component.ButtonValues2[i] = (KeyCode)EditorGUILayout.EnumPopup(m_Component.ButtonValues2[i]);
					m_Component.Buttons2[m_Component.ButtonKeys[i]] = (KeyCode)m_Component.ButtonValues2[i];

					if (GUILayout.Button("Remove", vp_EditorGUIUtility.SmallButtonStyle, GUILayout.MinWidth(50), GUILayout.MaxWidth(50), GUILayout.MinHeight(15)))
					{
						m_Component.ButtonKeys.RemoveAt(i);
						m_Component.ButtonValues.RemoveAt(i);
						--i;
					}

					GUI.backgroundColor = Color.white;
					
					GUILayout.Space(20);
	
					GUILayout.EndHorizontal();
					
					GUILayout.Space(5);

				}
			}
			
			if(m_Component.ButtonKeys.Count == 0)
			{
				GUILayout.BeginHorizontal();
				GUILayout.Space(20);
				EditorGUILayout.HelpBox("There are no Input Buttons. Click \"Add Input Button\" to add a new button or \"Restore Defaults\" To restore the default buttons.", MessageType.Info);
				GUILayout.Space(20);
				GUILayout.EndHorizontal();
			}
			
			GUILayout.Space(8f);
			
			GUILayout.BeginHorizontal();
			
			GUILayout.Space(10f);
	
			if (GUILayout.Button("Add Input Button", GUILayout.MinWidth(150), GUILayout.MinHeight(25)))
			{
				m_Component.AddButton("Button "+m_Component.ButtonKeys.Count, KeyCode.None);
			}
			if(m_Component.ButtonKeys.Count == 0)
			{
				if (GUILayout.Button("Restore Button Defaults", GUILayout.MinWidth(150), GUILayout.MinHeight(25)))
				{
					m_Component.SetupDefaults("Buttons");
				}
			}
			GUI.backgroundColor = Color.white;
			GUILayout.Space(10f);
			GUILayout.EndHorizontal();
			
			DrawSeparator();

		}
		
	}


	/// <summary>
	/// 
	/// </summary>
	public virtual void DoAxisFoldout()
	{

		GUILayout.Space(10);
		GUILayout.BeginHorizontal(GUILayout.MaxWidth(210));
		m_AxisFoldout = EditorGUILayout.Foldout(m_AxisFoldout, m_AxisFoldout && m_Component.AxisKeys.Count > 0 ? "Axis Name" : "Axes");
		if((m_Component.ControlType == 1) && m_AxisFoldout && m_Component.AxisKeys.Count > 0)
		{
			GUILayout.Space(80);
			EditorGUILayout.LabelField("Positive Key", GUILayout.MinWidth(100));
			GUILayout.Space(10);
			EditorGUILayout.LabelField("Negative Key", GUILayout.MaxWidth(100));
		}
		GUILayout.EndHorizontal();

		if (m_AxisFoldout)
		{
			if(m_Component.ControlType == 1)
				return;
		
			if (m_Component.AxisKeys != null)
			{
				GUILayout.Space(10);
			
				for (int i = 0; i < m_Component.AxisKeys.Count; ++i)
				{
	
					GUILayout.BeginHorizontal();
					GUILayout.Space(20);
					
					m_Component.AxisKeys[i] = EditorGUILayout.TextField(m_Component.AxisKeys[i], GUILayout.MaxWidth(100), GUILayout.MinWidth(100));
					GUILayout.Space(10);
					m_Component.AxisValues[i].Positive = (KeyCode)EditorGUILayout.EnumPopup(m_Component.AxisValues[i].Positive, GUILayout.MaxWidth(80), GUILayout.MinWidth(100));
					GUILayout.Space(10);
					m_Component.AxisValues[i].Negative = (KeyCode)EditorGUILayout.EnumPopup(m_Component.AxisValues[i].Negative, GUILayout.MinWidth(80));
					
					if (GUILayout.Button("Remove", vp_EditorGUIUtility.SmallButtonStyle, GUILayout.MinWidth(50), GUILayout.MaxWidth(50), GUILayout.MinHeight(15)))
					{
						m_Component.AxisKeys.RemoveAt(i);
						m_Component.AxisValues.RemoveAt(i);
						--i;
					}
					GUI.backgroundColor = Color.white;
					
					GUILayout.Space(20);
	
					GUILayout.EndHorizontal();
					
					GUILayout.Space(5);
				}
			}
			
			if(m_Component.AxisKeys.Count == 0)
			{
				GUILayout.BeginHorizontal();
				GUILayout.Space(20);
				EditorGUILayout.HelpBox("There are no Input axes. Click \"Add Unity Input Axis\" to add a new axis or \"Restore Defaults\" To restore the default axis.", MessageType.Info);
				GUILayout.Space(20);
				GUILayout.EndHorizontal();
			}
			
			GUILayout.Space(8f);
			
			GUILayout.BeginHorizontal();
			GUILayout.Space(10f);
			if (GUILayout.Button("Add Unity Input Axis", GUILayout.MinWidth(150), GUILayout.MinHeight(25)))
			{
				m_Component.AddAxis("Axis "+m_Component.AxisKeys.Count, KeyCode.None, KeyCode.None);
			}
			if(m_Component.AxisKeys.Count == 0)
			{
				if (GUILayout.Button("Restore Defaults", GUILayout.MinWidth(150), GUILayout.MinHeight(25)))
				{
					m_Component.SetupDefaults("Axis");
				}
			}
			GUI.backgroundColor = Color.white;
			GUILayout.Space(10f);
			GUILayout.EndHorizontal();
		}
		
	}


	/// <summary>
	/// 
	/// </summary>
	public virtual void DoUnityAxis()
	{
	
		// Unity Axis
		if(m_AxisFoldout)
		{
			if (m_Component.UnityAxis != null)
			{
				GUILayout.Space(10);
				
				GUILayout.BeginHorizontal();
				GUILayout.Space(20);
				EditorGUILayout.LabelField("Axis Name");
				GUILayout.EndHorizontal();
			
				for (int i = 0; i < m_Component.UnityAxis.Count; ++i)
				{
	
					GUILayout.BeginHorizontal();
					GUILayout.Space(20);
					
					m_Component.UnityAxis[i] = EditorGUILayout.TextField(m_Component.UnityAxis[i]);
					
					if (GUILayout.Button("Remove", vp_EditorGUIUtility.SmallButtonStyle, GUILayout.MinWidth(50), GUILayout.MaxWidth(50), GUILayout.MinHeight(15)))
					{
						m_Component.UnityAxis.RemoveAt(i);
						--i;
					}
					GUI.backgroundColor = Color.white;
					
					GUILayout.Space(20);
	
					GUILayout.EndHorizontal();
					
					GUILayout.Space(5);
				}
			}
			
			GUILayout.BeginHorizontal();
			GUILayout.Space(20);
			if(m_Component.UnityAxis.Count == 0)
			{
				EditorGUILayout.HelpBox("There are no Unity axes. Click \"Add Unity Input Axis\" to add a new Unity axis or \"Restore Defaults\" To restore the default Unity axis.", MessageType.Info);
			}
			else
			{
				EditorGUILayout.HelpBox("Axis names must be entered exactly as they appear in Unity's Input Inspector.", MessageType.Info);
			}
			GUILayout.Space(20);
			GUILayout.EndHorizontal();
			
			
			GUILayout.Space(8f);
			
			GUILayout.BeginHorizontal();
			GUILayout.Space(10f);
			if (GUILayout.Button("Add Unity Input Axis", GUILayout.MinWidth(150), GUILayout.MinHeight(25)))
			{
				m_Component.AddUnityAxis("Unity Axis "+m_Component.UnityAxis.Count);
			}
			if(m_Component.UnityAxis.Count == 0)
			{
				if (GUILayout.Button("Restore Unity Axis Defaults", GUILayout.MinWidth(150), GUILayout.MinHeight(25)))
				{
					m_Component.SetupDefaults("UnityAxis");
				}
			}
			GUI.backgroundColor = Color.white;
			GUILayout.Space(10f);
			GUILayout.EndHorizontal();
			
			DrawSeparator();
		}
	
	}


	/// <summary>
	/// 
	/// </summary>
	static public void DrawSeparator()
	{
		
		GUILayout.Space(12f);

		if (Event.current.type == EventType.Repaint)
		{
			Texture2D tex = blankTexture;
			Rect rect = GUILayoutUtility.GetLastRect();
			GUI.color = new Color(0f, 0f, 0f, 0.25f);
			GUI.DrawTexture(new Rect(0f, rect.yMin + 10f, Screen.width, 4f), tex);
			GUI.DrawTexture(new Rect(0f, rect.yMin + 10f, Screen.width, 1f), tex);
			GUI.DrawTexture(new Rect(0f, rect.yMin + 13f, Screen.width, 1f), tex);
			GUI.color = Color.white;
		}
		
	}


	// -------- GUI styles --------

	public static GUIStyle SmallButtonStyle
	{
		get
		{
			if (m_SmallButtonStyle == null)
			{
				m_SmallButtonStyle = new GUIStyle("Button");
				m_SmallButtonStyle.fontSize = 10;
				m_SmallButtonStyle.alignment = TextAnchor.MiddleCenter;
				m_SmallButtonStyle.margin.left = 1;
				m_SmallButtonStyle.margin.right = 1;
				m_SmallButtonStyle.padding = new RectOffset(0, 4, 0, 2);
			}
			return m_SmallButtonStyle;
		}
	}


	public static GUIStyle HeaderStyleSelected
	{
		get
		{
			if (m_HeaderStyle == null)
			{
				m_HeaderStyle = new GUIStyle("Label");
				m_HeaderStyle.fontSize = 12;
				//m_HeaderStyle.fontStyle = FontStyle.Bold;
				m_HeaderStyle.alignment = TextAnchor.MiddleLeft;

			}
			return m_HeaderStyle;
		}
	}

    
}