using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Assets.Enigma.Enigma.Core.Networking.Serialization;
using Assets.Enigma.Enigma.Core.Networking.Serialization.SerializationModel;
using Assets.Enigma.Enigma.Core.Utility;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Assets.Enigma.Enigma.Core.Networking
{
    public class ConnectionHandler
    {
        //todo: WE need exception handling
        public static readonly ConnectionHandler ConnectionHandlerInstance;
        private ISerializer _serializer;
        private ServerInfo _serverInfo;
        private TcpClient _tcpClient;
        private UdpClient _udpClient;
        private ConcurrentDictionary<Guid, NetworkEntity> _networkedEntitiesByGuid;
        private static int _tcpPortNumber = 5411; // refactor;
        private static int _udpPortNumber = 5412;

        static ConnectionHandler()
        {
            ConnectionHandlerInstance = new ConnectionHandler
            {
                _serverInfo = ServerInfo.CurrentServerInfo,
                _tcpClient = new TcpClient(ServerInfo.CurrentServerInfo.IpAddress.AddressFamily),
                _udpClient = new UdpClient(ServerInfo.CurrentServerInfo.IpAddress.AddressFamily),
                _networkedEntitiesByGuid = new ConcurrentDictionary<Guid, NetworkEntity>(),
                _serializer = new Serializer()
            };
        }

        public bool TryAddListener(NetworkEntity networkEntity)
        {
            return _networkedEntitiesByGuid.TryAdd(networkEntity.Guid, networkEntity);
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
            //TODO
        }

        private long GetLengthOfMessage(byte[] bytes)
        {
            var cr = Encoding.ASCII.GetBytes("\\r")[0];
            var newLine = Encoding.ASCII.GetBytes("\\n")[0];
            var index = 0;
            var lengthBytes = new byte[8];
            while (true)
            {
                if (bytes[index] == cr && bytes[index + 1] == newLine)
                {
                    break;
                }

                lengthBytes[index] = bytes[index];
            }

            return BitConverter.ToInt64(lengthBytes, 0);
        }

        private void ListenTcp()
        {
            var localEndPoint = new IPEndPoint(_serverInfo.IpAddress, _tcpPortNumber);
            var socket = new Socket(ServerInfo.CurrentServerInfo.IpAddress.AddressFamily, SocketType.Stream,
                                    ProtocolType.Tcp);
            socket.Bind(localEndPoint);
            socket.Listen(1000); // idk what im doing
            socket.Accept();
            for (;;)
            {
                var bytes = new byte[10];
                socket.Receive(bytes);
                var length = GetLengthOfMessage(bytes);
                var byteSize = new byte[length];
                socket.Receive(byteSize);
                DeserializeAndAddToDictionary(byteSize);
            }
        }

        private void DeserializeAndAddToDictionary(byte[] rawBytes)
        {
            var message = JsonConvert.DeserializeObject<NetworkWrapper>(Encoding.UTF8.GetString(rawBytes));
            if (_networkedEntitiesByGuid.ContainsKey(message.Guid) == false)
            {
                // if we don't have an object with this ID, create a new one.
                var gameObject = new GameObject(message.Guid.ToString());
                var netWorkEntity = gameObject.AddComponent<NetworkEntity>();
                netWorkEntity.Guid = message.Guid;
                while (_networkedEntitiesByGuid.TryAdd(message.Guid, netWorkEntity) != true) ;
            }

            foreach (var gameObject in message.GameObjects)
            {
                var targetNetworkGameObject = _networkedEntitiesByGuid[message.Guid];
                var jObject = JObject.FromObject(gameObject);
                var serializationTarget = _serializer.IdentifyBestTypeMatch(jObject);
                targetNetworkGameObject.SafeAdd(serializationTarget.Type, jObject);
            }
        }

        public void SendUdpUpdate(object value)
        {
            if (_udpClient.Client.Connected)
            {
                InitalizeConnections();
            }

            Debug.Assert(value != null);
            var contentAsBytes = GetStringAtUtf8Bytes(JsonUtility.ToJson(value));
            var message = contentAsBytes.LongLength.ToBytes().Concat(contentAsBytes);
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
            var message = contentAsBytes.LongLength.ToBytes().Concat(contentAsBytes);
            _tcpClient.Client.Send(message.ToArray());
        }

        private static byte[] GetStringAtUtf8Bytes(string value)
        {
            return Encoding.UTF8.GetBytes(value);
        }
    }
}