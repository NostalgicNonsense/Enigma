/////////////////////////////////////////////////////////////////////////////////
//
//	vp_SeparatorAttribute.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	editor class for rendering an Inspector separator
//
/////////////////////////////////////////////////////////////////////////////////

#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;


/// <summary>
/// 
/// </summary>
[System.Serializable]
public class vp_Separator
{
}


/// <summary>
/// 
/// </summary>
public class vp_SeparatorAttribute : PropertyAttribute
{
	public vp_SeparatorAttribute(){	}
}


/// <summary>
/// 
/// </summary>
[CustomPropertyDrawer(typeof(vp_SeparatorAttribute))]
public class vp_SeparatorDrawer : PropertyDrawer
{

	/// <summary>
	/// 
	/// </summary>
	public override void OnGUI(Rect pos, SerializedProperty prop, GUIContent label)
	{

		vp_PropertyDrawerUtility.Separator(pos);

	}

}


#endif

