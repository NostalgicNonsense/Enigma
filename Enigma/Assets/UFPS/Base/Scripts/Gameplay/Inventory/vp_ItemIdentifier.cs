/////////////////////////////////////////////////////////////////////////////////
//
//	vp_ItemIdentifier.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	this component can be added to a gameobject to associate it
//					with a certain item type. as an example, it is used on
//					first person weapon gameobjects to let vp_WeapnHandler know
//					which items to activate / deactivate (or decline the wielding
//					or firing of) depending on inventory status.
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class vp_ItemIdentifier : MonoBehaviour
{
	
	public vp_ItemType Type = null;
	
#if UNITY_EDITOR
	[vp_ItemID]
#endif
	public int ID;


	/// <summary>
	/// 
	/// </summary>
	protected virtual void OnEnable()
	{
		vp_TargetEventReturn<vp_ItemType>.Register(this.transform, "GetItemType", GetItemType);
		vp_TargetEventReturn<int>.Register(this.transform, "GetItemID", GetItemID);
	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void OnDisable()
	{

	}


	/// <summary>
	/// 
	/// </summary>
	public virtual vp_ItemType GetItemType()
	{
		return Type;
	}


	/// <summary>
	/// 
	/// </summary>
	public virtual int GetItemID()
	{
		return ID;
	}

#if UNITY_EDITOR
	[vp_HelpBox("Tips for identifying weapons:\n\n• 'An ItemType' object is required. 'ID' is optional.\n\n• Projectile weapons should have a 'UnitBank' object. \n\n• Melee weapons should have an 'Item' object.\n\n• An ID of zero (0) is the typical weapon setting, and will always target the FIRST record of 'ItemType' in the inventory (whether its ID is zero or not).\n\n• A positive ID will target a SPECIFIC inventory record of matching 'ItemType' and 'ID'.", UnityEditor.MessageType.Info, null, typeof(vp_FPWeapon), true)]
	public float weaponHelp;
#endif

}