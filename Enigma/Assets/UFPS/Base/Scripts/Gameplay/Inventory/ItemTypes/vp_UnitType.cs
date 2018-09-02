/////////////////////////////////////////////////////////////////////////////////
//
//	vp_UnitType.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	this UnitType ScriptableObject represents a limited, carriable
//					resource. units are typically used for powering 'UnitBank' inventory
//					item instances such as weapons and other devices. UnitType objects
//					are created from the top UFPS menu -> Wizards -> Create Item Type. 
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System.Collections;
using System.Collections.Generic;


[System.Serializable]
public class vp_UnitType : vp_ItemType
{

#if UNITY_EDITOR
	[vp_HelpBox("This particular ItemType is a 'UnitType' which represents a limited, carriable resource. For example: '9mmBullet', 'OxygenLitre', 'SilverCoin', or 'BatteryBar'.\n\n• Units are typically used for powering 'UnitBank' inventory item instances such as weapons and other devices.\n\n• NOTE: When loaded into UnitBank instances, Units will only consume inventory space as long as the inventory's 'Space' mode is set to 'Weight'.\n", UnityEditor.MessageType.Info)]
    public float unitTypeHelp;
#endif



}

