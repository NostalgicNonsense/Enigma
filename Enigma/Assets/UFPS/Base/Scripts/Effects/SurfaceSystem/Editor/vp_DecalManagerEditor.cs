/////////////////////////////////////////////////////////////////////////////////
//
//	vp_DecalManagerEditor.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	custom inspector for the vp_DecalManager class
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

[CustomEditor(typeof(vp_DecalManager))]

public class vp_DecalManagerEditor : Editor
{

	// target component
	public vp_DecalManager m_Component;

	/// <summary>
	/// hooks up the component object as the inspector target
	/// </summary>
	public virtual void OnEnable()
	{

		m_Component = (vp_DecalManager)target;

	}

	/// <summary>
	/// 
	/// </summary>
	public override void OnInspectorGUI()
	{

		GUI.color = Color.white;

		GUILayout.Space(10);
		m_Component.m_ShowHelp = GUILayout.Toggle(m_Component.m_ShowHelp, "Show help");
		GUILayout.Space(5);

		DoLimitsFoldout();
		DoPlacementFoldout();
		DoRemovalFoldout();
		DoDebugFoldout();

		if(m_Component.m_ShowHelp)
			vp_EditorGUIUtility.Separator();
		else
			GUILayout.Space(5);

		DoHelpBox();

		if (GUI.changed)
			EditorUtility.SetDirty(target);

	}


	/// <summary>
	/// 
	/// </summary>
	void DoHelpBox()
	{
		
		if (!m_Component.m_ShowHelp)
			return;
		GUILayout.BeginHorizontal();
		GUILayout.Space(10);
		EditorGUILayout.HelpBox("\n• The DecalManager is a scene manager for capping the amount of decals in the level and removing badly placed decals in non-intrusive and elegant ways.\n\n• 'Decal Limits' sets how many decals are allowed in the scene, and how they should age.\n\n• 'Placement Tests' has features to prevent decals from being placed in bad looking ways, for example: overlapping corners.\n\n• 'Removal on Fail' determines how and when decals that have been flagged for removal should be faded out, and when they should be instantly removed.\n\n", MessageType.Info);
		GUILayout.Space(20);
		GUILayout.EndHorizontal();

	}
	

	/// <summary>
	/// 
	/// </summary>
	public virtual void DoLimitsFoldout()
	{

		GUILayout.BeginHorizontal();
		GUILayout.Space(10);
		m_Component.m_LimitsFoldout = EditorGUILayout.Foldout(m_Component.m_LimitsFoldout, "Decal Limits");
		GUILayout.Space(10);
		GUILayout.EndHorizontal();

		if (m_Component.m_LimitsFoldout)
		{


			GUILayout.BeginHorizontal();
			GUILayout.Space(10);
			m_Component.DecalLimit = EditorGUILayout.IntSlider("Total", (int)m_Component.DecalLimit, 10, 300);
			GUILayout.Space(20);
			GUILayout.EndHorizontal();

			if (m_Component.m_ShowHelp)
			{
				GUILayout.BeginHorizontal();	GUILayout.Space(10);	GUI.enabled = false;
				EditorGUILayout.HelpBox("Only this many decals will be allowed in the scene.", MessageType.None);
				GUI.enabled = true;		GUILayout.Space(20);	GUILayout.EndHorizontal();
			}
			GUILayout.BeginHorizontal();
			GUILayout.Space(10);
			m_Component.WeatheredLimit = EditorGUILayout.IntSlider("Weathered", (int)m_Component.WeatheredLimit, 9, 299);
			GUILayout.Space(20);
			GUILayout.EndHorizontal();

			m_Component.WeatheredLimit = Mathf.Min(m_Component.WeatheredLimit, m_Component.DecalLimit - 1);

			if (m_Component.m_ShowHelp)
			{
				GUILayout.BeginHorizontal();	GUILayout.Space(10);	GUI.enabled = false;
				EditorGUILayout.HelpBox("This sets how many of the OLDEST decals will participate in a gradual process of fading just a tiny bit each time a new decal gets spawned (the oldest of these will be almost invisible).", MessageType.None);
				GUI.enabled = true;		GUILayout.Space(20);	GUILayout.EndHorizontal();
			}

			vp_EditorGUIUtility.Separator();

		}
		
	}


