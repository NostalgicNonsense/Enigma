/////////////////////////////////////////////////////////////////////////////////
//
//	vp_MPPlayerSpawner.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	handles multiplayer instantiation and destruction of player
//					gameobjects, plus the allocation of spawnpoints for - and
//					respawning of - players.
//
//					this component has an inspector list of available player types
//					(local + remote prefab combos for in-game visual representation).
//					only player types in this list will be able to spawn in multiplayer.
//
//					the 'Add Prefabs' and 'Add Components' foldouts lists objects that
//					will be auto-added to local vs. remote players immediately after
//					instantiation. this is a workflow optimization feature that allows
//					using the plain local player prefab + a plain wizard-generated
//					remote player prefab without further adjustments, potentially saving
//					you tons of time.
//
//					IMPORTANT: this component should be added to the gameobject _before_
//					the vp_MPMaster component
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ExitGames.Client.Photon;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class vp_MPPlayerSpawner : Photon.MonoBehaviour
{

	// only player types in this list will be able to spawn in multiplayer
	protected Dictionary<string, vp_MPPlayerType> m_AvailablePlayerTypes = new Dictionary<string, vp_MPPlayerType>();
	public List<vp_MPPlayerType> AvailablePlayerTypes;

	// array of component types that will break a local player
	protected System.Type[] IllegalLocalComponents = new System.Type[]
	{
		typeof(vp_MPRemotePlayer)
	};

	// array of component types that will break a remote player
	protected System.Type[] IllegalRemoteComponents = new System.Type[]
	{
		typeof(vp_FPCamera),
		typeof(vp_FPController),
		typeof(Camera),
		typeof(AudioListener),
		typeof(vp_FPInput),
		typeof(vp_SimpleCrosshair),
		typeof(vp_FootstepManager),	// TEMP
		typeof(vp_FPInteractManager),
		typeof(vp_SimpleHUD),
		typeof(vp_PainHUD),
		typeof(vp_FPEarthquake),
		typeof(CharacterController),
		typeof(vp_FPWeaponHandler),
		typeof(vp_FPPlayerDamageHandler),
		typeof(vp_MPLocalPlayer)
	};

	// --- properties ---

	private static vp_MPPlayerSpawner m_Instance = null;
	public static vp_MPPlayerSpawner Instance
	{
		get
		{
			if (m_Instance == null)
			{
				m_Instance = Component.FindObjectOfType(typeof(vp_MPPlayerSpawner)) as vp_MPPlayerSpawner;
				//if (m_Instance == null)
				//	Debug.LogError("Error (vp_MPPlayerSpawner) Found no player spawner object. This is bad!");
			}
			return m_Instance;
		}
	}

	// these classes represent objects that will be dynamically added
	// to a player after instantiation

	[System.Serializable]
	public class AddedPrefab
	{
		public GameObject Prefab = null;
		public string ParentName = "";
	}

	[System.Serializable]
	public class AddedComponent
	{

		public string ComponentName = "";
		public string TransformName = "";

		public AddedComponent(string componentName, string transformName = "")
		{
			ComponentName = componentName;
			TransformName = transformName;
		}

	}
	
	////////////// 'Add Prefabs' section ////////////////
	[System.Serializable]
	public class AddPrefabsSection
	{
		public List<AddedPrefab> Local = new List<AddedPrefab>();
		public List<AddedPrefab> Remote = new List<AddedPrefab>();

#if UNITY_EDITOR
		[vp_HelpBox("Names of prefabs that will be childed to every Local vs. Remote player prefab on instantiation, along with the name of a parent transform inside the hierarchy (empty = root). Use this for components that you wish to initialize with non-default values, such as fonts or textures. Do NOT use for weapons.", UnityEditor.MessageType.None, typeof(vp_MPPlayerSpawner), null, true)]
		public float addPrefabsHelp;
#endif


	}
	[SerializeField]
	protected AddPrefabsSection m_AddPrefabs;

	////////////// 'Add Components' section ////////////////
	[System.Serializable]
	public class AddComponentsSection
	{
		public List<AddedComponent> Local = new List<AddedComponent>(new AddedComponent[] { new AddedComponent("vp_MPPlayerStats")});
		public List<AddedComponent> Remote = new List<AddedComponent>(new AddedComponent[] { new AddedComponent("vp_MPPlayerStats") });

#if UNITY_EDITOR
		[vp_HelpBox("Names of components that will be added to every Local vs. Remote player prefab on instantiation, along with the name of a target transform inside the hierarchy (empty = root). NOTE: All components will have their default values.", UnityEditor.MessageType.None, typeof(vp_MPPlayerSpawner), null, true)]
		public float addComponentsHelp;
#endif


	}
	[SerializeField]
	protected AddComponentsSection m_AddComponents;


	/// <summary>
	/// 
	/// </summary>
	protected virtual void Awake()
	{

		// disable any player objects present in the scene by default.
		// in multiplayer, spawning is the only way to go
		DeactivateScenePlayers();

		// check user-added player types for errors and store them if OK
		ValidatePlayerTypes();

	}

	
	/// <summary>
	/// deactivates any players (local or remote) that are present
	/// in the scene upon Awake. BACKGROUND: player objects may be
	/// placed in the scene to facilitate updating their prefabs,
	/// however once a multiplayer session starts only instantiated
	/// players are allowed
	/// </summary>
	protected virtual void DeactivateScenePlayers()
	{

		vp_PlayerEventHandler[] players = FindObjectsOfType<vp_PlayerEventHandler>();
		foreach (vp_PlayerEventHandler p in players)
		{
			vp_Utility.Activate(p.gameObject, false);
		}

	}

	
	/// <summary>
	/// respawns the network player of transform 't' at a random, team
	/// based placement
	/// </summary>
	public static void TransmitRespawn(Transform t)
	{

		if (!PhotonNetwork.isMasterClient)
			return;

		vp_MPNetworkPlayer p = vp_MPNetworkPlayer.Get(t);
		if ((p != null) && vp_MPTeamManager.Exists && (p.TeamNumber > 0))
			TransmitPlayerRespawn(t, GetRandomPlacement(vp_MPTeamManager.GetTeamName(p.TeamNumber)));
		else
			TransmitPlayerRespawn(t, GetRandomPlacement());

	}


	/// <summary>
	/// respawns the network player of 'transform' at 'placement'
	/// </summary>
	public static void TransmitPlayerRespawn(Transform transform, vp_Placement placement)
	{
		
		if (!PhotonNetwork.isMasterClient)
			return;

		//UnityEngine.Debug.Log("respawning " + t.gameObject.name);

		vp_MPNetworkPlayer.RefreshPlayers();

		vp_MPNetworkPlayer player = vp_MPNetworkPlayer.Get(transform);
		if (player != null)
			player.photonView.RPC("ReceivePlayerRespawn", PhotonTargets.All, placement.Position, placement.Rotation);
		
	}
	
	
	/// <summary>
	/// gets a vp_Placement object based on the optional 'spawnPointTag'.
	/// if no tag is provided a random spawnspoint will be returned
	/// </summary>
	public static vp_Placement GetRandomPlacement(string spawnPointTag = null)
	{

		if (spawnPointTag == "NoTeam")
			spawnPointTag = null;

		//Debug.Log("spawnPointTag: " + spawnPointTag);

		vp_Placement placement = vp_SpawnPoint.GetRandomPlacement(0.5f, spawnPointTag);
		if (placement == null)
			placement = new vp_Placement();
		return placement;

	}


	/// <summary>
	/// this RPC constitutes a 'permission to spawn' sent to us by the
	/// master client in response to a request of such info that every
	/// player makes in 'vp_MPConnection -> OnJoinedRoom'. the initial
	/// spawn info can only be issued by the master client
	/// </summary>
	[PunRPC]
	void ReceiveInitialSpawnInfo(int id, PhotonPlayer player, Vector3 pos, Quaternion rot, string playerTypeName, int teamNumber, PhotonMessageInfo info)
	{
		
		if ((info.sender != PhotonNetwork.masterClient) &&
			(info.sender != PhotonNetwork.player))
			return;

		// initialize the newborn player with the mandatory 'on-join-stats'
		ExitGames.Client.Photon.Hashtable stats = new ExitGames.Client.Photon.Hashtable();
		stats.Add("Type", playerTypeName);
		stats.Add("Team", teamNumber);
		stats.Add("Position", pos);
		stats.Add("Rotation", rot);

		// announce arrival of new player
		if (player.ID > PhotonNetwork.player.ID)
			vp_MPDebug.Log(player.NickName + " joined the game"
				+ ((vp_MPTeamManager.Exists && (teamNumber > 0)) ? " team : " + vp_MPTeamManager.GetTeamName(teamNumber).ToUpper() : "")
				);
		else if (player.ID == PhotonNetwork.player.ID)
		{
			vp_Timer.In(0.1f, delegate()
			{
				//Debug.Log("teamNumber: " + teamNumber);
				
				try
				{
					vp_MPDebug.Log(
						"Welcome to '"
						+ PhotonNetwork.room.Name
						+ "' with "
						+ PhotonNetwork.room.PlayerCount.ToString()
						+ ((PhotonNetwork.room.PlayerCount == 1) ? " player (you)" : " players")
						+ "."
						);
					//vp_MPDebug.Log("Max players for room: " + PhotonNetwork.room.maxPlayers + ".");
					//vp_MPDebug.Log("Total players using app: " + PhotonNetwork.countOfPlayers);
					if (vp_MPTeamManager.Exists && (teamNumber > 0))
						vp_MPDebug.Log("Your team is: " + vp_MPTeamManager.GetTeamName(teamNumber).ToUpper());
				}
				catch
				{
					if (PhotonNetwork.room == null)
						Debug.Log("PhotonNetwork.room = null");
					else if (PhotonNetwork.room.Name == null)
						Debug.Log("PhotonNetwork.room.name = null");
					if (vp_MPTeamManager.Instance == null)
						Debug.Log("vp_MPTeamManager.Instance = null");
					else if (vp_MPTeamManager.GetTeamName(teamNumber) == null)
						Debug.Log("vp_MPMaster.GetTeamName(teamNumber) = null");
				}

			});
		}

		InstantiatePlayerPrefab(player, stats);

	}


	/// <summary>
	/// creates a new player gameobject in the scene based on a photon
	/// player and a playerstats hashtable
	/// </summary>
	public static void InstantiatePlayerPrefab(PhotonPlayer player, ExitGames.Client.Photon.Hashtable playerStats)
	{

		if (player == null)
		{
			//Debug.Log("Got spawninfo for player with ID: " + id + ", who appears to have left so skip.");
			return;
		}

		//Debug.Log("Got spawninfo for player with ID: " + id);

		// we need to extract a few stats in advance from 'playerStats' before
		// a networkplayer can be created: namely playertype, position and rotation.
		// with that info we can spawn the player prefab, and use 'SetStats' to
		// refresh that prefab with the _entire_ provided 'playerStats'
		vp_MPPlayerType playerType = GetPlayerTypeFromHashtable(playerStats);					// extract player type
		Vector3 pos = (Vector3)vp_MPPlayerStats.GetFromHashtable(playerStats, "Position");	// extract position
		//Debug.Log("initial stats for player "+player.ID+": " + playerStats.ToString().Replace("(System.String)", ""));
		Quaternion rot = (Quaternion)vp_MPPlayerStats.GetFromHashtable(playerStats, "Rotation");	// extract rotation											// TODO: extract rotation

		// instantiate prefab
		GameObject newPlayer;
		if (player.ID == PhotonNetwork.player.ID)
		{

			if(playerType.LocalPrefab == null)
			{
				Debug.LogError("Error (" + /*this +*/ ") Player type '" + playerType.name + "' has no local player prefab. Please assign one.");
				return;
			}

			newPlayer = Instantiate(playerType.LocalPrefab, pos, rot) as GameObject;
			vp_MPRemotePlayer r = newPlayer.GetComponentInChildren<vp_MPRemotePlayer>();
			if (r != null)
				Component.Destroy(r);
			vp_MPLocalPlayer l = newPlayer.GetComponentInChildren<vp_MPLocalPlayer>();
			if (l == null)
				l = newPlayer.AddComponent<vp_MPLocalPlayer>();
			
			Instance.AddPrefabs(newPlayer.transform, Instance.m_AddPrefabs.Local);
			Instance.AddComponents(newPlayer.transform, Instance.m_AddComponents.Local);

			// initialize the localplayer's stats hashtable with the entire 'playerStats'
			l.Stats.SetFromHashtable(playerStats);

			AddPhotonViewToPlayer(l, player.ID);

			// show/hide 3rd person weapons on the spawned player	// TODO: move to RefreshPlayers
			if(l.WeaponHandler != null)
				l.WeaponHandler.RefreshAllWeapons();

		}
		else
		{

			if (playerType.RemotePrefab == null)
			{
				Debug.LogError("Error (vp_MPPlayerSpawner) Player type '" + playerType.name + "' has no remote player prefab. Please assign one.");
				return;
			}

			newPlayer = Instantiate(playerType.RemotePrefab, pos, rot) as GameObject;
			vp_MPLocalPlayer l = newPlayer.GetComponentInChildren<vp_MPLocalPlayer>();
			if (l != null)
				Component.Destroy(l);
			vp_MPRemotePlayer r = newPlayer.GetComponentInChildren<vp_MPRemotePlayer>();
			if (r == null)
				r = newPlayer.AddComponent<vp_MPRemotePlayer>();

			r.SetAnimated(false);
			r.LastMasterPosition = pos;
			r.LastMasterRotation = rot;

			Instance.AddPrefabs(newPlayer.transform, Instance.m_AddPrefabs.Remote);
			Instance.AddComponents(newPlayer.transform, Instance.m_AddComponents.Remote);

			// initialize the remoteplayer's stats hashtable with the entire 'playerStats'
			//if (!PhotonNetwork.isMasterClient)	// TODO: needed?
			//	r.Inventory.Clear();
			r.Stats.SetFromHashtable(playerStats);

			AddPhotonViewToPlayer(r, player.ID);

			// show/hide 3rd person weapons on the spawned player
			if (r.WeaponHandler != null)
				r.WeaponHandler.RefreshAllWeapons();
			
		}

		vp_MPNetworkPlayer.RefreshPlayers();

	}


	/// <summary>
	/// given a full game state, this method instantiates any 'missing'
	/// player prefabs, that is: players spawned by RPC from a previous
	/// master that would otherwise be invisible (non-existant) to players
	/// having joined post a master client handover. by default this is
	/// called from 'vp_MPMaster -> SyncGameState'
	/// </summary>
	public void InstantiateMissingPlayerPrefabs(ExitGames.Client.Photon.Hashtable gameState)
	{

		// fetch all photonviews	// TODO: optimize?
		PhotonView[] views = Component.FindObjectsOfType(typeof(PhotonView)) as PhotonView[];

		foreach (object o in gameState.Keys)
		{
			//Debug.Log("3: " + o.ToString());

			if (o.GetType() != typeof(int))
				continue;
			//Debug.Log("4 GAME STATE PLAYER -> " + o.ToString());

			int id = (int)o;
			bool hasView = false;
			foreach (PhotonView f in views)
			{
				//Debug.Log("5: " + f.ToString());

				if (f.ownerId == id)
					hasView = true;
			}
			if (hasView)
				continue;
			//Debug.Log("6: NEED TO SPAWN PLAYER -> " + o.ToString());

			object pp;
			if (!gameState.TryGetValue(o, out pp))
			{
				//Debug.Log("failed to get hashtable for player " + o.ToString());
				continue;
			}
			ExitGames.Client.Photon.Hashtable playerStats = pp as ExitGames.Client.Photon.Hashtable;
			if (playerStats == null)
			{
				//Debug.Log("failed to get hashtable for player " + o.ToString());
				continue;
			}

			PhotonPlayer player = vp_MPPlayerSpawner.GetPhotonPlayerById(id);
			if (player == null)
			{
				//Debug.Log("failed to get photonplayer by id");
				continue;
			}

			vp_MPPlayerSpawner.InstantiatePlayerPrefab(player, playerStats);

		}

	}


	/// <summary>
	/// adds standard components to every player upon spawn, as defined under
	/// the editor 'Add Components' foldout
	/// </summary>
	protected virtual void AddComponents(Transform rootTransform, List<AddedComponent> components)
	{

		// TODO: cache types in a dictionary so we only have to use 'GetType' once per type

		if (rootTransform == null)
			return;

		foreach (AddedComponent o in components)
		{

			if (string.IsNullOrEmpty(o.ComponentName))
				continue;

			Transform t = null;
			if (!string.IsNullOrEmpty(o.TransformName))
				t = vp_Utility.GetTransformByNameInChildren(rootTransform, o.TransformName, true);
			if (t == null)
				t = rootTransform.transform;

			System.Type type = System.Type.GetType(o.ComponentName);
			Component res = t.gameObject.AddComponent(type);
			if (res == null)
			{
				Debug.LogError("Error (" + this + ") '" + o.ComponentName + "' does not exist or is not of type Component and can not be added to a player.");
				continue;
			}

		}

	}


	/// <summary>
	/// adds standard prefabs to every player upon spawn, as defined under
	/// the editor 'Add Prefabs' foldout
	/// </summary>
	protected virtual void AddPrefabs(Transform rootTransform, List<AddedPrefab> prefabs)
	{

		if (rootTransform == null)
			return;

		foreach (AddedPrefab o in prefabs)
		{

			if (o.Prefab == null)
				continue;
	#if UNITY_EDITOR	//'PrefabUtility' is only available in the editor
			if (PrefabUtility.GetPrefabType(o.Prefab) == PrefabType.None)
			{
				Debug.LogError("Error (" + this + ") The gameobject '" + o.Prefab.name + "' is not a prefab! Scene objects are not allowed as auto-added components.");
				continue;
			}
	#endif
			Transform t = null;
			if (!string.IsNullOrEmpty(o.ParentName))
				t = vp_Utility.GetTransformByNameInChildren(rootTransform, o.ParentName, true);
			if (t == null)
			{
				// TIP: uncomment this if missing target gameobject should not be allowed
				//Debug.LogError("Error (" + this + ") 'AddPrefabs' found no transform named '" + o.ParentName + "' in " + rootTransform + ".");
				continue;
			}

			GameObject n = (GameObject)GameObject.Instantiate(o.Prefab);
			n.transform.parent = t;
			n.transform.localPosition = Vector3.zero;

		}

	}


	/// <summary>
	/// 
	/// </summary>
	static void AddPhotonViewToPlayer(vp_MPNetworkPlayer networkPlayer, int id)
	{
		PhotonView p = null;

		p = (PhotonView)networkPlayer.gameObject.AddComponent<PhotonView>();

        //Dz fix
        int new_id = (id * 1000) + 1;
        while (PhotonView.Find(new_id) != null)
        {
            new_id = Random.Range(5, 9999);
        }

        p.viewID = new_id;
        //End Dz fix

        //p.viewID = (id * 1000) + 1;	// TODO: may crash with 'array index out of range' if a player is deactivated in its prefab
		p.onSerializeTransformOption = OnSerializeTransform.OnlyPosition;
		p.ObservedComponents = new List<Component>();
		p.ObservedComponents.Add(networkPlayer);
		p.synchronization = ViewSynchronization.UnreliableOnChange;

		PhotonNetwork.networkingPeer.RegisterPhotonView(p);
	}


	/// <summary>
	/// extracts the playertype stat from the passed hashtable
	/// </summary>
	static vp_MPPlayerType GetPlayerTypeFromHashtable(ExitGames.Client.Photon.Hashtable table)
	{

		string playerTypeName = vp_MPPlayerStats.GetFromHashtable(table, "Type") as string;
		if (string.IsNullOrEmpty(playerTypeName))
		{
			Debug.LogError("Error (vp_MPPlayerSpawner) Failed to extract playerTypeName.");
			return null;
		}

		vp_MPPlayerType playerType = GetPlayerTypeByName(playerTypeName);
		if (playerType == null)
		{
			Debug.LogError("Error (vp_MPPlayerSpawner) Failed to get playerType.'" + playerTypeName + "'.");
			return null;
		}

		return playerType;

	}
	

	/// <summary>
	/// returns a photon player by its ID
	/// </summary>
	public static PhotonPlayer GetPhotonPlayerById(int id)
	{

		foreach(PhotonPlayer p in PhotonNetwork.playerList)
		{
			if (p.ID == id)
				return p;
		}
		return null;

	}


	/// <summary>
	/// returns a player type object by its name
	/// </summary>
	public static vp_MPPlayerType GetPlayerTypeByName(string playerTypeName)
	{

		if (Instance == null)
			return null;

		vp_MPPlayerType playerType = null;
		if (!Instance.m_AvailablePlayerTypes.TryGetValue(playerTypeName, out playerType))
		{
			Debug.LogError("Error (vp_MPPlayerSpawner) Failed to get player type '" + playerTypeName + "'. Make sure this object exists in your MPPlayerSpawner->PlayerTypes list.");
			return null;
		}

		return playerType;

	}


	/// <summary>
	/// returns the default player type (if any)
	/// </summary>
	public static vp_MPPlayerType GetDefaultPlayerType()
	{

		if (Instance == null)
			return null;

		if(Instance.AvailablePlayerTypes == null)
			return null;

		if (Instance.AvailablePlayerTypes.Count < 1)
			return null;

		return Instance.AvailablePlayerTypes[0];

	}
	

	/// <summary>
	/// analyzes every user-added player type for errors, and if they
	/// pass: adds them to the player type dictionary
	/// </summary>
	protected virtual void ValidatePlayerTypes()
	{

		m_AvailablePlayerTypes.Clear();

		foreach (vp_MPPlayerType p in AvailablePlayerTypes)
		{

			if (p == null)
			{
				Debug.LogError("Error (" + this + ") Atleast one player type is null. Please make sure all player types are set or remove the ones that are empty.");
				return;
			}

			if (m_AvailablePlayerTypes.ContainsKey(p.name))
			{
				Debug.LogError("Error (" + this + ") Found two or more 'PlayerTypes' with identical names. This is not supported.");
				return;
			}

			if (p.LocalPrefab == null)
			{
				Debug.LogError("Error (" + this + ") LocalPrefab of player type: " + p + " is null. Please assign one.");
				return;
			}

			if (p.RemotePrefab == null)
			{
				Debug.LogError("Error (" + this + ") RemotePrefab of player type: " + p + " is null. Please assign one.");
				return;
			}

			if (p.RemotePrefab == p.LocalPrefab)
			{
				Debug.LogError("Error (" + this + ") Local and Remote prefabs of player type: " + p + " are identical. This is not supported.");
				return;
			}

			foreach (System.Type t in IllegalRemoteComponents)
			{
				if (p.RemotePrefab.GetComponentInChildren(t))
					Debug.LogError("Error (" + this + ") RemotePrefab of player type: " + p + " contains a component of type " + t.Name + ". This is not supported.");
			}

			foreach (System.Type t in IllegalLocalComponents)
			{
				if (p.LocalPrefab.GetComponentInChildren(t))
					Debug.LogError("Error (" + this + ") LocalPrefab of player type: " + p + " contains a component of type " + t.Name + ". This is not supported.");
			}

			m_AvailablePlayerTypes.Add(p.name, p);

		}

	}


	/// <summary>
	/// refreshes player list, which should result in the gameobject
	/// of this player getting destroyed
	/// </summary>
	void OnPhotonPlayerDisconnected(PhotonPlayer player)
	{

		vp_MPNetworkPlayer.RefreshPlayers();

	}


}

