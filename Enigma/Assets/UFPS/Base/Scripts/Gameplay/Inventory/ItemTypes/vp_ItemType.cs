/////////////////////////////////////////////////////////////////////////////////
//
//	vp_ItemType.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	the ItemType ScriptableObject is an 'ID card' for a gameplay
//					item. ItemType objects are typically attached to the
//					vp_ItemIdentifier and vp_ItemPickup components and used for
//					communication with the vp_Inventory component. ItemType objects
//					are created from the top UFPS menu -> Wizards -> Create Item Type. 
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;

[System.Serializable]
public class vp_ItemType : ScriptableObject
{

	public vp_ItemType()
	{
	}

#if UNITY_EDITOR
	[vp_Separator]	public vp_Separator s1;
#endif
	public string IndefiniteArticle = "a";
	public string DisplayName;
	public string Description;
	public Texture2D Icon;
#if UNITY_EDITOR
	[vp_FloatField("Inventory Space", 0.0f)]
#endif
	public float Space = 0.0f;

	[SerializeField]
	public string DisplayNameFull
	{ get { return IndefiniteArticle + " " + DisplayName; } }	// TIP: you could use System.Globalization -> TextInfo to convert this string to title case among other things

#if UNITY_EDITOR
	[vp_HelpBox("This object declares an ItemType, or an 'ID card' for an object.\n\n• ItemType objects are basically labels that can be attached to gameobjects (using the vp_ItemIdentifier component) saying \"this is a mace\", \"this is a pistol\", \"this is a machinegun bullet\" and so on.\n\n• The vp_Inventory system uses ItemTypes to keep track of stored items.\n\n• The vp_ItemPickup component use them to decide what type of item to distribute when picked up.\n", UnityEditor.MessageType.Info, null, null, false, vp_PropertyDrawerUtility.Space.Nothing)]
	public float itemTypeHelp;
#endif


}