	/// <summary>
	/// 
	/// </summary>
	public virtual void DoPlacementFoldout()
	{

		GUILayout.BeginHorizontal();
		GUILayout.Space(10);
		m_Component.m_PlacementFoldout = EditorGUILayout.Foldout(m_Component.m_PlacementFoldout, "Placement Tests");
		GUILayout.Space(10);
		GUILayout.EndHorizontal();
		
		if (m_Component.m_PlacementFoldout)
		{

			GUILayout.BeginHorizontal();
			GUILayout.Space(10);
			GUILayout.BeginVertical();

			m_Component.CleanupOverTime = GUILayout.Toggle(m_Component.CleanupOverTime, "Cleanup over time");

			if (m_Component.m_ShowHelp)
			{
				GUI.enabled = false;
				EditorGUILayout.HelpBox("When enabled (recommended) the DecalManager will slowly and gradually check all decals in the scene for surface contact. Over time, all failed decals will be removed.", MessageType.None);
				GUI.enabled = true;
			}

			if (m_Component.CleanupOverTime)
			{
				m_Component.VertexRaycastInterval = EditorGUILayout.Slider("Vertex Raycast Interval (sec)", m_Component.VertexRaycastInterval, 0.1f, 5.0f);

				m_Component.DecalsPerCleanupBatch = EditorGUILayout.IntSlider("Decals Per Batch", m_Component.DecalsPerCleanupBatch, 1, 10);

				if (m_Component.m_ShowHelp)
				{
					GUI.enabled = false;
					EditorGUILayout.HelpBox("On each interval (in seconds) ONE BATCH OF DECALS somewhere in the scene will each have ONE CORNER (vertex) tested for surface contact.", MessageType.None);
					GUI.enabled = true;
				}

				vp_EditorGUIUtility.Separator();

			}
			
			m_Component.InstantQuadCornerTest = GUILayout.Toggle(m_Component.InstantQuadCornerTest, "Instant quad corner test");
			if (m_Component.m_ShowHelp)
			{
				GUI.enabled = false;
				EditorGUILayout.HelpBox("When enabled, any decals spawning within range of the camera will have ALL FOUR CORNERS raycast for surface contact IMMEDIATELY. On fail, the decal will be removed.", MessageType.None);
				GUI.enabled = true;
			}
			if (m_Component.InstantQuadCornerTest)
			{
				m_Component.QuadRaycastRange = EditorGUILayout.IntSlider("Quad Raycast Range", m_Component.QuadRaycastRange, 1, 20);

				m_Component.MaxQuadRaycastsPerSecond = EditorGUILayout.IntSlider("Max Quad Raycasts / sec", m_Component.MaxQuadRaycastsPerSecond, 1, 20);

				if (m_Component.m_ShowHelp)
				{
					GUI.enabled = false;
					EditorGUILayout.HelpBox("Only this many QUADRUPLE RAYCASTS will be performed each second. Remaining ones will be buffered until the next second.", MessageType.None);
					GUI.enabled = true;
				}
				vp_EditorGUIUtility.Separator();

			}
			
			m_Component.AllowStretchedDecals = GUILayout.Toggle(m_Component.AllowStretchedDecals, "Allow stretched decals");

			if (m_Component.m_ShowHelp)
			{
				GUI.enabled = false;
				EditorGUILayout.HelpBox("If this is enabled (not recommended), any NON-STATIC objects with NON-UNIFORM SCALE will be allowed to receive decals (with a high risk of stretched decals). For more detailed info about this, please see the manual.", MessageType.None);
				GUI.enabled = true;
			}
			GUILayout.EndVertical();
			GUILayout.Space(20);
			GUILayout.EndHorizontal();
			
			vp_EditorGUIUtility.Separator();

		}

	}


