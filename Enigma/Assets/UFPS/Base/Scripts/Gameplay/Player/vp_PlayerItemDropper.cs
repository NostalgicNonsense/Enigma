/////////////////////////////////////////////////////////////////////////////////
//
//	vp_ItemDropper.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	implements the ability for gameobjects with a vp_Inventory
//					component to drop their contents into the scene by means of:
//						- removing an item record or unit from the inventory
//						- spawning a corresponding item pickup
//						- tossing it to the ground ahead
//
//					this can be used for players to drop their current weapon when
//					shot, or to drop a specific item at any time through script.
//					the system utilizes the vp_Toss script to throw objects with
//					simplistic simulated physics (no rigidbodies needed) to points
//					on the ground which are deterministic across multiplayer clients
//
//					USAGE:
//					1) put this script on the same transform as the player inventory.
//						all the pickups in the scene will automatically be droppable
//						after having been picked up
//					2) if you want the player to be able to drop items that are not
//						present as pickups in the scene, add their pickup prefabs to
//						the 'Droppables' list in the Inspector
//					3) if you want the player to drop items upon death, choose a
//						'DeathDropMode'
//					4) you can also make the player drop any type of item via script by
//						calling the various public 'TryDrop' methods
//
//					IMPORTANT:
//					- 1) every pickup prefab must have a vp_ItemPickup component
//					- 2) in multiplayer, it is vital that both the local and
//						remote player prefab shares exactly the same 'DeathDropMode'
//						(or a player may drop different items on different machines)
//					- 3) in the UFPS multiplayer add-on, it is also important that
//						the item types of all pickups are added to the 'vp_MPPickupManager',
//						(or players will not be able to pick them up)
//
//					NOTES:
//					- any dropped pickups are CLONES of the ones that were picked up,
//						meaning scene pickups may respawn while you carry the copy,
//						allowing for potentially lots of duplicate objects in the scene.
//						you may want to prolong or disable pickup respawns because of this.
//					- if 'ItemLifeTime' is zero (default) then the 'MaxRespawnTime' of the
//						dropped pickups's vp_Respawner (if any) will be used as lifetime.
//						if there is no respawner, the 'LifeTime' of the vp_Remover (if any)
//						will be used. if lifetime ends up 0, it will revert to 10 seconds.
//					- items are tossed in a circle around the player
//					- this component will disable any rigidbodies. however, if there is
//						a vp_RigidBodyFX component on the item, its effects will play
//						when the procedurally animated (vp_Toss) bounce triggers
//					- player will never drop more than 'MaxDrops' items at once. weapons
//						will be prioritized over other items and ammo
//	
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
#if UNITY_5_4_OR_NEWER
using UnityEngine.SceneManagement;
#endif

public class vp_PlayerItemDropper : MonoBehaviour
{

	public enum DeathDropMode
	{
		Nothing,			// player will drop nothing upon death (items can still be dropped by calling the 'TryDrop' methods in script)
		CurrentWeapon,      // player will drop the currently held weapon or throwing weapon unit (for example: one grenade)
		AllWeapons,         // player will drop all weapons, including one instance of each throwing weapon
		AllAmmo,            // player will drop all ammo, including all throwing weapons
		Everything          // player will drop all weapons, ammo and items in the inventory
	}

	public DeathDropMode DropOnDeath = DeathDropMode.CurrentWeapon;
	public float SpawnHeight = 1.25f;
	public float SpawnDistance = 1.0f;
	public float TossDistance = 1.0f;
	public int MaxDrops = 100;
	public float ItemLifeTime = 0.0f;
	public bool DisableBob = false;
	public bool DisableSpin = false;
	protected int m_LastDropFrame = -1;
	protected int m_ItemDropsThisFrame = -1;

	// audio
	public List<AudioClip> TossSounds = new List<AudioClip>();				// one will be randomly played on drop
	protected AudioSource m_Audio = null;
	protected float m_NextAllowedDropSoundTime = 0.0f;
	protected AudioSource Audio
	{
		get
		{
			if (m_Audio == null)
				m_Audio = GetComponent<AudioSource>();
			return m_Audio;
		}
	}

