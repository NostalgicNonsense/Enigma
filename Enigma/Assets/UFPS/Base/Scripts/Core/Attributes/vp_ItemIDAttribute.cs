/////////////////////////////////////////////////////////////////////////////////
//
//	vp_ItemIDAttribute.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	this attribute exposes the value of an item id property globally
//					so it can be set from another propertydrawer without the need for
//					complicated reflection
//
/////////////////////////////////////////////////////////////////////////////////

#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;


/// <summary>
/// 
/// </summary>
public class vp_ItemIDAttribute : PropertyAttribute
{

	public vp_ItemIDAttribute()
	{
	}

}


/// <summary>
/// 
/// </summary>
[CustomPropertyDrawer(typeof(vp_ItemIDAttribute))]
public class vp_ItemIDDrawer : PropertyDrawer
{

	// these two variables get set from the 'vp_ItemTypeAttribute'
	// when another property changes
	public static object ItemIDTargetObject = null;
	public static int ItemIDValue = -999999;

	private vp_ItemIDAttribute intAttribute { get { return ((vp_ItemIDAttribute)attribute); } }


	/// <summary>
	/// 
	/// </summary>
	public override float GetPropertyHeight(SerializedProperty prop, GUIContent label)
	{

		return 0;

	}


	/// <summary>
	/// 
	/// </summary>
	public override void OnGUI(Rect pos, SerializedProperty prop, GUIContent label)
	{

		if (ItemIDValue == -999999 || ItemIDTargetObject == null)
			ItemIDValue = prop.intValue;

		//prop.intValue = EditorGUI.IntField(pos, "ID", prop.intValue);		// uncomment to debug

		if (ItemIDTargetObject != null && ItemIDTargetObject == prop.serializedObject)
		{
			prop.intValue = ItemIDValue;
			ItemIDTargetObject = null;
		}

	}

}


#endif

