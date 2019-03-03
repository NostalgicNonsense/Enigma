using UnityEngine;
using MonoBehaviour = Photon_Unity_Networking.Plugins.PhotonNetwork.MonoBehaviour;

namespace Networking.Serialization
{
    [RequireComponent(typeof(NetworkEntity))]
    public abstract class NetworkedComponent : MonoBehaviour
    {
        private NetworkEntity _networkEntity;

        public void Start()
        {
            _networkEntity = GetComponent<NetworkEntity>();
        }

        protected void SendAsync()
        {
            
        }

        protected void SendSync()
        {

        }

        protected void Update()
        {
            var obj = this; // have to create a reference value to pass 
            _networkEntity.TryGetUpdates(ref obj);
        }
    }
}
