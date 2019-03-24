using global::Newtonsoft.Json.Linq;

namespace Enigma.Networking.Serialization
{
    public interface ISerializer
    {
        object Deserialize(string value);

        string Serialize(object value);

        object Deserialize(JObject value);
    }
}
