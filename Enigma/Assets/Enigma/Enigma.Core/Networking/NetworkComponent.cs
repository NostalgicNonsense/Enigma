using UnityEngine;

namespace Assets.Enigma.Enigma.Core.Networking
{
    [RequireComponent(typeof(NetworkEntity))]
    public abstract class NetworkedComponent : MonoBehaviour
    {
        private NetworkEntity _networkEntity;
        private bool HasNetworkAuthority { get; } // haven't decided how to do this yet.

        public void Start()
        {
            _networkEntity = GetComponent<NetworkEntity>();
        }

        protected void SendAsync()
        {
            _networkEntity.SendAsync(this);
        }

        protected void SendSync()
        {
            _networkEntity.SendAsync(this);
        }

        protected void Update()
        {
            var obj = this; // have to create a reference value to pass 
            _networkEntity.TryGetUpdates(obj);
        }
        
    }
}