	/// <summary>
	/// 
	/// </summary>
	public virtual void DoRemovalFoldout()
	{

		GUILayout.BeginHorizontal();
		GUILayout.Space(10);
		m_Component.m_RemovalFoldout = EditorGUILayout.Foldout(m_Component.m_RemovalFoldout, "Removal on Fail");
		GUILayout.Space(10);
		GUILayout.EndHorizontal();

		if (m_Component.m_RemovalFoldout)
		{

			GUILayout.BeginHorizontal();
			GUILayout.Space(10);
			GUILayout.BeginVertical();

			m_Component.RemoveDelay = EditorGUILayout.Slider("Delay (sec)", m_Component.RemoveDelay, 0.0f, 10.0f);
			if (m_Component.m_ShowHelp)
			{
				GUILayout.BeginHorizontal(); GUILayout.Space(10); GUI.enabled = false;
				EditorGUILayout.HelpBox("When a decal has been flagged for removal (but stays on screen) it will postpone fading out for this many seconds.", MessageType.None);
				GUI.enabled = true; GUILayout.Space(20); GUILayout.EndHorizontal();
			}

			m_Component.RemoveFadeoutSpeed = EditorGUILayout.Slider("Fadeout Speed", m_Component.RemoveFadeoutSpeed, 1.0f, 25.0f);
			if (m_Component.m_ShowHelp)
			{
				GUILayout.BeginHorizontal(); GUILayout.Space(10); GUI.enabled = false;
				EditorGUILayout.HelpBox("Lower values will make flagged decals fade out slowly. Maxing out the slider will make them disappear instantly.", MessageType.None);
				GUI.enabled = true; GUILayout.Space(20); GUILayout.EndHorizontal();
			}

			m_Component.AllowInstaRemoveIfOffscreen = EditorGUILayout.Toggle("Insta-remove if offscreen", m_Component.AllowInstaRemoveIfOffscreen);
			if (m_Component.m_ShowHelp)
			{
				GUILayout.BeginHorizontal(); GUILayout.Space(10); GUI.enabled = false;
				EditorGUILayout.HelpBox("If enabled, any decal that has been flagged for removal will disappear instantly if the player looks away from it, a \"now it's there - now it's not\" trick ...", MessageType.None);
				GUI.enabled = true; GUILayout.Space(20); GUILayout.EndHorizontal();
			}

			GUILayout.EndVertical();
			GUILayout.Space(20);
			GUILayout.EndHorizontal();

			vp_EditorGUIUtility.Separator();

		}

	}

	
	/// <summary>
	/// 
	/// </summary>
	public virtual void DoDebugFoldout()
	{

		GUILayout.BeginHorizontal();
		GUILayout.Space(10);
		m_Component.m_DebugFoldout = EditorGUILayout.Foldout(m_Component.m_DebugFoldout, "Debug");
		GUILayout.Space(10);
		GUILayout.EndHorizontal();

		if (m_Component.m_DebugFoldout)
		{

			if (!Application.isPlaying) 
				GUI.enabled = false;

			GUILayout.BeginHorizontal();
			GUILayout.Space(10);
			m_Component.DebugMode = EditorGUILayout.Toggle("Show raycast points", m_Component.DebugMode);
			GUILayout.Space(20);
			GUILayout.EndHorizontal();

			int activeDecals = 0;
			int fullyVisibleDecals = 0;
			int partlyFadedDecals = 0;
			int flaggedForRemoval = 0;
			int beingRemoved = 0;

			GUI.enabled = false;

			if (!Application.isPlaying)
			{
				activeDecals = 0;
				fullyVisibleDecals = 0;
				partlyFadedDecals = 0;
				flaggedForRemoval = 0;
				beingRemoved = 0;
			}
			else
			{
				foreach (GameObject o in m_Component.Decals)
				{
					if (o == null)
						continue;
					if (o.GetComponent<Renderer>() == null)
						continue;
					if (o.GetComponent<Renderer>().material == null)
						continue;
					if (o.GetComponent<Renderer>().material.color.a == 1)
						fullyVisibleDecals++;
					else
						partlyFadedDecals++;
				}

				activeDecals = m_Component.Decals.Count;
				flaggedForRemoval = vp_DecalManager.m_DecalsToRemoveWhenOffscreen.Count;
				beingRemoved = vp_DecalManager.m_RenderersToQuickFade.Count;

				EditorUtility.SetDirty(target);

			}

			GUILayout.BeginHorizontal();
			GUILayout.Space(10);
			GUILayout.BeginVertical();
			GUILayout.Label("Active decals: " + activeDecals + " / " + (int)m_Component.DecalLimit);
			GUILayout.Label("Fresh: " + fullyVisibleDecals + " / " + (int)(m_Component.DecalLimit - m_Component.WeatheredLimit));
			GUILayout.Label("Weathered: " + partlyFadedDecals + " / " + (int)m_Component.WeatheredLimit);
			GUILayout.Label("Flagged for removal: " + flaggedForRemoval);
			GUILayout.Label("Being Removed: " + beingRemoved);
			GUILayout.EndVertical();
			GUILayout.Space(20);
			GUILayout.EndHorizontal();
			GUILayout.Space(20);
			
			GUI.enabled = true;

		}

	}


}