	// droppable objects
	public List<GameObject> ExtraPickups = new List<GameObject>();			// items that can be dropped in addition to item pickups existing in the scene
	protected static Dictionary<string, GameObject> m_SceneDroppables = new Dictionary<string, GameObject>();	// auto-populated with every type of scene pickup
	protected static Dictionary<string, vp_ItemType> m_SceneItemTypesByName = new Dictionary<string, vp_ItemType>();
	protected static List<int> m_AvailableUnitAmounts = new List<int>();    // what are the known clip sizes of the various ammo pickups in the level and 'droppable' lists
	struct AmmoClipType														// NOTE: the UFPS inventory has no concept of ammo clips, but this system implements it internally
	{																		// in order to identify suitable item pickups when dropping ammo
		public AmmoClipType(string unitTypeName, int units)
		{
			UnitTypeName = unitTypeName;
			Units = units;
		}
		public string UnitTypeName;
		public int Units;
	}
	List<AmmoClipType> m_AvailableAmmoClips = new List<AmmoClipType>();		// the ammo item pickups available in this level, by name and unit amount

#if UNITY_EDITOR
	[vp_HelpBox("• Every gameobject with a vp_ItemPickup component, that is present in the scene on startup, can be dropped by this component.\n\n• Pickups that are not present in the scene on startup must be added to 'ExtraPickups'.\n\n• See the script comments for more detailed instructions.", UnityEditor.MessageType.None, null, null, false, vp_PropertyDrawerUtility.Space.Nothing)]
	public float surfaceTypeHelp;
#endif


	// internal stuff
	protected Transform m_DroppablesGroup;
	protected int m_UnitsInLastRemovedItem = -1;
	protected int m_IDOfLastRemovedItem = -1;

	protected vp_PlayerInventory m_Inventory = null;
	protected vp_PlayerInventory Inventory
	{
		get
		{
			if(m_Inventory == null)
				m_Inventory = GetComponent<vp_PlayerInventory>();
			return m_Inventory;
		}
	}

	protected vp_PlayerEventHandler m_Player = null;
	public vp_PlayerEventHandler Player
	{
		get
		{
			if (m_Player == null)
				m_Player = (vp_PlayerEventHandler)transform.root.GetComponentInChildren(typeof(vp_PlayerEventHandler));
			return m_Player;
		}
	}

	private vp_WeaponHandler m_WeaponHandler = null;
	protected vp_WeaponHandler WeaponHandler
	{
		get
		{
			if (m_WeaponHandler == null)
				m_WeaponHandler = transform.GetComponent<vp_WeaponHandler>();
			return m_WeaponHandler;
		}
	}
	

	/// <summary>
	/// 
	/// </summary>
	protected virtual void OnEnable()
	{

		GenerateItemDrops();	// do this for every player joining a game (they may carry additional item drops)

		m_LastDropFrame = -1;
		m_ItemDropsThisFrame = -1;

#if UNITY_5_4_OR_NEWER
		SceneManager.sceneLoaded += OnLevelLoad;
#endif

	}


	/// <summary>
	/// 
	/// </summary>
	private void OnDisable()
	{

#if UNITY_5_4_OR_NEWER
		SceneManager.sceneLoaded -= OnLevelLoad;
#endif

	}


	/// <summary>
	/// DEBUG: uncomment this to test item drop logic
	/// </summary>
	/*
	protected void Update()
	{

		if (Input.GetKeyUp(KeyCode.F))
			TryDropAmmoClip("Bullet", 19);      // try to drop a clip with 19 bullets

		if (Input.GetKeyUp(KeyCode.G))
			TryDropWeapon("Pistol", 15);        // try to drop a pistol loaded with 15 bullets

		if (Input.GetKeyUp(KeyCode.H))
			TryDropWeapon("Machinegun", 35);    // try to drop a machinegun loaded with 35 bullets

		if (Input.GetKeyUp(KeyCode.J))
			TryDropCurrentWeapon();             // try to drop the currently wielded weapon, with its currently loaded ammo

		if (Input.GetKeyUp(KeyCode.K))
			transform.root.SendMessage("Die");  // insta kill player (for testing death drop modes)

	}
	*/


