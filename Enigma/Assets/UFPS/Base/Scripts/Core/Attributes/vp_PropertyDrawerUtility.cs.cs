/////////////////////////////////////////////////////////////////////////////////
//
//	vp_PropertyDrawerUtility.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	utility class for drawing some common UFPS properties in
//					the Inspector
//
/////////////////////////////////////////////////////////////////////////////////

#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

public static class vp_PropertyDrawerUtility
{

	public enum Space
	{
		Nothing,
		EmptyLine,
		Separator
	}

	/// <summary>
	/// 
	/// </summary>
	public static void DrawIcon(Rect rect, Texture2D icon)
	{
		IconStyle.normal.background = icon;
		EditorGUI.LabelField(rect, GUIContent.none, IconStyle);
	}

	public static float ItemCardHeight = 20;
	public static float CalcAddObjectBoxHeight = 50;
	public static float ToggleHeight = 16;
	
	static double m_ClickTime;
	static double m_DoubleClickTimeSpan = 0.5;
	static float m_RightMargin = 23;


	/// <summary>
	/// 
	/// </summary>
	public static bool ItemCard(Rect pos, Texture2D icon, string label, object targetObject, ref int value1, string value1Name, System.Action value1Action, ref int value2, string value2Name, System.Action value2Action, System.Action deleteAction, int value1ExtraWidth = 0)
	{

		label = label.Replace("vp_", "");
		label = label.Replace("Type", "");
		label = label.Replace("()", "(Item)");

		Rect rect = pos;

		int value1Prev = value1;
		int value2Prev = value2;

		rect.x = (pos.x + pos.width) - CalcValueFieldSize(value1Name, true) - m_RightMargin;
		rect.y += 5;
		rect = ValueField(rect, ref value1, value1Name, value1Action, value1ExtraWidth, deleteAction);
		rect.x -= CalcValueFieldSize(value2Name);
		rect = ValueField(rect, ref value2, value2Name, value2Action);
		rect.width = (rect.x - pos.x) + 1;
		rect.x = pos.x;
		rect = ItemField(rect, label, targetObject, icon);

		return ((value1Prev != value1) || (value2Prev != value2));
	
	}


	/// <summary>
	/// 
	/// </summary>
	static Rect ItemField(Rect pos, string label, object targetObject, Texture2D icon)
	{
		
		if (GUI.Button(pos, label, ItemStyle) && (targetObject != null) && (targetObject is Object))
			EditorGUIUtility.PingObject((Object)targetObject);

		// --- icon ---

		if (icon != null)
		{
			Rect u = pos;
			u.x -= 12;
			u.y += EditorGUIUtility.isProSkin ? 3 : 2;
			u.height = 16;
			u.width = 16;
			DrawIcon(u, icon);
		}

		return pos;

	}


	/// <summary>
	/// 
	/// </summary>
	static Rect ValueField(Rect pos, ref int value, string name, System.Action action, float extraSize = 0, System.Action deleteAction = null)
	{

		if (name == null)
			name = "";
		pos.x -= extraSize;
		pos.width = ValueStyle.CalcSize(new GUIContent(name)).x + 19 + ((deleteAction == null) ? 0 : (21 - 3)) + extraSize;
		pos.height = 21;

		GUI.color = new Color(0.85f, 0.85f, 0.85f, 1);
		if (value != -1)
			GUI.Label(pos, name, ValueStyle);
		GUI.color = Color.white;

		if (value != -1)
		{
			// draw editbox
			Rect n1 = pos;
			n1.x += ValueStyle.CalcSize(new GUIContent(name)).x - 30;
			n1.y += 3;
			n1.width = 45 + extraSize;
			n1.height = 21;
			int oldValue = value;
			value = Mathf.Max(0, EditorGUI.IntField(n1, value, TextFieldStyle));
			if ((value != oldValue) && (action != null))
				action.Invoke();

		}

		if (deleteAction != null)
		{
			Rect d = pos;
			d.x = pos.x + pos.width - 21;
			d.width = 21;
			//GUI.Label(d, "", ValueStyle);
			d.y += 3;
			d.x += 3;
			if (GUI.Button(d, "X", SmallButtonStyle) && (deleteAction != null))
				deleteAction.Invoke();
			d.x -= 3;
			d.y -= 3;
		}

		return pos;

	}


