/////////////////////////////////////////////////////////////////////////////////
//
//	vp_WeaponThrower.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	NOTE: this script exists in two versions. this (base) version should
//					NOT be used on a 1st person player (only on AI or remote players).
//					any 1st person player should instead use 'vp_FPWeaponThrower'.
//
//					this script can be placed on the same gameobject as a vp_Weapon to
//					make it behave as a throwing weapon. the setup is somewhat complex
//					because we're dealing with an object that is both a WEAPON and its
//					own AMMO.
//
//					MECHANICS
//					when thrown, the in-hand object becomes invisible and a projectile version
//					of it is spawned. if more ammo exists, the weapon becomes visible again,
//					masquerading as another grenade / knife / rock from the player's backpack.
//					if no more ammo exists, the weapon is unwielded and may not be wielded again
//					until ammo exists. note that throwing weapons have a custom reload logic and
//					do not use the regular reload events or reloader components. the player
//					inventory script will keep feeding units to a throwing weapon from its internal
//					unitbank at all times. once a logical throwing weapon has been picked up it never
//					leaves the inventory, however it can only be wielded as long as there is ammo
//					for it). this creates the illusion that multiple weapons are carried and thrown.
//
//					CHECKLIST OF REQUIREMENTS
//					- the vp_Weapons's 'AnimationType' parameter must be set to 'Thrown'
//					- the vp_WeaponsShooter's 'Projectile' prefab should be a rigidbody
//						object with a script on it to give it forward velocity on Start/Awake.
//						see the provided vp_Grenade script for an example of this
//					- the player must have a vp_PlayerInventory component on it
//					- the weapon gameobject must have a vp_ItemIdentifier referencing a
//						UnitBank item type
//					- the UnitBank item type must have a 'Capacity' of '1', and be set to
//						'Reloadable'
//					- pickups for a throwing weapon need TWO vp_ItemPickup components: one
//						with a UnitBank for the logical throwing weapon, (e.g. 'GrenadeThrower')
//						and one with the logical ammo Unit (e.g. 'Grenade')
//
//					EXAMPLE CONTENT
//					- it is recommended to study the grenade weapon of the 'HeroHDWeapons' prefab
//					- the 'Grenade' and 'GrenadeThrower' item types (in Project View)
//					- the 'PickupGrenade01' and 'GrenadeLive' prefabs
//	
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class vp_WeaponThrower : MonoBehaviour
{

	public float AttackMinDuration = 1.0f;
	protected float m_OriginalAttackMinDuration = 0.0f;

	// --- properties for performance ---

	protected Transform m_Transform = null;
	public Transform Transform
	{
		get
		{
			if (m_Transform == null)
				m_Transform = transform;
			return m_Transform;
		}
	}

	protected Transform m_Root = null;
	public Transform Root
	{
		get
		{
			if (m_Root == null)
				m_Root = Transform.root;
			return m_Root;
		}
	}

	protected vp_Weapon m_Weapon = null;
	public vp_Weapon Weapon
	{
		get
		{
			if (m_Weapon == null)
				m_Weapon = (vp_Weapon)Transform.GetComponent(typeof(vp_Weapon));
			return m_Weapon;
		}
	}

	protected vp_WeaponShooter m_Shooter = null;
	public vp_WeaponShooter Shooter
	{
		get
		{
			if (m_Shooter == null)
				m_Shooter = (vp_WeaponShooter)Transform.GetComponent(typeof(vp_WeaponShooter));
			return m_Shooter;
		}
	}

	protected vp_UnitBankType m_UnitBankType = null;
	public vp_UnitBankType UnitBankType
	{
		get
		{
			if (ItemIdentifier == null)
				return null;
			vp_ItemType iType = m_ItemIdentifier.GetItemType();
			if(iType == null)
				return null;
			vp_UnitBankType uType;
			uType = iType as vp_UnitBankType;
			if(uType == null)
				return null;
			return uType;
		}
	}

	protected vp_UnitBankInstance m_UnitBank = null;
	public vp_UnitBankInstance UnitBank
	{
		get
		{
			if ((m_UnitBank == null) && (UnitBankType != null) && (Inventory != null))
			{
				foreach (vp_UnitBankInstance iu in Inventory.UnitBankInstances)
				{
					if (iu.UnitType == UnitBankType.Unit)
						m_UnitBank = iu;
				}
			}
			return m_UnitBank;
		}
	}

	protected vp_ItemIdentifier m_ItemIdentifier = null;
	public vp_ItemIdentifier ItemIdentifier
	{
		get
		{
			if (m_ItemIdentifier == null)
				m_ItemIdentifier = (vp_ItemIdentifier)Transform.GetComponent(typeof(vp_ItemIdentifier));
			return m_ItemIdentifier;
		}
	}

	protected vp_PlayerEventHandler m_Player = null;
	public vp_PlayerEventHandler Player
	{
		get
		{
			if (m_Player == null)
				m_Player = (vp_PlayerEventHandler)Root.GetComponentInChildren(typeof(vp_PlayerEventHandler));
			return m_Player;
		}
	}

	protected vp_PlayerInventory m_Inventory = null;
	public vp_PlayerInventory Inventory
	{
		get
		{
			if (m_Inventory == null)
				m_Inventory = (vp_PlayerInventory)Root.GetComponentInChildren(typeof(vp_PlayerInventory));
			return m_Inventory;
		}
	}

	
	/// <summary>
	/// 
	/// </summary>
	protected virtual void OnEnable()
	{

		if (Player == null)
			return;

		Player.Register(this);

		TryStoreAttackMinDuration();

		// cap the amount of weaponthrowers of this type to one
		Inventory.SetItemCap(ItemIdentifier.Type, 1, true);
		Inventory.CapsEnabled = true;

	}

	
	/// <summary>
	/// 
	/// </summary>
	protected virtual void OnDisable()
	{

		TryRestoreAttackMinDuration();

		if (Player != null)
			Player.Unregister(this);

	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void Start()
	{

		TryStoreAttackMinDuration();

		// check the setup for errors

		if (Weapon == null)
		{
			Debug.LogError("Throwing weapon setup error (" + this + ") requires a vp_Weapon or vp_FPWeapon component.");
			return;
		}

		if (UnitBankType == null)
		{
			Debug.LogError("Throwing weapon setup error (" + this + ") requires a vp_ItemIdentifier component with a valid UnitBank.");
			return;
		}

		if(Weapon.AnimationType != (int)vp_Weapon.Type.Thrown)
			Debug.LogError("Throwing weapon setup error (" + this + ") Please set 'Animation -> Type' of '" + Weapon + "' item type to 'Thrown'.");

		if (UnitBankType.Capacity != 1)
			Debug.LogError("Throwing weapon setup error (" + this + ") Please set 'Capacity' for the '" + UnitBankType.name + "' item type to '1'.");

	}


	/// <summary>
	/// 
	/// </summary>
	void TryStoreAttackMinDuration()
	{

		if (Player.Attack == null)
			return;

		if (m_OriginalAttackMinDuration == 0.0f)
			return;

		m_OriginalAttackMinDuration = Player.Attack.MinDuration;
		Player.Attack.MinDuration = AttackMinDuration;

	}


	/// <summary>
	/// 
	/// </summary>
	void TryRestoreAttackMinDuration()
	{

		if (Player.Attack == null)
			return;

		if (m_OriginalAttackMinDuration != 0.0f)
			return; 
		
		Player.Attack.MinDuration = m_OriginalAttackMinDuration;

	}


	/// <summary>
	/// returns true if ammo exists either in the inventory unitbank
	/// for this weapon, or in the corresponding internal unitbank
	/// </summary>
	protected bool HaveAmmoForCurrentWeapon
	{
		get
		{
			return ((Player.CurrentWeaponAmmoCount.Get() > 0)
					|| (Player.CurrentWeaponClipCount.Get() > 0));
		}
	}

	
	/// <summary>
	/// this system has a custom reload logic and does not use the
	/// regular reload events or reloader components
	/// </summary>
	protected virtual bool TryReload()
	{

		if (UnitBank == null)
			return false;

		return Inventory.TryReload(UnitBank);

	}

	
	/// <summary>
	/// 
	/// </summary>
	protected virtual void OnStart_Attack()
	{

		// 3rd person visuals logic
		if (!Player.IsFirstPerson.Get())
		{

			// make the weapon invisible in our hand at exactly the moment when the
			// projectile spawns. this makes it seem like we are throwing the weapon
			vp_Timer.In(Shooter.ProjectileSpawnDelay, delegate()
			{
				Weapon.Weapon3rdPersonModel.GetComponent<Renderer>().enabled = false;
			});

			// make the weapon reappear in our hand one second after the projectile
			// was thrown - but only if we have more ammo at that point
			vp_Timer.In(Shooter.ProjectileSpawnDelay + 1, delegate()
			{
				if(HaveAmmoForCurrentWeapon)
					Weapon.Weapon3rdPersonModel.GetComponent<Renderer>().enabled = true;
			});
		}

		// help feed ammo continuously into the throwing weapon (failsafe in case of inventory hiccup)
		if(Player.CurrentWeaponAmmoCount.Get() < 1)
			TryReload();

		// force-stop the attack activity half a second after the throw
		vp_Timer.In(Shooter.ProjectileSpawnDelay + 0.5f, delegate()
		{
			Player.Attack.Stop();
		});

	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual bool CanStart_Reload()
	{
		// block regular reloading because this system does not
		// use the regular reload logic
		return false;
	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void OnStop_Attack()
	{
		// help feed ammo continuously into the throwing weapon (failsafe in case of inventory hiccup)
		TryReload();
	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void OnStop_SetWeapon()
	{

		// clear the cached unitbank because it will not always be the same
		m_UnitBank = null;

	}



}