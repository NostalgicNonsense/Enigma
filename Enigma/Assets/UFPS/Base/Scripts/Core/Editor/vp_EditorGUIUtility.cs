/////////////////////////////////////////////////////////////////////////////////
//
//	vp_EditorGUIUtility.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	helper methods for standard editor GUI tasks
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

public static class vp_EditorGUIUtility
{

	private static GUIStyle m_LinkStyle = null;
	private static GUIStyle m_LabelWrapStyle = null;
	private static GUIStyle m_SmallTextStyle = null;
	private static GUIStyle m_NoteStyle = null;
	private static GUIStyle m_SmallButtonStyle = null;
	private static GUIStyle m_RightAlignedPathStyle = null;
	private static GUIStyle m_LeftAlignedPathStyle = null;
	private static GUIStyle m_CenteredBoxStyle = null;
	private static GUIStyle m_CenteredBoxStyleBold = null;
	private static GUIStyle m_CenteredStyleBold = null;


	/// <summary>
	/// creates a foldout button to clearly distinguish a section
	/// of controls from others
	/// </summary>
	public static bool SectionButton(string label, bool state)
	{

		GUI.color = new Color(0.9f, 0.9f, 1, 1);
		if (GUILayout.Button((state ? "- " : "+ ") + label.ToUpper(), GUILayout.Height(20)))
			state = !state;
		GUI.color = Color.white;

		return state;

	}


	/// <summary>
	/// creates a big 2-button toggle
	/// </summary>
	public static bool ButtonToggle(string label, bool state)
	{

		GUIStyle onStyle = new GUIStyle("Button");
		GUIStyle offStyle = new GUIStyle("Button");

		if (state)
			onStyle.normal = onStyle.active;
		else
			offStyle.normal = offStyle.active;

		EditorGUILayout.BeginHorizontal();
		GUILayout.Label(label);
		if (GUILayout.Button("ON", onStyle))
			state = true;
		if (GUILayout.Button("OFF", offStyle))
			state = false;
		EditorGUILayout.EndHorizontal();

		return state;

	}


	/// <summary>
	/// creates a small toggle
	/// </summary>
	public static bool SmallToggle(string label, bool state)
	{

		EditorGUILayout.BeginHorizontal();
		state = GUILayout.Toggle(state, label, GUILayout.MaxWidth(12));
		GUILayout.Label(label, LeftAlignedPathStyle);
		EditorGUILayout.EndHorizontal();

		return state;

	}


	/// <summary>
	/// creates an editable list of unity objects
	/// </summary>
	public static void ObjectList(string caption, List<UnityEngine.Object> list, Type type)
	{

		GUILayout.BeginHorizontal();
		GUILayout.Label(caption);
		GUILayout.FlexibleSpace();
		if (GUILayout.Button("Add", vp_EditorGUIUtility.SmallButtonStyle, GUILayout.MinWidth(50), GUILayout.MaxWidth(50), GUILayout.MinHeight(15)))
			list.Add(null);
		GUILayout.Space(37);
		GUILayout.EndHorizontal();

		for (int v = 0; v < list.Count; v++)
		{
			GUILayout.BeginHorizontal();
			list[v] = EditorGUILayout.ObjectField(v.ToString(), list[v], type, false);
			if (GUILayout.Button("X", vp_EditorGUIUtility.SmallButtonStyle, GUILayout.MinWidth(15), GUILayout.MaxWidth(15), GUILayout.MinHeight(15)))
				list.RemoveAt(v);
			GUILayout.EndHorizontal();
		}
		
	}

	
	/// <summary>
	/// creates a horizontal line to visually separate groups of
	/// controls
	/// </summary>
	public static void Separator()
	{

		GUI.color = new Color(1, 1, 1, 0.25f);
		GUILayout.Box("", "HorizontalSlider", GUILayout.Height(16));
		GUI.color = Color.white;

	}


