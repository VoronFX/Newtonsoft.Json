using System;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using NUnit.Framework;

namespace Newtonsoft.Json.CharacterizationTests.JsonTypeReflectorTests
{
    [TestFixture]
    public class NamingStrategyEdgeCaseTests
    {
        [Test]
        public void GetContainerNamingStrategy_CachesInstanceOnAttribute()
        {
            var attribute = new JsonObjectAttribute { NamingStrategyType = typeof(SnakeCaseNamingStrategy) };
            NamingStrategy? first = JsonTypeReflector.GetContainerNamingStrategy(attribute);
            Assert.That(JsonTypeReflector.GetContainerNamingStrategy(attribute), Is.SameAs(first));
        }

        [Test]
        public void GetContainerNamingStrategy_ChangingTypeResetsCachedInstance()
        {
            var attribute = new JsonObjectAttribute { NamingStrategyType = typeof(SnakeCaseNamingStrategy) };
            JsonTypeReflector.GetContainerNamingStrategy(attribute);

            attribute.NamingStrategyType = typeof(KebabCaseNamingStrategy);
            Assert.That(JsonTypeReflector.GetContainerNamingStrategy(attribute), Is.InstanceOf<KebabCaseNamingStrategy>());
        }

        [Test]
        public void GetContainerNamingStrategy_PassesParameters()
        {
            var attribute = new JsonObjectAttribute { NamingStrategyType = typeof(NamingWithArgs), NamingStrategyParameters = new object[] { "pre_" } };
            var strategy = (NamingWithArgs)JsonTypeReflector.GetContainerNamingStrategy(attribute)!;
            Assert.That(strategy.GetPropertyName("Name", false), Is.EqualTo("pre_Name"));
        }

        [Test]
        public void GetContainerNamingStrategy_ParametersWithoutType_ReturnsNull()
        {
            var attribute = new JsonObjectAttribute { NamingStrategyParameters = new object[] { "ignored" } };
            Assert.That(JsonTypeReflector.GetContainerNamingStrategy(attribute), Is.Null);
        }

        [Test]
        public void GetContainerNamingStrategy_NullAttribute_ThrowsNullReferenceException()
        {
            Assert.Throws<NullReferenceException>(() => JsonTypeReflector.GetContainerNamingStrategy(null!));
        }

        [Test]
        [Category("Suspicious")]
        public void CreateNamingStrategyInstance_WrongType_ThrowsRawInvalidCastException()
        {
            // ХАРАКТЕРИЗАЦІЯ: підозріло — тип не перевіряється, об'єкт спочатку створюється, а потім падає приведення.
            Assert.Throws<InvalidCastException>(() => JsonTypeReflector.CreateNamingStrategyInstance(typeof(IsoDateTimeConverter), null));
        }

        [Test]
        public void CreateNamingStrategyInstance_MissingConstructorArguments_ThrowsJsonException()
        {
            var ex = Assert.Throws<JsonException>(() => JsonTypeReflector.CreateNamingStrategyInstance(typeof(NamingWithArgs), null));
            Assert.That(ex!.InnerException!.Message, Does.StartWith("No parameterless constructor defined"));
        }
    }
}
