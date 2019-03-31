using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Assets.Enigma.Enigma.Core.Utility;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Assets.Enigma.Enigma.Core.Networking.Serialization
{
    public class Serializer : ISerializer
    {
        private static readonly IEnumerable<SerializationTarget> SerializationTargets;
        private static readonly JsonSerializerSettings Settings;

        #region  Constructor
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
        #endregion

        public SerializationTarget IdentifyBestTypeMatch(JObject value)
        {
            var bestMatchedType =
                SerializationTargets
                    .OrderByDescending(c => c.GetNumberOfParameterNameMatches(JObject.FromObject(value))).First();
            return bestMatchedType;
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