	/// <summary>
	/// 
	/// </summary>
	public static Rect DeleteButton(Rect pos, System.Action deleteAction)
	{

		pos.x = (pos.x + pos.width) - 44;
		pos.y += 8;
		pos.width = 21;
		pos.height = 21;
		GUI.Label(pos, "", ValueStyle);
		pos.y += 3;
		pos.x += 3;
		if (GUI.Button(pos, "X", SmallButtonStyle) && (deleteAction != null))
			deleteAction.Invoke();
		pos.x -= 3;
		pos.y -= 3;
		return pos;

	}


	/// <summary>
	/// 
	/// </summary>
	static float CalcValueFieldSize(string name, bool hasDeleteButton = false)
	{
		float f = 0.0f;
		if (!string.IsNullOrEmpty(name))
			f = ValueStyle.CalcSize(new GUIContent(name)).x + 18;
		if (hasDeleteButton)
			f += 21;
		return f;
	}


	/// <summary>
	/// 
	/// </summary>
	public static Object AddObjectBox(Rect pos, string objectTypeName, System.Type type, System.Action<Object> dragAction, float width = 208)
	{

		Object ObjectToAdd = null;

		// draw objectfield
		Rect py = pos;
		py.height = 16;
		py.width = width;
		py.x += 10;
		py.y += 10;
		GUI.Label(py, "Drag a" + objectTypeName + " object here.", ObjectFieldStyle);

		// draw frame
		AddObjectBoxBG(pos, width);

		Rect p = py;
		p.x -= 10;
		p.y -= 10;
		p.height = 35;
		p.width = py.width + 15;

		// draw invisible object field
		GUI.color = Color.clear;
		Rect px = p;
		px.width = py.width + 11;
		ObjectToAdd = EditorGUI.ObjectField(px, "", ObjectToAdd, type, false);	// <- object set by dragging
		GUI.color = Color.white;

		if (Event.current.commandName == "ObjectSelectorUpdated")
		{
			if (ObjectToAdd == null)
				ObjectToAdd = EditorGUIUtility.GetObjectPickerObject();	// <- object set via object picker
		}

		if (ObjectToAdd != null)
			dragAction.Invoke(ObjectToAdd);

		return ObjectToAdd;

	}



	/// <summary>
	/// 
	/// </summary>
	public static void AddObjectBoxBG(Rect pos, float width = 208)
	{

		pos.height = 35;
		pos.width = width + 15;
		GUI.Label(pos, "", FieldStyle);
		
	}


	/// <summary>
	/// 
	/// </summary>
	public static int CalcHelpBoxHeight(float width, string message)
	{
		return (int)(HelpboxIconTextBoxStyle.CalcHeight(new GUIContent(message), width));
	}


	/// <summary>
	/// 
	/// </summary>
	public static void HelpBox(Rect pos, string message, MessageType type, string manualURL = null)
	{

		pos.height = CalcHelpBoxHeight(pos.width, message);

		Rect m_IconPosition = pos;
		m_IconPosition.x += 5;
		m_IconPosition.y += 5;
		m_IconPosition.width = 32;
		m_IconPosition.height = 32;

		if (type == MessageType.None && !EditorGUIUtility.isProSkin)
			GUI.enabled = false;

		// draw text box
		GUI.TextArea(pos, message, ((type != MessageType.None) ? vp_PropertyDrawerUtility.HelpboxIconTextBoxStyle : vp_PropertyDrawerUtility.HelpboxTextBoxStyle));
					
		// draw icon
		GUIStyle m_HelpboxIconStyle = null;
		switch (type)
		{
			case MessageType.Error: m_HelpboxIconStyle = vp_PropertyDrawerUtility.HelpboxErrorIconStyle; break;
			case MessageType.Info: m_HelpboxIconStyle = vp_PropertyDrawerUtility.HelpboxInfoIconStyle; break;
			case MessageType.Warning: m_HelpboxIconStyle = vp_PropertyDrawerUtility.HelpboxWarningIconStyle; break;
			case MessageType.None: m_HelpboxIconStyle = vp_PropertyDrawerUtility.HelpboxNoIconStyle; break;
		}

		GUI.Label(m_IconPosition, GUIContent.none, m_HelpboxIconStyle);

		GUI.enabled = true;

		if (!string.IsNullOrEmpty(manualURL))
		{
			GUI.color = Color.clear;
			if (GUI.Button(pos, new GUIContent("", "Double-click for more help")))
			{

				if ((EditorApplication.timeSinceStartup - m_ClickTime) < m_DoubleClickTimeSpan)
					Application.OpenURL(manualURL);

				m_ClickTime = EditorApplication.timeSinceStartup;

			}
			GUI.color = Color.white;
		}
		
	}


