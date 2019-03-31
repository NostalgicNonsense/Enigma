using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using UnityEngine;

namespace Assets.Enigma.Enigma.Core.Networking.Serialization
{
    public class ComponentPropertyResolver : DefaultContractResolver
    {
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var property = base.CreateProperty(member, memberSerialization);
            if (property.PropertyType == typeof(GameObject) || property.PropertyType.IsAssignableFrom(typeof(Rigidbody)))
            {
                property.Ignored = true;
            }
            return property;
        }
    }
}