	/// <summary>
	/// scans the level and 'Droppables' list for item pickups and prepares hidden
	/// copies for dropping instances at runtime
	/// </summary>
	protected virtual void GenerateItemDrops()
	{

		List<vp_ItemPickup> pickups = new List<vp_ItemPickup>(FindObjectsOfType<vp_ItemPickup>());

		foreach (GameObject o in ExtraPickups)
		{
			if (o == null)
				continue;
			vp_ItemPickup i = (vp_ItemPickup)o.GetComponent<vp_ItemPickup>();
			if (i == null)
				continue;
			pickups.Add(i);
		}

		GameObject g = GameObject.Find("Droppables");
		if (g != null)
			m_DroppablesGroup = g.transform;
		if (m_DroppablesGroup == null)
		{
			m_DroppablesGroup = new GameObject("Droppables").transform;
		}

		foreach (vp_ItemPickup pickup in pickups)
		{
			string name = pickup.ItemTypeObject.name;
			if (pickup.ItemTypeObject is vp_UnitType)
				name += "," + pickup.Amount;

			if (!m_SceneDroppables.ContainsKey(name))
			{
				GameObject c = ClonePickup(name, pickup);
				if (c != null)
				{
					m_SceneDroppables.Add(name, c);
					if (!m_SceneItemTypesByName.ContainsKey(pickup.ItemTypeObject.name))
						m_SceneItemTypesByName.Add(pickup.ItemTypeObject.name, pickup.ItemTypeObject);

					if ((pickup.ItemTypeObject is vp_UnitType) && (!m_AvailableUnitAmounts.Contains(pickup.Amount)))
					{
						m_AvailableUnitAmounts.Add(pickup.Amount);
						m_AvailableUnitAmounts.Sort();
					}

				}

			}


		}

		StoreAmmoClipTypes();

	}


	/// <summary>
	/// erase all cached info about available item drops. for entering a new level.
	/// </summary>
	protected void ClearItemDrops()
	{

		m_SceneDroppables.Clear();
		m_SceneItemTypesByName.Clear();
		m_AvailableUnitAmounts.Clear();

	}


	/// <summary>
	/// creates a deactivated clone template of a pickup object that is
	/// adapted for instantiation and dropping in the scene. templates are
	/// stored in a scene group called 'Droppables'
	/// </summary>
	protected virtual GameObject ClonePickup(string name, vp_ItemPickup i)
	{

		GameObject template = (GameObject)vp_Utility.Instantiate(i.gameObject, Vector3.zero, i.gameObject.transform.rotation);
		template.name = name;

		// if there is a remover on the pickup, override its lifetime
		// if not, add a remover with our own lifetime, even if 0
		vp_Remover rm = template.GetComponent<vp_Remover>();
		if (rm == null)
			rm = template.AddComponent<vp_Remover>();
		rm.LifeTime = ItemLifeTime;

		// if there is a respawner on the pickup and our lifetime is 0,
		// use the max respawn time of the respawner as lifetime.
		// however, kill the respawner afterwards
			// NOTE: this will make dropped pickups be destroyed (or pooled)
			// when picked up, unlike scene pickups that have respawners (which
			// are merely temp-deactivated)
		vp_Respawner rs = template.GetComponent<vp_Respawner>();
		if (rs != null)
		{
			if (ItemLifeTime == 0.0f)
				rm.LifeTime = rs.MaxRespawnTime;
			Object.Destroy(rs);
		}

		// if lifetime is 0 at this point, fallback to 10 secs
		if (rm.LifeTime == 0.0f)
			rm.LifeTime = 10.0f;

		// add 0-2 seconds randomly to lifetime, so clusters of pickups won't
		// pop away exactly at the same time
		vp_MathUtility.SetSeed(i.ID);
		rm.LifeTime += Random.value * 2;

		// disable rigidbodiy and colliders (trigger colliders will be reactivated later)
		Rigidbody rb = template.GetComponent<Rigidbody>();
		if (rb != null)
			rb.isKinematic = true;

		Collider[] cs = template.GetComponents<Collider>();
		foreach (Collider c in cs)
		{
			c.enabled = false;
		}

		// set up item pickup
		vp_ItemPickup ip = template.GetComponent<vp_ItemPickup>();
		if (ip != null)
			ip.ID = 0;
		ip.Amount = i.Amount;

		// disable object and child it to the 'Droppables' group
		vp_Utility.Activate(template, false);
		template.transform.parent = m_DroppablesGroup;

		return template;

	}


	/// <summary>
	/// returns a pickup prefab of the passed 'itemType', if available
	/// </summary>
	protected virtual GameObject GetPickupPrefab(vp_ItemType itemType, int units = 0)
	{

		if (itemType == null)
			return null;

		GameObject o = null;

		if (itemType is vp_UnitType)    // ammo
			m_SceneDroppables.TryGetValue(itemType.name + "," + units, out o);
		else  // weapons and other items
			m_SceneDroppables.TryGetValue(itemType.name, out o);

		if (o == null)
			return null;

		return o;

	}


	/// <summary>
	/// instantiates and activates 'prefab' at 'position'
	/// </summary>
	protected virtual GameObject SpawnPickup(GameObject prefab, Vector3 position)
	{
		
		GameObject o = null;

		o = (GameObject)vp_Utility.Instantiate(
			prefab,
			position,
			Quaternion.Euler(new Vector3(prefab.transform.eulerAngles.x, Random.rotation.eulerAngles.y, prefab.transform.eulerAngles.z)));
		vp_Utility.Activate(o);

		Rigidbody rb = o.GetComponent<Rigidbody>();
		if (rb != null)
			rb.isKinematic = true;

		return o;

	}


