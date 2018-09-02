/////////////////////////////////////////////////////////////////////////////////
//
//	vp_MPPickupManager.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	a turnkey pickup manager for UFPS multiplayer scenes.
//
//					scans the scene for vp_ItemPickups and assigns IDs to them based
//					on their scene-load position. then handles network pickup logic,
//					removal and respawning based on the IDs. the main benefit of this
//					approach is that singleplayer pickups will work automatically in
//					multiplayer, without any need to add photonviews or any other setup.
//					the vp_GlobalEvent 'RegisterPickup' can be sent by other scripts to
//					register additional pickups during the game.
//
//					PLEASE NOTE: this system uses the 'ID' feature of the vp_ItemPickup
//					class so if you are relying on that feature for other things (e.g.
//					identifying mission / quest items) then maybe this is not for you
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

#if UNITY_5_4_OR_NEWER
	using UnityEngine.SceneManagement;
#endif

public class vp_MPPickupManager : Photon.MonoBehaviour
{

	public Dictionary<int, List<vp_ItemPickup>> Pickups = new Dictionary<int, List<vp_ItemPickup>>();
	public Dictionary<int, vp_Respawner> PickupRespawners = new Dictionary<int, vp_Respawner>();

	private static vp_MPPickupManager m_Instance = null;
	public static vp_MPPickupManager Instance
	{
		get
		{
			if (m_Instance == null)
				m_Instance = Component.FindObjectOfType(typeof(vp_MPPickupManager)) as vp_MPPickupManager;
			return m_Instance;
		}
	}