	/// <summary>
	/// Returns a layermask popup
	/// </summary>
	public static LayerMask LayerMaskField(string label, LayerMask selected, bool showSpecial, string tooltip = "")
	{

		List<string> layers = new List<string>();
		List<int> layerNumbers = new List<int>();

		string selectedLayers = "";

		for (int i = 0; i < 32; i++)
		{
			string layerName = LayerMask.LayerToName(i);

			if (layerName != "")
				if (selected == (selected | (1 << i)))
					if (selectedLayers == "")
						selectedLayers = layerName;
					else
						selectedLayers = "Mixed ...";
		}

		if (Event.current.type != EventType.MouseDown && Event.current.type != EventType.ExecuteCommand)
		{
			if (selected.value == 0)
				layers.Add("Nothing");
			else if (selected.value == -1)
				layers.Add("Everything");
			else
				layers.Add(selectedLayers);

			layerNumbers.Add(-1);
		}

		if (showSpecial)
		{
			layers.Add((selected.value == 0 ? "\u2714   " : "      ") + "Nothing");
			layerNumbers.Add(-2);

			layers.Add((selected.value == -1 ? "\u2714   " : "      ") + "Everything");
			layerNumbers.Add(-3);
		}

		for (int i = 0; i < 32; i++)
		{

			string layerName = LayerMask.LayerToName(i);

			if (layerName != "")
			{
				if (selected == (selected | (1 << i)))
					layers.Add("\u2714   " + layerName);
				else
					layers.Add("      " + layerName);

				layerNumbers.Add(i);
			}
		}

		bool preChange = GUI.changed;

		GUI.changed = false;

		int newSelected = 0;

		if (Event.current.type == EventType.MouseDown)
			newSelected = -1;


		string[] strings = layers.ToArray();
		GUIContent[] gcStrings = new GUIContent[strings.Length];
		for (int i = 0; i < strings.Length; i++)
			gcStrings[i] = new GUIContent(strings[i]);

		newSelected = EditorGUILayout.Popup(new GUIContent(label, tooltip), newSelected, gcStrings, EditorStyles.layerMaskField, GUILayout.MinWidth(100));

		if (GUI.changed && newSelected >= 0)
		{
			if (showSpecial && newSelected == 0)
				selected = 0;
			else if (showSpecial && newSelected == 1)
				selected = -1;
			else

				if (selected == (selected | (1 << layerNumbers[newSelected])))
					selected &= ~(1 << layerNumbers[newSelected]);
				else
					selected = selected | (1 << layerNumbers[newSelected]);
		}
		else
			GUI.changed = preChange;

		return selected;

	}


	/// <summary>
	/// Displays a Tag mask popup
	/// </summary>
	public static int TagMaskField(string label, int selected, ref List<string> list)
	{

		string[] options = UnityEditorInternal.InternalEditorUtility.tags;
		selected = EditorGUILayout.MaskField(selected, options, GUILayout.MinWidth(100));

		list.Clear();
		for (int i = 0; i < options.Length; i++)
			if ((selected & 1 << i) != 0)
				list.Add(options[i]);

		return selected;

	}


	/// <summary>
	/// returns the world scale of a screen pixel at a given world
	/// position, as seen from a given camera. this can be used to
	/// calculate overlaps with tool handles / gizmos. NOTE: this
	/// is all VERY brute force, so don't use every frame
	/// </summary>
	public static float GetPixelScaleAtWorldPosition(Vector3 position, Transform camera)
	{

		// in order to get a smooth result we calculate the distance to
		// all 8 pixels surrounding the source pixel and return average

		return (
		DistanceFromPixelToWorldPosition(-1, -1, position, camera) +
		DistanceFromPixelToWorldPosition(0, -1, position, camera) +
		DistanceFromPixelToWorldPosition(1, -1, position, camera) +
		DistanceFromPixelToWorldPosition(-1, 0, position, camera) +
		DistanceFromPixelToWorldPosition(1, 0, position, camera) +
		DistanceFromPixelToWorldPosition(-1, 1, position, camera) +
		DistanceFromPixelToWorldPosition(0, 1, position, camera) +
		DistanceFromPixelToWorldPosition(1, 1, position, camera))
		* 0.125f;	// div by 8

	}

	
	/// <summary>
	/// calculates the distance from a screen pixel at its world position
	/// to a given world position with a pixel offset. raycasts from the
	/// offset screen position to a camera-aligned plane at the world
	/// position, and returns the distance from the hit point to the
	/// world position
	/// </summary>
	private static float DistanceFromPixelToWorldPosition(float xOffset, float yOffset, Vector3 position, Transform camera)
	{

		Handles.BeginGUI();
		Vector2 point = HandleUtility.WorldToGUIPoint(position);
		point.x += xOffset;
		point.y += yOffset;
		Ray ray = HandleUtility.GUIPointToWorldRay(point);
		Plane plane = new Plane(-camera.forward, position);
		float distance = 0.0f;
		Vector3 hitPoint = Vector3.zero;
		if (plane.Raycast(ray, out distance))
			hitPoint = ray.GetPoint(distance);
		Handles.EndGUI();
		return Vector3.Distance(position, hitPoint);

	}
	

