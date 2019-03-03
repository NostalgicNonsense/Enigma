using System;
using System.Collections.Generic;
using System.Linq;
using Networking.Serialization;
using Networking.Serialization.SerializationModel;
using UnityEngine;

namespace Networking
{
    public class NetworkEntity : MonoBehaviour
    {
        public Guid Guid { get; set; }
        private ConnectionHandler _connectHandlerInstance;
        private Dictionary<Type, UpdateEvent<Component>> _networkedComponentsInGameObject;

        private void Awake()
        {
            var components = GetComponents(typeof(NetworkedComponent));
            _connectHandlerInstance = ConnectionHandler.ConnectionHandlerInstance;
            _networkedComponentsInGameObject = components.ToDictionary(c => c.GetType(), v => new UpdateEvent<Component>(v));
            _connectHandlerInstance.AddListener(this);
            _connectHandlerInstance.SendTcpUpdate(new NetworkWrapper(this));
        }

        public void SendAsync(object obj)
        {
            var message = new NetworkWrapper(this.Guid, new[] {obj});
            _connectHandlerInstance.SendUdpUpdate(message);
        }

        public void SendSync(object obj)
        {
            var message = new NetworkWrapper(this.Guid, new[] { obj });
            _connectHandlerInstance.SendTcpUpdate(message);
        }

        /// <summary>
        /// Method that updates component of type if component is ready to be updated
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        public void TryGetUpdates<T>(ref T value) where T : Component
        {
            if (_networkedComponentsInGameObject.ContainsKey(typeof(T)) &&
                _networkedComponentsInGameObject[typeof(T)].HasBeenUpdatedSinceLastGet)
            {
                value = (T) _networkedComponentsInGameObject[typeof(T)].Value;
            }
        }

        private struct UpdateEvent<T> where T : Component
        {
            private T _value;
            public bool HasBeenUpdatedSinceLastGet;

            public UpdateEvent(T value)
            {
                _value = value;
                HasBeenUpdatedSinceLastGet = false;
            }

            public T Value
            {
                get
                {
                    HasBeenUpdatedSinceLastGet = true;
                    return _value;
                }
                set
                {
                    HasBeenUpdatedSinceLastGet = false;
                    _value = value;
                }
            }
        }
    }
}
