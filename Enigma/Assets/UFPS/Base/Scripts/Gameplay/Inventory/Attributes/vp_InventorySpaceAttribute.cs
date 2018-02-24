/////////////////////////////////////////////////////////////////////////////////
//
//	vp_InventorySpaceAttribute.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	inventory space foldout editor logic and drawing
//
/////////////////////////////////////////////////////////////////////////////////
#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using System.Text;
using System.Collections.Generic;
using System.Globalization;
using System.Collections;

	

/// <summary>
/// 
/// </summary>
public class vp_InventorySpaceAttribute : PropertyAttribute
{
}


[CustomPropertyDrawer(typeof(vp_InventorySpaceAttribute))]
public class vp_InventorySpaceDrawer : PropertyDrawer
{

	private System.DateTime m_NextAllowedAutoRefreshTime = System.DateTime.Now;


	/// <summary>
	/// override to adjust with our own height. called by Unity
	/// </summary>
	public override float GetPropertyHeight(SerializedProperty prop, GUIContent label)
	{
		return prop.floatValue;
	}

	[SerializeField]
	private static bool m_ItemFoldout;
	[SerializeField]
	private static bool m_SpaceFoldout;


	/// <summary>
	/// 
	/// </summary>
	public override void OnGUI(Rect pos, SerializedProperty prop, GUIContent label)
	{

		int indentLevelBak = EditorGUI.indentLevel;
		EditorGUI.indentLevel = 1;

		prop.floatValue = DoItemsFoldout(pos, prop);
		EditorGUI.indentLevel = indentLevelBak;


	}


	float initialY = 0.0f;

	vp_Inventory.ItemCap m_CapToRemove;

	/// <summary>
	/// 
	/// </summary>
	float DoItemsFoldout(Rect pos, SerializedProperty prop)
	{
		
		initialY = pos.y;

		pos.height = 16;

		vp_Inventory inventory = ((vp_Inventory)prop.serializedObject.targetObject);

		// while the space foldout is open, we always refresh inventory
		// once per second, in order to catch changes to unit counts
		EditorUtility.SetDirty(prop.serializedObject.targetObject);
		if (System.DateTime.Now > m_NextAllowedAutoRefreshTime)
		{
			inventory.Refresh();
			m_NextAllowedAutoRefreshTime = System.DateTime.Now + System.TimeSpan.FromMilliseconds(1000);
		}

		pos.x += 4;
		bool prev = inventory.SpaceEnabled;
		inventory.SpaceEnabled = EditorGUI.Toggle(pos, "Enabled", inventory.SpaceEnabled);
		if (prev != inventory.SpaceEnabled)
			inventory.Refresh();

		pos.y += 16;
		GUI.enabled = inventory.SpaceEnabled;

		pos.width -= 15;

		// --- draw ---

		vp_Inventory.Mode prevM = inventory.SpaceMode;
		inventory.SpaceMode = (vp_Inventory.Mode)EditorGUI.EnumPopup(pos, "Mode", inventory.SpaceMode);
		if (prevM != inventory.SpaceMode)
		{
			inventory.TotalSpace = Mathf.Max(0.0f, inventory.TotalSpace);
			inventory.Refresh();
		}
		pos.y += 16;

		float prevC = inventory.TotalSpace;
		inventory.TotalSpace = EditorGUI.FloatField(pos, /*((inventory.SpaceMode == vp_Inventory.Mode.Mass) ? "Mass" : "Volume") + */"Capacity", inventory.TotalSpace);
		if (prevC != inventory.TotalSpace)
		{
			inventory.TotalSpace = Mathf.Max(0.0f, inventory.TotalSpace);
			inventory.Refresh();
		}
		pos.y += 19;

		pos.x += 16;
		pos.width -= 21;
		string percent = ((inventory.UsedSpace / inventory.TotalSpace) * 100.0f).ToString("F1");
		percent = percent.Replace(".0", "");
		if (inventory.TotalSpace > 0.0f)
			EditorGUI.ProgressBar(pos, (inventory.UsedSpace / inventory.TotalSpace), "Used: " + inventory.UsedSpace + "/" + inventory.TotalSpace + " (" + percent + "%)");
		else
			EditorGUI.ProgressBar(pos, 1.0f, "Unusable");

		pos.y += 26;

		GUI.enabled = true;
	
		return (pos.y - initialY);

	}


}

#endif





