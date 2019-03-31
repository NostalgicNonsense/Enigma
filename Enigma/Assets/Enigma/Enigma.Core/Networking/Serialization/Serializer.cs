using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Enigma.Networking;
using Enigma.Networking.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UtilityCode.Extensions;

namespace Assets.Enigma.Enigma.Core.Networking.Serialization
{
    public class Serializer : ISerializer
    {
        private static readonly IEnumerable<SerializationTarget> SerializationTargets;
        private static readonly JsonSerializerSettings Settings;
        private static readonly JsonLoadSettings LoadSettings;


        static Serializer()
        {
            // sorry about this..
            var typesInThisAssembly = AppDomain.CurrentDomain.GetAssemblies()
                                               .SelectMany(a => a.GetTypes().Where(
                                                           c => c.IsSubclassOf(typeof(NetworkedComponent))));
            SerializationTargets = typesInThisAssembly.Select(type => new SerializationTarget
            {
                ParameterNames = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                  .Select(parameterInfo => parameterInfo.Name).ToHashSet(),
                Type = type
            });

            var jsonResolver = new ComponentPropertyResolver();
            Settings = new JsonSerializerSettings
            {
                ContractResolver = jsonResolver
            };
        }

        public object Deserialize(JObject value)
        {
            var bestMatchedType =
                SerializationTargets
                    .OrderByDescending(c => c.GetNumberOfParameterNameMatches(JObject.FromObject(value))).First();
            return bestMatchedType.ReturnObjectOfType(value);
        }

        public object Deserialize(string value)
        {
            return Deserialize(JObject.Parse(value));
        }

        public string Serialize(object value)
        {
            return JsonConvert.SerializeObject(value, Settings);
        }

        public T Deserialize<T>(string value)
        {
            return JsonConvert.DeserializeObject<T>(value, Settings);
        }
    }
}