	/// <summary>
	/// 
	/// </summary>
	public static void Separator(Rect pos)
	{

		GUI.color = new Color(1, 1, 1, 0.25f);
		pos.x += 4;
		pos.width -= 8;
		GUI.Box(pos, GUIContent.none, "HorizontalSlider");
		GUI.color = Color.white;

	}


	/// <summary>
	/// 
	/// </summary>
	public static int CalcSeparatorHeight()
	{
		return 16;
	}


	/// <summary>
	/// an intfield that clamps between min and max
	/// </summary>
	public static int ClampedIntField(Rect pos, string label, int value, int min, int max)
	{

		return EditorGUI.IntField(pos, label, Mathf.Clamp(value, min, max));

	}


	/// <summary>
	/// a floatfield that clamps between min and max 
	/// </summary>
	public static float ClampedFloatField(Rect pos, string label, float value, float min, float max)
	{

		return EditorGUI.FloatField(pos, label, Mathf.Clamp(value, min, max));

	}


	/// <summary>
	/// 
	/// </summary>
	public static int CalcIntFieldHeight()
	{
		return 16;
	}

	// --- GUI styles ---

	private static GUIStyle m_ValueStyle = null;
	public static GUIStyle ValueStyle
	{
		get
		{
			if (m_ValueStyle == null)
			{
				m_ValueStyle = new GUIStyle(ItemStyle);
				m_ValueStyle.contentOffset = new Vector2(5, 5);
			}
			return m_ValueStyle;
		}
	}

	private static GUIStyle m_ItemStyle = null;
	public static GUIStyle ItemStyle
	{
		get
		{
			if (m_ItemStyle == null)
			{
				m_ItemStyle = new GUIStyle("TE NodeBoxSelected");
				m_ItemStyle.fontSize = 9;
				m_ItemStyle.wordWrap = false;
				m_ItemStyle.clipping = TextClipping.Clip;
				m_ItemStyle.contentOffset = new Vector2(22, 5);
				m_ItemStyle.padding.right = 25;
				m_ItemStyle.normal.textColor = new Color(0, 0, 0, 0.75f);
			}
			return m_ItemStyle;
		}
	}

	private static GUIStyle m_IconStyle = null;
	public static GUIStyle IconStyle
	{
		get
		{
			if (m_IconStyle == null)
			{
				m_IconStyle = new GUIStyle("Label");
				m_IconStyle.fixedWidth = 16;
				m_IconStyle.fixedHeight = 16;

			}
			return m_IconStyle;
		}
	}

	private static GUIStyle m_TextFieldStyle = null;
	public static GUIStyle TextFieldStyle
	{
		get
		{
			if (m_TextFieldStyle == null)
			{
				m_TextFieldStyle = new GUIStyle("TextField");
				m_TextFieldStyle.fixedHeight = 16;
			}
			return m_TextFieldStyle;
		}
	}


	private static GUIStyle m_SmallButtonStyle = null;
	public static GUIStyle SmallButtonStyle
	{
		get
		{
			if (m_SmallButtonStyle == null)
			{
				m_SmallButtonStyle = new GUIStyle("Button");
				m_SmallButtonStyle.fontSize = 8;
				m_SmallButtonStyle.alignment = TextAnchor.MiddleCenter;
				m_SmallButtonStyle.margin.left = 1;
				m_SmallButtonStyle.margin.right = 1;
				m_SmallButtonStyle.padding = new RectOffset(2, 4, 0, 2);
				m_SmallButtonStyle.fixedHeight = 15;
				m_SmallButtonStyle.fixedWidth = 15;
			}
			return m_SmallButtonStyle;
		}
	}

