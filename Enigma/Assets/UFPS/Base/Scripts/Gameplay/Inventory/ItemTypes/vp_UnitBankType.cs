/////////////////////////////////////////////////////////////////////////////////
//
//	vp_UnitBankType.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	the UnitBankType ScriptableObject represents a gameplay item that
//					can be loaded with a certain amount of compatible 'Units'. it is
//					typically used for keeping track of the inventory status of
//					projectile weapons, but can be used for all types of devices
//					that are powered by a limited, carriable resource. UnitBankType
//					objects are created from the top UFPS menu -> Wizards -> Create
//					Item Type. 
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System.Collections;
using System.Collections.Generic;


[System.Serializable]
public class vp_UnitBankType : vp_ItemType
{

#if UNITY_EDITOR
	[vp_Separator]
	public vp_Separator s3;
#endif

	[SerializeField]
	public vp_UnitType Unit = null;
	[SerializeField]
	public int Capacity = 10;
	[SerializeField]
	public bool Reloadable = true;
	[SerializeField]
	public bool RemoveWhenDepleted = false;

#if UNITY_EDITOR
	[vp_HelpBox("This object declares a 'UnitBank' item type. Inventory item instances that have been generated from this type can be loaded with a certain amount of compatible 'Units'. Some theoretical UnitBank types would be:\n\n• A 'Magnum357' carrying up to 6 units of type '357MagnumBullet'.\n\n• A 'RedSprayCan' filled with 100 units of type 'RedPaint', and which gets depleted when empty.\n\n• A 'CrossBow' loadable with a single unit of type 'IronBolt'.\n", UnityEditor.MessageType.Info)]
	public float h3;
#endif

}



