/////////////////////////////////////////////////////////////////////////////////
//
//	vp_MPPlayerStats.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	this component is a hub for all the common gameplay stats of
//					a player in multiplayer. it does not HOLD any data, but COLLECTS
//					it from various external components and EXPOSES it via the public
//						'Get', 'Set' and 'Erase' methods.
//					this is relied heavily upon by 'vp_MPMaster'. the basic stats are:
//						Type, Team, Health, Shots, Position, Rotation, Items and Weapon.
//
//					NOTE: by default, this component is automatically added to every player
//					upon spawn. if you inherit the component you must update the class name
//					to be auto-added. this can be altered in the Inspector. go to your
//					vp_MPPlayerSpawner component - > Add Components -> Local & Remote.
//
//					TIP: new stats can be added by inheriting this class. for an example
//					of this, see 'vp_DMPlayerStats' and 'vp_DMDamageCallbacks'
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class vp_MPPlayerStats : MonoBehaviour
{
	
	public Dictionary<string, Func<object>> Getters = new Dictionary<string, Func<object>>();
	public Dictionary<string, Action<object>> Setters = new Dictionary<string, Action<object>>();
	
	public float Health
	{
		get
		{
			if (NPlayer == null)
				return 0.0f;
			if (NPlayer.DamageHandler == null)
				return 0.0f;
			return NPlayer.DamageHandler.CurrentHealth;
		}
		set
		{
			if (NPlayer == null)
				return;
			if (NPlayer.DamageHandler == null)
				return;
			NPlayer.DamageHandler.CurrentHealth = value;
		}
	}

	// --- expected components ---

	vp_Inventory m_Inventory = null;
	public vp_Inventory Inventory
	{
		get
		{
			if (m_Inventory == null)
				m_Inventory = (vp_Inventory)gameObject.GetComponentInChildren<vp_Inventory>();
			return m_Inventory;
		}
	}


	protected vp_MPNetworkPlayer m_NPlayer = null;
	public vp_MPNetworkPlayer NPlayer
	{
		get
		{
			if (m_NPlayer == null)
				m_NPlayer = transform.root.GetComponentInChildren<vp_MPNetworkPlayer>();
			return m_NPlayer;
		}
	}


	/// <summary>
	/// hashtable of all the important player stats to be part of the
	/// game state. the master client will sync these stats with all
	/// other players in multiplayer. NOTE: the actual stat names are
	/// defined in the overridable method 'AddStats'
	/// </summary>
	public virtual ExitGames.Client.Photon.Hashtable All
	{
		// this should be used seldomly, by game state-updating methods
		// TODO: move to master client ?
		get
		{
			if (m_Stats == null)
				m_Stats = new ExitGames.Client.Photon.Hashtable();
			else
				m_Stats.Clear();
			foreach (string s in Getters.Keys)
			{
				m_Stats.Add(s, Get(s));
			}
			return m_Stats;
		}
		set
		{
			if (value == null)
			{
				m_Stats = null;
				return;
			}
			foreach (string s in Setters.Keys)
			{
				object o = GetFromHashtable(value, s);
				if (o != null)	// may be null if only a partial gamestate is received
				{
					Set(s, o);
				}
			}
		}
	}
	protected ExitGames.Client.Photon.Hashtable m_Stats;
	

	/// <summary>
	/// returns a list of the names of all player multiplayer stats
	/// </summary>
	public List<string> Names	//	TODO: make static, based on first player, if any (?)
	{
		get
		{
			if (m_StatNames == null)
			{
				m_StatNames = new List<string>();
				foreach (string s in Getters.Keys)
				{
					m_StatNames.Add(s);
				}
			}
			return m_StatNames;
		}
	}
	protected List<string> m_StatNames;

	// --- work variables ---

	protected static object m_Stat;
	


	/// <summary>
	/// 
	/// </summary>
	void Awake()
	{

		InitStats();

	}


	/// <summary>
	/// this class can be overridden to add additional stats, but
	/// NOTE: remember to include base.AddStats in the override.
	/// also don't call 'AddStats' from the derived class but
	/// leave this to 'Awake' in this class
	/// </summary>
	public virtual void InitStats()
	{

		// -------- getters --------

		if(NPlayer == null)
		{
			Debug.LogError("Error ("+this+") Found no vp_MPNetworkPlayer! Aborting ...");
			return;
		}

		Getters.Add("Type", delegate() { return NPlayer.PlayerType.name; });
		Getters.Add("Team", delegate() { return NPlayer.TeamNumber; });
		Getters.Add("Health", delegate() { return Health; });
		Getters.Add("Shots", delegate() { return NPlayer.Shots; });
		Getters.Add("Position", delegate() { return NPlayer.Transform.position; });
		Getters.Add("Rotation", delegate() { return NPlayer.Transform.root.rotation; });
		Getters.Add("Items", delegate()
		{
			//Debug.Log("--------> GET ITEMS OF " + Inventory.transform.root.gameObject.name);

			ExitGames.Client.Photon.Hashtable items = new ExitGames.Client.Photon.Hashtable();

			if (Inventory == null)
				return items;

			foreach (vp_ItemInstance i in Inventory.ItemInstances)
			{
				if (!items.ContainsKey(i.Type.name))
				{
					//Debug.Log("ADDING ITEM TO HASHTABLE: " + i.Type.name + ", amount: " + Inventory.GetItemCount(i.Type));
					items.Add(i.Type.name, Inventory.GetItemCount(i.Type));
				}
			}
			foreach (vp_UnitBankInstance u in Inventory.UnitBankInstances)
			{
				//Debug.Log("u: " + u);
				//Debug.Log("u.Type: " + u.Type);
				//Debug.Log("u.Type.name: " + u.Type.name);
				if (!items.ContainsKey(u.Type.name))
				{
					//Debug.Log("ADDING UNITBANK TO HASHTABLE: " + u.Type.name + ", units: " + u.Count);
					items.Add(u.Type.name, u.Count);
				}
			}
			foreach (vp_UnitBankInstance iu in Inventory.InternalUnitBanks)
			{
				//Debug.Log("iu: " + iu);
				//Debug.Log("iu.UnitType: " + iu.UnitType);
				//Debug.Log("iu.UnitType.name: " + iu.UnitType.name);
				if (iu.Count == 0)
					continue;
				if (!items.ContainsKey(iu.UnitType.name))
				{
					//Debug.Log("ADDING UNITS TO HASHTABLE: " +iu.UnitType.name + ", units: " + iu.Count);
					items.Add(iu.UnitType.name, iu.Count);
				}
			}
			return items;
		});
		Getters.Add("Weapon", delegate()
		{
			if (NPlayer.WeaponHandler == null)
				return 0;
			return NPlayer.WeaponHandler.CurrentWeaponIndex;
		});


		// -------- setters --------


		Setters.Add("Type", delegate(object val) { NPlayer.PlayerType = vp_MPPlayerSpawner.GetPlayerTypeByName((string)val); });
		Setters.Add("Team", delegate(object val) { NPlayer.TeamNumber = (int)val; });
		Setters.Add("Health", delegate(object val) { Health = (float)val; });
		// NOTE: 'Shots' must never be updated with a lower (lagged) value or
		// simulation will go out of sync. however, we should be able
		// to set it to zero for game reset purposes (?)
		Setters.Add("Shots", delegate(object val) { NPlayer.Shots = (((int)val > 0) ? Mathf.Max(NPlayer.Shots, (int)val) : 0); });
		Setters.Add("Position", delegate(object val) { NPlayer.LastMasterPosition = (Vector3)val; NPlayer.SetPosition(NPlayer.LastMasterPosition); });
		Setters.Add("Rotation", delegate(object val) { NPlayer.LastMasterRotation = (Quaternion)val; NPlayer.SetRotation(NPlayer.LastMasterRotation); });
		Setters.Add("Items", delegate(object val)
		{
			//Debug.Log("--------> TRYING TO SET ITEMS");

			ExitGames.Client.Photon.Hashtable items = val as ExitGames.Client.Photon.Hashtable;
			if (items == null)
			{
				Debug.Log("failed to cast items as hashtable");
				return;
			}
			foreach (string s in items.Keys)
			{
				object amount;
				items.TryGetValue(s, out amount);
				//Debug.Log("trying to set: " + s + " to amount: " + (int)amount);

				// try to give item ...
					// NOTE: the following only has effect locally. unless master
					// triggered this call, items will be missing on remote machines

				//bool success = 
				vp_MPItemList.TryGiveItem(transform, s, (int)amount);				// TODO: cache transform
				//if (success)
				//	Debug.Log("TryGiveItem SUCCESS: '" + s + "', amount: " + amount);
				//else
				//	Debug.Log("TryGiveItem FAIL: '" + s + "', amount: " + amount);

			}

			// refresh 1st person materials. this must be done after switching
			// weapons since 3rd person arms may have been toggled
			if (NPlayer is vp_MPLocalPlayer)
				(NPlayer as vp_MPLocalPlayer).RefreshMaterials();

		});
		Setters.Add("Weapon", delegate(object val)
		{
			if (NPlayer.Player == null)
				return;
			//bool r =
			NPlayer.Player.SetWeapon.TryStart((int)val);
			//Debug.Log("setting weapon of player " + NPlayer.Player + " to weapon: " + ((int)val).ToString() + ", result: " + r);

		});


	}


	/// <summary>
	/// erases all the stats of this particular player
	/// </summary>
	public static void EraseStats()
	{

		foreach (vp_MPNetworkPlayer player in vp_MPNetworkPlayer.Players.Values)
		{
			if (player == null)
				continue;
			player.Stats.All = null;
		}

	}
	

	/// <summary>
	/// resets health, shots and inventory to default + resurrects
	/// this player (if dead)
	/// </summary>
	public virtual void FullReset()
	{

		Health = NPlayer.DamageHandler.CurrentHealth = NPlayer.DamageHandler.MaxHealth;

		NPlayer.Shots = 0;

		Inventory.Reset();

		NPlayer.Player.Dead.Stop();

	}


	/// <summary>
	/// restores health, shots, inventory and life on all players
	/// </summary>
	public static void FullResetAll()
	{

		// reset all network players
		foreach (vp_MPNetworkPlayer p in vp_MPNetworkPlayer.Players.Values)
		{
			if (p == null)
				continue;
			p.Stats.FullReset();
		}

		vp_Timer.In(1, delegate()
		{
			vp_MPNetworkPlayer.RefreshPlayers();
		});

	}


	/// <summary>
	/// extracts a stat of a player given its string name
	/// </summary>
	public object Get(string stat)
	{
		Func<object> o = null;
		Getters.TryGetValue(stat, out o);
		if (o != null)
			return o.Invoke();
		Debug.LogError("Error ("+this+") The stat '"+stat+"' has not been declared by the player stats script.");
		return null;
	}


	/// <summary>
	/// sets a stat on a player given its string name
	/// </summary>
	public void Set(string stat, object val)
	{
		Action<object> o = null;
		Setters.TryGetValue(stat, out o);
		if (o != null)
			o.Invoke(val);
	}


	/// <summary>
	/// 
	/// </summary>
	public static void Set(vp_MPPlayerStats playerStats, string stat, object val)
	{

		if (playerStats == null)
		{
			Debug.LogError("Error (vp_MPPlayerStats) 'playerStats' was null.");
			return;
		}

		playerStats.Set(stat, val);

	}


	/// <summary>
	/// 
	/// </summary>
	public void SetFromHashtable(ExitGames.Client.Photon.Hashtable stats)
	{

		if (stats == null)
			return;

		bool weaponHandlerEnabled = false;
		if (NPlayer.WeaponHandler != null)
		{
			weaponHandlerEnabled = NPlayer.WeaponHandler.enabled;
			NPlayer.WeaponHandler.enabled = false;
		}

		foreach (object o in stats.Keys)
		{

			Set((string)o, GetFromHashtable(stats, (string)o));
			
		}

		if (NPlayer.WeaponHandler != null) 
			NPlayer.WeaponHandler.enabled = weaponHandlerEnabled;

	}
	

	/// <summary>
	/// extracts a value from a provided player state hashtable given
	/// its string name
	/// </summary>
	public static object GetFromHashtable(ExitGames.Client.Photon.Hashtable hashTable, string stat)
	{
		m_Stat = null;
		hashTable.TryGetValue(stat, out m_Stat);
		return m_Stat;
	}


}