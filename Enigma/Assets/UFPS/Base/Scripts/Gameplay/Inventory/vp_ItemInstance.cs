/////////////////////////////////////////////////////////////////////////////////
//
//	vp_ItemInstance.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	this internal class is used to represent an item record inside
//					the inventory. NOTE: it is not to be confused with the concept
//					of an in-world item gameobject instance (!)
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class vp_ItemInstance
{

	[SerializeField]
	public vp_ItemType Type;
	[SerializeField]
	public int ID = 0;

	[SerializeField]
	public vp_ItemInstance(vp_ItemType type, int id)
	{
		ID = id;
		Type = type;
	}


	/// <summary>
	/// 
	/// </summary>
	public virtual void SetUniqueID()
	{
		ID = vp_Utility.UniqueID;
	}

}




