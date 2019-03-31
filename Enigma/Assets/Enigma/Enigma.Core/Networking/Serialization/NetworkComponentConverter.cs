using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Assets.Enigma.Enigma.Core.Utility;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Assets.Enigma.Enigma.Core.Networking.Serialization
{
    public class NetworkComponentConverter : JsonConverter
    {
        private readonly HashSet<string> _blackListedPropertyNames =
            typeof(MonoBehaviour).GetProperties().Select(c => c.Name).ToHashSet();

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            // unused
            throw new NotImplementedException();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var instance = Activator.CreateInstance(objectType);
            var propertiesOfType = objectType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                             .ToDictionary(key => key.Name, value => value);
            var jObject = JObject.Load(reader);
            foreach (var tuple in jObject)
            {
                if (propertiesOfType.ContainsKey(tuple.Key) &&  !_blackListedPropertyNames.Contains(tuple.Key))
                {
                    AddValueByType(instance, propertiesOfType[tuple.Key], tuple.Value);
                }
            }

            return instance;
        }

        private void AddValueByType(object instance, PropertyInfo property, JToken value)
        {
            if (property.PropertyType.IsValueType || property.PropertyType == typeof(string))
            {
                property.SetValue(instance, value.ToObject(property.PropertyType));
            }
            else
            {
                property.SetValue(instance, JsonConvert.DeserializeAnonymousType(value.ToObject<string>(), property.PropertyType));
            }
        }

        public override bool CanConvert(Type objectType)
        {
            return true;
        }
    }
}