	/// <summary>
	/// 
	/// </summary>
	protected virtual void OnEnable()
	{

#if UNITY_5_4_OR_NEWER
		SceneManager.sceneLoaded += OnLevelLoad;
#endif

		vp_GlobalEvent<vp_ItemPickup, Transform>.Register("TransmitPickup", TransmitPickup);		// sent by 'vp_ItemPickup -> OnSuccess'
		vp_GlobalEvent<vp_ItemPickup>.Register("TransmitPickupRespawn", TransmitPickupRespawn);		// sent by 'vp_ItemPickup -> OnEnable'

		vp_GlobalEvent<vp_ItemPickup>.Register("RegisterPickup", RegisterPickup);					// may be sent by external scripts

	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void OnDisable()
	{

#if UNITY_5_4_OR_NEWER
		SceneManager.sceneLoaded -= OnLevelLoad;
#endif

		vp_GlobalEvent<vp_ItemPickup, Transform>.Unregister("TransmitPickup", TransmitPickup);
		vp_GlobalEvent<vp_ItemPickup>.Unregister("TransmitPickupRespawn", TransmitPickupRespawn);

		vp_GlobalEvent<vp_ItemPickup>.Register("RegisterPickup", RegisterPickup);

	}


	/// <summary>
	/// clears the dictionaries of scene pickups and scans the scene anew
	/// for any pickups, adding them to the dictionaries
	/// </summary>
	protected virtual void RegisterPickups()
	{

		Pickups.Clear();
		PickupRespawners.Clear();

		// find all pickups and assign IDs to them based on initial position
		List<vp_ItemPickup> pickups = new List<vp_ItemPickup>(FindObjectsOfType<vp_ItemPickup>());
		foreach (vp_ItemPickup p in pickups)
		{
			p.ID = vp_Utility.PositionToID(p.transform.position);

			// SNIPPET: the below line can be uncommented to show debug-IDs
			// floating above each pickup. to do so, also uncomment the
			// corresponding snippets in vp_ItemPickup.cs
			// NOTE: if you are having trouble seeing the IDs, try
			// increasing nametag height in the bracket that adds nametags, e.g: 'm_NameTag.WorldHeightOffset = 1.5f;'
			//p.ShowID = true;//

			RegisterPickup(p);

		}

	}


	/// <summary>
	/// registers a pickup under a position-based ID
	/// </summary>
	protected virtual void RegisterPickup(vp_ItemPickup p)
	{

		// since a single gameobject may have several vp_Pickup components
		// on them, pickups are stored in a dictionary with id keys and
		// pickup list values. (an example of a multi-pickup object is a
		// grenade, which has both a grenade thrower pickup and an ammo
		// pickup on the same object)

		if (!Pickups.ContainsKey(p.ID))
		{
			// if the pickup does not exist, we add a list with a single pickup
			// under the position based ID

			List<vp_ItemPickup> ip = new List<vp_ItemPickup>();
			ip.Add(p);
			Pickups.Add(p.ID, ip);
			vp_Respawner r = p.GetComponent<vp_Respawner>();
			if ((r != null)
				&& !PickupRespawners.ContainsKey(p.ID))
			{
				PickupRespawners.Add(p.ID, r);
				r.m_SpawnMode = vp_Respawner.SpawnMode.SamePosition;
			}
			//else TODO: warn
		}
		else
		{
			// if a pickup already exists with this id, we unpack the list,
			// remove it from the dictionary, add the new pickup and re-add
			// it under the same id
	
			List<vp_ItemPickup> ip;
			if(Pickups.TryGetValue(p.ID, out ip) && ip != null)
			{
				Pickups.Remove(p.ID);
				ip.Add(p);
				Pickups.Add(p.ID, ip);
			}
		}
		//else TODO: warn


	}


	/// <summary>
	/// sends RPC to remote machines telling them to hide a certain pickup
	/// and try to give its contents to a certain player
	/// </summary>
	protected virtual void TransmitPickup(vp_ItemPickup pickup, Transform recipient)
	{
		//Debug.Log("TransmitPickup: " + pickup.ID);
		if (!PhotonNetwork.isMasterClient)
			return;

		vp_MPNetworkPlayer player;
		if (!vp_MPNetworkPlayer.Players.TryGetValue(recipient.transform.root, out player))
			return;

		//Debug.Log(pickup + " with ID: " + pickup.ID + " was picked up on Master by player " + player.ID);

		photonView.RPC("ReceivePickup", PhotonTargets.Others, pickup.ID, player.ID);

	}


	/// <summary>
	/// sends RPC to remote machines telling them to reenable a pickup
	/// </summary>
	protected virtual void TransmitPickupRespawn(vp_ItemPickup pickup)
	{

		if (!PhotonNetwork.isMasterClient)
			return;

		//Debug.Log("Picked up: " + pickup + " with ID: " + pickup.ID);

		photonView.RPC("ReceivePickupRespawn", PhotonTargets.Others, pickup.ID);

	}

	
	/// <summary>
	/// responds to master giving a pickup to a player over the network
	/// </summary>
	[PunRPC]
	void ReceivePickup(int id, int playerID, PhotonMessageInfo info)
	{

		if (!info.sender.IsMasterClient)
			return;

		List<vp_ItemPickup> pickups;
		if (!Pickups.TryGetValue(id, out pickups))
			return;

		if(pickups[0].gameObject != null)
			vp_Utility.Activate(pickups[0].gameObject, false);

		vp_MPNetworkPlayer player;
		if (!vp_MPNetworkPlayer.PlayersByID.TryGetValue(playerID, out player))
			return;

		if (player == null)
			return;

		if (player.Collider == null)
			return;

		foreach (vp_ItemPickup p in pickups)
		{

			p.TryGiveTo(player.Collider);

		}

	}
	

	/// <summary>
	/// responds to master respawning a pickup over the network
	/// </summary>
	[PunRPC]
	void ReceivePickupRespawn(int id, PhotonMessageInfo info)
	{

		if (!info.sender.IsMasterClient)
			return;

		vp_Respawner pickupRespawner;
		if (!PickupRespawners.TryGetValue(id, out pickupRespawner))
			return;

		if (pickupRespawner == null)
			return;

		pickupRespawner.Respawn();

	}


	/// <summary>
	/// 
	/// </summary>
#if UNITY_5_4_OR_NEWER
	protected void OnLevelLoad(Scene scene, LoadSceneMode mode)
#else
	protected void OnLevelWasLoaded()
#endif
	{

		RegisterPickups();

	}


}
