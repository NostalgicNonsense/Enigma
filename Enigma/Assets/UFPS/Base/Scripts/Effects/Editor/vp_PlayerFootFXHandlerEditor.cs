/////////////////////////////////////////////////////////////////////////////////
//
//	vp_PlayerFootFXHandlerEditor.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	custom inspector for the vp_PlayerFootFXHandler class
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(vp_PlayerFootFXHandler))]

public class vp_PlayerFootFXHandlerEditor : Editor
{

	// target component
	public vp_PlayerFootFXHandler m_Component = null;

	// foldouts
	public static bool m_FootstepsFoldout;
	public static bool m_JumpingFoldout;
	public static bool m_FallimpactFoldout;
	public static bool m_DebugFoldout;
	public static bool m_StateFoldout;
	public static bool m_PresetFoldout = true;

	private string m_HelpString = "";

	private static vp_ComponentPersister m_Persister = null;


	/// <summary>
	/// hooks up the object to the inspector target
	/// </summary>
	public virtual void OnEnable()
	{

		m_Component = (vp_PlayerFootFXHandler)target;

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
	public virtual void OnDestroy()
	{

		if (m_Persister != null)
			m_Persister.IsActive = false;

	}


	/// <summary>
	/// 
	/// </summary>
	public override void OnInspectorGUI()
	{

		GUI.color = Color.white;

		string objectInfo = m_Component.gameObject.name;

		if (vp_Utility.IsActive(m_Component.gameObject))
			GUI.enabled = true;
		else
		{
			GUI.enabled = false;
			objectInfo += " (INACTIVE)";
		}

		if (!vp_Utility.IsActive(m_Component.gameObject))
		{
			GUI.enabled = true;
			return;
		}

		if (Application.isPlaying || m_Component.DefaultState.TextAsset == null)
		{

			DoFootstepsFoldout();
			DoJumpingFoldout();
			DoFallimpactFoldout();
			DoDebugFoldout();

		}
		else
			vp_PresetEditorGUIUtility.DefaultStateOverrideMessage();

		// state
		m_StateFoldout = vp_PresetEditorGUIUtility.StateFoldout(m_StateFoldout, m_Component, m_Component.States, m_Persister);

		// preset
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
			{
				m_Component.RefreshDefaultState();
				m_Component.Refresh();
			}

			if (m_Component.Persist)
				m_Persister.Persist();

			m_Component.Refresh();

		}

	}


	/// <summary>
	/// 
	/// </summary>
	public virtual void DoFootstepsFoldout()
	{

		m_FootstepsFoldout = EditorGUILayout.Foldout(m_FootstepsFoldout, "Footsteps");

		if (m_FootstepsFoldout)
		{

			m_Component.Mode = (int)((vp_PlayerFootFXHandler.FootstepMode)EditorGUILayout.EnumPopup("Mode", (vp_PlayerFootFXHandler.FootstepMode)m_Component.Mode));

			switch (m_Component.Mode)
			{
				case (int)vp_PlayerFootFXHandler.FootstepMode.Bypass:
					m_HelpString = "The player will trigger no footstep FX in this mode.";
					break;
				case (int)vp_PlayerFootFXHandler.FootstepMode.DetectBodyStep:
					m_HelpString = "FX will play under the foot that currently breaks the 'Trigger Height' plane.";
					break;
				case (int)vp_PlayerFootFXHandler.FootstepMode.DetectCameraBob:
					m_HelpString = "FX will play when the 1st person camera bob dips, alternating between the left and right foot.";
					break;
				case (int)vp_PlayerFootFXHandler.FootstepMode.FixedTimeInterval:
					m_HelpString = "FX will play using strict timing, alternating between the left and right foot.";
					break;
			}

			EditorGUILayout.HelpBox(m_HelpString, MessageType.None);

			if (m_Component.Mode == (int)vp_PlayerFootFXHandler.FootstepMode.Bypass)
				GUI.enabled = false;

			GUILayout.BeginHorizontal();
			m_Component.FootstepImpactEvent = (vp_ImpactEvent)EditorGUILayout.ObjectField("Impact Event", m_Component.FootstepImpactEvent, typeof(vp_ImpactEvent), false);
			if (GUILayout.Button("X", vp_EditorGUIUtility.SmallButtonStyle, GUILayout.MinWidth(15), GUILayout.MaxWidth(15), GUILayout.MinHeight(15)))
				m_Component.FootstepImpactEvent = null;
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			m_Component.FootLeft = (GameObject)EditorGUILayout.ObjectField("Left Foot", m_Component.FootLeft, typeof(GameObject), true);
			if (GUILayout.Button("X", vp_EditorGUIUtility.SmallButtonStyle, GUILayout.MinWidth(15), GUILayout.MaxWidth(15), GUILayout.MinHeight(15)))
				m_Component.FootLeft = null;
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal();
			m_Component.FootRight = (GameObject)EditorGUILayout.ObjectField("Right Foot", m_Component.FootRight, typeof(GameObject), true);
			if (GUILayout.Button("X", vp_EditorGUIUtility.SmallButtonStyle, GUILayout.MinWidth(15), GUILayout.MaxWidth(15), GUILayout.MinHeight(15)))
				m_Component.FootRight = null;
			GUILayout.EndHorizontal();

			GUI.enabled = true;

			if (m_Component.Mode != (int)vp_PlayerFootFXHandler.FootstepMode.Bypass)
			{
				switch (m_Component.Mode)
				{
					case (int)vp_PlayerFootFXHandler.FootstepMode.Bypass:
						break;
					case (int)vp_PlayerFootFXHandler.FootstepMode.DetectBodyStep:
						m_Component.TriggerHeight = EditorGUILayout.Slider("Trigger Height", m_Component.TriggerHeight, -1, 1);
						m_Component.Sensitivity = EditorGUILayout.Slider("Sensitivity", m_Component.Sensitivity, 0.1f, 2.0f);
						m_Component.ForceAlwaysAnimate = EditorGUILayout.Toggle("Force Always Animate", m_Component.ForceAlwaysAnimate);
						break;
					case (int)vp_PlayerFootFXHandler.FootstepMode.DetectCameraBob:
						break;
					case (int)vp_PlayerFootFXHandler.FootstepMode.FixedTimeInterval:
						m_Component.TimeInterval = EditorGUILayout.Slider("Time Interval", m_Component.TimeInterval, 0.1f, 1);
						break;
				}

				if (m_Component.Mode != (int)vp_PlayerFootFXHandler.FootstepMode.DetectCameraBob)
					m_Component.RequireMoveInput = EditorGUILayout.Toggle("Require move input", m_Component.RequireMoveInput);

				m_Component.VerifyGroundContact = EditorGUILayout.Toggle("Verify ground contact", m_Component.VerifyGroundContact);

			}

			if (m_Component.VerifyGroundContact == true)
				EditorGUILayout.HelpBox("WARNING: For performance reasons it is recommended to only enable this feature in rare circumstances. See the manual for more info.\n", MessageType.Warning);

			GUI.enabled = false;
			EditorGUILayout.HelpBox("What effect gets played depends on the 'ImpactType' along with the 'SurfaceType' the player is currently standing on.\n\n• See the manual for more detailed info about this component and how to set it up.\n", MessageType.None);
			GUI.enabled = true;


			if (m_Component.Mode == (int)vp_PlayerFootFXHandler.FootstepMode.DetectCameraBob)
			{
				if (m_Component.FPCamera == null)
					EditorGUILayout.HelpBox("This mode is designed for a local 1st person player, but this player has no vp_FPCamera component!", MessageType.Error);
				else if (Application.isPlaying && !m_Component.Player.IsFirstPerson.Get())
					EditorGUILayout.HelpBox("This mode does not work with a 3rd person camera.", MessageType.Error);
			}

			vp_EditorGUIUtility.Separator();
		}

	}


