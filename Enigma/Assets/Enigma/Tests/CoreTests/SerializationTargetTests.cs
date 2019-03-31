using System;
using System.Linq;
using Assets.Enigma.Enigma.Core.Networking;
using Assets.Enigma.Enigma.Core.Networking.Serialization;
using Assets.Enigma.Enigma.Core.Networking.Serialization.SerializationModel;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Assets.Enigma.Tests.CoreTests
{
    public class SerializationTargetTests
    {
        private static readonly Serializer Serializer = new Serializer();
        [Test]
        public void SerializationTargetTestsSimplePasses()
        {
            var gameObject = new GameObject();
            var obj1 = gameObject.AddComponent<TestObjOne>();
            var obj2 = gameObject.AddComponent<TestObjTwo>();

            obj1.StringValue = "testValue";
            obj1.NumericValue = 1337;

            obj2.StringValue = "otherValue";
            obj2.NumericValue = 1338;
            obj2.ByteValue = 0xff;

            var networkMessage = new NetworkWrapper(new Guid(), new[] {obj1, obj2});
            
            //Convert networkMessage into Json
            var networkMessageJson = Serializer.Serialize(networkMessage);

            //Deserialize said Json to erase the type info
            var deserializedMessage = Serializer.Deserialize<NetworkWrapper>(networkMessageJson);



            var results = deserializedMessage.GameObjects.Select(c => Serializer.IdentifyBestTypeMatch(JObject.FromObject(c)))
                                             .ToList();

            Assert.True(results.Any(c => c.Type == typeof(TestObjOne)));
            Assert.True(results.Any(c => c.Type == typeof(TestObjTwo)));
        }

        private class TestObjOne : NetworkedComponent
        {
            public string StringValue { get; set; }
            public int NumericValue { get; set; }
        }

        private class TestObjTwo : TestObjOne
        {
            public byte ByteValue { get; set; }
        }
    }
}
