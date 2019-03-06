using Newtonsoft.Json.Linq;

namespace Networking.Serialization
{
    public interface ISerializer
    {
        object Deserialize(string value);

        string Serialize(object value);

        object Deserialize(JObject value);
    }
}
