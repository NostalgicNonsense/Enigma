using System;
using System.Collections.Generic;

namespace Assets.Enigma.Enigma.Core.Networking.Serialization.SerializationModel
{
    // it's a struct to reduce the expense of building a wrapper object
    internal struct NetworkWrapper
    {
        public Guid Guid { get; set; }

        public IEnumerable<object> GameObjects { get; set; }

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
