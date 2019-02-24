namespace Enigma.Components.Networking.Serialization
{
    public interface ISerializer
    {
        object Deserialize(string value);

        string Serialize(object value);
    }
}
