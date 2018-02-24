/////////////////////////////////////////////////////////////////////////////////
//
//	vp_WeaponEditor.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	custom inspector for the vp_FPSWeapon class
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(vp_Weapon))]

public class vp_WeaponEditor : Editor
{

	// target component
	public vp_Weapon m_Component = null;

	// weapon foldouts
	// NOTE: these are static so they remain open when toggling
	// between different components. this simplifies copying
	// content (prefabs / sounds) between components
	public static bool m_WeaponRenderingFoldout;
	public static bool m_WeaponPositionFoldout;
	public static bool m_WeaponRotationFoldout;
	public static bool m_WeaponRetractionFoldout;
	public static bool m_WeaponShakeFoldout;
	public static bool m_WeaponBobFoldout;
	public static bool m_WeaponStepFoldout;
	public static bool m_WeaponIdleFoldout;
	public static bool m_WeaponSoundFoldout;
	public static bool m_WeaponAnimationFoldout;
	public static bool m_StateFoldout;
	public static bool m_PresetFoldout = true;

	private static vp_ComponentPersister m_Persister = null;

	private Vector3 positionDelta = Vector3.zero;
	private Quaternion rotationDelta = Quaternion.identity;
	private bool m_Persist = false;

	private Dictionary<Animator, bool> m_Animators = new Dictionary<Animator, bool>();

	public enum TypeName
	{
		None,
		Firearm,
		Melee,
		Thrown
	}

	public enum GripName
	{
		None,
		OneHanded,
		TwoHanded,
		TwoHandedHeavy
	}

	public void OnSceneGUI()
	{


		if (!Application.isPlaying)
			return;


		if (!m_Component.AllowEditTransform)
			return;

		m_Persist = false;

		if (rotationDelta != m_Component.transform.localRotation)
		{
			m_Component.RotationOffset = m_Component.transform.localEulerAngles;
			m_Persist = true;
		}

		if (positionDelta != m_Component.transform.localPosition)
		{
			m_Component.PositionOffset = m_Component.transform.localPosition;
			m_Persist = true;
		}

		if (m_Persist)
			m_Persister.Persist();

		positionDelta = m_Component.transform.position;
		rotationDelta = m_Component.transform.localRotation;

	}


