/////////////////////////////////////////////////////////////////////////////////
//
//	vp_WeaponHandler.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	toggles between weapons and manipulates weapon states depending
//					on currentplayer events and activities. this component requires
//					a player event handler and atleast one child gameobject with a
//					vp_FPWeapon component
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System.Collections.Generic;
using System;
using System.Collections;


public class vp_WeaponHandler : MonoBehaviour
{

	public int StartWeapon = 0;

	// weapon timing
	public float AttackStateDisableDelay = 0.5f;		// delay until weapon attack state is disabled after firing ends
	public float SetWeaponRefreshStatesDelay = 0.5f;	// delay until component states are refreshed after setting a new weapon
	public float SetWeaponDuration = 0.1f;				// amount of time between previous weapon disappearing and next weapon appearing

	// forced pauses in player activity
	public float SetWeaponReloadSleepDuration = 0.3f;	// amount of time to prohibit reloading during set weapon
	public float SetWeaponZoomSleepDuration = 0.3f;		// amount of time to prohibit zooming during set weapon
	public float SetWeaponAttackSleepDuration = 0.3f;	// amount of time to prohibit attacking during set weapon
	public float ReloadAttackSleepDuration = 0.3f;		// amount of time to prohibit attacking during reloading

	// reloading
	public bool ReloadAutomatically = true;
	
	protected vp_PlayerEventHandler m_Player = null;
	protected List<vp_Weapon> m_Weapons = null;// = new List<vp_Weapon>();
	public List<vp_Weapon> Weapons
	{
		get
		{
			if (m_Weapons == null)
				InitWeaponLists();
			return m_Weapons;
		}
		set 
		{
			m_Weapons = value;
		}
	}

	protected List<List<vp_Weapon>> m_WeaponLists = new List<List<vp_Weapon>>();
	
	protected int m_CurrentWeaponIndex = -1;
	protected vp_Weapon m_CurrentWeapon = null;
	public vp_Weapon CurrentWeapon { get { return m_CurrentWeapon; } }
	protected vp_Shooter m_CurrentShooter = null;
	public vp_Shooter CurrentShooter
	{
		get
		{

			if (CurrentWeapon == null)
				return null;

			if ((m_CurrentShooter != null) && ((!m_CurrentShooter.enabled) || (!vp_Utility.IsActive(m_CurrentShooter.gameObject))))
				return null;

			return m_CurrentShooter;	// NOTE: this is set in 'ActivateWeapon'

		}
	}

	// timers
	protected vp_Timer.Handle m_SetWeaponTimer = new vp_Timer.Handle();
	protected vp_Timer.Handle m_SetWeaponRefreshTimer = new vp_Timer.Handle();
	protected vp_Timer.Handle m_DisableAttackStateTimer = new vp_Timer.Handle();
	protected vp_Timer.Handle m_DisableReloadStateTimer = new vp_Timer.Handle();

	/// <summary> The index of a vp_Weapon as stored under the vp_FPCamera in alphabetical order. Indices start at 1. </summary>
	[Obsolete("Please use the 'CurrentWeaponIndex' parameter instead.")]
	public int CurrentWeaponID { get { return m_CurrentWeaponIndex; } }	// renamed to avoid confusion with vp_ItemType.ID


	/// <summary> The index of a vp_Weapon as stored under the vp_FPCamera in alphabetical order. Indices start at 1. </summary>
	public int CurrentWeaponIndex { get { return m_CurrentWeaponIndex; } }

	// comparer to sort the weapons alphabetically. this is used to
	// make ingame weapon order adhere to the alphabetical order of
	// weapon objects under the FPSCamera
	protected class WeaponComparer : IComparer
	{
		int IComparer.Compare(System.Object x, System.Object y)
		{ return ((new CaseInsensitiveComparer()).Compare(((vp_Weapon)x).gameObject.name, ((vp_Weapon)y).gameObject.name)); }
	}


