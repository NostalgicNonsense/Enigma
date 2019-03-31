using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Assets.Enigma.Enigma.Core.Networking.Serialization
{
    public class SerializationTarget
    {
        public Type Type { get; set; }
        public HashSet<string> ParameterNames { get; set; }

        public double GetNumberOfParameterNameMatches(JObject jObject)
        {
            var matches = 0d;
            foreach (var key in jObject)
            {
                if (ParameterNames.Contains(key.Key))
                {
                    matches++;
                }
            }

            return matches / ParameterNames.Count;
        }

        public object ReturnObjectOfType(JObject jObject)
        {
            return jObject.ToObject(Type);
        }
    }
}
