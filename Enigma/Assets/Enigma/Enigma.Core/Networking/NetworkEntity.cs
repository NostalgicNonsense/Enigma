using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Enigma.Enigma.Core.Networking.Serialization;
using Assets.Enigma.Enigma.Core.Networking.Serialization.SerializationModel;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Assets.Enigma.Enigma.Core.Networking
{
    public class NetworkEntity : MonoBehaviour
    {
        public Guid Guid { get; set; }
        private ConnectionHandler _connectHandlerInstance;
        private Dictionary<Type, UpdateEvent> _networkedComponentsInGameObject;
        private readonly object _lock = new object();

        private void Awake()
        {
            var components = GetComponents(typeof(NetworkedComponent));
            _connectHandlerInstance = ConnectionHandler.ConnectionHandlerInstance;
            _networkedComponentsInGameObject = components.ToDictionary(c => c.GetType(), v => new UpdateEvent());
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
        public void TryGetUpdates<T>(T value) where T : NetworkedComponent
        {
            if (_networkedComponentsInGameObject.ContainsKey(typeof(T)) &&
                _networkedComponentsInGameObject[typeof(T)].HasBeenUpdatedSinceLastGet)
            {
                _networkedComponentsInGameObject[typeof(T)].Update(value);
            }
        }

        private class UpdateEvent
        {
            private JObject _mostRecentMessage;
            public bool HasBeenUpdatedSinceLastGet { get; private set; }

            public UpdateEvent()
            {
                HasBeenUpdatedSinceLastGet = false;
            }

            public void AddNewestMessage(JObject jObject)
            {
                HasBeenUpdatedSinceLastGet = true;
                _mostRecentMessage = jObject;
            }

            public void Update(NetworkedComponent component)
            {
                HasBeenUpdatedSinceLastGet = false;
                ComponentUpdater.UpdateObjectOfType(component, _mostRecentMessage);
            }
        }

        public void SafeAdd(Type type, JObject jObject)
        {
            // we have a local lock here
            lock (_lock)
            {
                if (!_networkedComponentsInGameObject.ContainsKey(type))
                {
                    var newComponent = gameObject.AddComponent(type);
                    _networkedComponentsInGameObject.Add(type, new UpdateEvent());
                    ComponentUpdater.UpdateObjectOfType((NetworkedComponent)newComponent, jObject);
                }
                else
                {
                    _networkedComponentsInGameObject[type].AddNewestMessage(jObject);
                }

            }
        }
    }
}
