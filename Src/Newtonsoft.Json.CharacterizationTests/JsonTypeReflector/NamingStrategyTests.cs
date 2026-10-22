using Newtonsoft.Json.Serialization;
using NUnit.Framework;

namespace Newtonsoft.Json.CharacterizationTests.JsonTypeReflectorTests
{
    /// <summary>CreateNamingStrategyInstance і GetContainerNamingStrategy.</summary>
    [TestFixture]
    public class NamingStrategyTests
    {
        [Test]
        public void CreateNamingStrategyInstance_DefaultConstructor()
        {
            Assert.That(JsonTypeReflector.CreateNamingStrategyInstance(typeof(CamelCaseNamingStrategy), null), Is.InstanceOf<CamelCaseNamingStrategy>());
        }

        [Test]
        public void CreateNamingStrategyInstance_WithArguments()
        {
            var strategy = (CamelCaseNamingStrategy)JsonTypeReflector.CreateNamingStrategyInstance(typeof(CamelCaseNamingStrategy), new object[] { true, false });
            Assert.That(strategy.ProcessDictionaryKeys, Is.True);
            Assert.That(strategy.OverrideSpecifiedNames, Is.False);
        }

        [Test]
        public void GetContainerNamingStrategy_TypeSet_CreatesStrategy()
        {
            var attribute = new JsonObjectAttribute { NamingStrategyType = typeof(SnakeCaseNamingStrategy) };
            Assert.That(JsonTypeReflector.GetContainerNamingStrategy(attribute), Is.InstanceOf<SnakeCaseNamingStrategy>());
        }

        [Test]
        public void GetContainerNamingStrategy_NoTypeSet_ReturnsNull()
        {
            Assert.That(JsonTypeReflector.GetContainerNamingStrategy(new JsonObjectAttribute()), Is.Null);
        }
    }
}
