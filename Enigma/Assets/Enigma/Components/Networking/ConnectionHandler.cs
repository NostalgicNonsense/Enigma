using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace Enigma.Components.Networking
{
    public class ConnectionHandler
    {
        //todo: WE need exception handling
        public static ConnectionHandler ConnectionHandlerInstance;
        private ServerInfo _serverInfo;
        private TcpClient _tcpClient;
        private UdpClient _udpClient;

        static ConnectionHandler()
        {
            ConnectionHandlerInstance = new ConnectionHandler
            {
                _serverInfo = ServerInfo.CurrentServerInfo,
                _tcpClient = new TcpClient(ServerInfo.CurrentServerInfo.IpAddress.AddressFamily),
                _udpClient = new UdpClient(ServerInfo.CurrentServerInfo.IpAddress.AddressFamily),
            };
        }

        /// <summary>
        /// This method exists because opening connections in the constructor risks exceptions
        /// Which can kill our application
        /// </summary>
        private void InitalizeConnections()
        {
            _tcpClient.Connect(_serverInfo.IpAddress, _serverInfo.Port);
            _udpClient.Connect(_serverInfo.IpAddress, _serverInfo.Port);


            // start two new background threads.
            new Thread(ListenTcp).IsBackground = true;
            new Thread(ListenUdp).IsBackground = true;
        }

        private void ListenUdp()
        {

        }

        private void ListenTcp()
        {

        }

        public void SendUdpUpdate(object value)
        {
            if (_udpClient.Client.Connected)
            {
                InitalizeConnections();
            }

            Debug.Assert(value != null);
            var contentAsBytes = GetStringAtUtf8Bytes(JsonUtility.ToJson(value));
            _udpClient.Send(contentAsBytes, contentAsBytes.Length);
        }

        public void SendTcpUpdate(object value)
        {
            if (_udpClient.Client.Connected)
            {
                InitalizeConnections();
            }

            Debug.Assert(value != null);
            var contentAsBytes = GetStringAtUtf8Bytes(JsonUtility.ToJson(value));
            _tcpClient.Client.Send(contentAsBytes);
        }

        private static byte[] GetStringAtUtf8Bytes(string value)
        {
            return Encoding.UTF8.GetBytes(value);
        }
    
    }
}
