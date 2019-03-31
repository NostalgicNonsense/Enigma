using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;

namespace Assets.Enigma.Enigma.Core.Networking.Serialization
{
    public static class ComponentUpdater
    {
        public static void UpdateObjectOfType(NetworkedComponent component, JObject jObject)
        {
            var componentProperties = component.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                               .ToDictionary(key => key.Name, v => v);
            foreach (var key in jObject)
            {
                if (componentProperties.ContainsKey(key.Key))
                {
                    var property = componentProperties[key.Key];
                    property.SetValue(component, key.Value.ToObject(property.PropertyType));
                }
            }
        }
    }
}