	//////////////////////////////////////////////////////////
	// GUI styles
	//////////////////////////////////////////////////////////


	public static GUIStyle LinkStyle
	{
		get
		{
			if (m_LinkStyle == null)
			{
				m_LinkStyle = new GUIStyle("Label");
				m_LinkStyle.normal.textColor = (EditorGUIUtility.isProSkin ? new Color(0.8f, 0.8f, 1.0f, 1.0f) : Color.blue);
			}
			return m_LinkStyle;
		}
	}
	
	public static GUIStyle LabelWrapStyle
	{
		get
		{
			if (m_LabelWrapStyle == null)
			{
				m_LabelWrapStyle = new GUIStyle("Label");
				m_LabelWrapStyle.wordWrap = true;
			}
			return m_LabelWrapStyle;
		}
	}

	public static GUIStyle SmallTextStyle
	{
		get
		{
			if (m_SmallTextStyle == null)
			{
				m_SmallTextStyle = new GUIStyle("Label");
				m_SmallTextStyle.fontSize = 9;
				m_SmallTextStyle.wordWrap = true;
			}
			return m_SmallTextStyle;
		}
	}


	public static GUIStyle NoteStyle
	{
		get
		{
			if (m_NoteStyle == null)
			{
				m_NoteStyle = new GUIStyle("Label");
				m_NoteStyle.fontSize = 9;
				m_NoteStyle.alignment = TextAnchor.LowerCenter;
			}
			return m_NoteStyle;
		}
	}

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
				m_SmallButtonStyle.padding = new RectOffset(0, 4, 0, 2);
			}
			return m_SmallButtonStyle;
		}
	}
		
	public static GUIStyle RightAlignedPathStyle
	{
		get
		{
			if (m_RightAlignedPathStyle == null)
			{
				m_RightAlignedPathStyle = new GUIStyle("Label");
				m_RightAlignedPathStyle.fontSize = 9;
				m_RightAlignedPathStyle.alignment = TextAnchor.LowerRight;
			}
			return m_RightAlignedPathStyle;
		}
	}

	public static GUIStyle LeftAlignedPathStyle
	{
		get
		{
			if (m_LeftAlignedPathStyle == null)
			{
				m_LeftAlignedPathStyle = new GUIStyle("Label");
				m_LeftAlignedPathStyle.fontSize = 9;
				m_LeftAlignedPathStyle.alignment = TextAnchor.LowerLeft;
				m_LeftAlignedPathStyle.padding = new RectOffset(0, 0, 2, 0);
			}
			return m_LeftAlignedPathStyle;
		}
	}

	public static GUIStyle CenteredBoxStyle
	{
		get
		{
			if (m_CenteredBoxStyle == null)
			{
				m_CenteredBoxStyle = new GUIStyle("Label");
				m_CenteredBoxStyle.fontSize = 10;
				m_CenteredBoxStyle.alignment = TextAnchor.LowerLeft;
			}
			return m_CenteredBoxStyle;
		}
	}

	public static GUIStyle CenteredStyleBold
	{
		get
		{
			if (m_CenteredStyleBold == null)
			{
				m_CenteredStyleBold = new GUIStyle("Label");
				m_CenteredStyleBold.fontSize = 10;
				m_CenteredStyleBold.alignment = TextAnchor.LowerLeft;
				m_CenteredStyleBold.fontStyle = FontStyle.Bold;
			}
			return m_CenteredStyleBold;
		}
	}

	public static GUIStyle CenteredBoxStyleBold
	{
		get
		{
			if (m_CenteredBoxStyleBold == null)
			{
				m_CenteredBoxStyleBold = new GUIStyle("TextField");
				m_CenteredBoxStyleBold.fontSize = 10;
				m_CenteredBoxStyleBold.alignment = TextAnchor.LowerLeft;
				m_CenteredBoxStyleBold.fontStyle = FontStyle.Bold;
			}
			return m_CenteredBoxStyleBold;
		}
	}


}

