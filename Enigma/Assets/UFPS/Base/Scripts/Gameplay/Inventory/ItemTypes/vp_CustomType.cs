/////////////////////////////////////////////////////////////////////////////////
//
//	vp_CustomType.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	this is an example script for explaining how to create your own
//					ItemType ScriptableObjects.
//	
//					vp_Inventory.cs can handle your own item types derived from
//					vp_ItemType, vp_UnitBankType and vp_UnitType.
//	
//					PLEASE NOTE:
//					1)	you DO NOT need to create a new script like this for every new item type!!
//						(there can be a hundred ammo variants all using the existing 'UnitType').
//						you'd only do this in case you needed a new, archetypical item category with
//						very unique settings.
//					2)	while it's easy to add editable fields to custom item types, customizing their
//						rendering inside the inventory editor is vastly more complicated. for custom types,
//						it is strongly recommended to rely on the default 'Item', 'UnitBank' and 'Unit'
//						inventory editor appearances.
//	
//					here's how to create a custom ItemType:
//					1)	duplicate this file and replace _every_ instance of the word 'custom' in the
//						below code with a new word of your choosing. name the file accordingly.
//					2)	if you want to declare a new UNITBANK or UNIT modification, change the class to
//						derive from 'vp_UnitBankType' or 'vp_UnitType' (instead of 'vp_ItemType')
//					3)	in vp_UFPSMenu.cs, duplicate the code block relating to the 'Custom' item type
//						and, similarly, replace the word 'Custom' in that code block with your new word
//					4)	you should now be able to create new item types of your custom type by going
//						to the Unity top menu and clicking UFPS -> Create -> Item Type -> 'your type'
//					5)	Done! you can now edit your new item type *.cs file to add unlimited custom
//						fields and a suitable help text, all of which will show up in the Inspector
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class vp_CustomType : vp_ItemType
{

#if UNITY_EDITOR
	[vp_Separator]
	public vp_Separator customSeparator1;
#endif

	public bool Variable1 = false;
	public float Variable2 = 100.0f;

#if UNITY_EDITOR
    [vp_HelpBox("This is an example object. For info on how to create your own item types, see the comments in \"vp_CustomType.cs\"", UnityEditor.MessageType.Info)]
	public float customHelp;
#endif

}