	/// <summary>
	/// initializes the collider, id, amount and vp_Toss component of a pickup
	/// (vp_Toss is what moves the pickup in a nice bouncy arc when dropped).
	/// </summary>
	protected virtual vp_Toss InitPickup(GameObject pickup, int units, int id)
	{

		Collider[] cs = pickup.GetComponentsInChildren<Collider>();
		foreach (Collider c in cs)
		{
			if (c.isTrigger)
			{
				Collider cc = c;
				vp_Timer.In(1, delegate ()
				{
					if(cc != null)
						cc.enabled = true;
				});
			}
			else
				c.enabled = false;

		}

		// NOTE: in the case of throwing weapons (e.g. grenades), 2 vp_ItemPickup
		// components may be present on the pickup gameobject. the first is for
		// the grenade unit (ammo) type and the second is for the 'grenade thrower'
		// weapon. to account for this case, there's some special logic to initing
		// the components of a pickup prefab:

		// fetch all pickup components on the prefab
		vp_ItemPickup[] ips = pickup.GetComponentsInChildren<vp_ItemPickup>();
		if (ips.Length == 0)
			return null;	// abort if no pickup components

		// iterate through all pickup components on this prefab (usually only one)
		for (int v = 0; v < ips.Length; v++)
		{
			if (ips[v] == null)
			{
				if (v == 0)
					return null;	// abort if first pickup component is null
				continue;			// skip if the second or later component is null
			}
			ips[v].ID = id + v;		// id of first component will be 'id', the rest will be incremented from that
			if(v == 0)
				ips[v].Amount = units;  // only the first pickup component can have a count, the rest will be zero.
										// IMPORTANT: for grenades, the first vp_ItemPickup component must be for the unit, and the second for the throwing weapon
			else
				ips[v].Amount = 0;

			// handle edge cases where ammo pickups end up empty - by canceling the item drop
			if (ips[v].ItemTypeObject is vp_UnitType && (units == 0))
				return null;

			vp_GlobalEvent<vp_ItemPickup>.Send("RegisterPickup", ips[v]);   // this only has effect in multiplayer. NOTE: we must register all pickup 
																			// components on the transform (grenade throwers as well as grenade units)

		}

		// make sure the pickup has a toss component and return it
		vp_Toss toss = pickup.GetComponent<vp_Toss>();
		if(toss == null)
			toss = pickup.AddComponent<vp_Toss>();

		return toss;

	}


	/// <summary>
	/// upon success, unwields the current weapon of the player and waits
	/// for a little bit before tossing the weapon
	/// </summary>
	public virtual void TryDropCurrentWeapon(float pauseForUnwield = 0.3f)
	{

		if (WeaponHandler == null)
			return;

		if (WeaponHandler.CurrentWeapon == null)
			return;

		vp_ItemIdentifier identifier = WeaponHandler.CurrentWeapon.GetComponent<vp_ItemIdentifier>();
		if (identifier == null)
			return;

		vp_ItemType itemType = identifier.GetItemType();
		if (itemType == null)
			return;

		int units = Inventory.GetAmmoInCurrentWeapon();

		Player.Unwield.Send();

		vp_Timer.In(Mathf.Max(pauseForUnwield, 0.0f), () =>
		{
			TryDropWeapon(itemType, units);
		});

	}


	/// <summary>
	/// upon success, drops a weapon of the passed 'itemType'
	/// </summary>
	public virtual void TryDropWeapon(vp_ItemType itemType, int units = 0, int id = 0)
	{

		if (WeaponHandler == null)
			return;

		if (itemType == null)
			return;

		if (Player == null)
			return;

		if (Player.Reload.Active)
			return;

		if (Player.Attack.Active)
			Player.Attack.Stop();

		for (int v = (WeaponHandler.Weapons.Count - 1); v > -1; v--)
		{

			if (WeaponHandler.Weapons[v] == null)
				continue;

			vp_ItemIdentifier identifier = WeaponHandler.Weapons[v].GetComponent<vp_ItemIdentifier>();
			if (identifier == null)
				continue;

			vp_ItemType iType = identifier.GetItemType();
			if (iType == null)
				continue;

			if (iType != itemType)
				continue;

			if (id <= 0)
			{
				TryDropItem(itemType, units, 0);
				return;
			}
			else
			{
				TryDropItem(itemType, units, identifier.ID);
				return;
			}

		}

	}