	/// <summary>
	/// hooks up the FPSCamera object to the inspector target
	/// </summary>
	public void OnEnable()
	{

		m_Component = (vp_Weapon)target;

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

			DoRenderingFoldout();
			DoPositionFoldout();
			DoRotationFoldout();
			DoAnimationFoldout();

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
	void DoUseSceneViewHandlesButton(Tool tool)
	{

		if (!vp_Utility.IsActive(m_Component.gameObject))
			return;

		string toggleText = ((tool == Tool.Move)?"Move":"Rotate") + " In Scene View";

		if (!Application.isPlaying)
		{
			GUI.enabled = false;
			GUILayout.BeginHorizontal();
			GUILayout.BeginVertical();
			m_Component.AllowEditTransform = vp_EditorGUIUtility.SmallToggle(toggleText, m_Component.AllowEditTransform);
			EditorGUILayout.HelpBox("TIP: Scene view handles can be used at runtime.", MessageType.None);
			GUILayout.Space(5);
			GUILayout.EndVertical();
			GUILayout.Space(8);
			GUILayout.EndHorizontal();
			GUI.enabled = true;
		}
		else
		{
			GUILayout.BeginVertical();
			bool wasEditing = m_Component.AllowEditTransform;
			m_Component.AllowEditTransform = vp_EditorGUIUtility.SmallToggle(toggleText, m_Component.AllowEditTransform);

			if (m_Component.AllowEditTransform)
			{
				GUILayout.BeginHorizontal();
				EditorGUILayout.HelpBox("PLEASE NOTE: Scene view changes will only stick if 'Persist Play Mode Changes' (above) is enabled. While editing, animations and recoil are disabled.", MessageType.Warning);
				GUILayout.Space(10);
				GUILayout.EndHorizontal();
			}

			if (!wasEditing && m_Component.AllowEditTransform == true)
			{

				Tools.current = tool;

				m_Animators.Clear();
				foreach (Animator a in m_Component.Transform.root.GetComponentsInChildren<Animator>())
				{
					m_Animators.Add(a, a.enabled);
				}
				if ((m_Animators != null) && (m_Animators.Count > 0))
				{
					foreach (Animator a in m_Animators.Keys)
					{
						a.enabled = false;
					}
				}
				
			}
			else if (wasEditing && m_Component.AllowEditTransform == false)
			{
				if ((m_Animators != null) && (m_Animators.Count > 0))
				{
					foreach (Animator a in m_Animators.Keys)
					{
						bool state;
						if (m_Animators.TryGetValue(a, out state))
							a.enabled = true;//state;	// just setting it to true seems more robust, for now
					}
				}
			}
			GUILayout.EndVertical();

		}

	}


	/// <summary>
	/// 
	/// </summary>
	public virtual void DoRenderingFoldout()
	{

		m_WeaponRenderingFoldout = EditorGUILayout.Foldout(m_WeaponRenderingFoldout, "Rendering");
		if (m_WeaponRenderingFoldout)
		{

			m_Component.Weapon3rdPersonModel = (GameObject)EditorGUILayout.ObjectField("3rd Person (GameObject)", m_Component.Weapon3rdPersonModel, typeof(GameObject), true);
			m_Component.Weapon3rdPersonInvisibleMaterial = (Material)EditorGUILayout.ObjectField("Invisible Material", m_Component.Weapon3rdPersonInvisibleMaterial, typeof(Material), false);

			vp_EditorGUIUtility.Separator();

		}

	}


	/// <summary>
	/// 
	/// </summary>
	public virtual void DoPositionFoldout()
	{

		m_WeaponPositionFoldout = EditorGUILayout.Foldout(m_WeaponPositionFoldout, "Position");
		if (m_WeaponPositionFoldout)
		{

			if (m_Component.AllowEditTransform)
				GUI.enabled = false;
			m_Component.PositionOffset = EditorGUILayout.Vector3Field("Offset", m_Component.PositionOffset);
			GUI.enabled = true;

			GUILayout.BeginHorizontal();
			GUILayout.Space(28);
			DoUseSceneViewHandlesButton(Tool.Move);
			GUILayout.EndHorizontal();

			m_Component.PositionSpring2Stiffness = EditorGUILayout.Slider("Recoil Stiffness", m_Component.PositionSpring2Stiffness, 0, 1);
			m_Component.PositionSpring2Damping = EditorGUILayout.Slider("Recoil Damping", m_Component.PositionSpring2Damping, 0, 1);
			
			vp_EditorGUIUtility.Separator();
		}

	}


	/// <summary>
	/// 
	/// </summary>
	public virtual void DoRotationFoldout()
	{

		m_WeaponRotationFoldout = EditorGUILayout.Foldout(m_WeaponRotationFoldout, "Rotation");
		if (m_WeaponRotationFoldout)
		{

			if (m_Component.AllowEditTransform)
				GUI.enabled = false;
			
			m_Component.RotationOffset = EditorGUILayout.Vector3Field("Offset", m_Component.RotationOffset);
			GUI.enabled = true;

			GUILayout.BeginHorizontal();
			GUILayout.Space(28);
			DoUseSceneViewHandlesButton(Tool.Rotate);
			GUILayout.EndHorizontal();

			m_Component.RotationSpring2Stiffness = EditorGUILayout.Slider("Recoil Stiffn.", m_Component.RotationSpring2Stiffness, 0, 1);
			m_Component.RotationSpring2Damping = EditorGUILayout.Slider("Recoil Damp.", m_Component.RotationSpring2Damping, 0, 1);

			vp_EditorGUIUtility.Separator();
		}
	}


	/// <summary>
	/// 
	/// </summary>
	public virtual void DoAnimationFoldout()
	{

		m_WeaponAnimationFoldout = EditorGUILayout.Foldout(m_WeaponAnimationFoldout, "Animation");
		if (m_WeaponAnimationFoldout)
		{

			m_Component.AnimationGrip = (int)((GripName)EditorGUILayout.EnumPopup("Grip", (GripName)m_Component.AnimationGrip));
			m_Component.AnimationType = (int)((TypeName)EditorGUILayout.EnumPopup("Type", (TypeName)m_Component.AnimationType));

			GUI.enabled = false;
			GUILayout.Label("Should the character use one-handed or two-handed firearm\nor melee animations for this weapon in 3rd person?", vp_EditorGUIUtility.NoteStyle);
			GUI.enabled = true;

			vp_EditorGUIUtility.Separator();
		}

	}
	
}

