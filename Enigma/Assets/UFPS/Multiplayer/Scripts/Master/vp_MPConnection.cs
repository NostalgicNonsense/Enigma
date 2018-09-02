/////////////////////////////////////////////////////////////////////////////////
//
//	vp_MPConnection.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	initiates and manages the connection to Photon Cloud, regulates
//					room creation, max player count per room and logon timeout.
//					also keeps the 'IsMultiplayer' and 'IsMaster' flags up-to-date.
//					(these are quite often relied upon by Base UFPS classes)
//
/////////////////////////////////////////////////////////////////////////////////

// for Anti-Cheat Toolkit support (see the manual for more info)
#if ANTICHEAT
using CodeStage.AntiCheat.ObscuredTypes;
#endif

using UnityEngine;
using UnityEngine.SceneManagement;
using Hashtable = ExitGames.Client.Photon.Hashtable;


public class vp_MPConnection : Photon.MonoBehaviour
{

	// connection state
	public int MaxPlayersPerRoom = 16;              // if all available rooms have exactly this many players, the next player who joins will automatically create a new room

#if !ANTICHEAT
	public float LogOnTimeOut = 5.0f;               // if a stage in the initial connection process stalls for more than this many seconds, the connection will be restarted
	public int MaxConnectionAttempts = 10;          // after this many connection attempts, the script will abort and return to main menu
	protected int m_ConnectionAttempts = 0;
#else
	public ObscuredFloat LogOnTimeOut = 5.0f;
	public ObscuredInt MaxConnectionAttempts = 10;
	protected ObscuredInt m_ConnectionAttempts = 0;
#endif

	public string SceneToLoadOnDisconnect = "";     // this scene will be loaded when the 'Disconnect' method is executed

	public static bool StayConnected = false;       // as long as this is true, this component will relentlessly try to reconnect to the photon cloud
	protected ClientState m_LastClientState = ClientState.Uninitialized;
	protected vp_Timer.Handle m_ConnectionTimer = new vp_Timer.Handle();

	// ping
	public float PingReportInterval = 10.0f;
	protected int m_LastPing = 0;
	protected float m_NextAllowedPingTime = 0.0f;

	// instance
	public new bool DontDestroyOnLoad = true;
	public static vp_MPConnection Instance = null;