	/// <summary>
	/// upon success, drops a weapon of the passed itemtype name
	/// </summary>
	public virtual void TryDropWeapon(string itemTypeName, int units = 0, int id = 0)
	{

		vp_ItemType itemType;
		if (!m_SceneItemTypesByName.TryGetValue(itemTypeName, out itemType))
			return;

		TryDropItemInternal(itemType, transform.forward, units, id);

	}


	/// <summary>
	/// for dropping ammo clips. locally triggered. to be run by a local player
	/// in single player or by the master in multiplayer
	/// </summary>
	public virtual bool TryDropAmmoClip(string unitTypeName, int clipSize)
	{
		return TryDropAmmoClip(unitTypeName, transform.forward, clipSize);
	}

	public virtual bool TryDropAmmoClip(string unitTypeName, Vector3 direction, int clipSize)
	{

		vp_ItemType itemType;
		if (!m_SceneItemTypesByName.TryGetValue(unitTypeName, out itemType))
			return false;

		return TryDropItemInternal(itemType, direction, clipSize);

	}

	public virtual void TryDropAmmoClip(vp_UnitType unitType, int clipSize)
	{

		TryDropItemInternal(unitType, transform.forward, clipSize);

	}


	/// <summary>
	/// for dropping weapons or items. locally triggered. to be run by a local
	/// player in single player or by the master in multiplayer
	/// </summary>
	public virtual void TryDropItem(string itemTypeName, int id = 0)
	{
		TryDropItem(itemTypeName, transform.forward, id);
	}

	public virtual void TryDropItem(string itemTypeName, Vector3 direction, int id)
	{

		vp_ItemType itemType;
		if (!m_SceneItemTypesByName.TryGetValue(itemTypeName, out itemType))
			return;

		TryDropItemInternal(itemType, direction, 0, id);

	}

	public virtual void TryDropItem(vp_ItemType itemType, int id)
	{
		TryDropItemInternal(itemType, transform.forward, 0, id);
	}

	public virtual void TryDropItem(vp_ItemType itemType, int units, int id)
	{
		TryDropItemInternal(itemType, transform.forward, units, id);
	}


	/// <summary>
	/// attempts to drop all the items in the player's inventory. if there is a
	/// 'MaxDrops' cap (default: 8) then weapons will be prioritized over ammo
	/// </summary>
	public virtual void TryDropEverything()
	{

		//Debug.Log("m_AvailableUnitAmounts.Count: " + m_AvailableUnitAmounts.Count);

		// drop all weapons
		TryDropAllWeapons();

		// drop any remaining (non-weapon) unitbanks
		for (int v = (Inventory.UnitBankInstances.Count - 1); v > -1; v--)
		{
			if (Inventory.UnitBankInstances[v] == null)
				continue;

			Inventory.UnitBankInstances[v].Type.ToString();
			TryDropItemInternal(Inventory.UnitBankInstances[v].Type, transform.forward, Inventory.UnitBankInstances[v].Count, Inventory.UnitBankInstances[v].ID);

		}

		// drop any remaining (non-weapon) items
		for (int v = (Inventory.ItemInstances.Count - 1); v > -1; v--)
		{
			if (Inventory.ItemInstances[v] == null)
				continue;
			Inventory.ItemInstances[v].Type.ToString();
			TryDropItemInternal(Inventory.ItemInstances[v].Type, transform.forward, 0, Inventory.ItemInstances[v].ID);
		}

		// drop all ammo
		TryDropAllAmmo();

	}



	/// <summary>
	/// upon success, drops all the player's weapons
	/// </summary>
	protected virtual void TryDropAllWeapons()
	{

		if (WeaponHandler == null)
			return;

		foreach (KeyValuePair<vp_Weapon, vp_ItemIdentifier> w in Inventory.WeaponIdentifiers)
		{

			vp_Weapon weapon = w.Key;
			vp_ItemIdentifier weaponIdentifier = w.Value;

			// skip if weapon identifier has no item type 
			vp_ItemType itemType = weaponIdentifier.GetItemType();
			if (itemType == null)
				continue;

			// for unitbank weapons, try and set the weapon pickup's ammo
			// to the amount of ammo in the dropped weapon
			int units = 0;

			vp_UnitBankInstance unitBank = Inventory.GetUnitBankInstanceOfWeapon(weapon);
			if (unitBank != null)
				units = unitBank.Count;

			// drop the weapon
			TryDropWeapon(itemType, units, weaponIdentifier.ID);

		}

	}



