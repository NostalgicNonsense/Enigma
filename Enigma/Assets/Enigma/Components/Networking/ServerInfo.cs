using System.Net;

namespace Enigma.Components.Networking
{
    public class ServerInfo
    {
        public static ServerInfo CurrentServerInfo;

        static ServerInfo()
        {
            CurrentServerInfo = new ServerInfo();
        }

        // to be called in the future when we connect to a new server
        public void UpdateIpAddress(IPAddress ipAddress, int port)
        {
            IpAddress = ipAddress;
            Port = port;
        }

        public IPAddress IpAddress { get; private set; }
        public int Port { get; private set; }
    }
}
