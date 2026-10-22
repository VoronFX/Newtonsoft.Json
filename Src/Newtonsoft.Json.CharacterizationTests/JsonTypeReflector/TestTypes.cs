#region License
// Характеризаційні тести для Newtonsoft.Json.Serialization.JsonTypeReflector
// Практична робота №3, варіант 2. Типи-фікстури, що використовуються тестами.
#endregion

using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Newtonsoft.Json.CharacterizationTests.JsonTypeReflectorTests
{
    // JsonTypeReflector кешує результати в статичних ThreadSafeStore (за типом / членом),
    // тому кожен тест використовує ВЛАСНИЙ тип-фікстуру — тести не залежать один від одного
    // і від порядку виконання.

    // ---------- GetObjectMemberSerialization ----------
    public class MsPlain { public int X { get; set; } }

    [JsonObject(MemberSerialization.OptIn)]
    public class MsJsonObjectOptIn { }

    [JsonObject(MemberSerialization.Fields)]
    public class MsJsonObjectFields { }

    [DataContract]
    public class MsDataContract { }

    public class MsDerivedFromDataContract : MsDataContract { }

    public class MsSecondLevelDerivedFromDataContract : MsDerivedFromDataContract { }

    [Serializable]
    public class MsSerializable { }

    [Serializable]
    public class MsSerializableIgnored { }

    public class MsDerivedFromSerializable : MsSerializable { }

    [JsonObject(MemberSerialization.Fields)]
    [DataContract]
    public class MsJsonObjectAndDataContract { }

    [DataContract]
    [Serializable]
    public class MsDataContractAndSerializable { }

    // Публічне API: як ці правила проявляються при серіалізації
    [DataContract]
    public class ApiDataContractBase
    {
        [DataMember] public int Marked { get; set; } = 1;
        public int NotMarked { get; set; } = 2;
    }

    public class ApiDerivedFromDataContract : ApiDataContractBase
    {
        public int DerivedNotMarked { get; set; } = 3;
    }

    [Serializable]
    public class ApiSerializableBase
    {
        private int _field = 10;
        public int Property { get; set; } = 20;
        public int GetField() => _field;
    }

    public class ApiDerivedFromSerializable : ApiSerializableBase { }

    // ---------- GetDataContractAttribute / GetDataMemberAttribute ----------
    [DataContract(Name = "base-contract")]
    public class DcBase
    {
        [DataMember(Name = "field")] public int Field;
        [DataMember(Name = "prop")] public int Prop { get; set; }
        [DataMember(Name = "virtual")] public virtual int Virtual { get; set; }
        [DataMember(Name = "hidden")] public int Hidden { get; set; }
        public int NoAttribute { get; set; }
        public void Method() { }
    }

    public class DcDerived : DcBase
    {
        public override int Virtual { get; set; }
        public new int Hidden { get; set; }
    }

    public class DcSecondLevel : DcDerived
    {
        public override int Virtual { get; set; }
    }

    [DataContract(Name = "own-contract")]
    public class DcDerivedWithOwnContract : DcBase { }

    public class DcNone { public virtual int Virtual { get; set; } }

    // ---------- GetAttribute: інтерфейси, метадані, базові класи ----------
    [JsonObject(Title = "from-interface")]
    public interface IAttrSource
    {
        [JsonProperty("iface-value")] int Value { get; }
        int NoAttribute { get; }
    }

    public class AttrImplementsInterface : IAttrSource
    {
        public int Value => 1;
        public int NoAttribute => 2;
    }

    [JsonObject(Title = "own")]
    public class AttrImplementsInterfaceWithOwn : IAttrSource
    {
        [JsonProperty("own-value")] public int Value => 1;
        public int NoAttribute => 2;
    }

    public class AttrBase
    {
        [JsonProperty("base-x")] public virtual int X { get; set; }
    }

    public class AttrDerivedOverride : AttrBase
    {
        public override int X { get; set; }
    }

    public class AttrPlain { public int X { get; set; } }

    [JsonObject(Title = "metadata-title")]
    public class AttrMetadataBuddy
    {
        [JsonProperty("metadata-name")] public string? Name { get; set; }
    }

    [MetadataType(typeof(AttrMetadataBuddy))]
    public class AttrWithMetadata
    {
        public string? Name { get; set; }
        public string? NotInBuddy { get; set; }
    }

    [MetadataType(typeof(AttrMetadataBuddy))]
    [JsonObject(Title = "own-title")]
    public class AttrWithMetadataAndOwn
    {
        [JsonProperty("own-name")] public string? Name { get; set; }
    }

    public class EmptyBuddy { }

    public class PartialBuddy { public string? Other { get; set; } }

    [MetadataType(typeof(PartialBuddy))]
    public class AttrWithMetadataPartial
    {
        [JsonProperty("own-other")] public string? Other { get; set; }
    }

    [MetadataType(typeof(AttrMetadataBuddy))]
    public class AttrWithMetadataForSpoofTest { }

    [MetadataType(typeof(EmptyBuddy))]
    [JsonObject(Title = "own-only")]
    public class AttrWithEmptyMetadata
    {
        [JsonProperty("own-name")] public string? Name { get; set; }
    }

    public class AttrCtorParameter
    {
        public AttrCtorParameter([JsonProperty("param-name")] int value) { }
    }

    // ---------- Створення конвертерів ----------
    public abstract class TestConverterBase : JsonConverter
    {
        public override bool CanConvert(Type objectType) => false;
        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer) => null;
        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer) { }
    }

    public class ConvDefault : TestConverterBase { }

    public class ConvWithOverloads : TestConverterBase
    {
        public string Info { get; }
        public ConvWithOverloads() { Info = "default"; }
        public ConvWithOverloads(string s, int i) { Info = "string+int:" + s + i; }
        public ConvWithOverloads(long l) { Info = "long:" + l; }
        public ConvWithOverloads(object o) { Info = "object:" + o; }
    }

    public class ConvStringIntOnly : TestConverterBase
    {
        public ConvStringIntOnly(string s, int i) { }
    }

    public class ConvNoDefaultCtor : TestConverterBase
    {
        public ConvNoDefaultCtor(int x) { }
    }

    public class ConvPrivateDefaultCtor : TestConverterBase
    {
        private ConvPrivateDefaultCtor() { }
    }

    public class ConvThrowingCtor : TestConverterBase
    {
        public ConvThrowingCtor() { throw new InvalidOperationException("boom"); }
    }

    public class ConvParamsArray : TestConverterBase
    {
        public int Count { get; }
        public ConvParamsArray(params string[] values) { Count = values.Length; }
    }

    [JsonConverter(typeof(IsoDateTimeConverter))]
    public class ConvAttributeOnClass { }

    public class ConvAttributeOnProperty
    {
        [JsonConverter(typeof(StringEnumConverter), typeof(CamelCaseNamingStrategy))]
        public DayOfWeek Day { get; set; }

        [JsonConverter(typeof(ConvWithOverloads), "a", 1)]
        public int WithArgs { get; set; }

        public int NoConverter { get; set; }
    }

    [JsonConverter(typeof(object))]
    public class ConvAttributeNotAConverter { }

    [JsonConverter(typeof(ConvNoDefaultCtor))]
    public class ConvAttributeNoDefaultCtor { }

    public class NamingWithArgs : NamingStrategy
    {
        public string Prefix { get; }
        public NamingWithArgs(string prefix) { Prefix = prefix; }
        protected override string ResolvePropertyName(string name) => Prefix + name;
    }

    // ---------- Serializable / NonSerialized ----------
    [Serializable]
    public class SerMarked
    {
        [NonSerialized] public int Skipped;
        public int Kept;
    }

    public class SerDerived : SerMarked { }

    public class SerPlain { }

    // ---------- TypeDescriptor ----------
    [TypeConverter(typeof(ToStringConverter))]
    public class TdWithStringConverter { }

    [TypeConverter(typeof(NoStringConverter))]
    public class TdWithNonStringConverter { }

    [TypeConverter(typeof(TypeConverter))]
    public class TdWithBaseTypeConverter { }

    public class TdPlain { }

    public class ToStringConverter : TypeConverter
    {
        public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType) => destinationType == typeof(string);
    }

    public class NoStringConverter : TypeConverter
    {
        public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType) => false;
    }
}
