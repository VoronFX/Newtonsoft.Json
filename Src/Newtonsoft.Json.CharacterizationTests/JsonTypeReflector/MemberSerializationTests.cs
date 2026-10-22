using System;
using System.Runtime.Serialization;
using Newtonsoft.Json.Serialization;
using NUnit.Framework;

namespace Newtonsoft.Json.CharacterizationTests.JsonTypeReflectorTests
{
    /// <summary>GetObjectMemberSerialization і GetDataContractAttribute: як визначається режим відбору членів.</summary>
    [TestFixture]
    public class MemberSerializationTests
    {
        [Test]
        public void PlainClass_DefaultsToOptOut()
        {
            Assert.That(JsonTypeReflector.GetObjectMemberSerialization(typeof(MsPlain), false), Is.EqualTo(MemberSerialization.OptOut));
        }

        [Test]
        public void JsonObjectAttribute_ValueIsReturnedAsIs()
        {
            Assert.That(JsonTypeReflector.GetObjectMemberSerialization(typeof(MsJsonObjectOptIn), false), Is.EqualTo(MemberSerialization.OptIn));
            Assert.That(JsonTypeReflector.GetObjectMemberSerialization(typeof(MsJsonObjectFields), false), Is.EqualTo(MemberSerialization.Fields));
        }

        [Test]
        public void DataContractAttribute_MeansOptIn()
        {
            Assert.That(JsonTypeReflector.GetObjectMemberSerialization(typeof(MsDataContract), false), Is.EqualTo(MemberSerialization.OptIn));
        }

        [Test]
        public void SerializableAttribute_MeansFields()
        {
            Assert.That(JsonTypeReflector.GetObjectMemberSerialization(typeof(MsSerializable), false), Is.EqualTo(MemberSerialization.Fields));
        }

        [Test]
        public void GetDataContractAttribute_OnMarkedType_ReturnsAttribute()
        {
            DataContractAttribute? attribute = JsonTypeReflector.GetDataContractAttribute(typeof(DcBase));
            Assert.That(attribute, Is.Not.Null);
            Assert.That(attribute!.Name, Is.EqualTo("base-contract"));
        }
    }
}
