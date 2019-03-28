using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UtilityCode.Extensions;

namespace Enigma.Networking.Serialization
{
    public class Serializer : ISerializer
    {
        private static IEnumerable<SerializationTarget> _serializationTargets;

        static Serializer()
        {
            var typesInThisAssembly = typeof(Serializer)
                                      .Assembly.GetTypes().Where(c => c.IsSubclassOf(typeof(NetworkedComponent)));
            _serializationTargets = typesInThisAssembly.Select(type => new SerializationTarget
            {
                ParameterNames = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                  .Select(parameterInfo => parameterInfo.Name).ToHashSet(),
                Type = type
            });

            _serializationTargets.Append(new SerializationTarget
            {
                Type = typeof(Vector3),
                ParameterNames = typeof(Vector3).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                                .Select(parameterInfo => parameterInfo.Name).ToHashSet()
            });
        }

        public object Deserialize(JObject value)
        {
            var bestMatchedType =
                _serializationTargets
                    .OrderByDescending(c => c.GetNumberOfParameterNameMatches(JObject.FromObject(value))).First();
            return bestMatchedType.ReturnObjectOfType(value);
        }

        public object Deserialize(string value)
        {
            return Deserialize(JObject.Parse(value));
        }

        public string Serialize(object value)
        {
            return JsonUtility.ToJson(value);
        }
    }
}