	/// <summary>
	/// tries to drop all the ammo player is carrying. since UFPS has no ammo clip
	/// concept (all ammo is stored in a 'bullet pool' in the inventory) this script
	/// matches existing ammo pickups with the type of ammo dropped, and tries to
	/// drop as large (as few) ammo clips as possible per drop until the inventory
	/// has no more ammo (or not enough ammo to drop another clip)
	/// </summary>
	public virtual void TryDropAllAmmo()
	{

		bool retry = true;
		int iterations = 100;   // max iterations to prevent infinite loop for whatever reason

		while (retry && (iterations > 0))
		{
			retry = false;
			foreach (vp_ItemType itemType in m_SceneItemTypesByName.Values)
			{
				if (itemType is vp_UnitType)
				{
					vp_UnitType unitType = (itemType as vp_UnitType);

					// try to drop as large (as few) clips of every type as possible
					// until we have no more ammo that fits any available clip size
					for (int v = (m_AvailableUnitAmounts.Count - 1); v > -1; v--)
					{
						if (!(m_AvailableAmmoClips.Contains(new AmmoClipType(unitType.name, m_AvailableUnitAmounts[v]))))
							continue;
						if (TryDropAmmoClip(unitType.name, m_AvailableUnitAmounts[v]))
						{
							//Debug.Log("Dropped: " + itemType.name + "," + m_AvailableUnitAmounts[v]);
						}
						if (Inventory.GetUnitCount(unitType) >= m_AvailableUnitAmounts[v])
							retry = true;
					}

					// if ammo was for a throwing weapon, drop the loaded unit too
					if (Inventory.IsThrowingUnit(unitType))
					{
						TryDropLoadedThrowingUnit(unitType);
					}
				}
			}
			iterations--;
		}

	}


	/// <summary>
	/// logs all the various ammo pickup types available in the scene (and any
	/// components of this type's 'Droppables' list) by item type and amount.
	/// for more info, see the 'TryDropAllAmmo' comments
	/// </summary>
	protected virtual void StoreAmmoClipTypes()
	{

		foreach (vp_ItemType itemType in m_SceneItemTypesByName.Values)
		{
			if (itemType is vp_UnitType)
			{
				foreach (GameObject o in m_SceneDroppables.Values)
				{
					if (o == null)
						continue;
					vp_ItemPickup ip = o.GetComponent<vp_ItemPickup>();
					if (ip == null)
						continue;
					if (!(ip.ItemTypeObject is vp_UnitType))
						continue;

					for (int v = (m_AvailableUnitAmounts.Count - 1); v > -1; v--)
					{
						if (m_AvailableUnitAmounts[v] == ip.Amount)
						{
							if (!m_AvailableAmmoClips.Contains(new AmmoClipType(ip.ItemTypeObject.name, ip.Amount)))
							{
								m_AvailableAmmoClips.Add(new AmmoClipType(ip.ItemTypeObject.name, ip.Amount));
								//Debug.Log("added AmmoClipType: " + ip.ItemTypeObject.name + ", " + ip.Amount);
							}
						}
					}
					
				}
				
			}
		}

	}


	/// <summary>
	/// tries to drop the currenlty loaded throwing weapon unit in the form
	/// of an ammo unit (rather than in the form of a weapon)
	/// </summary>
	protected virtual void TryDropLoadedThrowingUnit(vp_UnitType unitType)
	{
		
		vp_UnitBankType unitBankType = Inventory.GetThrowingWeaponUnitBankType(unitType);
		if (unitBankType == null)
			return;

		for (int v = (WeaponHandler.Weapons.Count - 1); v > -1; v--)
		{

			if (WeaponHandler.Weapons[v] == null)
				continue;

			vp_ItemIdentifier identifier = WeaponHandler.Weapons[v].GetComponent<vp_ItemIdentifier>();
			if (identifier == null)
				continue;

			vp_ItemType iType = identifier.GetItemType();
			if (iType == null)
				continue;

			if (iType != unitBankType)
				continue;

			for (int u = (Inventory.UnitBankInstances.Count - 1); u > -1; u--)
			{
				//Debug.Log("unittype: " + Inventory.UnitBankInstances[u].Type + ", count: " + Inventory.UnitBankInstances[u].Count);
				if (Inventory.UnitBankInstances[u].UnitType == unitBankType.Unit)
				{

					// if the throwing weapon is currently loaded with one unit (likely), move that unit
					// from the throwing weapon to the internal unitbank and drop it as an ammo clip
					if (Inventory.UnitBankInstances[u].Count == 1)
					{
						// move unit to internal unitbank
						Inventory.UnitBankInstances[u].DoRemoveUnits(1);
						vp_UnitBankInstance internalUnitBank = Inventory.GetInternalUnitBank(Inventory.UnitBankInstances[u].UnitType);
						internalUnitBank.DoAddUnits(1);
						// drop the moved unit
						if (TryDropAmmoClip(Inventory.UnitBankInstances[u].UnitType.name, 1))
						{
							//Debug.Log("Dropped: " + Inventory.UnitBankInstances[v].UnitType.name + ",1");
						}
						continue;
					}
				}
			}

		}

	}


