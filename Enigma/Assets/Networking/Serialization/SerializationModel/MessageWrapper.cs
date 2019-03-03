using System;
using System.Collections.Generic;
using UnityEngine;
using UtilityCode.Extensions;

namespace Networking.Serialization.SerializationModel
{
    // it's a struct to reduce the expense of building a wrapper object
    internal struct NetworkWrapper
    {
        public Guid Guid { get; }

        public IEnumerable<Component> GameObjects { get; }

        public NetworkWrapper(NetworkEntity entity)
        {
            Guid = entity.Guid;
            GameObjects = entity.GetAllComponents();
        }

        public NetworkWrapper(NetworkEntity entity, IEnumerable<Component> gameObjectsToSend)
        {
            Guid = entity.Guid;
            GameObjects = gameObjectsToSend;
        }
    }
}
