/////////////////////////////////////////////////////////////////////////////////
//
//	vp_Inventory.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	an inventory system that can be used to give any gameobject
//					(a player, a stash, a box, a vehicle trunk, a medical cabinet
//					etc. etc.) the capability of storing item records of three
//					fundamental types: vp_ItemType, vp_UnitType and vp_UnitBankType.
//
//					it has logic for limiting the amount of storable items in various
//					ways using caps, weight and space. this is customisable down to
//					item type level.
//
//					NOTES:
//					1)	the system relies on ItemType objects, which can be created
//						from the top UFPS menu -> Wizards -> Create Item Type.
//					2)	ItemType objects are attached to the vp_ItemIdentifier and
//						vp_ItemPickup components and used in all communication with the
//						inventory.
//					3)	this system uses lists instead of dictionaries for serialization
//						purposes. this should stay fast unless you mean to store item
//						instances in the thousands.
//					4)	if there's a need to store more than a few scores of a certain
//						item type, then always consider using vp_UnitType.
//					5)	the vp_TargetEvent system is mostly used for interaction with
//						external objects (as opposed to the vp_PlayerEventHandler),
//						since vp_Inventory doesn't necessarily sit on a player.
//					6)	the derived class vp_PlayerInventory should be used for players
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public class vp_Inventory : MonoBehaviour
{

	////////////// 'Item' section ////////////////
	[System.Serializable]
	public class ItemRecordsSection
	{

#if UNITY_EDITOR
		[vp_InventoryItems]
		public float itemList;

		[vp_HelpBox(typeof(ItemRecordsSection), UnityEditor.MessageType.None, typeof(vp_Inventory), null, true)]
		public float helpbox;
#endif

	}
	[SerializeField]
	protected ItemRecordsSection m_ItemRecords;

	////////////// 'Caps' section ////////////////
	[System.Serializable]
	public class ItemCapsSection
	{

#if UNITY_EDITOR
		[vp_InventoryCaps]
		public float itemList;
		[vp_HelpBox(typeof(ItemCapsSection), UnityEditor.MessageType.None, typeof(vp_Inventory), null, true)]
		public float helpbox;
#endif

	}
	[SerializeField]
	protected ItemCapsSection m_ItemCaps;

	////////////// 'Space Limit' section ////////////////
	[System.Serializable]
	public class SpaceLimitSection
	{

#if UNITY_EDITOR
		[vp_InventorySpace]
		public float spaceLimit;
		[vp_HelpBox(typeof(SpaceLimitSection), UnityEditor.MessageType.None, typeof(vp_Inventory), null, true)]
		public float helpbox;
#endif

	}
	[SerializeField]
	protected SpaceLimitSection m_SpaceLimit;
	
	protected Transform m_Transform = null;
	protected Transform Transform
	{
		get
		{
			if(m_Transform == null)
				m_Transform = transform;
			return m_Transform;
		}
	}

	[System.Serializable]
	public class ItemCap
	{

		[SerializeField]
		public vp_ItemType Type = null;
		[SerializeField]
		public int Cap = 0;
		[SerializeField]
		public ItemCap(vp_ItemType type, int cap)
		{
			Type = type;
			Cap = cap;
		}
	}

	[SerializeField]
	[HideInInspector]
	public List<vp_ItemInstance> ItemInstances = new List<vp_ItemInstance>();

	[SerializeField]
	[HideInInspector]
	public List<ItemCap> m_ItemCapInstances = new List<ItemCap>();

	[SerializeField]
	[HideInInspector]
	protected List<vp_UnitBankInstance> m_UnitBankInstances = new List<vp_UnitBankInstance>();
	public List<vp_UnitBankInstance> UnitBankInstances
	{
		get
		{
			return m_UnitBankInstances;
		}
	}
	[SerializeField]
	[HideInInspector]
	protected List<vp_UnitBankInstance> m_InternalUnitBanks = new List<vp_UnitBankInstance>();
	public List<vp_UnitBankInstance> InternalUnitBanks
	{
		get
		{
			return m_InternalUnitBanks;
		}
	}

	protected const int UNLIMITED = -1;
	protected const int UNIDENTIFIED = -1;
	protected const int MAXCAPACITY = -1;

	[SerializeField]
	[HideInInspector]
	public bool CapsEnabled = false;

	[SerializeField]
	[HideInInspector]
	public bool SpaceEnabled = false;

	public enum Mode
	{
		Weight,
		Volume
	}

	[SerializeField]
	[HideInInspector]
	public Mode SpaceMode = Mode.Weight;

	[SerializeField]
	[HideInInspector]
	public bool AllowOnlyListed = false;
	
	[SerializeField]
	[HideInInspector]
	protected float m_TotalSpace = 100;
	public float TotalSpace
	{
		get
		{
			return Mathf.Max(-1, m_TotalSpace);
		}
		set
		{
			m_TotalSpace = value;
		}
	}

	[SerializeField]
	[HideInInspector]
	protected float m_UsedSpace;
	public float UsedSpace
	{
		set
		{
			m_UsedSpace = Mathf.Max(0.0f, value);
		}
		get
		{
			return Mathf.Max(0.0f, m_UsedSpace);
		}
	}


	[SerializeField]
	[HideInInspector]
	public float RemainingSpace
	{
		get
		{
			return Mathf.Max(0.0f, (TotalSpace - UsedSpace));
		}
	}


	/// <summary>
	/// 
	/// </summary>
	protected struct StartItemRecord
	{
		public vp_ItemType Type;
		public int ID;
		public int Amount;
		public StartItemRecord(vp_ItemType type, int id, int amount)
		{
			Type = type;
			ID = id;
			Amount = amount;
		}
	}


	protected bool m_Result;
	protected List<StartItemRecord> m_StartItems = new List<StartItemRecord>();
	protected bool m_FirstItemsDirty = true;
	protected Dictionary<vp_ItemType, vp_ItemInstance> m_FirstItemsOfType = new Dictionary<vp_ItemType, vp_ItemInstance>(100);
	protected vp_ItemInstance m_GetFirstItemInstanceResult;
	protected bool m_ItemDictionaryDirty = true;
	protected Dictionary<int, vp_ItemInstance> m_ItemDictionary = new Dictionary<int, vp_ItemInstance>();
	protected vp_ItemInstance m_GetItemResult;


	/// <summary>
	/// 
	/// </summary>
	protected virtual void Awake()
	{

		SaveInitialState();

	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void Start()
	{

		Refresh();

	}



	/// <summary>
	/// 
	/// </summary>
	protected virtual void OnEnable()
	{

		vp_TargetEventReturn<vp_Inventory>.Register(Transform, "GetInventory", GetInventory);

		vp_TargetEventReturn<vp_ItemType, int, bool>.Register(Transform, "TryGiveItem", TryGiveItem);
		vp_TargetEventReturn<vp_ItemType, int, bool>.Register(Transform, "TryGiveItems", TryGiveItems);
		vp_TargetEventReturn<vp_UnitBankType, int, int, bool>.Register(Transform, "TryGiveUnitBank", TryGiveUnitBank);
		vp_TargetEventReturn<vp_UnitType, int, bool>.Register(Transform, "TryGiveUnits", TryGiveUnits);
		vp_TargetEventReturn<vp_UnitBankType, int, int, bool>.Register(Transform, "TryDeduct", TryDeduct);
		vp_TargetEventReturn<vp_ItemType, int>.Register(Transform, "GetItemCount", GetItemCount);
		
	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void OnDisable()
	{

		vp_TargetEventReturn<vp_ItemType, int, bool>.Unregister(Transform, "TryGiveItem", TryGiveItem);
		vp_TargetEventReturn<vp_ItemType, int, bool>.Unregister(Transform, "TryGiveItems", TryGiveItems);
		vp_TargetEventReturn<vp_UnitBankType, int, int, bool>.Unregister(Transform, "TryGiveUnitBank", TryGiveUnitBank);
		vp_TargetEventReturn<vp_UnitType, int, bool>.Unregister(Transform, "TryGiveUnits", TryGiveUnits);
		vp_TargetEventReturn<vp_UnitBankType, int, int, bool>.Unregister(Transform, "TryDeduct", TryDeduct);
		vp_TargetEventReturn<vp_ItemType, int>.Unregister(Transform, "GetItemCount", GetItemCount);

		vp_TargetEventReturn<vp_Inventory>.Unregister(Transform, "HasInventory", GetInventory);

	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual vp_Inventory GetInventory()
	{
		return this;
	}


	/// <summary>
	/// tries to add a number of items by type and amount.
	/// NOTE: all items with have ID '0'.
	/// </summary>
	public virtual bool TryGiveItems(vp_ItemType type, int amount)
	{
		bool result = false;
		while (amount > 0)
		{
			if (TryGiveItem(type, 0))
				result = true;
			amount--;
		}
		return result;	
	}


	/// <summary>
	/// tries to add items by type, amount and ID.
	/// </summary>
	public virtual bool TryGiveItem(vp_ItemType itemType, int id)
	{

		if (itemType == null)
		{
			Debug.LogError("Error (" + vp_Utility.GetErrorLocation(2) + ") Item type was null.");
			return false;
		}

		// forward to the correct method if this was a unit type
		vp_UnitType unitType = itemType as vp_UnitType;
		if (unitType != null)
			return TryGiveUnits(unitType, id);	// in this case treat int argument as 'amount'

		// forward to the correct method if this was a unitbank type
		vp_UnitBankType unitBankType = itemType as vp_UnitBankType;
		if (unitBankType != null)
			return TryGiveUnitBank(unitBankType, unitBankType.Capacity, id);

		// enforce item cap for this type of item
		if (CapsEnabled)
		{
			int capacity = GetItemCap(itemType);
			if ((capacity != UNLIMITED) && (GetItemCount(itemType) >= capacity))
				return false;
		}

		// enforce space limitations of the inventory
		if (SpaceEnabled)
		{
			if ((UsedSpace + itemType.Space) > TotalSpace)
				return false;
		}

		DoAddItem(itemType, id);

		return true;

	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void DoAddItem(vp_ItemType type, int id)
	{
		//Debug.Log("DoAddItem");
		ItemInstances.Add(new vp_ItemInstance(type, id));
		if (SpaceEnabled)
			m_UsedSpace += type.Space;
		m_FirstItemsDirty = true;
		m_ItemDictionaryDirty = true;
		SetDirty();
	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void DoRemoveItem(vp_ItemInstance item)
	{
		//Debug.Log("DoRemoveItem");
		if (item as vp_UnitBankInstance != null)
		{
			DoRemoveUnitBank(item as vp_UnitBankInstance);
			return;
		}
		
		ItemInstances.Remove(item);

		m_FirstItemsDirty = true;
		m_ItemDictionaryDirty = true;

		if (SpaceEnabled)
			m_UsedSpace = Mathf.Max(0, (m_UsedSpace - item.Type.Space));
		SetDirty();
	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void DoAddUnitBank(vp_UnitBankType unitBankType, int id, int unitsLoaded)
	{
		//Debug.Log("DoAddUnitBank");
		vp_UnitBankInstance bank = new vp_UnitBankInstance(unitBankType, id, this);
		m_UnitBankInstances.Add(bank);
		m_FirstItemsDirty = true;
		m_ItemDictionaryDirty = true;
		if ((SpaceEnabled && !bank.IsInternal))
			m_UsedSpace += unitBankType.Space;								// consume inventory space for the unitbank
		bank.TryGiveUnits(unitsLoaded);
		if (((SpaceEnabled && !bank.IsInternal) && (SpaceMode == Mode.Weight) && (unitBankType.Unit != null)))
			m_UsedSpace += (unitBankType.Unit.Space * bank.Count);		// consume inventory space for the loaded units
		SetDirty();
	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void DoRemoveUnitBank(vp_UnitBankInstance bank)
	{
		//Debug.Log("DoRemoveUnitBank");
		if (!bank.IsInternal)
		{
			m_UnitBankInstances.RemoveAt(m_UnitBankInstances.IndexOf(bank));
			m_FirstItemsDirty = true;
			m_ItemDictionaryDirty = true;
			if (SpaceEnabled)
			{
				m_UsedSpace -= bank.Type.Space;									// release inventory space for the unitbank
				if (SpaceMode == Mode.Weight)
					m_UsedSpace -= (bank.UnitType.Space * bank.Count);			// release inventory space for the loaded units
			}
		}
		else
			m_InternalUnitBanks.RemoveAt(m_InternalUnitBanks.IndexOf(bank));
		SetDirty();
	}


	/// <summary>
	/// 
	/// </summary>
	public virtual bool DoAddUnits(vp_UnitBankInstance bank, int amount)
	{
		//Debug.Log("DoAddUnits");
		return bank.DoAddUnits(amount);
	}


	/// <summary>
	/// 
	/// </summary>
	public virtual bool DoRemoveUnits(vp_UnitBankInstance bank, int amount)
	{
		//Debug.Log("DoRemoveUnits");
		return bank.DoRemoveUnits(amount);
	}
	
	
	/// <summary>
	/// add 'amount' units of 'unitType' to the inventory's internal
	/// unit bank for this type and return the amount successfully added.
	/// if the inventory does not yet have an internal unit bank for
	/// 'unitType', one will be created
	/// </summary>
	public virtual bool TryGiveUnits(vp_UnitType unitType, int amount)
	{
		//Debug.Log("TryGiveUnits: " + unitType + ", " + amount);
		if (GetItemCap(unitType) == 0)
			return false;

		return TryGiveUnits(GetInternalUnitBank(unitType), amount);

	}


	/// <summary>
	/// 
	/// </summary>
	public virtual bool TryGiveUnits(vp_UnitBankInstance bank, int amount)
	{

		if (bank == null)
			return false;

		amount = Mathf.Max(0, amount);

		if (SpaceEnabled && (bank.IsInternal || (SpaceMode == Mode.Weight)))
		{
			if (RemainingSpace < (amount * bank.UnitType.Space))
			{
				amount = ((int)(RemainingSpace / bank.UnitType.Space));
				return DoAddUnits(bank, amount);
			}
		}

		return DoAddUnits(bank, amount);

	}


	/// <summary>
	/// 
	/// </summary>
	public virtual bool TryRemoveUnits(vp_UnitType unitType, int amount)
	{

		vp_UnitBankInstance bank = GetInternalUnitBank(unitType);
		if (bank == null)
			return false;

		return DoRemoveUnits(bank, amount);

	}


	/// <summary>
	/// tries to add an item unit bank (such as a firearm) and - upon success -
	/// tries to fill it with 'unitsLoaded' units. NOTES: 1) unitbanks can only
	/// be added to the inventory one at a time.
	/// 2) 'unitsLoaded' will not be altered to fit in the inventory. that is:
	/// a weapon loaded with more bullets than the player can carry will be
	/// impossible to pick up.
	/// </summary>
	public virtual bool TryGiveUnitBank(vp_UnitBankType unitBankType, int unitsLoaded, int id)
	{
		//Debug.Log("TryGiveUnitBank: " + unitBankType + ", " + unitsLoaded);
		if (unitBankType == null)
		{
			Debug.LogError("Error (" + vp_Utility.GetErrorLocation() + ") 'unitBankType' was null.");
			return false;
		}

		if (CapsEnabled)
		{

			// enforce item cap for this type of unitbank
			int capacity = GetItemCap(unitBankType);
			if ((capacity != UNLIMITED) && (GetItemCount(unitBankType) >= capacity))
				return false;

			// enforce unit capacity of the unitbank type
			if (unitBankType.Capacity != UNLIMITED)
				unitsLoaded = Mathf.Min(unitsLoaded, unitBankType.Capacity);

		}

		if (SpaceEnabled)
		{

			// enforce space (mass / volume) limitations of the inventory
			switch (SpaceMode)
			{
				case Mode.Weight:
					if (unitBankType.Unit == null)
					{
						Debug.LogError("Error (vp_Inventory) UnitBank item type " + unitBankType + " can't be added because its unit type has not been set.");
						return false;
					}
					if (((UsedSpace + unitBankType.Space) + (unitBankType.Unit.Space * unitsLoaded)) > TotalSpace)
						return false;
					break;
				case Mode.Volume:
					if ((UsedSpace + unitBankType.Space) > TotalSpace)
						return false;
					break;
			}

		}

		DoAddUnitBank(unitBankType, id, unitsLoaded);
		
		return true;

	}




	/// <summary>
	/// tries to remove a number of items by type and amount,
	/// regardless of their IDs
	/// </summary>
	public virtual bool TryRemoveItems(vp_ItemType type, int amount)
	{

		bool result = false;
		while (amount > 0)
		{
			if (TryRemoveItem(type, UNIDENTIFIED))
				result = true;
			amount--;
		}
		return result;

	}


	/// <summary>
	/// tries to remove an item by type and ID
	/// </summary>
	public virtual bool TryRemoveItem(vp_ItemType type, int id)
	{

		return TryRemoveItem(GetItem(type, id) as vp_ItemInstance);

	}


	/// <summary>
	/// tries to remove an item instance directly
	/// </summary>
	public virtual bool TryRemoveItem(vp_ItemInstance item)
	{

		if (item == null)
			return false;

		DoRemoveItem(item);
		SetDirty();

		return true;

	}


	/// <summary>
	/// tries to remove a number of unitbanks by type and amount,
	/// regardless of their IDs
	/// </summary>
	public virtual bool TryRemoveUnitBanks(vp_UnitBankType type, int amount)
	{

		bool result = false;
		while (amount > 0)
		{
			if (TryRemoveUnitBank(type, UNIDENTIFIED))
				result = true;
			amount--;
		}
		return result;

	}


	/// <summary>
	/// tries to remove a unitbank by type and ID
	/// </summary>
	public virtual bool TryRemoveUnitBank(vp_UnitBankType type, int id)
	{

		return TryRemoveUnitBank(GetItem(type, id) as vp_UnitBankInstance);

	}


	/// <summary>
	/// tries to remove a unitbank instance directly
	/// </summary>
	public virtual bool TryRemoveUnitBank(vp_UnitBankInstance unitBank)
	{
		
		if (unitBank == null)
			return false;

		DoRemoveUnitBank(unitBank);
		SetDirty();

		return true;

	}


	/// <summary>
	/// NOTE: only for item unit banks (such as weapons)
	/// </summary>
	public virtual bool TryReload(vp_ItemType itemType, int unitBankId)
	{

		return TryReload(GetItem(itemType, unitBankId) as vp_UnitBankInstance, MAXCAPACITY);

	}


	/// <summary>
	/// NOTE: only for item unit banks (such as weapons)
	/// </summary>
	public virtual bool TryReload(vp_ItemType itemType, int unitBankId, int amount)
	{
		return TryReload(GetItem(itemType, unitBankId) as vp_UnitBankInstance, amount);
	}


	/// <summary>
	/// NOTE: only for item unit banks (such as weapons)
	/// </summary>
	public virtual bool TryReload(vp_UnitBankInstance bank)
	{
		return TryReload(bank, MAXCAPACITY);
	}


	/// <summary>
	/// NOTE: only for item unit banks (such as weapons)
	/// </summary>
	public virtual bool TryReload(vp_UnitBankInstance bank, int amount)
	{

		if ((bank == null) || (bank.IsInternal) || (bank.ID == UNIDENTIFIED))
		{
			Debug.LogWarning("Warning (" + vp_Utility.GetErrorLocation() + ") 'TryReloadUnitBank' could not identify a target item. If you are trying to add units to the main inventory please instead use 'TryGiveUnits'.");
			return false;
		}

		// fetch the amount of units in the unitbank prior to reloading
		int preLoadedUnits = bank.Count;		

		// if unitbank is full, there's no point in reloading
		if (preLoadedUnits >= bank.Capacity)
			return false;

		// fetch the current amount of suitable units in the inventory
		int prevInventoryCount = GetUnitCount(bank.UnitType);

		// if inventory is empty, there's not much more to do
		if (prevInventoryCount < 1)
			return false;

		// an amount of -1 means 'fill her up'
		if (amount == MAXCAPACITY)
			amount = bank.Capacity;

		// remove as many units as we can from the inventory
		TryRemoveUnits(bank.UnitType, amount);

		// figure out how many units we managed to remove from inventory
		int unitsRemoved = Mathf.Max(0, (prevInventoryCount - GetUnitCount(bank.UnitType)));

		// try to load the target unit bank with the removed units
		// NOTE: we can never fail on space limit here (since the units we
		// reload with were already in the inventory). however, space may
		// have been deducted when we removed from the internal bank so
		// we need to refresh space when moving units over to the unitbank
		if (!DoAddUnits(bank, unitsRemoved))
			return false;

		// let's see how many units we managed to transfer to the unitbank
		int unitsLoaded = Mathf.Max(0, (bank.Count - preLoadedUnits));

		// if we managed to load zero units, report failure
		if (unitsLoaded < 1)
			return false;

		// if we succeeded in loading some units - but not as many
		// as we removed from the inventory - put the excess units
		// back in the internal unit bank
		if ((unitsLoaded > 0) && (unitsLoaded < unitsRemoved))
			TryGiveUnits(bank.UnitType, (unitsRemoved - unitsLoaded));

		return true;
	
	}


	/// <summary>
	/// NOTE: for use with item unitbanks (such as weapons)
	/// </summary>
	public virtual bool TryDeduct(vp_UnitBankType unitBankType, int unitBankId, int amount)
	{

		vp_UnitBankInstance bank = ((unitBankId < 1) ?	GetItem(unitBankType) as vp_UnitBankInstance :
														GetItem(unitBankType, unitBankId) as vp_UnitBankInstance);

		if (bank == null)
			return false;

		if (!DoRemoveUnits(bank, amount))
			return false;

		// if this operation emptied the unitbank and it's not an
		// internal unitbank, see if it should be depleted
		if ((bank.Count <= 0) && ((bank.Type as vp_UnitBankType).RemoveWhenDepleted))
			DoRemoveUnitBank(bank);

		return true;

	}


	/// <summary>
	/// 
	/// </summary>
	public virtual vp_ItemInstance GetItem(vp_ItemType itemType)
	{

		// TIP: turn the comments in this method into debug output for
		// a very detailed look at the code flow at runtime

		// recreate the dictionary if needed (items may have been added, removed)
		
		if (m_FirstItemsDirty)
		{
			//Debug.Log("recreating the 'm_FirstItemsOfType' dictionary");
			m_FirstItemsOfType.Clear();
			foreach (vp_ItemInstance itemInstance in ItemInstances)
			{
				if (itemInstance == null)
					continue;
				if (!m_FirstItemsOfType.ContainsKey(itemInstance.Type))
					m_FirstItemsOfType.Add(itemInstance.Type, itemInstance);
			}
			foreach (vp_UnitBankInstance itemInstance in UnitBankInstances)
			{
				if (itemInstance == null)
					continue;
				if(!m_FirstItemsOfType.ContainsKey(itemInstance.Type))
					m_FirstItemsOfType.Add(itemInstance.Type, itemInstance);
			}
			m_FirstItemsDirty = false;
		}

		//Debug.Log("trying to fetch an instance of the target item type");
		if ((itemType == null) || !m_FirstItemsOfType.TryGetValue(itemType, out m_GetFirstItemInstanceResult))
		{
			//Debug.Log("no match: returning null");
			return null;
		}

		//Debug.Log("an instance of the target item type was found: perform a null check");
		if (m_GetFirstItemInstanceResult == null)
		{
			//Debug.Log("the instance was null: so refresh dictionary and run the method all over again");
			m_FirstItemsDirty = true;
			return GetItem(itemType);
		}

		//Debug.Log("item was found");
		return m_GetFirstItemInstanceResult;

	}


	/// <summary>
	/// returns the first item instance associated with 'id' (if any).
	/// if 'id' is zero or lower, returns the first item of 'itemType'.
	/// this method uses a temporary dictionary to speed things up.
	/// if the item can't be found in the dictionary, tries to add it
	/// to the dictionary from the serialized list. returns null if
	/// 'itemType' is null or no matching item can be found
	/// </summary>
	public vp_ItemInstance GetItem(vp_ItemType itemType, int id)
	{

		//Debug.Log("GetItem @ " + Time.frameCount);

		// TIP: turn the comments in this method into debug output for
		// a very detailed look at the code flow at runtime

		if (itemType == null)
		{
			Debug.LogError("Error (" + vp_Utility.GetErrorLocation(1, true) + ") Sent a null itemType to 'GetItem'.");
			return null;
		}

		if (id < 1)
		{
			//Debug.Log("no ID was specified: returning the first item of matching type");
			return GetItem(itemType);
		}

		if (m_ItemDictionaryDirty)
		{
			//Debug.Log("resetting the dictionary");
			m_ItemDictionary.Clear();
			m_ItemDictionaryDirty = false;
		}

		//Debug.Log("we have an ID (" + id + "): try to fetch an associated instance from the dictionary");
		if (!m_ItemDictionary.TryGetValue(id, out m_GetItemResult))
		{

			//Debug.Log("DID NOT find item in the dictionary: trying to find it in the list");
			m_GetItemResult = GetItemFromList(itemType, id);
			if ((m_GetItemResult != null) && (id > 0))
			{

				//Debug.Log("found id '"+id+"' in the list! adding it to dictionary");
				m_ItemDictionary.Add(id, m_GetItemResult);
			}
		}
		else if (m_GetItemResult != null)
		{

			//Debug.Log("DID find a quick-match by ID ("+id+") in the dictionary: verifying the item type");
			if (m_GetItemResult.Type != itemType)
			{

				//Debug.Log("type (" + m_GetItemResult.Type + ") was wrong (expected " + itemType + ") trying to get item from the list");
				Debug.LogWarning("Warning: (vp_Inventory) Player has vp_FPWeapons with identical, non-zero vp_ItemIdentifier IDs! This is much slower than using zero or differing IDs.");
				m_GetItemResult = GetItemFromList(itemType, id);
			}
		}
		else
		{

			//Debug.Log("we found item in the dictionary but it was null");
			m_ItemDictionary.Remove(id);
			GetItem(itemType, id);
		}


		//if (m_GetItemResult != null)	Debug.Log("found item: " + m_GetItemResult + ", Type: " + m_GetItemResult.Type + ", ID: " + m_GetItemResult.ID);
		//else							Debug.Log("found no matching item");

		return m_GetItemResult;

	}


	/// <summary>
	/// returns the first item in the inventory by its ItemType
	/// object name. WARNING: this method is potentially quite slow
	/// </summary>
	public virtual vp_ItemInstance GetItem(string itemTypeName)
	{
		//Debug.Log("itemTypeName: " + itemTypeName);
		for (int v = 0; v < InternalUnitBanks.Count; v++)
		{
			if (InternalUnitBanks[v].UnitType.name == itemTypeName)
				return InternalUnitBanks[v];
		}

		for (int v = 0; v < m_UnitBankInstances.Count; v++)
		{
			if (m_UnitBankInstances[v].Type.name == itemTypeName)
				return m_UnitBankInstances[v];
		}

		for (int v = 0; v < ItemInstances.Count; v++)
		{
			if (ItemInstances[v].Type.name == itemTypeName)
				return ItemInstances[v];
		}

		return null;

	}



	/// <summary>
	/// if id is set: looks for a specific item, if not:
	/// returns the first item found
	/// </summary>
	protected virtual vp_ItemInstance GetItemFromList(vp_ItemType itemType, int id = UNIDENTIFIED)
	{

		for (int v = 0; v < m_UnitBankInstances.Count; v++)
		{
			if (m_UnitBankInstances[v].Type != itemType)
				continue;
			if ((id == UNIDENTIFIED) || m_UnitBankInstances[v].ID == id)
				return m_UnitBankInstances[v];
		}

		for (int v = 0; v < ItemInstances.Count; v++)
		{
			if (ItemInstances[v].Type != itemType)
				continue;
			if ((id == UNIDENTIFIED) || ItemInstances[v].ID == id)
				return ItemInstances[v];
		}

		return null;

	}


	/// <summary>
	/// 
	/// </summary>
	public virtual bool HaveItem(vp_ItemType itemType, int id = UNIDENTIFIED)
	{
		if (itemType == null)
			return false;
		return GetItem(itemType, id) != null;
	}


	/// <summary>
	/// NOTE: only for internal inventory unit banks!
	/// </summary>
	public virtual vp_UnitBankInstance GetInternalUnitBank(vp_UnitType unitType)
	{
		
		for (int v = 0; v < m_InternalUnitBanks.Count; v++)
		{
			// is item a unit bank?
			if (m_InternalUnitBanks[v].GetType() != typeof(vp_UnitBankInstance))
				continue;

			// is item internal? (has no vp_ItemType)
			if (m_InternalUnitBanks[v].Type != null)
				continue;
			vp_UnitBankInstance b = (vp_UnitBankInstance)m_InternalUnitBanks[v];
			if(b.UnitType != unitType)
				continue;
			return b;
		}

		SetDirty();

		vp_UnitBankInstance bank = new vp_UnitBankInstance(unitType, this);
		// set capacity to the item cap set for the unit type (if any)
		// if no such cap exists, capacity will be unlimited
		bank.Capacity = GetItemCap(unitType);

		m_InternalUnitBanks.Add(bank);

		return bank;

	}


	/// <summary>
	/// 
	/// </summary>
	public virtual bool HaveInternalUnitBank(vp_UnitType unitType)
	{

		for (int v = 0; v < m_InternalUnitBanks.Count; v++)
		{
			// is item a unit bank?
			if (m_InternalUnitBanks[v].GetType() != typeof(vp_UnitBankInstance))
				continue;

			// is item internal? (has no vp_ItemType)
			if (m_InternalUnitBanks[v].Type != null)
				continue;
			vp_UnitBankInstance b = (vp_UnitBankInstance)m_InternalUnitBanks[v];
			if (b.UnitType != unitType)
				continue;
			return true;
		}

		return false;

	}


	/// <summary>
	/// this method recalculates used inventory space by iterating
	/// all items. useful when the item type 'Space' parameter may
	/// have been altered in the editor for items that are present
	/// in the inventory. NOTE: does not impose item caps. only
	/// updates currently used space
	/// </summary>
	public virtual void Refresh()
	{

		// --- capacity ---

		for (int v = 0; v < m_InternalUnitBanks.Count; v++)
		{
			m_InternalUnitBanks[v].Capacity = GetItemCap(m_InternalUnitBanks[v].UnitType);
		}

		// --- space ---

		if (!SpaceEnabled)
			return;

		m_UsedSpace = 0;

		for (int v = 0; v < ItemInstances.Count; v++)
		{
			m_UsedSpace += ItemInstances[v].Type.Space;
		}

		for (int v = 0; v < m_UnitBankInstances.Count; v++)
		{

			switch (SpaceMode)
			{
				case Mode.Weight:
					m_UsedSpace += m_UnitBankInstances[v].Type.Space + (m_UnitBankInstances[v].UnitType.Space * m_UnitBankInstances[v].Count);
					break;
				case Mode.Volume:
					m_UsedSpace += m_UnitBankInstances[v].Type.Space;
					break;
			}
				
		}

		for (int v = 0; v < m_InternalUnitBanks.Count; v++)
		{
			m_UsedSpace += (m_InternalUnitBanks[v].UnitType.Space * m_InternalUnitBanks[v].Count);
		}

	}


	/// <summary>
	/// 
	/// </summary>
	public virtual int GetItemCount(vp_ItemType type)
	{

		vp_UnitType unitType = type as vp_UnitType;
		if (unitType != null)
			return GetUnitCount(unitType);

		int count = 0;
		
		for (int v = 0; v < ItemInstances.Count; v++)
		{
			if (ItemInstances[v].Type == type)
				count++;
		}

		for (int v = 0; v < m_UnitBankInstances.Count; v++)
		{
			if (m_UnitBankInstances[v].Type == type)
				count++;
		}
				
		return count;

	}


	/// <summary>
	/// sets item count to higher or lower than current, regardless
	/// of item cap. this can be used for game designs allowing
	/// for a one-time addition of excess items. NOTE: if caps
	/// should be imposed, instead use 'TryGiveItem.
	/// </summary>
	public virtual void SetItemCount(vp_ItemType type, int amount)
	{

		if (type is vp_UnitType)
		{
			SetUnitCount((vp_UnitType)type, amount);
			return;
		}

		// if we are to ignore item caps and inventory space, temporarily disable them
		bool capsEnabledBak = CapsEnabled;
		bool spaceEnabledBak = SpaceEnabled;

		CapsEnabled = false;
		SpaceEnabled = false;

		// either give or remove items to reach target amount
		int amountToGive = amount - GetItemCount(type);
		if (amountToGive > 0)
			TryGiveItems(type, amount);
		else if (amountToGive < 0)
			TryRemoveItems(type, -amount);

		CapsEnabled = capsEnabledBak;
		SpaceEnabled = spaceEnabledBak;

	}


	/// <summary>
	/// 
	/// </summary>
	public virtual void SetUnitCount(vp_UnitType unitType, int amount)
	{

		TrySetUnitCount(GetInternalUnitBank((vp_UnitType)unitType), amount);

	}

	/// <summary>
	/// 
	/// </summary>
	public virtual void SetUnitCount(vp_UnitBankInstance bank, int amount)
	{

		if (bank == null)
			return;

		amount = Mathf.Max(0, amount);

		if (amount == bank.Count)
			return;

		int prevInventoryCount = bank.Count;

		if (!DoRemoveUnits(bank, bank.Count))
			bank.Count = prevInventoryCount;

		if (amount == 0)
			return;

		if (!DoAddUnits(bank, amount))
			bank.Count = prevInventoryCount;

	}


	/// <summary>
	/// 
	/// </summary>
	public virtual bool TrySetUnitCount(vp_UnitType unitType, int amount)
	{

		return TrySetUnitCount(GetInternalUnitBank((vp_UnitType)unitType), amount);

	}


	/// <summary>
	/// 
	/// </summary>
	public virtual bool TrySetUnitCount(vp_UnitBankInstance bank, int amount)
	{

		if (bank == null)
			return false;

		amount = Mathf.Max(0, amount);

		if (amount == bank.Count)
			return true;

		int prevInventoryCount = bank.Count;

		if (!DoRemoveUnits(bank, bank.Count))
			bank.Count = prevInventoryCount;

		if (amount == 0)
			return true;

		if(bank.IsInternal)
		{
			m_Result = TryGiveUnits(bank.UnitType, amount);
			if (m_Result == false)
				bank.Count = prevInventoryCount;
			return m_Result;
		}

		m_Result = TryGiveUnits(bank, amount);
		if (m_Result == false)
			bank.Count = prevInventoryCount;
		return m_Result;

	}


	/// <summary>
	/// 
	/// </summary>
	public virtual int GetItemCap(vp_ItemType type)
	{

		if (!CapsEnabled)
			return UNLIMITED;

		for (int v = 0; v < m_ItemCapInstances.Count; v++)
		{
			if (m_ItemCapInstances[v].Type == type)
				return m_ItemCapInstances[v].Cap;
		}

		if (AllowOnlyListed)
			return 0;

		return UNLIMITED;

	}


	/// <summary>
	/// 
	/// </summary>
	public virtual void SetItemCap(vp_ItemType type, int cap, bool clamp = false)
	{

		SetDirty();

		// see if we have an existing cap for 'type'
		for (int v = 0; v < m_ItemCapInstances.Count; v++)
		{
			// if so, change the cap
			if (m_ItemCapInstances[v].Type == type)
			{
				m_ItemCapInstances[v].Cap = cap;
				goto found;
			}
		}
		
		// if not found, create a new cap
		m_ItemCapInstances.Add(new ItemCap(type, cap));

	found:

		// if type is a unit, update capacity of the unit bank
		if (type is vp_UnitType)
		{
			for (int v = 0; v < m_InternalUnitBanks.Count; v++)
			{
				if ((m_InternalUnitBanks[v].UnitType != null) && (m_InternalUnitBanks[v].UnitType == type))
				{
					m_InternalUnitBanks[v].Capacity = cap;
					// clamp amount of units, if specified
					if (clamp)
						m_InternalUnitBanks[v].ClampToCapacity();
				}
			}
		}
		// clamp amount of items, if specified
		else if (clamp)
		{
			if (GetItemCount(type) > cap)
				TryRemoveItems(type, (GetItemCount(type) - cap));
		}

	}


	/// <summary>
	/// 
	/// </summary>
	public virtual int GetUnitCount(vp_UnitType unitType)
	{

		vp_UnitBankInstance v = GetInternalUnitBank(unitType);
		if (v == null)
			return 0;
		return v.Count;

	}


	/// <summary>
	/// 
	/// </summary>
	public virtual void SaveInitialState()
	{

		for (int v = 0; v < InternalUnitBanks.Count; v++)
		{
			m_StartItems.Add(new StartItemRecord(InternalUnitBanks[v].UnitType, 0, InternalUnitBanks[v].Count));
		}

		for (int v = 0; v < m_UnitBankInstances.Count; v++)
		{
			m_StartItems.Add(new StartItemRecord(m_UnitBankInstances[v].Type, m_UnitBankInstances[v].ID, m_UnitBankInstances[v].Count));
		}

		for (int v = 0; v < ItemInstances.Count; v++)
		{
			m_StartItems.Add(new StartItemRecord(ItemInstances[v].Type, ItemInstances[v].ID, 1));
		}

	}

	
	/// <summary>
	/// 
	/// </summary>
	public virtual void Reset()
	{

		Clear();

		for (int v = 0; v < m_StartItems.Count; v++)
		{
			if (m_StartItems[v].Type.GetType() == typeof(vp_ItemType))
				TryGiveItem(m_StartItems[v].Type, m_StartItems[v].ID);
			else if (m_StartItems[v].Type.GetType() == typeof(vp_UnitBankType))
				TryGiveUnitBank((m_StartItems[v].Type as vp_UnitBankType), m_StartItems[v].Amount, m_StartItems[v].ID);
			else if (m_StartItems[v].Type.GetType() == typeof(vp_UnitType))
				TryGiveUnits((m_StartItems[v].Type as vp_UnitType), m_StartItems[v].Amount);
			else if (m_StartItems[v].Type.GetType().BaseType == typeof(vp_ItemType))
				TryGiveItem(m_StartItems[v].Type, m_StartItems[v].ID);
			else if (m_StartItems[v].Type.GetType().BaseType == typeof(vp_UnitBankType))
				TryGiveUnitBank((m_StartItems[v].Type as vp_UnitBankType), m_StartItems[v].Amount, m_StartItems[v].ID);
			else if (m_StartItems[v].Type.GetType().BaseType == typeof(vp_UnitType))
				TryGiveUnits((m_StartItems[v].Type as vp_UnitType), m_StartItems[v].Amount);
		}

	}


	/// <summary>
	/// 
	/// </summary>
	public virtual void Clear()
	{

		for (int v = InternalUnitBanks.Count - 1; v > -1; v--)
		{
			DoRemoveUnitBank(InternalUnitBanks[v]);
		}
		for (int v = m_UnitBankInstances.Count - 1; v > -1; v--)
		{
			DoRemoveUnitBank(m_UnitBankInstances[v]);
		}
		for (int v = ItemInstances.Count - 1; v > -1; v--)
		{
			DoRemoveItem(ItemInstances[v]);
		}

	}


	/// <summary>
	/// 
	/// </summary>
	public virtual void SetTotalSpace(float spaceLimitation)
	{

		SetDirty();

		TotalSpace = Mathf.Max(0, spaceLimitation);

	}


	/// <summary>
	/// 
	/// </summary>
	public virtual void SetDirty()
	{

#if UNITY_EDITOR
		EditorUtility.SetDirty(this);
#endif

	}

	/// <summary>
	/// 
	/// </summary>
	public virtual void ClearItemDictionaries()
	{
#if UNITY_EDITOR
		m_FirstItemsDirty = true;
		m_ItemDictionaryDirty = true;
		m_FirstItemsOfType.Clear();
		m_ItemDictionary.Clear();
#endif
	}

}

