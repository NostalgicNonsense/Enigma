/////////////////////////////////////////////////////////////////////////////////
//
//	vp_HelpBoxAttribute.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	a helpbox implementation that can be used without a customeditor.
//
/////////////////////////////////////////////////////////////////////////////////

#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;




/// <summary>
/// a helpbox implementation that can be used without a customeditor
/// </summary>
public class vp_HelpBoxAttribute : PropertyAttribute
{

	public string Message;			// message that will be shown in the helpbox
	public string ManualURL;		// link to manual chapter. if set, this link will open in a web browser when the box is double-clicked

	public readonly bool ForceHeight;	// if this is true, the helpbox will always be forced to 'Height'
	public readonly bool ExtraHeight;	// if this is true, the helpbox will always be forced to 'Height'
		
	public readonly MessageType Type;	// determines which icon to show: 'Info', 'Warning', 'Error' or 'None'
										// NOTE: 'None' is drawn without an icon and with !GUI.enabled,
										// providing an option for a very subtle helpbox
	
	public Type TargetType;				// if set, the helpbox will only display on components of this type
	public Type RequireComponent;		// if set, the helpbox will only display if the gameobject has a component of this type

	public vp_PropertyDrawerUtility.Space Bottom;	// what to add immediately below the helpbox (nothing, an empty line or a separator)


	/// <summary>
	/// 
	/// </summary>
	public vp_HelpBoxAttribute(string message, MessageType type = MessageType.Info, Type targetType = null, Type requireComponent = null,/* int initialHeight = 50, */bool extraHeight = false, vp_PropertyDrawerUtility.Space bottom = vp_PropertyDrawerUtility.Space.Separator)
	{
		Message = message;
		Type = type;
		Bottom = bottom;
		ExtraHeight = extraHeight;
		TargetType = targetType;
		RequireComponent = requireComponent;
	}

	/// <summary>
	/// 
	/// </summary>
	public vp_HelpBoxAttribute(System.Type objectType, MessageType type = MessageType.Info, Type targetType = null, Type requireComponent = null,/* int initialHeight = 50, */bool extraHeight = false, vp_PropertyDrawerUtility.Space bottom = vp_PropertyDrawerUtility.Space.Separator)
	{
		Message = vp_Help.GetText(objectType);
		ManualURL = vp_Help.GetURL(objectType);
		Type = type;
		Bottom = bottom;
		ExtraHeight = extraHeight;
		TargetType = targetType;
		RequireComponent = requireComponent;
	}

}


/// <summary>
/// property drawer with editor rendering functions
/// </summary>
[CustomPropertyDrawer(typeof(vp_HelpBoxAttribute))]
public class vp_HelpBoxDrawer : PropertyDrawer
{
	
	private vp_HelpBoxAttribute helpAttribute { get { return ((vp_HelpBoxAttribute)attribute); } }

	private bool m_Enabled = true;

	private Type m_TargetType = null;
	private Component m_RequiredComponent = null;
	

	/// <summary>
	/// override to adjust with our own height. called by Unity
	/// </summary>
	public override float GetPropertyHeight(SerializedProperty prop, GUIContent label)
	{

		if (!m_Enabled)
			return 0;

		if (prop.propertyType != SerializedPropertyType.Float)
		{
			Debug.LogError("Error (vp_HelpBox -> \"" + prop.name + "\") vp_HelpBoxes can only be used on 'float' properties (needed to serialize helpbox height).");
			return 0;
		}

		if (prop.floatValue == -1)
			return 0;

		return prop.floatValue;

	}


	/// <summary>
	/// 
	/// </summary>
	public override void OnGUI(Rect pos, SerializedProperty prop, GUIContent label)
	{

		if (string.IsNullOrEmpty(helpAttribute.Message))
			m_Enabled = false;

		if (!m_Enabled)
			return;

		if (prop.propertyType != SerializedPropertyType.Float)
			return;

		if (helpAttribute.TargetType != null)
		{
			if (m_TargetType == null)
			{
				m_TargetType = prop.serializedObject.targetObject.GetType();
				//Debug.Log(prop.serializedObject.targetObject.GetType().BaseType);	// uncomment to print currently drawn base type
			}

			if (helpAttribute.TargetType != m_TargetType &&
				helpAttribute.TargetType != m_TargetType.BaseType)	// TODO: evaluate. if this is too inclusive, add new attribute parameter
			{
				m_Enabled = false;
				return;
			}
		}

		if (helpAttribute.RequireComponent != null)
		{

			if (m_RequiredComponent == null)
				m_RequiredComponent = ((Component)prop.serializedObject.targetObject).GetComponent(helpAttribute.RequireComponent.ToString());

			if (m_RequiredComponent == null)
			{
				m_Enabled = false;
				return;
			}

		}

		if (prop.floatValue == -1)
			return;

		Rect rect = pos;
		rect.x += 16;
		rect.y += 10;
		rect.width -= 30;
		
		vp_PropertyDrawerUtility.HelpBox(rect, helpAttribute.Message, helpAttribute.Type, helpAttribute.ManualURL);

		if (!helpAttribute.ForceHeight)
			prop.floatValue = vp_PropertyDrawerUtility.CalcHelpBoxHeight(rect.width, helpAttribute.Message) +
				((helpAttribute.Bottom != vp_PropertyDrawerUtility.Space.Nothing) ? 16 : 0) +
				20;

		pos.y += prop.floatValue - 20;
		if (helpAttribute.Bottom == vp_PropertyDrawerUtility.Space.Separator)
			vp_PropertyDrawerUtility.Separator(pos);

	}
			
}


#endif

