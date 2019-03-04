using System;
using System.Collections.Generic;
using UnityEngine;

namespace Networking.Serialization.SerializationModel
{
    // it's a struct to reduce the expense of building a wrapper object
    internal struct NetworkWrapper
    {
        public Guid Guid { get; }

        public IEnumerable<object> GameObjects { get; }

        public NetworkWrapper(NetworkEntity entity)
        {
            Guid = entity.Guid;
            GameObjects = entity.GetComponents(typeof(NetworkedComponent));
        }

        public NetworkWrapper(Guid guid, IEnumerable<object> gameObjectsToSend)
        {
            Guid = guid;
            GameObjects = gameObjectsToSend;
        }
    }
}
