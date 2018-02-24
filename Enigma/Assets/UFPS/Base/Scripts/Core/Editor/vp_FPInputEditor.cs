/////////////////////////////////////////////////////////////////////////////////
//
//	vp_FPInputEditor.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	custom inspector for the vp_FPInput class
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(vp_FPInput))]

public class vp_FPInputEditor : Editor
{

	// target component
	public vp_FPInput m_Component = null;

	// input foldouts
	// NOTE: these are static so they remain open when toggling
	// between different components. this simplifies copying
	// content (prefabs / sounds) between components
	public static bool m_MouseCursorFoldout;
	public static bool m_MouseLookFoldout;
	public static bool m_StateFoldout;
	public static bool m_PresetFoldout = true;

	private static vp_ComponentPersister m_Persister = null;

	
	/// <summary>
	/// hooks up the FPSCamera object to the inspector target
	/// </summary>
	public void OnEnable()
	{

		m_Component = (vp_FPInput)target;

		if (m_Persister == null)
			m_Persister = new vp_ComponentPersister();
		m_Persister.Component = m_Component;
		m_Persister.IsActive = true;

		if (m_Component.DefaultState == null)
			m_Component.RefreshDefaultState();
		
	}


	/// <summary>
	/// disables the persister and removes its reference
	/// </summary>
	void OnDestroy()
	{

		if (m_Persister != null)
			m_Persister.IsActive = false;

	}

	
	/// <summary>
	/// 
	/// </summary>
	public override void OnInspectorGUI()
	{

		if (Application.isPlaying || m_Component.DefaultState.TextAsset == null)
		{

			DoMouseLookFoldout();
			DoMouseCursorFoldout();

		}
		else
			vp_PresetEditorGUIUtility.DefaultStateOverrideMessage();

		// state foldout
		m_StateFoldout = vp_PresetEditorGUIUtility.StateFoldout(m_StateFoldout, m_Component, m_Component.States, m_Persister);

		// preset foldout
		m_PresetFoldout = vp_PresetEditorGUIUtility.PresetFoldout(m_PresetFoldout, m_Component);

		// update default state and persist in order not to loose inspector tweaks
		// due to state switches during runtime - UNLESS a runtime state button has
		// been pressed (in which case user wants to toggle states as opposed to
		// reset / alter them)
		if (GUI.changed &&
			(!vp_PresetEditorGUIUtility.RunTimeStateButtonTarget == m_Component))
		{

			EditorUtility.SetDirty(target);

			if (Application.isPlaying)
				m_Component.RefreshDefaultState();

			if (m_Component.Persist)
				m_Persister.Persist();
	
			m_Component.Refresh();

		}
		
	}


	/// <summary>
	/// 
	/// </summary>
	public virtual void DoMouseCursorFoldout()
	{

		m_MouseCursorFoldout = EditorGUILayout.Foldout(m_MouseCursorFoldout, "Mouse Cursor");
		if (m_MouseCursorFoldout)
		{

			m_Component.MouseCursorForced = EditorGUILayout.Toggle("Forced (always shown)", m_Component.MouseCursorForced);
			m_Component.MouseCursorBlocksMouseLook = EditorGUILayout.Toggle("Blocks Mouselook", m_Component.MouseCursorBlocksMouseLook);
			GUILayout.BeginHorizontal();
			GUILayout.Label("Mouse Cursor Zones");
			GUILayout.EndHorizontal();

			for (int v = m_Component.MouseCursorZones.Length - 1; v > -1; v--)
			{
				GUILayout.BeginHorizontal();
				m_Component.MouseCursorZones[v] = EditorGUILayout.RectField(m_Component.MouseCursorZones[v]);
				if (GUILayout.Button("X", vp_EditorGUIUtility.SmallButtonStyle, GUILayout.MinWidth(15), GUILayout.MaxWidth(15), GUILayout.MinHeight(15)))
				{
					List<Rect> l = new List<Rect>(m_Component.MouseCursorZones);
					l.RemoveAt(v);
					m_Component.MouseCursorZones = l.ToArray();
				}
				GUILayout.EndHorizontal();
			}
			if (GUILayout.Button("Add Zone", GUILayout.MinWidth(90), GUILayout.MaxWidth(90)))
			{
				List<Rect> l = new List<Rect>(m_Component.MouseCursorZones);
				l.Add(new Rect());
				m_Component.MouseCursorZones = l.ToArray();
			}

			vp_EditorGUIUtility.Separator();

		}

	}


	/// <summary>
	/// 
	/// </summary>
	public virtual void DoMouseLookFoldout()
	{

		m_MouseLookFoldout = EditorGUILayout.Foldout(m_MouseLookFoldout, "Mouse Look");
		if (m_MouseLookFoldout)
		{

			m_Component.MouseLookSensitivity = EditorGUILayout.Vector2Field("Sensitivity", m_Component.MouseLookSensitivity);
			m_Component.MouseLookMutePitch = EditorGUILayout.Toggle("Mute Pitch", m_Component.MouseLookMutePitch);
			m_Component.MouseLookMuteYaw = EditorGUILayout.Toggle("Mute Yaw", m_Component.MouseLookMuteYaw);
			m_Component.MouseLookSmoothSteps = EditorGUILayout.IntSlider("Smooth Steps", m_Component.MouseLookSmoothSteps, 1, 20);
			m_Component.MouseLookSmoothWeight = EditorGUILayout.Slider("Smooth Weight", m_Component.MouseLookSmoothWeight, 0, 1);
			m_Component.MouseLookAcceleration = EditorGUILayout.Toggle("Acceleration", m_Component.MouseLookAcceleration);
			if (!m_Component.MouseLookAcceleration)
				GUI.enabled = false;
			m_Component.MouseLookAccelerationThreshold = EditorGUILayout.Slider("Acc. Threshold", m_Component.MouseLookAccelerationThreshold, 0, 5);
			GUI.enabled = true;
			m_Component.MouseLookInvert = EditorGUILayout.Toggle("Invert Mouse", m_Component.MouseLookInvert);

			vp_EditorGUIUtility.Separator();
		}

	}



}

