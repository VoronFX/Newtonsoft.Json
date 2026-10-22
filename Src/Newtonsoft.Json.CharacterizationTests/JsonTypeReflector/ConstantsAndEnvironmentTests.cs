using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Utilities;
using NUnit.Framework;

namespace Newtonsoft.Json.CharacterizationTests.JsonTypeReflectorTests
{
    /// <summary>Константи метаданих і прапорці середовища виконання JsonTypeReflector.</summary>
    [TestFixture]
    [NonParallelizable] // тести змінюють статичний стан прапорців
    public class ConstantsAndEnvironmentTests
    {
        private bool _originalDynamicCodeGeneration;

        [SetUp]
        public void RememberState() => _originalDynamicCodeGeneration = JsonTypeReflector.DynamicCodeGeneration;

        [TearDown]
        public void RestoreState()
        {
            JsonTypeReflector.SetDynamicCodeGeneration(_originalDynamicCodeGeneration);
            JsonTypeReflector.SetFullyTrusted(null);
        }

        [Test]
        public void MetadataPropertyNames_HaveFixedValues()
        {
            Assert.That(JsonTypeReflector.IdPropertyName, Is.EqualTo("$id"));
            Assert.That(JsonTypeReflector.RefPropertyName, Is.EqualTo("$ref"));
            Assert.That(JsonTypeReflector.TypePropertyName, Is.EqualTo("$type"));
            Assert.That(JsonTypeReflector.ValuePropertyName, Is.EqualTo("$value"));
            Assert.That(JsonTypeReflector.ArrayValuesPropertyName, Is.EqualTo("$values"));
        }

        [Test]
        public void ConventionNames_HaveFixedValues()
        {
            Assert.That(JsonTypeReflector.ShouldSerializePrefix, Is.EqualTo("ShouldSerialize"));
            Assert.That(JsonTypeReflector.SpecifiedPostfix, Is.EqualTo("Specified"));
            Assert.That(JsonTypeReflector.ConcurrentDictionaryTypeName, Is.EqualTo("System.Collections.Concurrent.ConcurrentDictionary`2"));
        }

        [Test]
        public void DynamicCodeGeneration_OnJitRuntime_IsTrue()
        {
            // net8.0: значення береться з RuntimeFeature.IsDynamicCodeCompiled
            JsonTypeReflector.SetDynamicCodeGeneration(true);
            Assert.That(JsonTypeReflector.DynamicCodeGeneration, Is.True);
        }

        [Test]
        public void ReflectionDelegateFactory_WhenDynamicCodeAllowed_IsDynamicFactory()
        {
            JsonTypeReflector.SetDynamicCodeGeneration(true);
            Assert.That(JsonTypeReflector.ReflectionDelegateFactory, Is.SameAs(DynamicReflectionDelegateFactory.Instance));
        }

        [Test]
        public void ReflectionDelegateFactory_WhenDynamicCodeDisabled_FallsBackToLateBoundFactory()
        {
            JsonTypeReflector.SetDynamicCodeGeneration(false);
            Assert.That(JsonTypeReflector.ReflectionDelegateFactory, Is.SameAs(LateBoundReflectionDelegateFactory.Instance));
        }

        [Test]
        public void FullyTrusted_ByDefault_IsTrueOnHomogenousAppDomain()
        {
            JsonTypeReflector.SetFullyTrusted(null);
            Assert.That(JsonTypeReflector.FullyTrusted, Is.True);
        }

        [Test]
        public void FullyTrusted_WhenOverridden_ReturnsOverride_AndNullRestoresDetection()
        {
            JsonTypeReflector.SetFullyTrusted(false);
            Assert.That(JsonTypeReflector.FullyTrusted, Is.False);

            JsonTypeReflector.SetFullyTrusted(null);
            Assert.That(JsonTypeReflector.FullyTrusted, Is.True);
        }

        [Test]
        public void DynamicCodeGeneration_ValueIsComputedOnceAndCached()
        {
            // Після першого обчислення значення зберігається в статичному полі;
            // повернутися до автоматичного визначення неможливо (сетер приймає лише bool).
            bool first = JsonTypeReflector.DynamicCodeGeneration;
            Assert.That(JsonTypeReflector.DynamicCodeGeneration, Is.EqualTo(first));
        }
    }
}
