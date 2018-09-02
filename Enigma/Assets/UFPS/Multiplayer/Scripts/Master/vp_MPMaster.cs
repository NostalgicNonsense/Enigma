/////////////////////////////////////////////////////////////////////////////////
//
//	vp_MPMaster.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	this component is king of the multiplayer game!
//					it manages overall game logic of the master client. implements
//					multiplayer game time cycles, assembles and broadcasts full
//					or partial game states with game phase, clock and player stats.
//					allocates team and initial spawnpoint to joining players, plus
//					broadcasts our own version of the simulation in case of a local
//					master client handover
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Hashtable = ExitGames.Client.Photon.Hashtable;

#if UNITY_5_4_OR_NEWER
	using UnityEngine.SceneManagement;
#endif

public class vp_MPMaster : Photon.MonoBehaviour
{
	
	public float GameLength = (5 * 60);		// default: 5 minutes
	public float PauseLength = 20;			// default: 20 seconds
	public string CurrentLevel = "";		// current level loaded on the master and enforced on all clients. master will load this on login
#if UNITY_EDITOR
	[vp_HelpBox("'CurrentLevel' will be loaded by the master on login. Joining players will fetch this string from the master and proceed to load the correct level. At any time, the master method 'TransmitLoadLevel' can be called to make everyone load a new level.", UnityEditor.MessageType.None, typeof(vp_MPMaster), null, false, vp_PropertyDrawerUtility.Space.Nothing)]
	public float currentLevelHelp;
#endif

	protected bool m_TookOverGame = false;	// will always be false as long as we're a regular client. will be true if we
											// join as master, or if we become master (as soon as our first full game
											// state has been broadcast) and will never go false again in the same game
	
	private static vp_MPMaster m_Instance;
	public static vp_MPMaster Instance
	{
		get
		{
			if (m_Instance == null)
				m_Instance = Component.FindObjectOfType<vp_MPMaster>();
			return m_Instance;
		}
	}

	public enum GamePhase
	{
		NotStarted,
		Playing,
		BetweenGames
	}
	public static GamePhase Phase = GamePhase.NotStarted;

	protected static Dictionary<Transform, int> m_ViewIDsByTransform = new Dictionary<Transform, int>();
	protected static Dictionary<int, Transform> m_TransformsByViewID = new Dictionary<int, Transform>();


	/// <summary>
	/// 
	/// </summary>
	protected virtual void Awake()
	{

		DeactivateOtherMasters();

	}

