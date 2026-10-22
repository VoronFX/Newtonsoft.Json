using System;
using Newtonsoft.Json.Serialization;
using NUnit.Framework;

namespace Newtonsoft.Json.CharacterizationTests.JsonTypeReflectorTests
{
    /// <summary>Пріоритети та межові випадки GetObjectMemberSerialization, а також їхній прояв у публічному API.</summary>
    [TestFixture]
    public class MemberSerializationEdgeCaseTests
    {
        [Test]
        public void JsonObject_HasPriorityOverDataContract()
        {
            Assert.That(JsonTypeReflector.GetObjectMemberSerialization(typeof(MsJsonObjectAndDataContract), false), Is.EqualTo(MemberSerialization.Fields));
        }

        [Test]
        public void DataContract_HasPriorityOverSerializable()
        {
            Assert.That(JsonTypeReflector.GetObjectMemberSerialization(typeof(MsDataContractAndSerializable), false), Is.EqualTo(MemberSerialization.OptIn));
        }

        [Test]
        public void Serializable_IgnoredWhenFlagSet_FallsBackToOptOut()
        {
            Assert.That(JsonTypeReflector.GetObjectMemberSerialization(typeof(MsSerializableIgnored), true), Is.EqualTo(MemberSerialization.OptOut));
        }

        [Test]
        [Category("Suspicious")]
        public void DerivedTypes_DataContractIsInherited_ButSerializableIsNot()
        {
            // ХАРАКТЕРИЗАЦІЯ: підозріло — непослідовність: DataContract шукається в базових класах
            // (хоча коментар у коді каже «DataContractAttribute does not have inheritance»),
            // а Serializable — ні. Нащадок [DataContract]-класу стає OptIn, нащадок [Serializable] — OptOut.
            Assert.That(JsonTypeReflector.GetObjectMemberSerialization(typeof(MsDerivedFromDataContract), false), Is.EqualTo(MemberSerialization.OptIn));
            Assert.That(JsonTypeReflector.GetObjectMemberSerialization(typeof(MsSecondLevelDerivedFromDataContract), false), Is.EqualTo(MemberSerialization.OptIn));
            Assert.That(JsonTypeReflector.GetObjectMemberSerialization(typeof(MsDerivedFromSerializable), false), Is.EqualTo(MemberSerialization.OptOut));
        }

        [Test]
        [Category("Suspicious")]
        public void PublicApi_DerivedFromDataContract_SerializesOnlyDataMembers()
        {
            // Та сама непослідовність через JsonConvert: нові властивості нащадка мовчки зникають.
            Assert.That(JsonConvert.SerializeObject(new ApiDerivedFromDataContract()), Is.EqualTo("{\"Marked\":1}"));
        }

        [Test]
        public void PublicApi_SerializableAttribute_IsIgnoredByDefaultResolver()
        {
            // DefaultContractResolver.IgnoreSerializableAttribute = true за замовчуванням,
            // тому гілка «Serializable → Fields» у звичайному JsonConvert не задіяна.
            Assert.That(JsonConvert.SerializeObject(new ApiSerializableBase()), Is.EqualTo("{\"Property\":20}"));
        }

        [Test]
        public void PublicApi_SerializableHonoured_BaseSerializesFields_DerivedSerializesPublicProperties()
        {
            var settings = new JsonSerializerSettings { ContractResolver = new DefaultContractResolver { IgnoreSerializableAttribute = false } };
            Assert.That(JsonConvert.SerializeObject(new ApiSerializableBase(), settings), Is.EqualTo("{\"_field\":10,\"<Property>k__BackingField\":20}"));
            Assert.That(JsonConvert.SerializeObject(new ApiDerivedFromSerializable(), settings), Is.EqualTo("{\"Property\":20}"));
        }

        [Test]
        public void NullType_ThrowsArgumentNullExceptionFromCache()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => JsonTypeReflector.GetObjectMemberSerialization(null!, false));
            Assert.That(ex!.ParamName, Is.EqualTo("key"));
        }
    }
}
