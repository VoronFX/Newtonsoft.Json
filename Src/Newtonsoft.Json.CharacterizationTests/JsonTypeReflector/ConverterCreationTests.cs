using System;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using NUnit.Framework;

namespace Newtonsoft.Json.CharacterizationTests.JsonTypeReflectorTests
{
    /// <summary>GetJsonConverter / CreateJsonConverterInstance і внутрішній GetCreator.</summary>
    [TestFixture]
    public class ConverterCreationTests
    {
        [Test]
        public void CreateJsonConverterInstance_WithNullArgs_UsesDefaultConstructor()
        {
            JsonConverter converter = JsonTypeReflector.CreateJsonConverterInstance(typeof(ConvDefault), null);
            Assert.That(converter, Is.InstanceOf<ConvDefault>());
        }

        [Test]
        public void CreateJsonConverterInstance_WithMatchingArgs_UsesParameterizedConstructor()
        {
            var converter = (ConvWithOverloads)JsonTypeReflector.CreateJsonConverterInstance(typeof(ConvWithOverloads), new object[] { "x", 5 });
            Assert.That(converter.Info, Is.EqualTo("string+int:x5"));
        }

        [Test]
        public void CreateJsonConverterInstance_EveryCallReturnsNewInstance()
        {
            JsonConverter first = JsonTypeReflector.CreateJsonConverterInstance(typeof(IsoDateTimeConverter), null);
            JsonConverter second = JsonTypeReflector.CreateJsonConverterInstance(typeof(IsoDateTimeConverter), null);
            Assert.That(second, Is.Not.SameAs(first));
        }

        [Test]
        public void GetJsonConverter_ClassAttribute_CreatesConverter()
        {
            Assert.That(JsonTypeReflector.GetJsonConverter(typeof(ConvAttributeOnClass)), Is.InstanceOf<IsoDateTimeConverter>());
        }

        [Test]
        public void GetJsonConverter_PropertyAttributeWithParameters_PassesParametersToConstructor()
        {
            var day = typeof(ConvAttributeOnProperty).GetProperty(nameof(ConvAttributeOnProperty.Day))!;
            var converter = (StringEnumConverter)JsonTypeReflector.GetJsonConverter(day)!;
            Assert.That(converter.NamingStrategy, Is.InstanceOf<CamelCaseNamingStrategy>());

            var withArgs = typeof(ConvAttributeOnProperty).GetProperty(nameof(ConvAttributeOnProperty.WithArgs))!;
            Assert.That(((ConvWithOverloads)JsonTypeReflector.GetJsonConverter(withArgs)!).Info, Is.EqualTo("string+int:a1"));
        }

        [Test]
        public void GetJsonConverter_NoAttribute_ReturnsNull()
        {
            var property = typeof(ConvAttributeOnProperty).GetProperty(nameof(ConvAttributeOnProperty.NoConverter))!;
            Assert.That(JsonTypeReflector.GetJsonConverter(property), Is.Null);
            Assert.That(JsonTypeReflector.GetJsonConverter(typeof(MsPlain)), Is.Null);
        }
    }
}