	/// <summary>
	/// internal method for handling all aspects of dropping any item type.
	/// NOTE: in multiplayer, this only gets run on the master, which in turn
	/// triggers 'MPClientDropItem' on clients
	/// </summary>
	protected virtual bool TryDropItemInternal(vp_ItemType itemType, Vector3 direction, int units = 0, int id = 0)
	{

		//Debug.Log("TryDropItemInternal: " + itemType + ", " + units);

		if (!vp_Gameplay.IsMaster)
			return false;

		// fetch pickup prefab of the item type
		GameObject prefab = GetPickupPrefab(itemType, units);
		if (prefab == null)
			return false;

		// try to remove item records or units from the inventory
		if (!TryRemoveItem(itemType, units, id))
				return false;

		// keep track of the number of item drop attempts for circular distribution and max cap
		UpdateDropCount();

		// abort if we can't drop any more items at once
		if (m_ItemDropsThisFrame >= MaxDrops)
			return false;		


		// spawn a pickup
		GameObject pickup = SpawnPickup(
			prefab,
			transform.position                      // position of inventory owner
			+ (Vector3.up * SpawnHeight)			// plus a height offset
			+ (direction * SpawnDistance)			// plus a little bit away from body of inventory holder
			);
		if (pickup == null)
			return false;

		// in the UFPS multiplayer add-on, every pickup must be assigned a unique id on
		// spawn / startup (+ the id feature is reserved for the vp_MPPickupManager system)
		if (vp_Gameplay.IsMultiplayer)
			id = vp_Utility.UniqueID;

		// initialize the pickup with a collision timer, units, id and a toss component
		vp_Toss toss = InitPickup(pickup, units, id);
		if (toss == null)
		{
			Object.Destroy(pickup);
			return false;
		}
		
		// item drop success! play drop sound
		TryPlayDropSound();

		// offset direction based on how many items we have thrown this frame, so as
		// to not throw items inside each other
		direction = GetCircleDirection(direction);

		// toss the pickup away from the owner
		// the first 8 items will be tossed to 'm_DestDistance'. after that,
		// 75% of the initial distance will be added every 8 items, causing
		// large amounts of items to be arranged in concentric rings
		float additionalDistance = (((int)((m_ItemDropsThisFrame - 1) / 8)) * (TossDistance * 0.75f));
		float targetYaw = Random.Range(-180, 180);
		Vector3 finalPosition = toss.Toss(pickup.transform.position, targetYaw, direction, (TossDistance + additionalDistance), !DisableBob, !DisableSpin);

		//Debug.Log("item drops: " + m_ItemDropsThisFrame + ", additional: " + additionalDistance);

		// notify master object
		vp_ItemPickup itemPickup = pickup.GetComponentInChildren<vp_ItemPickup>();
		if (itemPickup != null)
			vp_GlobalEvent<object[]>.Send("TransmitDropItem", new object[] { itemType.name, finalPosition, targetYaw, transform.root, itemPickup.ID, itemPickup.Amount });

		return true;

	}


	/// <summary>
	/// keep track of how many items we are dropping at the same time. this is
	/// used to impose the 'MaxDrops' cap, and for preventing multiple drop sounds
	/// to play at the same time
	/// </summary>
	protected virtual void UpdateDropCount()
	{

		if (m_ItemDropsThisFrame == 0)
			m_LastDropFrame = Time.frameCount;

		if (Time.frameCount == m_LastDropFrame)
			m_ItemDropsThisFrame++;
		else
			m_ItemDropsThisFrame = 0;

	}


	/// <summary>
	/// offsets a direction depending on the amount of items we have dropped this
	/// frame. this is used to toss items in an irregular circle (as opposed to a
	/// single point on the ground ahead of the player where items would intersect)
	/// </summary>
	protected virtual Vector3 GetCircleDirection(Vector3 direction)
	{

		// first item is thrown directly ahead, the remaining ones are thrown with
		// 22.5 degree gaps, randomly to the left or right (forming two half circles).
		// the fewer objects, the more they will cluster in front of the player.
		// the more objects, the more they will spread out
		float dirOffset = (m_ItemDropsThisFrame * 22.5f);	// rotate depending on amount of drops
		dirOffset *= (Random.value < 0.5f ? 1 : -1);			// choose left or right half circle

		return Quaternion.Euler(0, dirOffset, 0) * direction;

	}


