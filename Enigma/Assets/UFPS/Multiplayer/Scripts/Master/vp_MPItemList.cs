/////////////////////////////////////////////////////////////////////////////////
//
//	vp_MPItemList.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	defines all the item types that can be distributed by the master
//					client in the current level, and contains methods to award these
//					items to players. this typically occurs on-join and on-pickup.
//
//					PLEASE NOTE: in order for an item pickup to work in a scene, this
//					list must contain all item types defined in all scene pickups.
//					if you are using a player with weapons not listed here / not present
//					in the form of pickups, you are unlikely to be able to wield them
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class vp_MPItemList : MonoBehaviour
{

	// TODO: auto-add scene vp_ItemPickups

	public List<vp_ItemType> ItemTypes = new List<vp_ItemType>();
	protected Dictionary<string, vp_ItemType> m_ItemTypesByString = new Dictionary<string, vp_ItemType>();
	static Dictionary<Transform, vp_Inventory> m_RecipientInventories = new Dictionary<Transform, vp_Inventory>();

	private static vp_MPItemList m_Instance = null;
	public static vp_MPItemList Instance
	{
		get
		{
			if (m_Instance == null)
				m_Instance = Component.FindObjectOfType(typeof(vp_MPItemList)) as vp_MPItemList;
			return m_Instance;
		}
	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void Awake()
	{

		foreach (vp_ItemType p in ItemTypes)
		{
			if(p != null)
				m_ItemTypesByString.Add(p.name, p);
		}

	}
	

	/// <summary>
	/// tries to award an item to a recipient by transform, item type
	/// object name and amount
	/// </summary>
	public static bool TryGiveItem(Transform recipient, string itemTypeObjectName, int amount)
	{
		if (Instance == null)
			return false;
		vp_ItemType itemType;
		if (!Instance.m_ItemTypesByString.TryGetValue(itemTypeObjectName, out itemType) || itemType == null)
		{
			Debug.LogError("Error (" + Instance + ") Item type '" + itemTypeObjectName + "' is not present in the vp_MPItemList component. All item types in a multiplayer game must be present in this list.");
			return false;
		}

		return TryGiveItem(recipient, itemType, amount);

	}
	

	/// <summary>
	/// tries to award an item to a recipient by transform, item type
	/// and amount
	/// </summary>
	public static bool TryGiveItem(Transform recipient, vp_ItemType itemType, int amount)
	{

		if (recipient == null)
		{
			Debug.LogError("Error (" + Instance + ") Recipient was null.");
			return false;
		}

		vp_Inventory inventory;
		if (!m_RecipientInventories.TryGetValue(recipient, out inventory))
		{
			inventory = vp_TargetEventReturn<vp_Inventory>.SendUpwards(recipient, "GetInventory");
			m_RecipientInventories.Add(recipient, inventory);
		}

		if (inventory == null)
		{
			Debug.LogError("Error (" + Instance + ") Failed to find an enabled inventory on '" + recipient + "'.");
			return false;
		}

		if (TryMatchExistingItem(inventory, recipient, itemType, amount))
			return true;

		return TryGiveItemAsNew(recipient, itemType, amount);
	
	}


	/// <summary>
	/// returns true if inventory has items of 'itemType', and the
	/// amount of existing items is modified or unchanged. returns
	/// false if there are no matching items
	/// </summary>
	protected static bool TryMatchExistingItem(vp_Inventory inventory, Transform recipient, vp_ItemType itemType, int amount)
	{

		//Debug.Log("--- TryMatchExistingItem ---");
		//Debug.Log("recipient: " + recipient);
		//Debug.Log("item type: " + itemType);
		//Debug.Log("amount: " + amount);

		System.Type type = itemType.GetType();

		if ((type == typeof(vp_UnitBankType)) || (type.BaseType == typeof(vp_UnitBankType)))
		{

			vp_UnitBankInstance ui = inventory.GetItem(itemType) as vp_UnitBankInstance;
			if (ui != null)
			{
				ui.Count = amount;
				return true;
			}
		}
		else if ((type == typeof(vp_UnitType)) || (type.BaseType == typeof(vp_UnitType)))
		{
			if (inventory.HaveInternalUnitBank(itemType as vp_UnitType))
			{
				vp_UnitBankInstance ui = inventory.GetInternalUnitBank(itemType as vp_UnitType);
				ui.Count = amount;
				return true;
			}
		}
		else if ((type == typeof(vp_ItemType)) || (type.BaseType == typeof(vp_ItemType)))
		{

			int existing = inventory.GetItemCount(itemType);
			//Debug.Log("currently existing in inventory: " + existing);

			if (existing > 0)
			{
				int adjustment = (amount - existing);
				//Debug.Log("needed adjustment: " + adjustment);

				if (adjustment < 0)
				{
					//Debug.Log("adjustment less than zero. running 'TryRemoveItems' with argument '" + adjustment + "' and result: " +
					inventory.TryRemoveItems(itemType, Mathf.Abs(adjustment));
					//);
				}
				else if (adjustment > 0)
				{
					//Debug.Log("adjustment more than zero. running 'TryGiveItems' with argument '" + adjustment + "' and result: " +
					inventory.TryGiveItems(itemType, adjustment);
					//);
				}

				return true;
			}
		}

		//Debug.Log("------------------------");

		return false;

	}


	/// <summary>
	/// returns true if we managed to add new items of 'itemType',
	/// false if not
	/// </summary>
	protected static bool TryGiveItemAsNew(Transform recipient, vp_ItemType itemType, int amount)
	{

		System.Type type = itemType.GetType();

		if (type == typeof(vp_ItemType))
			return vp_TargetEventReturn<vp_ItemType, int, bool>.SendUpwards(recipient, "TryGiveItem", itemType, 0);
		else if (type == typeof(vp_UnitBankType))
			return vp_TargetEventReturn<vp_UnitBankType, int, int, bool>.SendUpwards(recipient, "TryGiveUnitBank", (itemType as vp_UnitBankType), amount, 0);
		else if (type == typeof(vp_UnitType))
			return vp_TargetEventReturn<vp_UnitType, int, bool>.SendUpwards(recipient, "TryGiveUnits", (itemType as vp_UnitType), amount);
		else if (type.BaseType == typeof(vp_ItemType))
			return vp_TargetEventReturn<vp_ItemType, int, bool>.SendUpwards(recipient, "TryGiveItem", itemType, 0);
		else if (type.BaseType == typeof(vp_UnitBankType))
			return vp_TargetEventReturn<vp_UnitBankType, int, int, bool>.SendUpwards(recipient, "TryGiveUnitBank", (itemType as vp_UnitBankType), amount, 0);
		else if (type.BaseType == typeof(vp_UnitType))
			return vp_TargetEventReturn<vp_UnitType, int, bool>.SendUpwards(recipient, "TryGiveUnits", (itemType as vp_UnitType), amount);

		return false;

	}


}