	/// <summary>
	/// 
	/// </summary>
	protected virtual void OnEnable()
	{

		Instance = this;
		vp_Gameplay.IsMultiplayer = true;

	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void OnDisable()
	{

		Instance = null;
		vp_Gameplay.IsMultiplayer = false;

	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void Start()
	{

		if(StayConnected)
			Connect();

		if (DontDestroyOnLoad)
			Object.DontDestroyOnLoad(transform.root.gameObject);

	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void Update()
	{

		UpdateConnectionState();

		UpdatePing();

		// SNIPPET: uncomment to test disconnect
		//if (Input.GetKeyUp(KeyCode.K))
		//	Disconnect();

	}


	/// <summary>
	///	detects cases where the connection process has stalled,
	///	disconnects and tries to connect again
	/// </summary>
	protected virtual void UpdateConnectionState()
	{

		if (!StayConnected)
			return;

		if (PhotonNetwork.connectionStateDetailed != m_LastClientState)
		{
			string s = PhotonNetwork.connectionStateDetailed.ToString();
			s = ((PhotonNetwork.connectionStateDetailed == ClientState.Joined) ? "--- " + s + " ---" : s);
			if (s == "PeerCreated")
				s = "Connecting to the " + PhotonNetwork.PhotonServerSettings.PreferredRegion.ToString().ToUpper() + " cloud ...";
			vp_MPDebug.Log(s);
		}

		if (PhotonNetwork.connectionStateDetailed == ClientState.Joined)
		{
			if (m_ConnectionTimer.Active)
			{
				m_ConnectionTimer.Cancel();
				m_ConnectionAttempts = 0;
			}
		}
		else if ((PhotonNetwork.connectionStateDetailed != m_LastClientState) && !m_ConnectionTimer.Active)
		{
			Reconnect();
			vp_Timer.In(LogOnTimeOut, delegate ()
			{
				m_ConnectionAttempts++;
				if (m_ConnectionAttempts < MaxConnectionAttempts)
				{
					vp_MPDebug.Log("Retrying (" + m_ConnectionAttempts + ") ...");
					Reconnect();
				}
				else
				{
					vp_MPDebug.Log("Failed to connect (tried " + m_ConnectionAttempts + " times).");
					Disconnect();
				}
			}, m_ConnectionTimer);
		}

		m_LastClientState = PhotonNetwork.connectionStateDetailed;

	}


	/// <summary>
	/// reports ping every 10 (default) seconds by storing it as a custom player
	/// prefs value in the Photon Cloud. 'Ping' is defined as the roundtrip time to
	/// the Photon server and it is only reported if it has changed
	/// </summary>
	public virtual void UpdatePing()
	{

		// only report ping every 10 (default) seconds
		if (Time.time < m_NextAllowedPingTime)
			return;
		m_NextAllowedPingTime = Time.time + PingReportInterval;

		// get the roundtrip time to the photon server
		int ping = PhotonNetwork.GetPing();

		// only report ping if it changed since last time
		if (ping == m_LastPing)
			return;
		m_LastPing = ping;

		// send the ping as a custom player property (the first time it will be
		// created, from then on it will be updated)
		Hashtable playerCustomProps = new Hashtable();
		playerCustomProps["Ping"] = ping;
		PhotonNetwork.player.SetCustomProperties(playerCustomProps);

	}


	/// <summary>
	/// this method smooths over a harmless error case where 'TryCreateRoom' fails
	/// because someone was creating the same room name at the exact same time as us,
	/// and 'TryCreateRoom' failed to sort it out. instead of pausing the editor and
	/// showing a scary crash dialog, we should keep calm, carry on and reconnect
	/// </summary>
	void OnPhotonCreateRoomFailed()
	{

		// unpause editor (if paused)
#if UNITY_EDITOR
		if (UnityEditor.EditorApplication.isPaused)
			UnityEditor.EditorApplication.isPaused = false;
#endif

		// close crash popup (if any)
		vp_CrashPopup crashPopup = FindObjectOfType<vp_CrashPopup>();
		if (crashPopup != null)
			crashPopup.Reset();

	}


	/// <summary>
	/// creates a new room numbered 'current room count + 1', or joins
	/// that room if someone else has just created it
	/// </summary>
	protected virtual void TryCreateRoom()
	{

		//vp_MPDebug.Log("trying to create room: " + "Room" + (PhotonNetwork.countOfRooms + 1).ToString());

		string roomName = "Room" + (PhotonNetwork.countOfRooms + 1).ToString();

		// if someone else is creating the wanted room right now, join it instead of creating it
		foreach (RoomInfo room in PhotonNetwork.GetRoomList())
		{
			if (room.Name == roomName)
			{
				//vp_MPDebug.Log("someone else was creating " + "Room" + (PhotonNetwork.countOfRooms + 1).ToString() + " so joining it");
				PhotonNetwork.JoinRoom(room.Name);
				return;
			}
		}

		// noone else is creating the wanted room, so create it!
		if (PhotonNetwork.CreateRoom(roomName))
		{
			//vp_MPDebug.Log("create room success");
		}

	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual bool TryJoinRoom()
	{
		//vp_MPDebug.Log("trying to join room: " + "Room" + (PhotonNetwork.countOfRooms).ToString());
		return PhotonNetwork.JoinRoom("Room" + (PhotonNetwork.countOfRooms).ToString());
	}



	/// <summary>
	/// 
	/// </summary>
	protected virtual void Connect()
	{

		PhotonNetwork.ConnectUsingSettings(vp_Gameplay.Version);
		m_LastClientState = ClientState.Uninitialized;

	}


	/// <summary>
	/// used internally to disconnect and immediately reconnect
	/// </summary>
	protected virtual void Reconnect()
	{

		if (PhotonNetwork.connectionStateDetailed != ClientState.Disconnected
			&& PhotonNetwork.connectionStateDetailed != ClientState.PeerCreated)
		{
			PhotonNetwork.Disconnect();
		}

		Connect();

		m_LastClientState = ClientState.Uninitialized;

	}


	/// <summary>
	/// disconnects the player from an ongoing game, loads a blank level
	/// (if provided) and sends a globalevent informing external objects
	/// of the disconnect. TIP: call this method from anywhere using:
	/// vp_MPConnection.Instance.Disconnect();
	/// </summary>
	public virtual void Disconnect()
	{

		if (PhotonNetwork.connectionStateDetailed == ClientState.Disconnected)
			return;

		// explicitly destroy all player objects (these usually survive a level load)
		vp_MPNetworkPlayer[] players = FindObjectsOfType<vp_MPNetworkPlayer>();
		foreach (vp_MPNetworkPlayer p in players)
		{
			vp_Utility.Destroy(p.transform.root.gameObject);
		}

		// disable UFPSMP auto-reconnection and disconnect from Photon
		vp_MPConnection.StayConnected = false;
		PhotonNetwork.Disconnect();
		m_ConnectionAttempts = 0;
		m_LastClientState = ClientState.Disconnected;

		// load a blank scene (if provided) to destroy the currently played level.
		// NOTE: in the UFPSMP demos, by default some master gameplay objects (such
		// as this component) will survive
		if (!string.IsNullOrEmpty(SceneToLoadOnDisconnect))
			SceneManager.LoadScene(SceneToLoadOnDisconnect);

		// send a message to inform external objects that we have disconnected
		// NOTE: in the UFPSMP demos, this will reset 'vp_MPDemoMainMenu'
		vp_GlobalEvent.Send("Disconnected");

		vp_MPDebug.Log("--- Disconnected ---");

	}
	

	/// <summary>
	/// 
	/// </summary>
	void OnPhotonRandomJoinFailed()
	{
		//PhotonNetwork.CreateRoom(null);
	}


	/// <summary>
	/// 
	/// </summary>
	void OnJoinedLobby()
	{

		// update name of this player in the cloud
		PhotonNetwork.player.NickName = vp_Gameplay.PlayerName;

		//vp_MPDebug.Log("Total players using app: " + PhotonNetwork.countOfPlayers);
		
	}


	/// <summary>
	/// 
	/// </summary>
	void OnReceivedRoomListUpdate()
	{

		if ((PhotonNetwork.countOfPlayersInRooms % MaxPlayersPerRoom) == 0)
			TryCreateRoom();
		else
			TryJoinRoom();

	}


	/// <summary>
	/// 
	/// </summary>
	void OnJoinedRoom()
	{
		
		if (PhotonNetwork.isMasterClient)
			PhotonNetwork.room.MaxPlayers = MaxPlayersPerRoom;

		// TODO: use PhotonNetwork.LoadLevel to load level while automatically pausing network queue
		// ("call this in OnJoinedRoom to make sure no cached RPCs are fired in the wrong scene")
		// also, get level from room properties / master

		// send spawn request to master client
		string name = "Unnamed";

		// sent as RPC instead of in 'OnPhotonPlayerConnected' because the
		// MasterClient does not run the latter for itself + we don't want
		// to do the request on all clients

		if(FindObjectOfType<vp_MPMaster>())	// in rare cases there might not be a vp_MPMaster, for example: a chat lobby
			photonView.RPC("RequestInitialSpawnInfo", PhotonTargets.MasterClient, PhotonNetwork.player, 0, name);

		vp_Gameplay.IsMaster = PhotonNetwork.isMasterClient;

	}




	/// <summary>
	/// updates the 'IsMaster' flag which gets read by Base UFPS classes.
	/// also, announces players leaving in the chat (if any)
	/// </summary>
	void OnPhotonPlayerDisconnected(PhotonPlayer player)
	{

		vp_Gameplay.IsMaster = PhotonNetwork.isMasterClient;

		vp_MPDebug.Log(player.NickName + " left the game");	// NOTE: the 'joined' message is posted by vp_MPPlayerSpawner which has extended team info

	}
		

}