	/// <summary>
	/// forces a multiplayer client to drop an item (remotely triggered by
	/// the master in multiplayer)
	/// </summary>
	public virtual void MPClientDropItem(string itemTypeName, Vector3 targetPosition, float targetYaw, int id, int units)
	{


		vp_ItemType itemType;
		if (!m_SceneItemTypesByName.TryGetValue(itemTypeName, out itemType))
			return;

		if (itemType == null)
			return;

		if(itemType is vp_UnitType)
			TryRemoveItem(itemType, units);
		else
			TryRemoveItem(itemType);

		// fetch pickup prefab of the item type
		GameObject prefab = GetPickupPrefab(itemType, units);
		if (prefab == null)
			return;

		// spawn a pickup
		GameObject pickup = SpawnPickup(
			prefab,
			transform.position					// position of inventory owner
			+ (Vector3.up * SpawnHeight)		// plus a height offset
												// plus offset in the direction of the target position (but at the original height)
			+ vp_3DUtility.HorizontalVector((targetPosition - transform.position).normalized * SpawnDistance)
			);
		if (pickup == null)
			return;

		// initialize the pickup with a collision timer, a toss component,
		// and the units and id as specified by master
		vp_Toss toss = InitPickup(pickup, units, id);
		if (toss == null)
		{
			Object.Destroy(pickup);
			return;
		}

		// toss the pickup to the specified position - without local collision
		// detection - which is assumed to have been already taken care of on
		// the master
		toss.Toss(pickup.transform.position, targetPosition, targetYaw, !DisableBob, !DisableSpin);

		// play sound
		TryPlayDropSound();

	}


	/// <summary>
	/// plays a random drop sound from a list, but never plays a ton of
	/// sounds at once (even if we drop many items in one frame)
	/// </summary>
	protected virtual void TryPlayDropSound()
	{

		if (Time.time < m_NextAllowedDropSoundTime)
			return;

		vp_AudioUtility.PlayRandomSound(Audio, TossSounds);

		m_NextAllowedDropSoundTime = Time.time + 0.2f;

	}


	/// <summary>
	/// tries to remove an item record or unit from the player inventory
	/// </summary>
	protected virtual bool TryRemoveItem(vp_ItemType itemType, int units = 1, int id = 0)
	{
		//Debug.Log("TryRemoveItem: " + itemType.name + ", " + amount);

		if (Inventory == null)
		{
			Debug.LogError("Error (" + this + ") Tried to remove an item but there is no vp_Inventory!");
			return false;
		}

		vp_ItemInstance i = null;
		if(id != 0)
			i = Inventory.GetItem(itemType, id);
		else
			i = Inventory.GetItem(itemType.name);

		if (i == null)
			return false;

		id = i.ID;

		if (itemType is vp_UnitType)
		{

			if (!Inventory.TryRemoveUnits((itemType as vp_UnitType), units))
			{
				//Debug.Log("Failed to remove " + amount + " units of type: " + itemType);
				return false;
			}


		}
		else
		{
			if (!Inventory.TryRemoveItems(itemType, 1))
			{
				//Debug.Log("Failed to remove an item of type: " + itemType);
				return false;
			}
		}

		return true;

	}


	/// <summary>
	/// (UnityMessage target) when this method executes, items may or may not be
	/// dropped, depending on the current 'DeathDropMode' and player inventory
	/// status. called externally by vp_DamageHandler (among others)
	/// </summary>
	protected virtual void Die()
	{

		switch (DropOnDeath)
		{
			case DeathDropMode.Nothing:
				return;
			case DeathDropMode.CurrentWeapon:
				TryDropCurrentWeapon(0.2f);
				return;
			case DeathDropMode.AllWeapons:
				TryDropAllWeapons();
				return;
			case DeathDropMode.AllAmmo:
				TryDropAllAmmo();
				return;
			case DeathDropMode.Everything:
				TryDropEverything();
				return;
		}

	}


#if UNITY_5_4_OR_NEWER
	protected void OnLevelLoad(Scene scene, LoadSceneMode mode)
#else
	protected void OnLevelWasLoaded()
#endif
	{

		ClearItemDrops();
		GenerateItemDrops();

	}


}
