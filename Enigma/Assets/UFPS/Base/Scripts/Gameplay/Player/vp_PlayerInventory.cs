/////////////////////////////////////////////////////////////////////////////////
//
//	vp_PlayerInventory.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	a version of vp_Inventory that is aware of the PlayerEventHandler
//					and uses its events
//
///////////////////////////////////////////////////////////////////////////////// 

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class vp_PlayerInventory : vp_Inventory
{

	protected Dictionary<vp_ItemType, object> m_PreviouslyOwnedItems = new Dictionary<vp_ItemType, object>();
	protected vp_ItemIdentifier m_WeaponIdentifierResult;
	protected string m_MissingHandlerError = "Error (vp_PlayerInventory) this component must be on the same transform as a vp_PlayerEventHandler + vp_WeaponHandler.";

	protected Dictionary<vp_ItemInstance, vp_Weapon> m_ItemWeapons = null;

	protected Dictionary<vp_ItemType, vp_UnitBankInstance> m_ThrowingWeaponUnitBankInstances = new Dictionary<vp_ItemType, vp_UnitBankInstance>();
	protected Dictionary<vp_UnitType, vp_UnitBankType> m_ThrowingWeaponUnitBankTypes = new Dictionary<vp_UnitType, vp_UnitBankType>();
	protected List<vp_UnitType> m_ThrowingWeaponUnitTypes = new List<vp_UnitType>();
	protected bool m_HaveThrowingWeaponInfo = false;

	protected Dictionary<vp_Weapon, vp_ItemIdentifier> m_WeaponIdentifiers = null;
	public Dictionary<vp_Weapon, vp_ItemIdentifier> WeaponIdentifiers
	{
		get
		{
			if (m_WeaponIdentifiers == null)
			{
				m_WeaponIdentifiers = new Dictionary<vp_Weapon, vp_ItemIdentifier>();
				foreach (vp_Weapon w in WeaponHandler.Weapons)
				{
					vp_ItemIdentifier i = w.GetComponent<vp_ItemIdentifier>();
					if (i != null)
					{
						m_WeaponIdentifiers.Add(w, i);
					}
				}
			}
			return m_WeaponIdentifiers;
		}
	}

	protected Dictionary<vp_UnitType, List<vp_Weapon>> m_WeaponsByUnit = null;
	public Dictionary<vp_UnitType, List<vp_Weapon>> WeaponsByUnit
	{
		get
		{
			if (m_WeaponsByUnit == null)
			{
				m_WeaponsByUnit = new Dictionary<vp_UnitType, List<vp_Weapon>>();
				foreach (vp_Weapon w in WeaponHandler.Weapons)
				{
					vp_ItemIdentifier i;
					if (WeaponIdentifiers.TryGetValue(w, out i) && (i != null))
					{
						vp_UnitBankType uType = i.Type as vp_UnitBankType;
						if ((uType != null) && (uType.Unit != null))
						{
							List<vp_Weapon> weaponsWithUnitType;
							if (m_WeaponsByUnit.TryGetValue(uType.Unit, out weaponsWithUnitType))
							{
								if (weaponsWithUnitType == null)
									weaponsWithUnitType = new List<vp_Weapon>();
								m_WeaponsByUnit.Remove(uType.Unit);
							}
							else
								weaponsWithUnitType = new List<vp_Weapon>();
							weaponsWithUnitType.Add(w);
							m_WeaponsByUnit.Add(uType.Unit, weaponsWithUnitType);
						}
					}
				}

			}
			return m_WeaponsByUnit;
		}
	}

	protected vp_ItemInstance m_CurrentWeaponInstance = null;
	protected virtual vp_ItemInstance CurrentWeaponInstance
	{
		get
		{
			if (Application.isPlaying && (WeaponHandler.CurrentWeaponIndex == 0))
			{
				m_CurrentWeaponInstance = null;
				return null;
			}

			if (m_CurrentWeaponInstance == null)
			{
				if (CurrentWeaponIdentifier == null)
				{
					MissingIdentifierError();
					m_CurrentWeaponInstance = null;
					return null;
				}
				m_CurrentWeaponInstance = GetItem(CurrentWeaponIdentifier.Type, CurrentWeaponIdentifier.ID);
			}

			return m_CurrentWeaponInstance;

		}
	}

	private vp_PlayerEventHandler m_Player = null;	// should never be referenced directly
	protected vp_PlayerEventHandler Player	// lazy initialization of the event handler field
	{
		get
		{
			if (this == null)
				return null;
			if (m_Player == null)
				m_Player = transform.GetComponent<vp_PlayerEventHandler>();
			return m_Player;
		}
	}

	private vp_WeaponHandler m_WeaponHandler = null;	// should never be referenced directly
	protected vp_WeaponHandler WeaponHandler	// lazy initialization of the weapon handler field
	{
		get
		{
			if (m_WeaponHandler == null)
				m_WeaponHandler = transform.GetComponent<vp_WeaponHandler>();
			return m_WeaponHandler;
		}
	}

	public vp_ItemIdentifier CurrentWeaponIdentifier
	{
		get
		{
			if (!Application.isPlaying)
				return null;
			return GetWeaponIdentifier(WeaponHandler.CurrentWeapon);
		}
	}


	////////////// 'AutoWield' section ////////////////
	[System.Serializable]
	public class AutoWieldSection
	{
		public bool Always = false;
		public bool IfUnarmed = true;
		public bool IfOutOfAmmo = true;
		public bool IfNotPresent = true;
		public bool FirstTimeOnly = true;

#if UNITY_EDITOR
		[vp_HelpBox(typeof(AutoWieldSection), UnityEditor.MessageType.None, typeof(vp_PlayerInventory), null, true)]
		public float helpbox;
#endif

	}
	[SerializeField]
	protected AutoWieldSection m_AutoWield;
	

	////////////// 'Misc' section ////////////////
	[System.Serializable]
	public class MiscSection
	{
		public bool ResetOnRespawn = true;
	}
	[SerializeField]
	protected MiscSection m_Misc;


	/// <summary>
	/// 
	/// </summary>
	protected virtual vp_ItemIdentifier GetWeaponIdentifier(vp_Weapon weapon)
	{

		if (!Application.isPlaying)
			return null;

		if (weapon == null)
			return null;

		if (!WeaponIdentifiers.TryGetValue(weapon, out m_WeaponIdentifierResult))
		{

			if (weapon == null)
				return null;

			m_WeaponIdentifierResult = weapon.GetComponent<vp_ItemIdentifier>();

			if (m_WeaponIdentifierResult == null)
				return null;

			if (m_WeaponIdentifierResult.Type == null)
				return null;

			WeaponIdentifiers.Add(weapon, m_WeaponIdentifierResult);

		}

		return m_WeaponIdentifierResult;

	}


	/// <summary>
	/// 
	/// </summary>
	protected override void Awake()
	{
		base.Awake();


		// NOTE: if either handler is missing we'll dump an error to the
		// console rather than use 'RequireComponent' (auto-adding these
		// could lead to real messy player setups)
		if (Player == null || WeaponHandler == null)
			Debug.LogError(m_MissingHandlerError);

	}


	/// <summary>
	/// registers this component with the event handler (if any)
	/// </summary>
	protected override void OnEnable()
	{

		base.OnEnable();

		// allow this monobehaviour to talk to the player event handler
		if (Player != null)
			Player.Register(this);

		UnwieldMissingWeapon();

	}


	/// <summary>
	/// unregisters this component from the event handler (if any)
	/// </summary>
	protected override void OnDisable()
	{

		base.OnDisable();

		// unregister this monobehaviour from the player event handler
		if (Player != null)
			Player.Unregister(this);

	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual bool MissingIdentifierError(int weaponIndex = 0)
	{

		if (!Application.isPlaying)
			return false;
		
		if (weaponIndex < 1)
			return false;

		if (WeaponHandler == null)
			return false;

		if(!(WeaponHandler.Weapons.Count > weaponIndex - 1))
			return false;

		Debug.LogWarning(string.Format("Warning: Weapon gameobject '" + WeaponHandler.Weapons[weaponIndex - 1].name + "' lacks a properly set up vp_ItemIdentifier component!"));

		return false;

	}


	/// <summary>
	/// 
	/// </summary>
	protected override void DoAddItem(vp_ItemType type, int id)
	{

		bool alreadyHaveIt = vp_Gameplay.IsMultiplayer ? HaveItem(type) : HaveItem(type, id);	// NOTE: id not supported in UFPS multiplayer add-on

		base.DoAddItem(type, id);

		TryWieldNewItem(type, alreadyHaveIt);

	}


	/// <summary>
	/// 
	/// </summary>
	protected override void DoRemoveItem(vp_ItemInstance item)
	{

		Unwield(item);
		base.DoRemoveItem(item);

	}


	/// <summary>
	/// 
	/// </summary>
	protected override void DoAddUnitBank(vp_UnitBankType unitBankType, int id, int unitsLoaded)
	{

		bool alreadyHaveIt = vp_Gameplay.IsMultiplayer ?
			HaveItem(unitBankType)			// NOTE: id not supported in UFPS multiplayer add-on
			: HaveItem(unitBankType, id);	// singleplayer

		base.DoAddUnitBank(unitBankType, id, unitsLoaded);

		TryWieldNewItem(unitBankType, alreadyHaveIt);

	}


	
	/// <summary>
	/// 
	/// </summary>
	protected virtual void TryWieldNewItem(vp_ItemType type, bool alreadyHaveIt)
	{

		bool haveHadItBefore = m_PreviouslyOwnedItems.ContainsKey(type);
		if (!haveHadItBefore)
			m_PreviouslyOwnedItems.Add(type, null);

		// --- see if we should try to wield a weapon because of this item pickup ---

		if ((m_AutoWield != null) && m_AutoWield.Always)
			goto tryWield;

		if ((m_AutoWield != null) && m_AutoWield.IfUnarmed && (WeaponHandler.CurrentWeaponIndex < 1))
			goto tryWield;

		if ((m_AutoWield != null) && m_AutoWield.IfOutOfAmmo
			&& (WeaponHandler.CurrentWeaponIndex > 0)
			&& (WeaponHandler.CurrentWeapon.AnimationType != (int)vp_Weapon.Type.Melee)
			&& m_Player.CurrentWeaponAmmoCount.Get() < 1)
			goto tryWield;

		if ((m_AutoWield != null) && m_AutoWield.IfNotPresent && !m_AutoWield.FirstTimeOnly && !alreadyHaveIt)
			goto tryWield;

		if ((m_AutoWield != null) && m_AutoWield.FirstTimeOnly && !haveHadItBefore)
			goto tryWield;

		return;

	tryWield:

		if ((type is vp_UnitBankType))
			TryWield(GetItem(type));
		else if (type is vp_UnitType)
			TryWieldByUnit(type as vp_UnitType);
		else if (type is vp_ItemType)	// tested last since the others derive from it
			TryWield(GetItem(type));
		else
		{
			System.Type baseType = type.GetType();
			if (baseType == null)
				return;
			baseType = baseType.BaseType;
			if ((baseType == typeof(vp_UnitBankType)))
				TryWield(GetItem(type));
			else if (baseType == typeof(vp_UnitType))
				TryWieldByUnit(type as vp_UnitType);
			else if (baseType == typeof(vp_ItemType))
				TryWield(GetItem(type));
		}
		
	}


	/// <summary>
	/// 
	/// </summary>
	protected override void DoRemoveUnitBank(vp_UnitBankInstance bank)
	{

		Unwield(bank);
		base.DoRemoveUnitBank(bank);

	}


	/// <summary>
	/// 
	/// </summary>
	public virtual vp_Weapon GetWeaponOfItemInstance(vp_ItemInstance itemInstance)
	{

		if (m_ItemWeapons == null)
		{
			m_ItemWeapons = new Dictionary<vp_ItemInstance, vp_Weapon>();
		}

		vp_Weapon weapon;
		m_ItemWeapons.TryGetValue(itemInstance, out weapon);
		if (weapon != null)
			return weapon;

		try
		{
			for (int v = 0; v < WeaponHandler.Weapons.Count; v++)
			{
				vp_ItemInstance i = GetItemInstanceOfWeapon(WeaponHandler.Weapons[v]);

				Debug.Log("weapon with index: " + v + ", item instance: " + ((i == null) ? "(have none)" : i.Type.ToString()));
				
				if (i != null)
				{
					if (i.Type == itemInstance.Type)
					{
						weapon = WeaponHandler.Weapons[v];
						m_ItemWeapons.Add(i, weapon);
						return weapon;
					}
				}
			}
		}
		catch
		{
			Debug.LogError("Exception " + this + " Crashed while trying to get item instance for a weapon. Likely a nullreference.");
		}

		return null;
	}

	
	/// <summary>
	/// 
	/// </summary>
	public override bool DoAddUnits(vp_UnitBankInstance bank, int amount)
	{
		if(bank == null)
			return false;

		int prevUnitCount = GetUnitCount(bank.UnitType);

		bool result = base.DoAddUnits(bank, amount);

		// if units were added to the inventory (and not to a weapon)
		if ((result == true && bank.IsInternal) )
		{

			try
			{
				TryWieldNewItem(bank.UnitType, (prevUnitCount != 0));
			}
			catch
			{
				// DEBUG: uncomment on elusive item wielding issues
				//Debug.LogError("Error (" + this + ") Failed to wield new item.");
			}

			// --- auto reload firearms ---

			if(!((Application.isPlaying) && WeaponHandler.CurrentWeaponIndex == 0))
			{
				// fetch the inventory record for the current weapon to see
				// if we should reload it straight away
				vp_UnitBankInstance curBank = (CurrentWeaponInstance as vp_UnitBankInstance);
				if (curBank != null)
				{
					// if the currently wielded weapon uses the same kind of units,
					// and is currently out of ammo ...
					if ((bank.UnitType == curBank.UnitType) && (curBank.Count == 0))
					{
						Player.AutoReload.Try();	// try to auto-reload (success determined by weaponhandler)
					}
				}
			}
			
		}

		return result;

	}


	/// <summary>
	/// 
	/// </summary>
	public override bool DoRemoveUnits(vp_UnitBankInstance bank, int amount)
	{

		bool result = base.DoRemoveUnits(bank, amount);

		if (bank.Count == 0)
			vp_Timer.In(0.3f, delegate() { Player.AutoReload.Try(); });		// try to auto-reload (success determined by weaponhandler)

		return result;
	}
	

	/// <summary>
	///	
	/// </summary>
	public vp_UnitBankInstance GetUnitBankInstanceOfWeapon(vp_Weapon weapon)
	{

		return GetItemInstanceOfWeapon(weapon) as vp_UnitBankInstance;

	}


	/// <summary>
	///	
	/// </summary>
	public vp_ItemInstance GetItemInstanceOfWeapon(vp_Weapon weapon)
	{

		vp_ItemIdentifier itemIdentifier = GetWeaponIdentifier(weapon);
		if (itemIdentifier == null)
			return null;

		vp_ItemInstance ii = GetItem(itemIdentifier.Type);

		return ii;

	}


	/// <summary>
	/// 
	/// </summary>
	public int GetAmmoInWeapon(vp_Weapon weapon)
	{

		vp_UnitBankInstance unitBank = GetUnitBankInstanceOfWeapon(weapon);
		if (unitBank == null)
			return 0;

		return unitBank.Count;

	}


	/// <summary>
	/// 
	/// </summary>
	public int GetExtraAmmoForWeapon(vp_Weapon weapon)
	{

		vp_UnitBankInstance unitBank = GetUnitBankInstanceOfWeapon(weapon);
		if (unitBank == null)
			return 0;

		return GetUnitCount(unitBank.UnitType);

	}


	/// <summary>
	/// 
	/// </summary>
	public int GetAmmoInCurrentWeapon()
	{
		return OnValue_CurrentWeaponAmmoCount;
	}


	/// <summary>
	/// 
	/// </summary>
	public int GetExtraAmmoForCurrentWeapon()
	{
		return OnValue_CurrentWeaponClipCount;
	}


	/// <summary>
	/// analyzes the player weapons and inventory unitbank and unit types to
	/// figure out what weapons are throwing weapons, and caches this info
	/// </summary>
	protected virtual void StoreThrowingWeaponInfo()
	{

		foreach (vp_Weapon weapon in WeaponHandler.Weapons)
		{

			// if the weapon's animation type is 'thrown' ...
			if (!(weapon.AnimationType == (int)vp_Weapon.Type.Thrown))   
				continue;

			// ... and it has an item identifier ...
			vp_ItemIdentifier identifier = weapon.GetComponent<vp_ItemIdentifier>();
			if (identifier == null)
				continue;

			// ... and the identifier is for a unitbank ...
			vp_UnitBankType unitBankType = (identifier.GetItemType() as vp_UnitBankType);
			if (unitBankType == null)
				continue;

			// --- then consider it a throwing weapon unitbank type ---

			// store the unitbank type under its unit type
			if(!m_ThrowingWeaponUnitBankTypes.ContainsKey(unitBankType.Unit))
				m_ThrowingWeaponUnitBankTypes.Add(unitBankType.Unit, unitBankType);

			// store the unit type as known throwing weapon ammo
			if (!m_ThrowingWeaponUnitTypes.Contains(unitBankType.Unit))
				m_ThrowingWeaponUnitTypes.Add(unitBankType.Unit);

			// if the inventory has a unitbank instance for this weapon ...
			vp_UnitBankInstance unitBankInstance = GetUnitBankInstanceOfWeapon(weapon);
			if (unitBankInstance == null)
				continue;

			// ... then store it by its unitbank type
			if (!m_ThrowingWeaponUnitBankInstances.ContainsKey(unitBankType))
				m_ThrowingWeaponUnitBankInstances.Add(unitBankType, unitBankInstance);

		}

		m_HaveThrowingWeaponInfo = true;

	}


	/// <summary>
	/// returns the first known vp_UnitBankInstance of a certain UnitBankType,
	/// provided that the player rig has a vp_Weapon with the same UnitBankType
	/// that has 'AnimationType:Thrown'.
	/// </summary>
	public virtual vp_UnitBankInstance GetThrowingWeaponUnitBankInstance(vp_UnitBankType unitBankType)
	{
		
		if (WeaponHandler == null)
			return null;

		if (!m_HaveThrowingWeaponInfo)
			StoreThrowingWeaponInfo();

		vp_UnitBankInstance unitBankInstance = null;
		m_ThrowingWeaponUnitBankInstances.TryGetValue(unitBankType, out unitBankInstance);

		return unitBankInstance;

	}


	/// <summary>
	/// returns the UnitBank type of the first known vp_Weapon that has the
	/// passed UnitType and 'AnimationType:Thrown'.
	/// </summary>
	public virtual vp_UnitBankType GetThrowingWeaponUnitBankType(vp_UnitType unitType)
	{
		if (!m_HaveThrowingWeaponInfo)
			StoreThrowingWeaponInfo();


		vp_UnitBankType unitBankType = null;
		m_ThrowingWeaponUnitBankTypes.TryGetValue(unitType, out unitBankType);
		return unitBankType;

	}


	/// <summary>
	/// returns true if the player rig has a vp_Weapon with the same UnitBankType
	/// and 'AnimationType:Thrown'.
	/// </summary>
	public virtual bool IsThrowingUnitBank(vp_UnitBankType unitBankType)
	{

		return GetThrowingWeaponUnitBankInstance(unitBankType) != null;

	}


	/// <summary>
	/// returns true if the player rig has a vp_Weapon with the same UnitType and
	/// 'AnimationType:Thrown'.
	/// </summary>
	public virtual bool IsThrowingUnit(vp_UnitType unitType)
	{

		if (!m_HaveThrowingWeaponInfo)
			StoreThrowingWeaponInfo();

		return (m_ThrowingWeaponUnitTypes.Contains(unitType));

	}


	/// <summary>
	/// unwields the currently wielded weapon if not present in
	/// the inventory
	/// </summary>
	protected virtual void UnwieldMissingWeapon()
	{

		if (!Application.isPlaying)
			return;

		if (WeaponHandler.CurrentWeaponIndex < 1)
			return;

		if ((CurrentWeaponIdentifier != null) &&
			HaveItem(CurrentWeaponIdentifier.Type, CurrentWeaponIdentifier.ID))
			return;

		if (CurrentWeaponIdentifier == null)
			MissingIdentifierError(WeaponHandler.CurrentWeaponIndex);

		Player.SetWeapon.TryStart(0);

	}

	
	/// <summary>
	/// 
	/// </summary>
	protected bool TryWieldByUnit(vp_UnitType unitType)
	{
		
		// try to find a weapon with this unit type
		List<vp_Weapon> weaponsWithUnitType;
		if(WeaponsByUnit.TryGetValue(unitType, out weaponsWithUnitType)
			&& (weaponsWithUnitType != null)
			&& (weaponsWithUnitType.Count > 0)
			)
		{
			// try to set the first weapon we find that uses this unit type
			foreach (vp_Weapon w in weaponsWithUnitType)
			{
				if (m_Player.SetWeapon.TryStart(WeaponHandler.Weapons.IndexOf(w) + 1))
					return true;	// found matching weapon: stop looking
			}
		}

		return false;
		
	}

	
	/// <summary>
	/// wields the vp_Weapon mapped to 'item' (if any)
	/// </summary>
	protected virtual void TryWield(vp_ItemInstance item)
	{

		if (!Application.isPlaying)
			return;

		if (Player.Dead.Active)
			return;

		if (!WeaponHandler.enabled)
			return;

		int index;
		vp_ItemIdentifier identifier;
		for (index = 1; index < WeaponHandler.Weapons.Count + 1; index++)
		{

			identifier = GetWeaponIdentifier(WeaponHandler.Weapons[index - 1]);

			if (identifier == null)
				continue;

			if (item.Type != identifier.Type)
				continue;

			if (identifier.ID == 0)
				goto found;

			if (item.ID != identifier.ID)
				continue;

			goto found;

		}
		return;

	found:

		Player.SetWeapon.TryStart(index);

	}


	/// <summary>
	/// if 'item' is a currently wielded weapon, unwields it
	/// </summary>
	protected virtual void Unwield(vp_ItemInstance item)
	{

		// TODO: to wield next weapon here should perhaps be an optional bool

		if (!Application.isPlaying)
			return;

		if (WeaponHandler.CurrentWeaponIndex == 0)
			return;

		if (CurrentWeaponIdentifier == null)
		{
			MissingIdentifierError();
			return;
		}

		if (item.Type != CurrentWeaponIdentifier.Type)
			return;

		if ((CurrentWeaponIdentifier.ID != 0) && (item.ID != CurrentWeaponIdentifier.ID))
			return;

		Player.SetWeapon.Start(0);
		if (!Player.Dead.Active)
			vp_Timer.In(0.35f, delegate()	{	Player.SetNextWeapon.Try();	});

		vp_Timer.In(1.0f, UnwieldMissingWeapon);

	}


	/// <summary>
	/// 
	/// </summary>
	public override void Refresh()
	{
		base.Refresh();

		UnwieldMissingWeapon();
	}


	/// <summary>
	/// returns true if the inventory contains a weapon by the index
	/// fed as an argument to the 'SetWeapon' activity. false if not.
	/// this is used to regulate which weapons the player currently
	/// has access to.
	/// </summary>
	protected virtual bool CanStart_SetWeapon()
	{

		int index = (int)Player.SetWeapon.Argument;
		if (index == 0)
			return true;

		if ((index < 1) || index > (WeaponHandler.Weapons.Count))
			return false;

		vp_ItemIdentifier weaponIdentifier = GetWeaponIdentifier(WeaponHandler.Weapons[index - 1]);
		if (weaponIdentifier == null)
			return MissingIdentifierError(index);

		bool haveItem = HaveItem(weaponIdentifier.Type, weaponIdentifier.ID);

		// see if weapon is thrown
		if (haveItem && (vp_Weapon.Type)WeaponHandler.Weapons[index-1].AnimationType == vp_Weapon.Type.Thrown)
		{

			if (GetAmmoInWeapon(WeaponHandler.Weapons[index - 1]) < 1)
			{
				vp_UnitBankType uType = weaponIdentifier.Type as vp_UnitBankType;
				if (uType == null)
				{
					Debug.LogError("Error (" + this + ") Tried to wield thrown weapon " + WeaponHandler.Weapons[index-1] + " but its item identifier does not point to a UnitBank.");
					return false;
				}
				else 
				{
					if (!TryReload(uType, weaponIdentifier.ID))	// NOTE: ID might not work for identification here because of multiplayer add-on pickup logic
					{
						//Debug.Log("uType: " + uType + ", weapon.ID: " + weapon.ID);
						//Debug.Log("failed because: no thrower wielded and no extra ammo");
						return false;
					}
				}
				//Debug.Log("success because: no thrower wielded but we have extra ammo");
			}
			//else
			//Debug.Log("success because: thrower wielded");

		}

		return haveItem;

	}
	

	/// <summary>
	/// tries to remove one unit from ammo level of current weapon
	/// </summary>
	protected virtual bool OnAttempt_DepleteAmmo()
	{

		// TODO: perhaps this should be checked in vp_Inventory

		if (CurrentWeaponIdentifier == null)
			return MissingIdentifierError();

		if (WeaponHandler.CurrentWeapon.AnimationType == (int)vp_Weapon.Type.Melee)
			return true;

		if (WeaponHandler.CurrentWeapon.AnimationType == (int)vp_Weapon.Type.Thrown)
			TryReload(CurrentWeaponInstance as vp_UnitBankInstance);

		return TryDeduct(CurrentWeaponIdentifier.Type as vp_UnitBankType, CurrentWeaponIdentifier.ID, 1);

	}


	/// <summary>
	/// tries to reload current weapon with any compatible units left
	/// in the inventory.
	/// </summary>
	protected virtual bool OnAttempt_RefillCurrentWeapon()
	{

		if (CurrentWeaponIdentifier == null)
			return MissingIdentifierError();

		return TryReload(CurrentWeaponIdentifier.Type as vp_UnitBankType, CurrentWeaponIdentifier.ID);

	}


	/// <summary>
	/// 
	/// </summary>
	public override void Reset()
	{

		m_PreviouslyOwnedItems.Clear();
		m_CurrentWeaponInstance = null;
		
		if ((m_Misc != null) && !m_Misc.ResetOnRespawn)
			return;

		base.Reset();

	}


	/// <summary>
	/// gets or sets the current weapon's ammo count
	/// </summary>
	protected virtual int OnValue_CurrentWeaponAmmoCount
	{
		get
		{
			vp_UnitBankInstance weapon = CurrentWeaponInstance as vp_UnitBankInstance;
			if (weapon == null)
				return 0;
			return weapon.Count;
		}
		set
		{
			vp_UnitBankInstance weapon = CurrentWeaponInstance as vp_UnitBankInstance;
			if (weapon == null)
				return;
			weapon.TryGiveUnits(value);
		}
	}


	/// <summary>
	/// gets or sets the current weapon's ammo count
	/// </summary>
	protected virtual int OnValue_CurrentWeaponMaxAmmoCount
	{
		get
		{
			vp_UnitBankInstance weapon = CurrentWeaponInstance as vp_UnitBankInstance;
			if (weapon == null)
				return 0;
			return weapon.Capacity;
		}
	}


	/// <summary>
	/// returns the amount of bullets for the current weapon
	/// that is currently available in an internal unit bank
	/// </summary>
	protected virtual int OnValue_CurrentWeaponClipCount
	{
		get
		{

			vp_UnitBankInstance weapon = CurrentWeaponInstance as vp_UnitBankInstance;
			if (weapon == null)
				return 0;

			return GetUnitCount(weapon.UnitType);

		}

	}


	/// <summary>
	/// returns the amount of items or units in the inventory by
	/// ItemType object name. WARNING: this event is potentially
	/// quite slow
	/// </summary>
	protected virtual int OnMessage_GetItemCount(string itemTypeObjectName)
	{

		vp_ItemInstance item = GetItem(itemTypeObjectName);
		if (item == null)
			return 0;

		// if item is an internal unitbank, return its unit count
		vp_UnitBankInstance unitBank = (item as vp_UnitBankInstance);
		if ((unitBank != null) && (unitBank.IsInternal))
			return GetItemCount(unitBank.UnitType);

		// if it's a regular item or unitbank, return the amount
		// of similar instances
		return GetItemCount(item.Type);

	}


	/// <summary>
	/// tries to add an amount of items to the item count.
	/// NOTE: this event should be passed an object array where
	/// the first object is of type 'vp_ItemType', and the second
	/// (optional) object is of type 'int', representing the amount
	/// of items to add
	/// </summary>
	protected virtual bool OnAttempt_AddItem(object args)
	{

		object[] arr = (object[])args;

		// fail if item type is unknown
		vp_ItemType type = arr[0] as vp_ItemType;
		if (type == null)
			return false;

		int amount = (arr.Length == 2) ? (int)arr[1] : 1;

		if (type is vp_UnitType)
			return TryGiveUnits((type as vp_UnitType), amount);

		return TryGiveItems(type, amount);

	}


	/// <summary>
	/// tries to remove an amount of items from the item count.
	/// NOTE: this event should be passed an object array where
	/// the first object is of type 'vp_ItemType', and the second
	/// (optional) object is of type 'int', representing the amount
	/// of items to remove
	/// </summary>
	protected virtual bool OnAttempt_RemoveItem(object args)
	{

		object[] arr = (object[])args;

		// fail if item type is unknown
		vp_ItemType type = arr[0] as vp_ItemType;
		if (type == null)
			return false;

		int amount = (arr.Length == 2) ? (int)arr[1] : 1;

		if(type is vp_UnitType)
			return TryRemoveUnits((type as vp_UnitType), amount);

		return TryRemoveItems(type, amount);

	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual Texture2D OnValue_CurrentAmmoIcon
	{
		get
		{
			if (CurrentWeaponInstance == null)
				return null;
			if (CurrentWeaponInstance.Type == null)
				return null;

			vp_UnitBankType u = CurrentWeaponInstance.Type as vp_UnitBankType;
			if (u == null)
				return null;
			if (u.Unit == null)
				return null;
			return u.Unit.Icon;
		}
	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void OnStop_SetWeapon()
	{
		m_CurrentWeaponInstance = null;
	}

}


