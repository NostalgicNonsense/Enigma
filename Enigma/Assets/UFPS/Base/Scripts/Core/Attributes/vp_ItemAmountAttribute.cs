/////////////////////////////////////////////////////////////////////////////////
//
//	vp_ItemAmountAttribute.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	this attribute exposes the value of an item amount property globally
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
public class vp_ItemAmountAttribute : PropertyAttribute
{

	public vp_ItemAmountAttribute()
	{
	}

}


/// <summary>
/// 
/// </summary>
[CustomPropertyDrawer(typeof(vp_ItemAmountAttribute))]
public class vp_ItemAmountDrawer : PropertyDrawer
{

	// these two variables get set from the 'vp_ItemTypeAttribute'
	// when another property changes
	public static object ItemAmountTargetObject = null;
	public static int ItemAmountValue = -999999;


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

		if ((ItemAmountValue == -999999) || ItemAmountTargetObject == null)
			ItemAmountValue = prop.intValue;

		//prop.intValue = EditorGUI.IntField(pos, "Amount", prop.intValue);	// uncomment to debug

		if (ItemAmountTargetObject != null && ItemAmountTargetObject == prop.serializedObject)
		{
			prop.intValue = ItemAmountValue;
			ItemAmountTargetObject = null;
		}
				
	}

}


#endif

