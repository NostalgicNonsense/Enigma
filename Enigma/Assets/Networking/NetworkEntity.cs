using System;
using System.Collections.Generic;
using System.Linq;
using Networking.Serialization.SerializationModel;
using UnityEngine;

namespace Networking
{
    public class NetworkEntity : MonoBehaviour
    {
        public Guid Guid { get; set; }
        private ConnectionHandler _connectHandlerInstance;
        private Dictionary<Type, UpdateEvent<Component>> _networkedComponentsInGameObject;
        private object _lock = new object();

        private void Awake()
        {
            var components = GetComponents(typeof(NetworkedComponent));
            _connectHandlerInstance = ConnectionHandler.ConnectionHandlerInstance;
            _networkedComponentsInGameObject = components.ToDictionary(c => c.GetType(), v => new UpdateEvent<Component>(v));
            while (_connectHandlerInstance.TryAddListener(this) != true)
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

            public UpdateEvent(T value, bool newNetworkObject)
            {
                _value = value;
                HasBeenUpdatedSinceLastGet = false;
            }

            public void Update(object obj)
            {
                Value = (T)obj;
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

        public void SafeAdd(object value)
        {
            // we have a local lock here
            lock (_lock)
            {
                if (_networkedComponentsInGameObject.ContainsKey(value.GetType()))
                {
                    _networkedComponentsInGameObject[value.GetType()].Update(value);
                }
                else
                {
                    var newComponent = gameObject.AddComponent(value.GetType());
                    _networkedComponentsInGameObject.Add(value.GetType(), new UpdateEvent<Component>(newComponent));
                }
            }
        }
    }
}
