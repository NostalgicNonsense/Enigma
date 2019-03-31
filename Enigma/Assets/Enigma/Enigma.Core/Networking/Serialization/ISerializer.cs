using Newtonsoft.Json.Linq;

namespace Assets.Enigma.Enigma.Core.Networking.Serialization
{
    public interface ISerializer
    {
        T Deserialize<T>(string value);
        string Serialize(object value);

        SerializationTarget IdentifyBestTypeMatch(JObject value);
    }
}
