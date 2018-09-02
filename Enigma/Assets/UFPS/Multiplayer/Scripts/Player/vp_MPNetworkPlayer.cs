/////////////////////////////////////////////////////////////////////////////////
//
//	vp_MPNetworkPlayer.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	base class for the UFPS player object in multiplayer. implements
//					basic network functionality for respawn, firing, dying and teleport.
//					declares some fundamental stats and creates references to a number
//					of expected player components. this is the abstract base class of
//					vp_MPLocalPlayer and vp_MPRemotePlayer, acting as a bridge between
//					the photon cloud and every UFPS player object in your scene (whether
//					remote or local). it is game mode agnostic
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public abstract partial class vp_MPNetworkPlayer : Photon.MonoBehaviour
{

	// fundamental stats
	public int ID { get { return photonView.ownerId; } }	// returns id of corresponding photon player
	public vp_MPPlayerType PlayerType = null;				// determines the local and remote player prefabs to be used for visual representation
	public int TeamNumber = 0;								// optional, can be used for competitive gameplay or just for organizing players into groups
	public int Shots = 0;                                   // amount of times the player has spawned projectiles. used for establishing deterministic
															// random seeds that will be the same on all machines _without_ sending data over the network

	// returns the client's current roundtrip time to the photon server.
	// NOTE: this is reported locally by every client in 'vp_MPConnection.UpdatePing'
	public int Ping                                         
	{
		get
		{
			if (photonView == null)
				return 0;
			if (photonView.owner == null)
				return 0;
			if (photonView.owner.CustomProperties["Ping"] == null)
				return 0;
			return (int)photonView.owner.CustomProperties["Ping"];
		}
	}

	// misc
	public Transform Transform = null;
	public vp_MPTeam Team { get { return (vp_MPTeamManager.Exists ? vp_MPTeamManager.Instance.Teams[TeamNumber] : null); } }
	public new bool DontDestroyOnLoad = true;

	// work variables
	[HideInInspector]
	public Vector3 LastMasterPosition = Vector3.zero;
	[HideInInspector]
	public Quaternion LastMasterRotation = Quaternion.identity;

	// --- required components ---

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

	protected vp_Controller m_Controller = null;
	public vp_Controller Controller
	{
		get
		{
			if (m_Controller == null)
				m_Controller = (vp_Controller)transform.root.GetComponentInChildren(typeof(vp_Controller));
			return m_Controller;
		}
	}

	protected vp_MPPlayerStats m_Stats = null;
	public vp_MPPlayerStats Stats
	{
		get
		{
			if (m_Stats == null)
				m_Stats = (vp_MPPlayerStats)transform.root.GetComponentInChildren(typeof(vp_MPPlayerStats));
			return m_Stats;
		}
	}
	
	// --- expected components ---

	protected Dictionary<int, vp_WeaponShooter> m_Shooters = new Dictionary<int, vp_WeaponShooter>();

	protected vp_WeaponHandler m_WeaponHandler = null;
	public vp_WeaponHandler WeaponHandler
	{
		get
		{
			if (m_WeaponHandler == null)
				m_WeaponHandler = (vp_WeaponHandler)transform.root.GetComponentInChildren(typeof(vp_WeaponHandler));
			return m_WeaponHandler;
		}
	}

	Collider m_Collider = null;
	public Collider Collider
	{
		get
		{
			if (m_Collider == null)
				m_Collider = Transform.GetComponentInChildren<Collider>();
			return m_Collider;
		}
	}

	protected vp_PlayerDamageHandler m_DamageHandler = null;
	public vp_PlayerDamageHandler DamageHandler
	{
		get
		{
			if (m_DamageHandler == null)
				m_DamageHandler = (vp_PlayerDamageHandler)transform.root.GetComponentInChildren<vp_PlayerDamageHandler>();
			return m_DamageHandler;
		}
	}

	protected vp_Respawner m_Respawner = null;
	public vp_Respawner Respawner
	{
		get
		{
			if (m_Respawner == null)
				m_Respawner = (vp_Respawner)transform.root.GetComponentInChildren<vp_Respawner>();
			return m_Respawner;
		}
	}

	protected vp_PlayerItemDropper m_ItemDropper = null;
	public vp_PlayerItemDropper ItemDropper
	{
		get
		{
			if (m_ItemDropper == null)
				m_ItemDropper = (vp_PlayerItemDropper)transform.root.GetComponentInChildren<vp_PlayerItemDropper>();
			return m_ItemDropper;
		}
	}


	// --- static dictionaries of player info and stats ---

	/// <summary>
	/// dictionary of players, stored by transform.
	/// a network player will be added to this dictionary on Awake,
	/// and removed by 'RefreshPlayers' when it no longer exists
	/// </summary>
	public static Dictionary<Transform, vp_MPNetworkPlayer> Players
	{
		get
		{
			if (m_Players == null)
				m_Players = new Dictionary<Transform, vp_MPNetworkPlayer>();
			return m_Players;
		}
	}
	protected static Dictionary<Transform, vp_MPNetworkPlayer> m_Players = null;
	

	/// <summary>
	/// dictionary of players, stored by ID.
	/// a network player will be added to this dictionary by the 'Get(id)'
	/// method, and removed by 'RefreshPlayers' when it no longer exists.
	/// TODO: this dictionary should not be public since it is not as reliable
	/// as the public 'Players' dictionary, or as using the Get(id) method.
	/// </summary>
	public static Dictionary<int, vp_MPNetworkPlayer> PlayersByID
	{
		get
		{
			if (m_PlayersByID == null)
				m_PlayersByID = new Dictionary<int, vp_MPNetworkPlayer>();
			return m_PlayersByID;
		}
	}
	protected static Dictionary<int, vp_MPNetworkPlayer> m_PlayersByID = null;

	
	/// <summary>
	/// a keycollection returning the integer IDs of all players
	/// </summary>
	public static Dictionary<int, vp_MPNetworkPlayer>.KeyCollection IDs
	{
		get
		{
			return PlayersByID.Keys;
		}
	}
	

	/// <summary>
	/// 
	/// </summary>
	public virtual void Awake()
	{

		Transform = transform;

		// ensure that all network players ever created are added to the
		// player list. NOTE: we don't add players to the 'PlayerIDs'
		// dictionary here, since ID has not been assigned yet and is 0
		if (!Players.ContainsKey(Transform.root) && !Players.ContainsValue(this))
			Players.Add(Transform.root, this);

		if(DontDestroyOnLoad)
			UnityEngine.Object.DontDestroyOnLoad(transform.gameObject);

	}


	/// <summary>
	/// WARNING: never override this without calling base, or remote
	/// players won't be able to fire
	/// </summary>
	public virtual void Start()
	{

		InitShooters();

	}
	

	/// <summary>
	/// 
	/// </summary>
	public virtual void Update() { }


	/// <summary>
	/// 
	/// </summary>
	public virtual void FixedUpdate() { }


	/// <summary>
	/// updates position, rotation and velocity over the network
	/// </summary>
	public abstract void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info);


	/// <summary>
	/// updates weapon over the network
	/// </summary>
	protected abstract void ForceSyncWeapon(PhotonStream stream);


	/// <summary>
	/// removes departed players and refreshes components of remaining
	/// players. this includes team color, spawnpoint targets, collider
	/// event logic, body materials and gameobject names in the editor
	/// </summary>
	public static void RefreshPlayers()
	{

		// --- removes departed players ---

		List<int> nullIDs = null;
		List<Transform> nullTransforms = null;
		List<GameObject> departedPlayers = null;

		// find network players whose photon players have left
		foreach (vp_MPNetworkPlayer p in vp_MPNetworkPlayer.Players.Values)
		{
			if (p == null)
				continue;
			if ((p.photonView == null)  || (p.photonView.owner == null))
			{
				if (departedPlayers == null)
					departedPlayers = new List<GameObject>();
				departedPlayers.Add(p.gameObject);
			}
		}

		if (departedPlayers != null)
		{
			foreach (GameObject g in departedPlayers)
			{
				UnityEngine.Object.DestroyImmediate(g);
			}
		}

		// find transforms that have no network player
		foreach (Transform key in Players.Keys)
		{
			vp_MPNetworkPlayer player;
			Players.TryGetValue(key, out player);
			if (player == null)
			{
				if (nullTransforms == null)
					nullTransforms = new List<Transform>();
				nullTransforms.Add(key);
				continue;
			}

			if (!PlayersByID.ContainsValue(player))
			{
				if(player.ID != 0)
					PlayersByID.Add(player.ID, player);
			}

		}

		// find ids that have no player
		foreach (int key in PlayersByID.Keys)
		{
			vp_MPNetworkPlayer player;
			PlayersByID.TryGetValue(key, out player);
			if (player == null)
			{
				if (nullIDs == null)
					nullIDs = new List<int>();
				nullIDs.Add(key);
			}
		}

		// remove null IDs and transforms
		if (nullTransforms != null)
		{
			foreach (Transform t in nullTransforms)
			{
				Players.Remove(t);
			}
		}

		if (nullIDs != null)
		{
			foreach (int i in nullIDs)
			{
				PlayersByID.Remove(i);
			}
		}

		// --- refresh components of remaining players ---

		foreach (vp_MPNetworkPlayer p in vp_MPNetworkPlayer.Players.Values)
		{
			if (p == null)
				continue;
			p.RefreshComponents();
		}
		
	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void RefreshComponents()
	{

#if UNITY_EDITOR
		// update gameobject name in editor hierarchy view with
		// local/remote and master/client status
		gameObject.name = ID.ToString()
			+ ((ID == PhotonNetwork.player.ID) ? " (LOCAL)" : "")
			+ (((photonView.owner.IsMasterClient) ? "(MASTER)" : ""))
			;
		gameObject.name = gameObject.name.Replace(")(", ", ");
#endif

		// refresh respawner
		if (Respawner != null)
		{
			Respawner.m_SpawnMode = vp_Respawner.SpawnMode.SpawnPoint;
			if (vp_MPTeamManager.Exists)
				Respawner.SpawnPointTag = vp_MPTeamManager.GetTeamName(TeamNumber);		// we have teams - use team spawnpoint tags
		}

	}

	
	/// <summary>
	/// instantly teleports player to a position and rotation. remote
	/// player overrides this to prevent lerping its position
	/// </summary>
	public virtual void SetPosition(Vector3 position, Quaternion rotation)
	{

		SetPosition(position);
		SetRotation(rotation);

	}


	/// <summary>
	/// instantly teleports player to a position. remote player overrides
	/// this to prevent lerping its position
	/// </summary>
	public virtual void SetPosition(Vector3 position)
	{

		this.Transform.position = position;

	}


	/// <summary>
	/// instantly sets player rotation
	/// </summary>
	public virtual void SetRotation(Quaternion rotation)
	{

		if (Player == null)
			return;
		Player.Rotation.Set(rotation.eulerAngles);

	}


	/// <summary>
	/// initializes vp_Shooters in this player hierarchy for network
	/// purposes. see comments in vp_MPLocalPlayer and vp_MPRemotePlayer
	/// to understand how each class handles this
	/// </summary>
	public abstract void InitShooters();
	

	/// <summary>
	/// returns the vp_MPNetworkPlayer associated with a certain
	/// photon player id
	/// </summary>
	public static vp_MPNetworkPlayer Get(int id)
	{

		vp_MPNetworkPlayer player = null;
		if (!PlayersByID.TryGetValue(id, out player))
		{
			foreach (vp_MPNetworkPlayer p in Players.Values)
			{
				if (p == null)
					continue;
				if (p.ID == id)
				{
					PlayersByID.Add(id, p);
					return p;
				}
			}
		}

		return player;

	}


	/// <summary>
	/// returns the vp_MPNetworkPlayer associated with 'transform' (if any)
	/// </summary>
	public static vp_MPNetworkPlayer Get(Transform transform)
	{

		vp_MPNetworkPlayer player = null;
		Players.TryGetValue(transform, out player);
		return player;

	}


	/// <summary>
	/// returns photon player name of photon player with 'playerID'
	/// </summary>
	public static string GetName(int playerID)
	{

		for (int p = 0; p < PhotonNetwork.playerList.Length; p++)
		{
			if (PhotonNetwork.playerList[p].ID == playerID)
			{
				if (PhotonNetwork.playerList[p].NickName == "Player")
					return "Player " + playerID.ToString();
				else
					return PhotonNetwork.playerList[p].NickName;
			}
		}

		return "Unknown";

	}



	/// <summary>
	/// teleports all players to new positions as determined by
	/// their team spawnpoint settings
	/// </summary>
	public static void TransmitRespawnAll()
	{

		if (!PhotonNetwork.isMasterClient)
			return;

		foreach (vp_MPNetworkPlayer p in vp_MPNetworkPlayer.Players.Values)
		{
			if (p == null)
				continue;
			p.Player.Platform.Set(null);
			vp_Placement placement = null;
			if(vp_MPTeamManager.Exists)
				placement = vp_MPPlayerSpawner.GetRandomPlacement(vp_MPTeamManager.GetTeamName(p.TeamNumber));
			else
				placement = vp_MPPlayerSpawner.GetRandomPlacement();
			p.photonView.RPC("ReceivePlayerRespawn", PhotonTargets.All, placement.Position, placement.Rotation);
		}

	}


	/// <summary>
	/// 
	/// </summary>
	public static void TransmitUnFreezeAll()
	{

		if (!PhotonNetwork.isMasterClient)
			return;

		// abort ongoing setweapon activity on all players locally in master scene
		// or the activity may hang on level load
		foreach (vp_MPNetworkPlayer p in vp_MPNetworkPlayer.Players.Values)
		{
			if (p == null)
				continue;
			p.Player.SetWeapon.Stop();
		}

		// unfreeze all players on all machines
		vp_MPMaster.Instance.photonView.RPC("ReceiveUnFreeze", PhotonTargets.All);

	}


	/// <summary>
	/// this method can be used to protect against general distance cheats
	/// (like sending an RPC to push a button when you're in fact nowhere
	/// near it). for example: can be called on the master to verify that a
	/// player is within range of an object that it wants to manipulate.
	/// by default the method will return true if the player is inside or
	/// less than 2 meters away from the bounding box of 'collider', and
	/// false if not
	/// </summary>
	public bool IsCloseTo(Collider otherCollider, float distance = 2)
	{

		distance = Mathf.Max(distance, Player.Radius.Get() + Controller.SkinWidth);

		if (Vector3.Distance(Collider.bounds.center, otherCollider.ClosestPointOnBounds(Collider.bounds.center)) < distance)
			return true;	// player is in proximity to, or touching the collider bounds

		if (otherCollider.bounds.Contains(Collider.bounds.center))
			return true;	// player center is inside collider bounds (addresses cases where the above
							// distance check might fail due to standing inside very large bounds)

		if (otherCollider.bounds.Contains(Collider.bounds.center - (Vector3.up * (Player.Height.Get() * 0.5f))))
			return true;	// player is standing on collider bounds

		if (otherCollider.bounds.Contains(Collider.bounds.center + (Vector3.up * (Player.Height.Get() * 0.5f))))
			return true;	// player head is touching collider bounds

		// player is not close to the collider
		return false;

	}


	/// <summary>
	/// iterates the 'Shots' variable by one. this triggers every time a
	/// remote player fires a weapon (once per weapon discharge) and is
	/// used as a seed for generating deterministic random bullet spread
	/// that will be the same on all machines _without_ sending possibly
	/// hacked data over the network. the 'Shots' variable is later fetched
	/// in vp_Shooter's 'GetFireSeed' method and this is hooked up in the
	/// 'InitShooters' method of vp_MPLocalPlayer and vp_MPRemotePlayer.
	/// in short: prevents accuracy hacks
	/// </summary>
	[PunRPC]
	public virtual void FireWeapon(int weaponIndex, Vector3 position, Quaternion rotation, PhotonMessageInfo info)
	{

		// must be increased even if no shot is eventually fired, or bullet
		// simulation will go out of sync
		Shots++;

	}

	
	/// <summary>
	/// when this RPC arrives, the player will die immediately because the
	/// master client says so
	/// </summary>
	[PunRPC]
	public virtual void ReceivePlayerKill(PhotonMessageInfo info)
	{

		//Debug.Log(this.GetType() + ".ReceivePlayerKill");

		if (info.sender != PhotonNetwork.masterClient)
			return;

		// local master is not allowed to call 'DamageHandler.Die' on itself (infinite loop)
		if (PhotonNetwork.player == PhotonNetwork.masterClient)
			return;

		DamageHandler.Die();

	}

	
	/// <summary>
	/// when this RPC arrives the player will be instantly teleported
	/// to the position dictated by the master client
	/// </summary>
	[PunRPC]
	public virtual void ReceivePlayerRespawn(Vector3 position, Quaternion rotation, PhotonMessageInfo info)
	{

		//Debug.Log(this.GetType() + ".ReceivePlayerRespawn");

		if (info.sender != PhotonNetwork.masterClient)
			return;

		if (info.sender == PhotonNetwork.player)
		{
			// since we're the master, exit here to avoid an infinite loop.
			// (+ position has already been tweaked by the local respawner)
			// but snap position & rotation before aborting!
			transform.position = position;
			transform.rotation = rotation;
			SetPosition(position, rotation);
			return;
		}

		Respawner.PickSpawnPoint(position, rotation);	// pick the spawn point that the master has chosen for us

		SetPosition(position, rotation);

	}


	/// <summary>
	/// tells this player to drop a certain item. sent by the master.
	/// NOTE: requires a vp_ItemDropper component
	/// </summary>
	[PunRPC]
	public void ReceiveDropItem(string itemTypeName, Vector3 targetPosition, float targetYaw, int playerID, int itemID, int unitAmount, PhotonMessageInfo info)
	{

		if (!info.sender.IsMasterClient)
			return;

		if (playerID != ID)
			return;

		if (ItemDropper == null)
			return;

		ItemDropper.MPClientDropItem(itemTypeName, targetPosition, targetYaw, itemID, unitAmount);

	}


}