	/// <summary>
	/// 
	/// </summary>
	protected virtual void OnEnable()
	{

#if UNITY_5_4_OR_NEWER
		SceneManager.sceneLoaded += OnLevelLoad;
#endif

		// register empty damage and kill delegates to prevent a crash
		// in case the scene has no 'vp_MPDamageCallbacks' component
		//vp_GlobalEvent<Transform, Transform, float>.Register("TransmitDamage", delegate(Transform t1, Transform t2, float f) { });		// sent by vp_DamageHandler
		//vp_GlobalEvent<Transform>.Register("TransmitKill", delegate(Transform t) { });													// sent by vp_PlayerDamageHandler
		//+ NetworkRespawn

		vp_GlobalEvent<object[]>.Register("TransmitDropItem", TransmitDropItem);

	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void OnDisable()
	{

#if UNITY_5_4_OR_NEWER
		SceneManager.sceneLoaded -= OnLevelLoad;
#endif

		// unregister empty damage and kill delegates to prevent a crash
		// in case the scene has no 'vp_MPDamageCallbacks' component
		//vp_GlobalEvent<Transform, Transform, float>.Unregister("TransmitDamage", delegate(Transform t1, Transform t2, float f) { });
		//vp_GlobalEvent<Transform>.Unregister("TransmitKill", delegate(Transform t) { });
		//+ NetworkRespawn

	}
	

	/// <summary>
	/// 
	/// </summary>
	protected virtual void Update()
	{

		// set game over
		if ((Phase == GamePhase.Playing) && (vp_MPClock.Running == false))
			StopGame();

		// SNIPPET: here is an example of loading a level on the master. all clients
		// will automatically load the new level and the game mode will reset
		//if (Input.GetKeyUp(KeyCode.L))
		//	TransmitLoadLevel(CurrentLevel);	// 'CurrentLevel' can be changed to the name of any level that has been added (!) to the Build Settings

		// start new game
		if ((Phase == GamePhase.BetweenGames) && (vp_MPClock.Running == false))
		{
			ResetGame();
			StartGame();
		}

		if (m_TookOverGame && (PhotonNetwork.connectionStateDetailed != ClientState.Joined))
			m_TookOverGame = false;

	}
	

	/// <summary>
	/// respawns all players, unfreezes the local player, restarts
	/// the game clock and broadcasts the game state
	/// </summary>
	public void StartGame()
	{

		if (!PhotonNetwork.isMasterClient)
			return;

		vp_MPNetworkPlayer.TransmitRespawnAll();

		vp_MPNetworkPlayer.TransmitUnFreezeAll();

		Phase = GamePhase.Playing;
		vp_MPClock.Set(GameLength);

		TransmitGameState();

		//Debug.Log("StartGame @ " + vp_MPClock.LocalTime + " with end time: " + (vp_MPClock.LocalTime + vp_MPClock.Duration) + ". time left is: " + vp_MPClock.TimeLeft);

	}


	/// <summary>
	/// freezes the local player, pauses game time and broadcasts
	/// the game state
	/// </summary>
	public void StopGame()
	{

		if (!PhotonNetwork.isMasterClient)
			return;

		photonView.RPC("ReceiveFreeze", PhotonTargets.All);

		Phase = GamePhase.BetweenGames;
		vp_MPClock.Set(PauseLength);

		TransmitGameState();

	}

	
	/// <summary>
	/// restores health, shots, inventory and death on all players, and
	/// cleans up the scene for the next round
	/// </summary>
	public void ResetGame()
	{

		vp_MPPlayerStats.FullResetAll();

		photonView.RPC("ReceiveSceneCleanup", PhotonTargets.All);

	}


	/// <summary>
	/// caches and returns the photonview id of the given transform.
	/// ids are stored in a dictionary that resets on level load
	/// </summary>
	public static int GetViewIDOfTransform(Transform t)
	{

		if (t == null)
			return 0;

		int id = 0;

		if (!m_ViewIDsByTransform.TryGetValue(t, out id))
		{
			PhotonView p = t.GetComponent<PhotonView>();
			if (p != null)
				id = p.viewID;
			m_ViewIDsByTransform.Add(t, id);	// add (even if '0' to prevent searching again)
		}

		return id;

	}


	/// <summary>
	/// caches and returns the transform of the given photonview id.
	/// transforms are stored in a dictionary that resets on level load
	/// </summary>
	public static Transform GetTransformOfViewID(int id)
	{

		Transform t = null;

		if (!m_TransformsByViewID.TryGetValue(id, out t))
		{
			foreach (PhotonView p in FindObjectsOfType<PhotonView>())
			{
				if (p.viewID == id)
				{
					t = p.transform;
					m_TransformsByViewID.Add(id, p.transform);
					return p.transform;
				}
			}
			m_TransformsByViewID.Add(id, t);	// add (even if not found, to avoid searching again)
		}

		return t;

	}


	/// <summary>
	/// pushes the master client's version of the game state and all
	/// player stats onto another client. if 'player' is null, the game
	/// state will be pushed onto _all_ other clients. 'gameState' can
	/// optionally be provided for cases where only a partial game state
	/// (a few stats on a few players) needs to be sent. by default the
	/// method will assemble and broadcast all stats of all players.
	/// </summary>
	public void TransmitGameState(PhotonPlayer targetPlayer, ExitGames.Client.Photon.Hashtable gameState = null)
	{
		
		if (!PhotonNetwork.isMasterClient)
			return;

		//DumpGameState(gameState);

		// if no (partial) gamestate has been provided, assemble and
		// broadcast the entire gamestate
		if (gameState == null)
			gameState = AssembleGameState();

		//DumpGameState(gameState);

		if (targetPlayer == null)
		{
			//Debug.Log("sending to all" + Time.time);
			photonView.RPC("ReceiveGameState", PhotonTargets.Others, (ExitGames.Client.Photon.Hashtable)gameState);
		}
		else
		{
			//Debug.Log("sending to " + targetPlayer + "" + Time.time);
			photonView.RPC("ReceiveGameState", targetPlayer, (ExitGames.Client.Photon.Hashtable)gameState);
		}

		if (vp_MPTeamManager.Exists)
			vp_MPTeamManager.Instance.RefreshTeams();

	}


	/// <summary>
	/// pushes the master client's version of the game state and all
	/// player stats onto all other clients. 'gameState' can optionally
	/// be provided for cases where only a partial game state (a few
	/// stats on a few players) needs to be sent. by default the method
	/// will assemble and broadcast all stats of all players.
	/// </summary>
	public void TransmitGameState(ExitGames.Client.Photon.Hashtable gameState = null)
	{
		TransmitGameState(null, gameState);
	}


	/// <summary>
	/// broadcasts a game state consisting of a certain array of
	/// stats extracted from the specified players. parameters
	/// 2 and up should be strings identifying the included stats.
	/// the returned gamestate will report the same list of stat
	/// names for all players
	/// </summary>
	public void TransmitPlayerState(int[] playerIDs, params string[] stats)
	{

		if (!PhotonNetwork.isMasterClient)
			return;

		ExitGames.Client.Photon.Hashtable playerState = AssembleGameStatePartial(playerIDs, stats);
		if (playerState == null)
		{
			Debug.LogError("Error: (" + this + ") Failed to assemble partial gamestate.");
			return;
		}

		photonView.RPC("ReceivePlayerState", PhotonTargets.Others, (ExitGames.Client.Photon.Hashtable)playerState);

	}


	/// <summary>
	/// broadcasts a game state consisting of an individual array
	/// of stats extracted from each specified player. parameters
	/// 2 and up should be arrays of strings identifying the stats
	/// included for each respective player. the returned gamestate
	/// may include unique stat names for each player
	/// </summary>
	public void TransmitPlayerState(int[] playerIDs, params string[][] stats)
	{

		if (!PhotonNetwork.isMasterClient)
			return;

		ExitGames.Client.Photon.Hashtable state = AssembleGameStatePartial(playerIDs, stats);
		if (state == null)
		{
			Debug.LogError("Error: (" + this + ") Failed to assemble partial gamestate.");
			return;
		}

		photonView.RPC("ReceivePlayerState", PhotonTargets.Others, (ExitGames.Client.Photon.Hashtable)state);

	}


	/// <summary>
	/// the game state can only be pushed out by the master client.
	/// however it is stored and kept in sync across clients in the
	/// form of the properties on the actual network players.
	/// in case a client becomes master it pushes out a new game
	/// state based on the network players in its own scene
	/// </summary>
	protected static ExitGames.Client.Photon.Hashtable AssembleGameState()
	{

		// NOTE: don't add custom integer keys, since ints are used
		// for player identification. for example, adding a key '5'
		// might result in a crash when player 5 tries to join.
		// adding string (or other type) keys should be fine

		if (!PhotonNetwork.isMasterClient)
			return null;

		vp_MPPlayerStats.EraseStats();	// NOTE: sending an RPC with a re-used gamestate will crash! we must create new gamestates every time

		vp_MPNetworkPlayer.RefreshPlayers();

		ExitGames.Client.Photon.Hashtable state = new ExitGames.Client.Photon.Hashtable();

		// -------- add game phase, game time and duration --------

		state.Add("Phase", Phase);
		state.Add("TimeLeft", vp_MPClock.TimeLeft);
		state.Add("Duration", vp_MPClock.Duration);

		// -------- add the stats of all players (includes health) --------

		foreach (vp_MPNetworkPlayer player in vp_MPNetworkPlayer.Players.Values)
		{
			if (player == null)
				continue;
			// add a player stats hashtable with the key 'player.ID'
			ExitGames.Client.Photon.Hashtable stats = player.Stats.All;
			if (stats != null)
				state.Add(player.ID, stats);
		}

		// -------- add the health of all non-player damagehandlers --------

		foreach (vp_DamageHandler d in vp_DamageHandler.Instances.Values)
		{
			if (d is vp_PlayerDamageHandler)
				continue;
			if (d == null)
				continue;
			PhotonView p = d.GetComponent<PhotonView>();
			if (p == null)
				continue;
			// add the view id for a damagehandler photon view, along with its health.
			// NOTE: we send and unpack the view id negative since some will potentially
			// be the same as existing player id:s in the hashtable (starting at 1)
			state.Add(-p.viewID, (float)d.CurrentHealth);	// NOTE: cast to float required for Anti-Cheat Toolkit support

		}

		// -------- add note of any disabled pickups --------

		foreach (int id in vp_MPPickupManager.Instance.Pickups.Keys)
		{

			List<vp_ItemPickup> p;
			vp_MPPickupManager.Instance.Pickups.TryGetValue(id, out p);
			if ((p == null) || (p.Count < 1) || p[0] == null)
				continue;

			if (vp_Utility.IsActive(p[0].transform.gameObject))
				continue;

			// there are two predicted cases were an ID might already be in the state:
			// 1) a player ID is the same as a pickup ID. this is highly unlikely since
			//		player IDs start at 1 and pickup IDs are ~six figure numbers. also,
			//		only currently disabled pickups are included in the state making it
			//		even more unlikely
			// 2: a pickup has two vp_ItemPickup components with the same ID which is
			//		the case with throwing weapons (grenades). this is highly likely,
			//		but in this case it's fine to ignore the ID second time around
			if (!state.ContainsKey(id))
				state.Add(id, false);

		}

		if (state.Count == 0)
			UnityEngine.Debug.LogError("Failed to get gamestate.");

		return state;

	}


	/// <summary>
	/// assembles a game state consisting of a certain array of
	/// stats extracted from the specified players. parameters
	/// 2 and up should be strings identifying the included stats.
	/// the returned gamestate will report the same array of stat
	/// names for all players
	/// </summary>
	protected virtual ExitGames.Client.Photon.Hashtable AssembleGameStatePartial(int[] playerIDs, params string[] stats)
	{

		ExitGames.Client.Photon.Hashtable state = new ExitGames.Client.Photon.Hashtable();

		for (int v = 0; v < playerIDs.Length; v++)
		{
			if (state.ContainsKey(playerIDs[v]))	// safety measure in case int array has duplicate id:s
			{
				Debug.LogWarning("Warning (" + this + ") Trying to add same player twice to a partial game state (not good). Duplicates will be ignored.");
				continue;
			}
			state.Add(playerIDs[v], ExtractPlayerStats(vp_MPNetworkPlayer.Get(playerIDs[v]), stats));
		}

		return state;

	}


	/// <summary>
	/// assembles a game state consisting of an individual array
	/// of stats extracted from each specified player. parameters
	/// 2 and up should be arrays of strings identifying the stats
	/// included for each respective player. the returned gamestate
	/// may include unique stat names for every player
	/// </summary>
	protected virtual ExitGames.Client.Photon.Hashtable AssembleGameStatePartial(int[] playerIDs, params string[][] stats)
	{

		ExitGames.Client.Photon.Hashtable state = new ExitGames.Client.Photon.Hashtable();
		
		for (int v = 0; v < playerIDs.Length; v++)
		{
			if (state.ContainsKey(playerIDs[v]))	// safety measure in case int array has duplicate id:s
			{
				Debug.LogWarning("Warning (" + this + ") Trying to add same player twice to a partial game state (not good). Duplicates will be ignored.");
				continue;
			}
			state.Add(playerIDs[v], ExtractPlayerStats(vp_MPNetworkPlayer.Get(playerIDs[v]), stats[v]));

		}

		return state;

	}


	/// <summary>
	/// creates a hashtable with only a few of the stats of a
	/// certain player
	/// </summary>
	protected virtual ExitGames.Client.Photon.Hashtable ExtractPlayerStats(vp_MPNetworkPlayer player, params string[] stats)
	{

		if (!PhotonNetwork.isMasterClient)
			return null;

		// create a player hashtable with only the given stats
		ExitGames.Client.Photon.Hashtable table = new ExitGames.Client.Photon.Hashtable();
		//string str = "Extracting stats for player: " + vp_MPNetworkPlayer.GetName(player.ID);
		foreach (string s in stats)
		{
			object o = player.Stats.Get(s);
			if (o == null)
			{
				Debug.LogError("Error: (" + this + ") Player stat '" + s + "' could not be retrieved from player " + player.ID + ".");
				continue;
			}
			table.Add(s, o);
			//str += ": " + s + "(" + o + ")";
		}

		//Debug.Log(str);

		return table;

	}


	/// <summary>
	/// broadcasts a command to load a new level on all clients (master
	/// included) then resets the game which will prompt all player stats
	/// to be reset and players to respawn on new spawnpoints.
	/// NOTES: 1) a scene being loaded must not have a vp_MPConnection
	/// in it. the connection component should be loaded in a startup
	/// scene with 'DontDestroyOnLoad' set to true. 2) the game clock will
	/// be reset as a direct result of loading a new level
	/// </summary>
	public void TransmitLoadLevel(string levelName)
	{

		if (!PhotonNetwork.isMasterClient)
			return;

		vp_MPMaster.Phase = vp_MPMaster.GamePhase.NotStarted;

		photonView.RPC("ReceiveLoadLevel", PhotonTargets.All, levelName);
		
	}


	/// <summary>
	/// sends a command to load a new level to a specific client. this is
	/// typically sent to players who join an existing game. if the joining
	/// player is the master, the game state will start up for the first time
	/// </summary>
	public void TransmitLoadLevel(PhotonPlayer targetPlayer, string levelName)
	{

		if (!PhotonNetwork.isMasterClient)
			return;

		if (string.IsNullOrEmpty(levelName))
		{
			Debug.LogError("Error (" + this + ") TransmitLoadlevel -> Level name was null or empty. Remember to set a default level name on the master component.");
			return;
		}

		photonView.RPC("ReceiveLoadLevel", targetPlayer, levelName);
		
	}


	/// <summary>
	/// loads a new level on this machine as commanded by the master. the
	/// game mode will typically be reset by the master when this happens,
	/// prompting a player stat reset and respawn
	/// </summary>
	[PunRPC]
	public void ReceiveLoadLevel(string levelName, PhotonMessageInfo info)
	{

		if (info.sender != PhotonNetwork.masterClient)
			return;

		PhotonNetwork.LoadLevel(levelName);
		if(!PhotonNetwork.networkingPeer.loadingLevelAndPausedNetwork)
		{
			Debug.LogError("Error (" + this + ") Failed to load level: '" + levelName + "'. You may need to 1) add it to the Build Settings, or 2) verify the default level name on the master component.");
			return;
		}

		CurrentLevel = levelName;

		if (PhotonNetwork.isMasterClient)
		{
			ResetGame();
			StartGame();
		}

	}

	
	/// <summary>
	/// every client sends this RPC from 'vp_MPConnection -> 'OnJoinedRoom'.
	/// its purpose is to allocate a team and player type to the joinee, use
	/// this info to figure out a matching spawn point, and respond with a
	/// 'permission to spawn' at a certain position, with a certain team and
	/// a suitable player prefab
	/// </summary>
	[PunRPC]
	public void RequestInitialSpawnInfo(PhotonPlayer player, int id, string name)
	{

		if (!PhotonNetwork.isMasterClient)
			return;

		// make every joining player load the current level
		TransmitLoadLevel(player, CurrentLevel);

		if (PhotonNetwork.room.PlayerCount > 1)
		{
			// for non-masters we send spawn info straight away. this assumes
			// the master has already loaded the level and knows the spawnpoints
			TransmitInitialSpawnInfo(player, id, name);
		}
		else
		{
			// if we're the FIRST player in the game we take over the game, but
			// we must wait sending spawn info to ourself until 'OnLevelLoad'
			// triggers (because there are no spawnpoints yet)
			m_TookOverGame = true;
		}

	}


	/// <summary>
	/// 
	/// </summary>
	public void TransmitInitialSpawnInfo(PhotonPlayer player, int id, string name)
	{
		
		// allocate team number, player type and spawnpoint
		int teamNumber = 0;
		string playerTypeName = null;
		vp_Placement placement = null;

		if (vp_MPTeamManager.Exists)
		{
			teamNumber = ((vp_MPTeamManager.Instance.Teams.Count <= 1) ? 0 : vp_MPTeamManager.Instance.GetSmallestTeam());
			playerTypeName = vp_MPTeamManager.Instance.GetTeamPlayerTypeName(teamNumber);
			placement = vp_MPPlayerSpawner.GetRandomPlacement(vp_MPTeamManager.GetTeamName(teamNumber));
		}

		if (placement == null)
			placement = vp_MPPlayerSpawner.GetRandomPlacement();

		if (string.IsNullOrEmpty(playerTypeName))
		{
			vp_MPPlayerType playerType = vp_MPPlayerSpawner.GetDefaultPlayerType();
			if (playerType != null)
				playerTypeName = playerType.name;
			else
				Debug.LogError("Error (" + this + ") Failed to assign PlayerType to player " + id.ToString() + ". Make sure player types are assigned in your vp_MPPlayerSpawner and vp_MPMaster components.");

		}

		// spawn
		photonView.RPC("ReceiveInitialSpawnInfo", PhotonTargets.All, id, player, placement.Position, placement.Rotation, playerTypeName, teamNumber);

		// if JOINING player is the master, refresh the game clock since
		// there are no other players and the game needs to get started
		if (player.IsMasterClient)
			StartGame();

		// send the entire game state to the joining player
		// NOTE: we don't need to send the game state of the joinee to all
		// the other players since it has just spawned in the form of a
		// fresh, clean copy of the remote player prefab in question
		TransmitGameState(player);

	}


	/// <summary>
	/// RPC
	/// </summary>
	protected void TransmitDropItem(object[] obj)
	{

		if (!PhotonNetwork.isMasterClient)
			return;

		if (obj == null)
			return;

		if (obj.Length != 6)
			return;

		// item type name
		string itemTypeName = obj[0] as string;
		if ((itemTypeName == null) || string.IsNullOrEmpty(itemTypeName))
			return;

		// final drop position
		Vector3 targetPosition = (Vector3)obj[1];

		// final drop rotation
		float targetYaw = (float)obj[2];

		// player transform
		Transform playerTransform = obj[3] as Transform;
		if (playerTransform == null)
			return;

		// network player from transform
		vp_MPNetworkPlayer player = vp_MPNetworkPlayer.Get(playerTransform);
		if (player == null)
			return;

		// pickup id
		int pickupID = (int)obj[4];

		// ammo
		int unitAmount = (int)obj[5];

		player.photonView.RPC("ReceiveDropItem", PhotonTargets.Others, itemTypeName, targetPosition, targetYaw, player.ID, pickupID, unitAmount);

	}


	/// <summary>
	/// the master client sends this RPC to push its version of the game
	/// state onto all other clients. 'game state' can mean the current
	/// game time and phase + all stats of all players, or it can mean a
	/// partial game state, such as an updated score + frag count for a
	/// sole player. also, instantiates any missing player prefabs
	/// </summary>
	[PunRPC]
	protected virtual void ReceiveGameState(ExitGames.Client.Photon.Hashtable gameState, PhotonMessageInfo info)
	{

		//vp_MPDebug.Log("GOT FULL GAMESTATE!");

		//DumpGameState(gameState);

		if ((info.sender != PhotonNetwork.masterClient) ||
			(info.sender.IsLocal))
			return;

		//vp_MPDebug.Log("Gamestate updated @ " + info.timestamp);
		//Debug.Log("Gamestate updated @ " + info.timestamp);

		// -------- extract game phase, game time and duration --------

		// TODO: make generic method 'ExtractStat' that does this
		object phase;
		if ((gameState.TryGetValue("Phase", out phase) && (phase != null)))
			Phase = (GamePhase)phase;

		object timeLeft;
		object duration;
		if ((gameState.TryGetValue("TimeLeft", out timeLeft) && (timeLeft != null))
			&& (gameState.TryGetValue("Duration", out duration) && (duration != null)))
			vp_MPClock.Set((float)timeLeft, (float)duration);

		// -------- instantiate missing player prefabs --------

		vp_MPPlayerSpawner.Instance.InstantiateMissingPlayerPrefabs(gameState);
		
		// -------- refresh stats of all players --------

		ReceivePlayerState(gameState, info);

		// -------- refresh health of all non-player damage handlers --------

		foreach (vp_DamageHandler d in vp_DamageHandler.Instances.Values)
		{
			if (d == null)
				continue;
			if (d is vp_PlayerDamageHandler)
				continue;
			PhotonView p = d.GetComponent<PhotonView>();
			if (p == null)
				continue;
			object currentHealth;
			if (gameState.TryGetValue(-p.viewID, out currentHealth) && (currentHealth != null))
			{
				d.CurrentHealth = (float)currentHealth;
				if (d.CurrentHealth <= 0.0f)
					vp_Utility.Activate(d.gameObject, false);
			}
			else
				vp_MPDebug.Log("Failed to extract health of damage handler " + p.viewID + " from gamestate");
		}

		// -------- disable any pickups noted as currently disabled in the state --------

		//vp_MPDebug.Log("DISABLED PICKUPS: " + vp_MPPickupManager.Instance.Pickups.Keys.Count);

		foreach (int id in vp_MPPickupManager.Instance.Pickups.Keys)
		{

			List<vp_ItemPickup> p;
			vp_MPPickupManager.Instance.Pickups.TryGetValue(id, out p);
			if ((p == null) || (p.Count < 1) || p[0] == null)
				continue;

			object isDisabled;
			if (gameState.TryGetValue(id, out isDisabled) && (isDisabled != null))
				vp_Utility.Activate(p[0].transform.gameObject, false);

		}
		
		// -------- refresh all teams --------

		if(vp_MPTeamManager.Exists)
			vp_MPTeamManager.Instance.RefreshTeams();

	}


	/// <summary>
	/// 
	/// </summary>
	[PunRPC]
	protected virtual void ReceivePlayerState(ExitGames.Client.Photon.Hashtable gameState, PhotonMessageInfo info)
	{

		//Debug.Log("GOT PLAYER STATE!");

		if ((info.sender != PhotonNetwork.masterClient) ||
			(info.sender.IsLocal))
			return;

		// -------- refresh stats of all included players --------

		foreach (vp_MPNetworkPlayer player in vp_MPNetworkPlayer.Players.Values)
		{

			if (player == null)
				continue;

			object stats;
			if (gameState.TryGetValue(player.ID, out stats) && (stats != null))
				player.Stats.SetFromHashtable((ExitGames.Client.Photon.Hashtable)stats);
			//else
			//    vp_MPDebug.Log("Failed to extract player " + player.ID + " stats from gamestate");

		}

		// -------- refresh all teams --------

		if(vp_MPTeamManager.Exists)
			vp_MPTeamManager.Instance.RefreshTeams();

	}


	/// <summary>
	/// disarms, stops and locks the local player so that it
	/// cannot move. used when starting non-gameplay game phases,
	/// such as between deathmatch games
	/// </summary>
	[PunRPC]
	protected virtual void ReceiveFreeze(PhotonMessageInfo info)
	{

		if (!info.sender.IsMasterClient)
			return;

		//vp_MPDebug.Log("Time's up! Restarting game ...");

		vp_MPLocalPlayer.Freeze();

	}


	/// <summary>
	/// allows local player to move again and tries to wield the
	/// first weapon. used when ending non-gameplay game phases,
	/// such as when starting a new deathmatch game
	/// </summary>
	[PunRPC]
	protected virtual void ReceiveUnFreeze(PhotonMessageInfo info)
	{

		if (!info.sender.IsMasterClient)
			return;

		vp_MPLocalPlayer.UnFreeze();

	}


	/// <summary>
	/// removes all dropped pickups and all debris resulting from
	/// the fighting. intended for use when a new round starts
	/// </summary>
	[PunRPC]
	protected virtual void ReceiveSceneCleanup(PhotonMessageInfo info)
	{

		// despawn all dropped pickups
		vp_Toss[] allTossedObjects = Component.FindObjectsOfType<vp_Toss>();
		for (int v = (allTossedObjects.Length - 1); v > -1; v--)
		{
			if (allTossedObjects[v] == null)
				continue;
			vp_Utility.Destroy(allTossedObjects[v].transform.gameObject);
		}

		// despawn all debris such as decals and explosion rubble
		// NOTE: this must iterate all objects in the scene (potentially slow in very complex scenes)
		GameObject[] allGameObjects = FindObjectsOfType<GameObject>();
		for (int v = (allGameObjects.Length - 1); v > -1; v--)
		{
			if (allGameObjects[v].layer == vp_Layer.Debris)
				vp_Utility.Destroy(allGameObjects[v]);
		}

	}


	/// <summary>
	/// dumps a game state hashtable to the console
	/// </summary>
	public static void DumpGameState(ExitGames.Client.Photon.Hashtable gameState)
	{

		string s = "--- GAME STATE ---\n(click to view)\n\n";

		if (gameState == null)
		{
			Debug.Log("DumpGameState: Passed gamestate was null: assembling full gamestate.");
			gameState = AssembleGameState();
		}

		foreach (object key in gameState.Keys)
		{
			if (key.GetType() == typeof(int))
			{
				object player;
				gameState.TryGetValue(key, out player);
				s += vp_MPNetworkPlayer.GetName((int)key) + ":\n";

				foreach (object o in ((ExitGames.Client.Photon.Hashtable)player).Keys)
				{
					s += "\t\t" + (o.ToString()) + ": ";
					object val;
					((ExitGames.Client.Photon.Hashtable)player).TryGetValue(o, out val);
					s += val.ToString().Replace("(System.String)", "") + "\n";
				}
			}
			else
			{
				object val;
				gameState.TryGetValue(key, out val);
				s += key.ToString() + ": " + val.ToString();
			}
			s += "\n";
		}

		UnityEngine.Debug.Log(s);

	}


	/// <summary>
	/// detects and responds to a master client handover
	/// </summary>
	protected virtual void OnPhotonPlayerDisconnected(PhotonPlayer player)
	{

		// refresh master control of every vp_MPRigidbody in the scene (done here
		// rather than in the objects themselves because we need to iterate any
		// deactivated / disabled ones too)
		vp_MPRigidbody.RefreshMasterControlAll();

		if (!PhotonNetwork.isMasterClient)
			return;

		// if this machine becomes master, broadcast our gamestate!
		if (!m_TookOverGame)
		{

			// reinitialize every respawner in this scene or anything not currently
			// alive will fail to respawn (respawn timer being local to previous master)
			vp_Respawner.ResetAll(true);	// give a new respawn time to anything waiting to respawn

			// force our gamestate onto everyone else
			TransmitGameState();

			// remember that we have taken over the game. this can only happen once
			m_TookOverGame = true;

		}

	}



	/// <summary>
	/// when a new level is loaded, clears any cached scene objects.
	/// also transmits initial spawn info to the first ever player
	/// </summary>
#if UNITY_5_4_OR_NEWER
	protected void OnLevelLoad(Scene scene, LoadSceneMode mode)
#else
	protected void OnLevelWasLoaded()
#endif
	{

		// abort this method if it runs too early which may happen in
		// a standalone build

		if (vp_Gameplay.CurrentLevelName != CurrentLevel)
			return;

		// clear any cached objects from the previous scene
		m_ViewIDsByTransform.Clear();
		m_TransformsByViewID.Clear();
		vp_MPRigidbody.Instances.Clear();
		vp_MPNetworkPlayer.Players.Clear();
		vp_MPNetworkPlayer.PlayersByID.Clear();

		if (PhotonNetwork.connected == false)
			return;

		// the first player in the game must send itself spawn info here.
		// this can't be done in 'RequestInitialSpawnInfo' because the
		// spawnpoints are unknown until 'OnLevelLoad'
		if ((!vp_MPNetworkPlayer.PlayersByID.ContainsKey(PhotonNetwork.player.ID)) && (PhotonNetwork.room.PlayerCount == 1))
			TransmitInitialSpawnInfo(PhotonNetwork.player, PhotonNetwork.player.ID, PhotonNetwork.player.NickName);

	}


	/// <summary>
	/// makes sure this game state is the only one operating on this
	/// scene. for when using multiple game mode master prefabs in a
	/// scene and potentially forgetting to having just one enabled
	/// </summary>
	protected virtual void DeactivateOtherMasters()
	{

		if (!enabled)
			return;

		vp_MPMaster[] masters = Component.FindObjectsOfType<vp_MPMaster>() as vp_MPMaster[];
		foreach (vp_MPMaster g in masters)
		{
			if (g.gameObject != gameObject)
				vp_Utility.Activate(g.gameObject, false);	// there can be only one!
		}

	}


	/// <summary>
	/// dumps all network players to the console
	/// </summary>
	protected virtual void DebugDumpPlayers()
	{

		string debugMsg = "Players (excluding self): ";

		for (int p = 0; p < PhotonNetwork.playerList.Length; p++)
		{
			if (PhotonNetwork.playerList[p].ID == PhotonNetwork.player.ID)
				continue;

			PhotonView[] views = Component.FindObjectsOfType(typeof(PhotonView)) as PhotonView[];
			bool hasView = false;
			foreach (PhotonView f in views)
			{

				if (f.ownerId == PhotonNetwork.playerList[p].ID)
					hasView = true;

			}

			debugMsg += PhotonNetwork.playerList[p].ID.ToString() + (hasView ? " (has view)" : " (has no view)") + ", ";

		}

		if (debugMsg.Contains(", "))
			debugMsg = debugMsg.Remove(debugMsg.LastIndexOf(", "));

		vp_MPDebug.Log(debugMsg);

	}


}