	/// <summary>
	/// 
	/// </summary>
	public virtual void DoJumpingFoldout()
	{

		m_JumpingFoldout = EditorGUILayout.Foldout(m_JumpingFoldout, "Jumping");

		if (m_JumpingFoldout)
		{
			GUILayout.BeginHorizontal();
			m_Component.JumpImpactEvent = (vp_ImpactEvent)EditorGUILayout.ObjectField("Impact Event", m_Component.JumpImpactEvent, typeof(vp_ImpactEvent), false);
			if (GUILayout.Button("X", vp_EditorGUIUtility.SmallButtonStyle, GUILayout.MinWidth(15), GUILayout.MaxWidth(15), GUILayout.MinHeight(15)))
				m_Component.FootstepImpactEvent = null;
			GUILayout.EndHorizontal();
		}

	}


	/// <summary>
	/// 
	/// </summary>
	public virtual void DoFallimpactFoldout()
	{

		m_FallimpactFoldout = EditorGUILayout.Foldout(m_FallimpactFoldout, "Fall Impact");

		if (m_FallimpactFoldout)
		{
			GUILayout.BeginHorizontal();
			m_Component.FallImpactEvent = (vp_ImpactEvent)EditorGUILayout.ObjectField("Impact Event", m_Component.FallImpactEvent, typeof(vp_ImpactEvent), false);
			if (GUILayout.Button("X", vp_EditorGUIUtility.SmallButtonStyle, GUILayout.MinWidth(15), GUILayout.MaxWidth(15), GUILayout.MinHeight(15)))
				m_Component.FootstepImpactEvent = null;
			GUILayout.EndHorizontal();

			m_Component.FallImpactThreshold = EditorGUILayout.Slider("Threshold", m_Component.FallImpactThreshold, 0.01f, 1);

		}

	}


	/// <summary>
	/// 
	/// </summary>
	public virtual void DoDebugFoldout()
	{
		m_DebugFoldout = EditorGUILayout.Foldout(m_DebugFoldout, "Debug");
		if (m_DebugFoldout)
		{
			m_Component.PauseOnEveryStep = EditorGUILayout.Toggle("Pause on every step", m_Component.PauseOnEveryStep);
			m_Component.MuteLocalFootsteps = EditorGUILayout.Toggle("Mute local footsteps", m_Component.MuteLocalFootsteps);
			vp_EditorGUIUtility.Separator();
			
		}
	}
	
}

