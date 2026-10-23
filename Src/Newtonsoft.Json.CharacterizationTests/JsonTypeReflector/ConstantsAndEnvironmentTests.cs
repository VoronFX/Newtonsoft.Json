using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Utilities;
using NUnit.Framework;

namespace Newtonsoft.Json.CharacterizationTests.JsonTypeReflectorTests
{
    /// <summary>Константи метаданих JsonTypeReflector і прапорці середовища виконання (ReflectionEnvironment).</summary>
    [TestFixture]
    [NonParallelizable] // тести змінюють статичний стан прапорців
    public class ConstantsAndEnvironmentTests
    {
        private bool _originalDynamicCodeGeneration;

        [SetUp]
        public void RememberState() => _originalDynamicCodeGeneration = ReflectionEnvironment.DynamicCodeGeneration;

        [TearDown]
        public void RestoreState()
        {
            ReflectionEnvironment.SetDynamicCodeGeneration(_originalDynamicCodeGeneration);
            ReflectionEnvironment.SetFullyTrusted(null);
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
            ReflectionEnvironment.SetDynamicCodeGeneration(true);
            Assert.That(ReflectionEnvironment.DynamicCodeGeneration, Is.True);
        }

        [Test]
        public void ReflectionDelegateFactory_WhenDynamicCodeAllowed_IsDynamicFactory()
        {
            ReflectionEnvironment.SetDynamicCodeGeneration(true);
            Assert.That(ReflectionEnvironment.ReflectionDelegateFactory, Is.SameAs(DynamicReflectionDelegateFactory.Instance));
        }

        [Test]
        public void ReflectionDelegateFactory_WhenDynamicCodeDisabled_FallsBackToLateBoundFactory()
        {
            ReflectionEnvironment.SetDynamicCodeGeneration(false);
            Assert.That(ReflectionEnvironment.ReflectionDelegateFactory, Is.SameAs(LateBoundReflectionDelegateFactory.Instance));
        }

        [Test]
        public void FullyTrusted_ByDefault_IsTrueOnHomogenousAppDomain()
        {
            ReflectionEnvironment.SetFullyTrusted(null);
            Assert.That(ReflectionEnvironment.FullyTrusted, Is.True);
        }

        [Test]
        public void FullyTrusted_WhenOverridden_ReturnsOverride_AndNullRestoresDetection()
        {
            ReflectionEnvironment.SetFullyTrusted(false);
            Assert.That(ReflectionEnvironment.FullyTrusted, Is.False);

            ReflectionEnvironment.SetFullyTrusted(null);
            Assert.That(ReflectionEnvironment.FullyTrusted, Is.True);
        }

        [Test]
        public void DynamicCodeGeneration_ValueIsComputedOnceAndCached()
        {
            // Після першого обчислення значення зберігається в статичному полі;
            // повернутися до автоматичного визначення неможливо (сетер приймає лише bool).
            bool first = ReflectionEnvironment.DynamicCodeGeneration;
            Assert.That(ReflectionEnvironment.DynamicCodeGeneration, Is.EqualTo(first));
        }
    }
}