	/// <summary>
	/// while the SetWeapon activity is active, returns the weapon that will be
	/// set as the result of this activity. returns null if SetWeapon is inactive
	/// or the weapon cannot be determined
	/// </summary>
	public vp_Weapon WeaponBeingSet
	{
		get
		{

			if (!m_Player.SetWeapon.Active)
				return null;

			if (m_Player.SetWeapon.Argument == null)
				return null;

			return Weapons[Mathf.Max(0, (int)m_Player.SetWeapon.Argument - 1)];
		}
	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void Awake()
	{
		
		// store the first player event handler found in the top of our transform hierarchy
		m_Player = (vp_PlayerEventHandler)transform.root.GetComponentInChildren(typeof(vp_PlayerEventHandler));

		if(Weapons != null)
			StartWeapon = Mathf.Clamp(StartWeapon, 0, Weapons.Count);

	}


	/// <summary>
	/// 
	/// </summary>
	protected void InitWeaponLists()
	{

		// first off, always store all weapons contained under our main FPS camera (if any)
		List<vp_Weapon> camWeapons = null;
		vp_FPCamera camera = transform.GetComponentInChildren<vp_FPCamera>();
		if (camera != null)
		{
			camWeapons = GetWeaponList(camera.transform);
			if ((camWeapons != null) && (camWeapons.Count > 0))
				m_WeaponLists.Add(camWeapons);

		}

		List<vp_Weapon> allWeapons = new List<vp_Weapon>(transform.GetComponentsInChildren<vp_Weapon>());

		// if the camera weapons were all the weapons we have, return
		if ((camWeapons != null) && (camWeapons.Count == allWeapons.Count))
		{
			Weapons = m_WeaponLists[0];
			return;
		}

		// identify every unique gameobject that holds weapons as direct children
		List<Transform> weaponContainers = new List<Transform>();
		foreach (vp_Weapon w in allWeapons)
		{

			if ((camera != null) && camWeapons.Contains(w))
				continue;
			if (!weaponContainers.Contains(w.Parent))
				weaponContainers.Add(w.Parent);
		}

		// create one weapon list for every container found
		foreach (Transform t in weaponContainers)
		{
			List<vp_Weapon> weapons = GetWeaponList(t);
			DeactivateAll(weapons);
			m_WeaponLists.Add(weapons);
		}

		// abort and disable weapon handler in case no weapons were found
		if (m_WeaponLists.Count < 1)
		{
			Debug.LogError("Error (" + this + ") WeaponHandler found no weapons in its hierarchy. Disabling self.");
			enabled = false;
			return;
		}

		// start out with the first weapon list by default. on a 1st person
		// player, this would typically be the weapons stored under the camera
		Weapons = m_WeaponLists[0];
		
	}


	/// <summary>
	/// 
	/// </summary>
	public void EnableWeaponList(int index)
	{

		if (m_WeaponLists == null)
			return;

		if (m_WeaponLists.Count < 1)
			return;

		if ((index < 0) || (index > (m_WeaponLists.Count - 1)))
			return;

		Weapons = m_WeaponLists[index];

	}


	/// <summary>
	/// 
	/// </summary>
	protected List<vp_Weapon> GetWeaponList(Transform target)
	{

		List<vp_Weapon> weapons = new List<vp_Weapon>();

		if (target.GetComponent<vp_Weapon>())
		{
			Debug.LogError("Error: (" + this + ") Hierarchy error. This component should sit above any vp_Weapons in the gameobject hierarchy.");
			return weapons;
		}
		
		// add the gameobjects of any weapon components to the weapon list
		foreach (vp_Weapon w in target.GetComponentsInChildren<vp_Weapon>(true))
		{
			weapons.Insert(weapons.Count, w);
		}

		if (weapons.Count == 0)
		{
			Debug.LogError("Error: (" + this + ") Hierarchy error. This component must be added to a gameobject with vp_Weapon components in child gameobjects.");
			return weapons;
		}

		// sort the weapons alphabetically
		IComparer comparer = new WeaponComparer();
		weapons.Sort(comparer.Compare);

		return weapons;

	}


	/// <summary>
	/// registers this component with the event handler (if any).
	/// also, sets any weapon that may have been active on this
	/// component the last time it was disabled
	/// </summary>
	protected virtual void OnEnable()
	{

		// allow this monobehaviour to talk to the player event handler
		if (m_Player != null)
			m_Player.Register(this);

	}

	
	/// <summary>
	/// unregisters this component from the event handler (if any)
	/// </summary>
	protected virtual void OnDisable()
	{

		// unregister this monobehaviour from the player event handler
		if (m_Player != null)
			m_Player.Unregister(this);

	}

	/// <summary>
	/// 
	/// </summary>
	protected virtual void Update()
	{

		InitWeapon();

		UpdateFiring();

	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void UpdateFiring()
	{

		// weaponhandler only fires for local and AI players. any other type
		// of player will have to handle their own firing (for example:
		// multiplayer remote players)
		if (!m_Player.IsLocal.Get() && !m_Player.IsAI.Get())
			return;

		// we continuously try to fire the weapon while player is in attack
		// mode, but if it's not: bail out
		if (!m_Player.Attack.Active)
			return;

		// weapon can only be fired if fully wielded
		if (m_Player.SetWeapon.Active || ((m_CurrentWeapon != null) && !m_CurrentWeapon.Wielded))
			return;

		m_Player.Fire.Try();
		
	}
	

	/// <summary>
	/// this method will disable the currently activated weapon and activate
	/// the one with 'weaponIndex'. if index is zero, no weapon will be activated.
	/// NOTE: this method will make any old weapon INSTANTLY pop away and make the
	/// new one pop into view. for smooth transitions, please instead use the
	/// vp_PlayerEventHandler 'SetWeapon' event. example: m_Player.SetWeapon.TryStart(3);
	/// </summary>
	public virtual void SetWeapon(int weaponIndex)
	{

		if ((Weapons == null) || (Weapons.Count < 1))
		{
			Debug.LogError("Error: (" + this + ") Tried to set weapon with an empty weapon list.");
			return;
		}

		if (weaponIndex < 0 || weaponIndex > Weapons.Count)
		{
			Debug.LogError("Error: (" + this + ") Weapon list does not have a weapon with index: " + weaponIndex);
			return;
		}

		// before putting old weapon away, make sure it's in a neutral
		// state next time it is activated
		if (m_CurrentWeapon != null)
			m_CurrentWeapon.ResetState();

		// deactivate all weapons
		DeactivateAll(Weapons);

		// activate the new weapon
		ActivateWeapon(weaponIndex);
		
	}
	

	/// <summary>
	/// 
	/// </summary>
	public void DeactivateAll(List<vp_Weapon> weaponList)
	{

		foreach (vp_Weapon weapon in weaponList)
		{
			weapon.ActivateGameObject(false);
			vp_FPWeapon fpWeapon = weapon as vp_FPWeapon;
			if ((fpWeapon != null) && (fpWeapon.Weapon3rdPersonModel != null))
				vp_Utility.Activate(fpWeapon.Weapon3rdPersonModel, false);
		}

		m_CurrentShooter = null;

	}


	/// <summary>
	/// 
	/// </summary>
	public void ActivateWeapon(int index)
	{

		m_CurrentWeaponIndex = index;
		m_CurrentWeapon = null;
		if (m_CurrentWeaponIndex > 0)
		{
			m_CurrentWeapon = Weapons[m_CurrentWeaponIndex - 1];
			if (m_CurrentWeapon != null)
				m_CurrentWeapon.ActivateGameObject(true);
		}

		if(m_CurrentWeapon != null)
			m_CurrentShooter = CurrentWeapon.GetComponent<vp_Shooter>();

	}


	/// <summary>
	/// 
	/// </summary>
	public virtual void CancelTimers()
	{

		vp_Timer.CancelAll("EjectShell");
		m_DisableAttackStateTimer.Cancel();
		m_SetWeaponTimer.Cancel();
		m_SetWeaponRefreshTimer.Cancel();

	}


	/// <summary>
	/// sets layer of the weapon model, for controlling which
	/// camera the weapon is rendered by
	/// </summary>
	public virtual void SetWeaponLayer(int layer)
	{

		if (m_CurrentWeaponIndex < 1 || m_CurrentWeaponIndex > Weapons.Count)
			return;

		vp_Layer.Set(Weapons[m_CurrentWeaponIndex - 1].gameObject, layer, true);

	}


	/// <summary>
	/// clears and refreshes weapon in first frame
	/// </summary>
	void InitWeapon()
	{
		
		if (m_CurrentWeaponIndex == -1)
		{

			SetWeapon(0);

			// set start weapon (if specified, and if inventory allows it)
			vp_Timer.In(SetWeaponDuration + 0.1f, delegate()
			{
				if (StartWeapon > 0 && (StartWeapon < (Weapons.Count+1)))
				{
					if (!m_Player.SetWeapon.TryStart(StartWeapon) && !vp_Gameplay.IsMultiplayer)
						Debug.LogWarning("Warning (" + this + ") Requested 'StartWeapon' (" + Weapons[StartWeapon-1].name + ") was denied, likely by the inventory. Make sure it's present in the inventory from the beginning.");
				}
			});

		}

	}


	/// <summary>
	/// 
	/// </summary>
	public void RefreshAllWeapons()
	{
		foreach (vp_Weapon w in Weapons)
		{
			w.Refresh();
			w.RefreshWeaponModel();
		}
	}
	

	/// <summary>
	/// 
	/// </summary>
	public int GetWeaponIndex(vp_Weapon weapon)
	{
		return Weapons.IndexOf(weapon) + 1;
	}


	/// <summary>
	/// this callback is triggered right after the 'Reload activity
	/// has been approved for activation. this event usually results
	/// from player input, but may also be sent by things that give
	/// ammo to the player, e.g. weapon pickups
	/// </summary>
	protected virtual void OnStart_Reload()
	{

		// prevent attacking for a while after reloading
		m_Player.Attack.Stop(m_Player.CurrentWeaponReloadDuration.Get() + ReloadAttackSleepDuration);


	}
	

	/// <summary>
	/// this callback is triggered right after the SetWeapon activity
	/// has been approved for activation. it moves the current weapon
	/// model to its exit offset, changes the weapon model and moves
	/// the new weapon into view. this message is usually broadcast
	/// by vp_FPInput, but may also be sent by things that have given
	/// weapons to the player, e.g. weapon pickups
	/// </summary>
	protected virtual void OnStart_SetWeapon()
	{
		// abort timers that won't be needed anymore
		CancelTimers();
		
		// prevent these player activities during the weapon switch (unless switching to a melee weapon)
		if ((WeaponBeingSet == null) || (WeaponBeingSet.AnimationType != (int)vp_Weapon.Type.Melee))
		{
			m_Player.Reload.Stop(SetWeaponDuration + SetWeaponReloadSleepDuration);
			m_Player.Zoom.Stop(SetWeaponDuration + SetWeaponZoomSleepDuration);
			m_Player.Attack.Stop(SetWeaponDuration + SetWeaponAttackSleepDuration);
		}

		// instantly unwield current weapon. this moves the weapon
		// to exit offset and plays an unwield sound
		if (m_CurrentWeapon != null)
			m_CurrentWeapon.Wield(false);

		// make 'OnStop_SetWeapon' trigger in 'SetWeaponDuration' seconds
		// (it will set the new weapon and refresh component states)
		m_Player.SetWeapon.AutoDuration = SetWeaponDuration;

	}


	/// <summary>
	/// this callback is triggered when the 'SetWeapon' activity deactivates
	/// </summary>
	protected virtual void OnStop_SetWeapon()
	{
        // fetch weapon index from when 'SetWeapon.TryStart' was called
        int weapon = 0;
		if(m_Player.SetWeapon.Argument != null)
			weapon = (int)m_Player.SetWeapon.Argument;

		// hides the old weapon and activates the new one (at its exit offset)
		SetWeapon(weapon);

		// smoothly moves the new weapon into view and plays a wield sound
		if (m_CurrentWeapon != null)
			m_CurrentWeapon.Wield();

		// make all player components resume their states from before
		// the weapon switch
		vp_Timer.In(SetWeaponRefreshStatesDelay, delegate()
		{
			if ((this != null) && (m_Player != null))
			{
				m_Player.RefreshActivityStates();

				if (m_CurrentWeapon != null)
				{
					if (m_Player.CurrentWeaponAmmoCount.Get() == 0)
					{
						// the weapon came empty, but if we have ammo clips for it,
						// try reloading in 0.5 secs
						m_Player.AutoReload.Try();	// try to auto-reload
					}
				}
			}

		}, m_SetWeaponRefreshTimer);

	}



	/// <summary>
	/// adds a condition (a rule set) that must be met for the
	/// event handler 'SetWeapon' activity to successfully activate.
	/// NOTE: other scripts may have added conditions to this
	/// activity aswell
	/// </summary>
	protected virtual bool CanStart_SetWeapon()
	{

		// fetch weapon index from when 'SetWeapon.TryStart' was called
		int weapon = (int)m_Player.SetWeapon.Argument;

		// can't set a weapon that is already set
		if (weapon == m_CurrentWeaponIndex)
			return false;

		// can't set an unexisting weapon
		if (weapon < 0 || weapon > Weapons.Count)
			return false;

		// can't set a new weapon while reloading
		if (m_Player.Reload.Active)
			return false;

		return true;

	}


	/// <summary>
	/// adds a condition (a rule set) that must be met for the
	/// player to be allowed to pull the trigger: that is, whether
	/// the event handler 'Attack' activity is allowed to activate.
	/// NOTE: other scripts may have added conditions to this
	/// activity aswell.
	/// </summary>
	protected virtual bool CanStart_Attack()
	{

		// can't attack if there's no weapon
		if (m_CurrentWeapon == null)
			return false;

		// can't start attack if we're already attacking
		if (m_Player.Attack.Active)
			return false;

		// can't start attack if we're switching weapons
		if (m_Player.SetWeapon.Active)
			return false;

		// can't start attack while reloading
		if (m_Player.Reload.Active)
			return false;

		// attacking is allowed
		return true;

	}


	/// <summary>
	/// this callback is triggered when the 'Attack activity deactivates
	/// </summary>
	protected virtual void OnStop_Attack()
	{

		// the Attack activity does not automatically disable the
		// component's Attack state, so schedule disabling it in
		// 'AttackStateDisableDelay' seconds
		vp_Timer.In(AttackStateDisableDelay, delegate()
		{
			if ((this != null) && (m_Player != null))
			{
				if (!m_Player.Attack.Active)
				{
					if (m_CurrentWeapon != null)
						m_CurrentWeapon.SetState("Attack", false);
				}
			}
		}, m_DisableAttackStateTimer);

	}


	/// <summary>
	/// toggles to the previous weapon if currently allowed,
	/// otherwise attempts to skip past it
	/// </summary>
	protected virtual bool OnAttempt_SetPrevWeapon()
	{

		int i = m_CurrentWeaponIndex - 1;

		// skip past weapon '0'
		if (i < 1)
			i = Weapons.Count;

		int iterations = 0;
		while (!m_Player.SetWeapon.TryStart(i))
		{

			i--;
			if (i < 1)
				i = Weapons.Count;
			iterations++;
			if (iterations > Weapons.Count)
				return false;

		}

		return true;

	}


	/// <summary>
	/// toggles to the next weapon if currently allowed,
	/// otherwise attempts to skip past it
	/// </summary>
	protected virtual bool OnAttempt_SetNextWeapon()
	{

		int i = m_CurrentWeaponIndex + 1;

		int iterations = 0;
		while (!m_Player.SetWeapon.TryStart(i))
		{

			if (i > Weapons.Count + 1)
				i = 0;

			i++;
			iterations++;
			if (iterations > Weapons.Count)
				return false;
		}

		return true;

	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual bool OnAttempt_SetWeaponByName(string name)
	{

		for(int v=0; v< Weapons.Count; v++)
		{

			if (Weapons[v].name == (string)name)
				return m_Player.SetWeapon.TryStart(v+1);
		}

		return false;

	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual bool OnValue_CurrentWeaponWielded
	{
		get
		{
			if (m_CurrentWeapon == null)
				return false;
			return m_CurrentWeapon.Wielded;
		}
	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual string OnValue_CurrentWeaponName
	{
		get
		{
			if (m_CurrentWeapon == null || Weapons == null)
				return "";
			return m_CurrentWeapon.name;

		}
	}
	

	/// <summary>
	/// 
	/// </summary>
	protected virtual int OnValue_CurrentWeaponID
	{
		get
		{
			return m_CurrentWeaponIndex;
		}
	}
	

	/// <summary>
	/// 
	/// </summary>
	protected virtual int OnValue_CurrentWeaponIndex
	{
		get
		{
			return m_CurrentWeaponIndex;
		}
	}


	/// <summary>
	/// 
	/// </summary>
	public virtual int OnValue_CurrentWeaponType
	{
		get
		{
			return ((CurrentWeapon == null) ? 0 : CurrentWeapon.AnimationType);
		}
	}


	/// <summary>
	/// 
	/// </summary>
	public virtual int OnValue_CurrentWeaponGrip
	{
		get
		{
			return ((CurrentWeapon == null) ? 0 : CurrentWeapon.AnimationGrip);

		}
	}


	/// <summary>
	/// 
	/// </summary>
	public virtual void OnMessage_Unwield()
	{
		m_Player.SetWeapon.TryStart(0);
	}

}


