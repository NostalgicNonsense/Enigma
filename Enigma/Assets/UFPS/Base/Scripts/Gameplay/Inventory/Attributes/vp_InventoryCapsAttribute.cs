/////////////////////////////////////////////////////////////////////////////////
//
//	vp_InventoryCapsAttribute.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	inventory caps foldout editor logic and drawing
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
public class vp_InventoryCapsAttribute : PropertyAttribute
{
}


[CustomPropertyDrawer(typeof(vp_InventoryCapsAttribute))]
public class vp_InventoryCapsDrawer : PropertyDrawer
{

	protected vp_Inventory.ItemCap m_CapToRemove;
	protected float m_InitialY = 0.0f;

	private string m_ItemCapAlreadyExistsCaption = "Item cap already exists";
	private string m_ItemCapAlreadyExistsMessage = "An item cap for \"{0}\" has already been added. You may adjust or delete this item cap in the list.";

	[SerializeField]
	private static bool m_ItemFoldout;
	[SerializeField]
	private static bool m_CapsFoldout;
	[SerializeField]
	private static bool m_SpaceFoldout;


	/// <summary>
	/// override to adjust with our own height. called by Unity
	/// </summary>
	public override float GetPropertyHeight(SerializedProperty prop, GUIContent label)
	{
		return prop.floatValue;
	}
	

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


	/// <summary>
	/// 
	/// </summary>
	float DoItemsFoldout(Rect pos, SerializedProperty prop)
	{

		m_InitialY = pos.y;

		pos.height = 16;

		vp_Inventory inventory = ((vp_Inventory)prop.serializedObject.targetObject);


		pos.x += 4;
		bool prev = inventory.CapsEnabled;
		inventory.CapsEnabled = EditorGUI.Toggle(pos, "Enabled", inventory.CapsEnabled);
		if (prev != inventory.CapsEnabled)
			inventory.Refresh();

		pos.y += 16;
		GUI.enabled = inventory.CapsEnabled;

		prev = inventory.AllowOnlyListed;
		inventory.AllowOnlyListed = EditorGUI.Toggle(pos, "Allow only listed types", inventory.AllowOnlyListed);
		if (prev != inventory.AllowOnlyListed)
			inventory.Refresh();

		pos.y +=
		((inventory.m_ItemCapInstances.Count > 0) ? 16 : 11); 
		pos.x += 16;
		pos.width -= 15;

		// --- draw item caps ---
		
		for (int v = 0; v < inventory.m_ItemCapInstances.Count; v++)
		{

			vp_Inventory.ItemCap itemCap = inventory.m_ItemCapInstances[v];

			if ((itemCap != null) && (itemCap.Type != null))
			{
				string name = itemCap.Type.ToString();
				int NO_VALUE = -1;
				vp_PropertyDrawerUtility.ItemCard(pos,
					itemCap.Type.Icon,
					name,
					itemCap.Type,
					ref itemCap.Cap,
					"Cap",
					delegate()
					{
						inventory.SetItemCap(itemCap.Type, itemCap.Cap);
						if (itemCap.Type is vp_UnitType)
							inventory.Refresh();
					},
					ref NO_VALUE,
					null,
					null,
					delegate()
					{
						m_CapToRemove = itemCap;
					});
				pos.y += 21;
			}
		}

		// --- draw 'add object' box ---

		pos.y += 16;

				vp_PropertyDrawerUtility.AddObjectBox(pos, "n ItemType", typeof(vp_ItemType), delegate(Object itemType)
		{
			if (inventory.GetItemCap((vp_ItemType)itemType) != -1 && !inventory.AllowOnlyListed)
				EditorUtility.DisplayDialog(m_ItemCapAlreadyExistsCaption, string.Format(m_ItemCapAlreadyExistsMessage, itemType), "OK");

			int defaultCap = 10;
			if (itemType is vp_UnitBankType)
				defaultCap = 1;
			else if (itemType is vp_UnitType)
				defaultCap = 100;
			inventory.SetItemCap((vp_ItemType)itemType, defaultCap);
			inventory.Refresh();
		});

		pos.y += vp_PropertyDrawerUtility.CalcAddObjectBoxHeight - 5;

		// handle removed item caps
		if (m_CapToRemove != null)
		{
			inventory.m_ItemCapInstances.Remove(m_CapToRemove);
			m_CapToRemove = null;
		}

		GUI.enabled = true;
		
		return (pos.y - m_InitialY);

	}


}

#endif





