/////////////////////////////////////////////////////////////////////////////////
//
//	vp_ItemTypeAttribute.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	inspector item card editor logic and drawing
//
/////////////////////////////////////////////////////////////////////////////////

#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using System.Text;
using System.Collections.Generic;


using System.Collections;
using System.Linq;
using System;

public class vp_ItemDrawer : PropertyDrawer
{

}


/// <summary>
/// 
/// </summary>
[CustomPropertyDrawer(typeof(vp_ItemType))]
public class vp_ItemTypeDrawer : vp_ItemDrawer
{

	/// <summary>
	/// override to adjust with our own height. called by Unity
	/// </summary>
	public override float GetPropertyHeight(SerializedProperty prop, GUIContent label)
	{

		if (prop.serializedObject.targetObject.GetType().BaseType == typeof(ScriptableObject))
			return 16;	// drawing a regular object field on a ScriptableObject
		else
			return 55;	// drawing an item card on an advanced component such as vp_Inventory or vp_ItemIdentifier

	}
	
	/// <summary>
	/// 
	/// </summary>
	public override void OnGUI(Rect pos, SerializedProperty prop, GUIContent label)
	{

		// if drawing the top object field of a ScriptableObject vp_ItemType, draw it disabled
		if (prop.serializedObject.targetObject.GetType().BaseType == typeof(ScriptableObject) && prop.name == "m_Script")
		{
			GUI.enabled = false;
			EditorGUI.ObjectField(pos, "", prop.objectReferenceValue, typeof(vp_ItemType), false);
			GUI.enabled = true;
		}

		// below this line we're drawing an item card on an advanced
		// component such as vp_Inventory or vp_ItemIdentifier

		int indentLevelBak = EditorGUI.indentLevel;
		EditorGUI.indentLevel = 1;
		pos.x += 20;
		pos.y += 20;

		if (prop.objectReferenceValue == null)	// box is empty: draw empty slot
		{
			vp_PropertyDrawerUtility.AddObjectBox(pos, "n ItemType", typeof(vp_ItemType),
			delegate(UnityEngine.Object itemType)
			{
				prop.objectReferenceValue = itemType;
			}, pos.width - 50);
		}
		else // box is filled, draw itemcard
		{

			// item identifiers are always drawn as simple item cards
			if (prop.serializedObject.targetObject.GetType() == typeof(vp_ItemIdentifier))
			{
				DrawItem(pos, prop);
				return;
			}

			// in the inventory, unit banks and units have some extra bells and whistles
			if (prop.objectReferenceValue.GetType() == typeof(vp_ItemType))
				DrawItem(pos, prop);
			else if (prop.objectReferenceValue.GetType() == typeof(vp_UnitBankType))
				DrawUnitBank(pos, prop);
			else if (prop.objectReferenceValue.GetType() == typeof(vp_UnitType))
				DrawInternalUnitBank(pos, prop);
			else if (prop.objectReferenceValue.GetType().BaseType == typeof(vp_ItemType))
				DrawItem(pos, prop);
			else if (prop.objectReferenceValue.GetType().BaseType == typeof(vp_UnitBankType))
				DrawUnitBank(pos, prop);
			else if (prop.objectReferenceValue.GetType().BaseType == typeof(vp_UnitType))
				DrawInternalUnitBank(pos, prop);

		}
		EditorGUI.indentLevel = indentLevelBak;

	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void DrawItem(Rect pos, SerializedProperty prop)
	{

		vp_PropertyDrawerUtility.AddObjectBoxBG(pos, pos.width - 50);

		int NOVALUE = -1;
		pos.width -= 22;
		pos.x += 6;
		pos.y += 2;
		vp_ItemType item = (vp_ItemType)prop.objectReferenceValue;
		string name = item.ToString();

		if (vp_PropertyDrawerUtility.ItemCard(pos,
			((item == null) ? null : item.Icon),
			name,
			((item == null) ? null : item),
			ref vp_ItemIDDrawer.ItemIDValue,
			"ID",
			null,
			ref NOVALUE,
			"",
			null,
			delegate()
			{
				prop.objectReferenceValue = null;
				vp_ItemAmountDrawer.ItemAmountValue = 0;
				vp_ItemIDDrawer.ItemIDValue = 0;
			}, 0))
		{
			vp_ItemIDDrawer.ItemIDTargetObject = prop.serializedObject;
		}
	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void DrawUnitBank(Rect pos, SerializedProperty prop)
	{
		vp_PropertyDrawerUtility.AddObjectBoxBG(pos, pos.width - 50);

		pos.width -= 22;
		pos.x += 6;
		pos.y += 2;
		vp_ItemType item = (vp_UnitBankType)prop.objectReferenceValue;
		string name = item.ToString();
		
		if (vp_PropertyDrawerUtility.ItemCard(pos,
			((item == null) ? null : item.Icon),
			name,
			((item == null) ? null : item),
			ref vp_ItemAmountDrawer.ItemAmountValue,
			"Units",
			null,
			ref vp_ItemIDDrawer.ItemIDValue,
			"ID",
			null,
			delegate()
			{
				prop.objectReferenceValue = null;
				vp_ItemAmountDrawer.ItemAmountValue = 0;
				vp_ItemIDDrawer.ItemIDValue = 0;
			}, 0))
		{
			vp_ItemAmountDrawer.ItemAmountTargetObject = prop.serializedObject;
			vp_ItemIDDrawer.ItemIDTargetObject = prop.serializedObject;
		}
	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void DrawInternalUnitBank(Rect pos, SerializedProperty prop)
	{
		vp_PropertyDrawerUtility.AddObjectBoxBG(pos, pos.width - 50);

		//int NOVALUE = -1;		// uncomment to hide ID field
		pos.width -= 22;
		pos.x += 6;
		pos.y += 2;
		vp_ItemType item = (vp_ItemType)prop.objectReferenceValue;
		string name = item.ToString();

		//if(prop.serializedObject.targetObject.GetType() == typeof(vp_ItemPickup))		// this can be done to identify the type of host component

		if (vp_PropertyDrawerUtility.ItemCard(pos,
			((item == null) ? null : item.Icon),
			name,
			((item == null) ? null : item),
			ref vp_ItemAmountDrawer.ItemAmountValue,
			"Units",
			null,
			ref vp_ItemIDDrawer.ItemIDValue,	// set to 'NOVALUE' to hide ID field
			"ID",								// set to "" to hide ID field
			null,
			delegate()
			{
				prop.objectReferenceValue = null;
				vp_ItemAmountDrawer.ItemAmountValue = 0;
				vp_ItemIDDrawer.ItemIDValue = 0;
			}, 0))
		{
			vp_ItemAmountDrawer.ItemAmountTargetObject = prop.serializedObject;
		}
	}

}



/// <summary>
/// 
/// </summary>
[CustomPropertyDrawer(typeof(vp_UnitType))]
public class vp_UnitTypeDrawer : vp_ItemDrawer
{
	public override void OnGUI(Rect pos, SerializedProperty prop, GUIContent label)
	{
		if (prop.name == "m_Script")
		{
			GUI.enabled = false;
			EditorGUI.ObjectField(pos, "", prop.objectReferenceValue, typeof(vp_UnitType), false);
			GUI.enabled = true;
		}
		else
			prop.objectReferenceValue = EditorGUI.ObjectField(pos, Spaces.AddSpaces(prop.name), prop.objectReferenceValue, typeof(vp_UnitType), false);
	}
}


/// <summary>
/// 
/// </summary>
[CustomPropertyDrawer(typeof(vp_UnitBankType))]
public class vp_UnitBankTypeDrawer : vp_ItemDrawer
{
	public override void OnGUI(Rect pos, SerializedProperty prop, GUIContent label)
	{
		if (prop.name == "m_Script")
		{
			GUI.enabled = false;
			EditorGUI.ObjectField(pos, "", prop.objectReferenceValue, typeof(vp_UnitBankType), false);
			GUI.enabled = true;
		}
		else
			prop.objectReferenceValue = EditorGUI.ObjectField(pos, Spaces.AddSpaces(prop.name), prop.objectReferenceValue, typeof(vp_UnitBankType), false);
	}
}


/// <summary>
/// TODO: integrate into vp_EditorGUIUtility
/// </summary>
public static class Spaces
{

	static Dictionary<string, string> StringsWithSpaces = new Dictionary<string, string>();

	public static string AddSpaces(string text)
	{
		if (string.IsNullOrEmpty(text))
			return string.Empty;

		string t;
		if (StringsWithSpaces.TryGetValue(text, out t))
			return t;

		StringBuilder newText = new StringBuilder(text.Length * 2);
		newText.Append(text[0]);
		for (int i = 1; i < text.Length; i++)
		{
			if (char.IsUpper(text[i]))
				if (text[i - 1] != ' ' && text[i - 1] != '_' && !char.IsUpper(text[i - 1]))
					newText.Append(' ');
			newText.Append(text[i]);
		}

		StringsWithSpaces.Add(text, newText.ToString());

		return newText.ToString();
	}
}

#endif