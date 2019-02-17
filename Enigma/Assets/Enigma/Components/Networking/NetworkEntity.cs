using System.Net.Sockets;
using Enigma.Components.Networking;
using UnityEngine;

public class NetworkEntity : MonoBehaviour
{

    private ServerInfo _serverInfo;
    private SharedTcpClient
    private TcpClient _tcpClient;

	void Start () {
		_serverInfo = ServerInfo.CurrentServerInfo;
	    _tcpClient = new TcpClient();
        _tcpClient.c
	}
	
	
}
