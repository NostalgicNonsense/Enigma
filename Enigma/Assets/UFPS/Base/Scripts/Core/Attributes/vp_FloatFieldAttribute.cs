/////////////////////////////////////////////////////////////////////////////////
//
//	vp_FloatFieldAttribute.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	this attribute exposes a float field in the editor, capped
//					between a min and max value
//
/////////////////////////////////////////////////////////////////////////////////

#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;


/// <summary>
/// 
/// </summary>
public class vp_FloatFieldAttribute : PropertyAttribute
{

	public readonly string Label;
	public float Min;
	public float Max;

	public vp_FloatFieldAttribute(string label, float min = -10000000, float max = 10000000)
	{
		Label = label;
		Min = min;
		Max = max;
	}

}


/// <summary>
/// 
/// </summary>
[CustomPropertyDrawer(typeof(vp_FloatFieldAttribute))]
public class vp_FloatFieldDrawer : PropertyDrawer
{

	private vp_FloatFieldAttribute FloatAttribute { get { return ((vp_FloatFieldAttribute)attribute); } }


	/// <summary>
	/// 
	/// </summary>
	public override void OnGUI(Rect pos, SerializedProperty prop, GUIContent label)
	{

		prop.floatValue = vp_PropertyDrawerUtility.ClampedFloatField(pos, FloatAttribute.Label, prop.floatValue, FloatAttribute.Min, FloatAttribute.Max);

	}

}


#endif