	private static GUIStyle m_ObjectFieldStyle = null;
	public static GUIStyle ObjectFieldStyle
	{
		get
		{
			if (m_ObjectFieldStyle == null)
			{
				m_ObjectFieldStyle = new GUIStyle("ObjectField");
				m_ObjectFieldStyle.fontSize = 9;
				m_ObjectFieldStyle.padding.top = 0;
				m_ObjectFieldStyle.padding.bottom = 0;
			}
			return m_ObjectFieldStyle;
		}
	}

	private static GUIStyle m_FieldStyle = null;
	public static GUIStyle FieldStyle
	{
		get
		{
			if (m_FieldStyle == null)
			{
				GUIStyle style = new GUIStyle("Helpbox");	// TODO: hard code values
				m_FieldStyle = new GUIStyle("TL SelectionButton");
				m_FieldStyle.fontSize = 9;
				m_FieldStyle.wordWrap = style.wordWrap;
				m_FieldStyle.alignment = style.alignment;
				m_FieldStyle.padding.top = 35;
				m_FieldStyle.padding.left = 15;
				m_FieldStyle.padding.bottom = 5;
				m_FieldStyle.padding.right = 15;
				m_FieldStyle.normal.textColor = new Color(0, 0, 0, 0.5f);
			}
			return m_FieldStyle;
		}
	}


	// --- helpbox ---

	private static GUIStyle m_HelpboxIconTextBoxStyle = null;
	public static GUIStyle HelpboxIconTextBoxStyle
	{
		get
		{
			if (m_HelpboxIconTextBoxStyle == null)
			{
				m_HelpboxIconTextBoxStyle = new GUIStyle("HelpBox");
				m_HelpboxIconTextBoxStyle.padding.top = 10;
				m_HelpboxIconTextBoxStyle.padding.left = 40;
				m_HelpboxIconTextBoxStyle.padding.bottom = 10;
				m_HelpboxIconTextBoxStyle.padding.right = 10;
			}
			return m_HelpboxIconTextBoxStyle;
		}
	}

	private static GUIStyle m_HelpboxTextBoxStyle = null;
	public static GUIStyle HelpboxTextBoxStyle
	{
		get
		{
			if (m_HelpboxTextBoxStyle == null)
			{
				m_HelpboxTextBoxStyle = new GUIStyle(HelpboxIconTextBoxStyle);
				m_HelpboxTextBoxStyle.padding.left = 15;
			}
			return m_HelpboxTextBoxStyle;
		}
	}

	private static GUIStyle m_HelpboxInfoIconStyle = null;
	public static GUIStyle HelpboxInfoIconStyle
	{
		get
		{
			if (m_HelpboxInfoIconStyle == null)
				m_HelpboxInfoIconStyle = new GUIStyle("CN EntryInfo");
			return m_HelpboxInfoIconStyle;
		}
	}

	private static GUIStyle m_HelpboxErrorIconStyle = null;
	public static GUIStyle HelpboxErrorIconStyle
	{
		get
		{
			if (m_HelpboxErrorIconStyle == null)
				m_HelpboxErrorIconStyle = new GUIStyle("CN EntryError");
			return m_HelpboxErrorIconStyle;
		}
	}

	private static GUIStyle m_HelpboxWarningIconStyle = null;
	public static GUIStyle HelpboxWarningIconStyle
	{
		get
		{
			if (m_HelpboxWarningIconStyle == null)
				m_HelpboxWarningIconStyle = new GUIStyle("CN EntryWarn");
			return m_HelpboxWarningIconStyle;
		}
	}

	private static GUIStyle m_HelpboxNoIconStyle = null;
	public static GUIStyle HelpboxNoIconStyle
	{
		get
		{
			if (m_HelpboxNoIconStyle == null)
				m_HelpboxNoIconStyle = new GUIStyle("CN Message");
			return m_HelpboxNoIconStyle;
		}
	}

}

#endif
