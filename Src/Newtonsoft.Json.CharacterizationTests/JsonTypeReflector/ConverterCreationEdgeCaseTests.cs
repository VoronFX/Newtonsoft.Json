using System;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Utilities;
using NUnit.Framework;

namespace Newtonsoft.Json.CharacterizationTests.JsonTypeReflectorTests
{
    /// <summary>Межові випадки та шляхи помилок при створенні конвертерів (GetCreator).</summary>
    [TestFixture]
    public class ConverterCreationEdgeCaseTests
    {
        [Test]
        public void EmptyArgsArray_IsTreatedAsParameterizedCall_AndMatchesDefaultConstructor()
        {
            // new object[0] не дорівнює null, тому йде гілка пошуку конструктора за типами (Type[0]) —
            // знаходиться конструктор без параметрів.
            var converter = (ConvWithOverloads)JsonTypeReflector.CreateJsonConverterInstance(typeof(ConvWithOverloads), new object[0]);
            Assert.That(converter.Info, Is.EqualTo("default"));
        }

        [Test]
        [Category("Suspicious")]
        public void IntArgument_SelectsLongConstructor_InsteadOfObjectConstructor()
        {
            // ХАРАКТЕРИЗАЦІЯ: підозріло — конструктор обирається Type.GetConstructor(Type[]) за типами
            // фактичних значень; стандартний binder виконує розширювальне перетворення int → long.
            var converter = (ConvWithOverloads)JsonTypeReflector.CreateJsonConverterInstance(typeof(ConvWithOverloads), new object[] { 5 });
            Assert.That(converter.Info, Is.EqualTo("long:5"));
        }

        [Test]
        public void ArgumentOfDerivedType_MatchesObjectParameter()
        {
            var converter = (ConvWithOverloads)JsonTypeReflector.CreateJsonConverterInstance(typeof(ConvWithOverloads), new object[] { new Version(1, 2) });
            Assert.That(converter.Info, Is.EqualTo("object:1.2"));
        }

        [Test]
        [Category("Suspicious")]
        public void NullArgument_IsRejected_WithWrappedInvalidOperationException()
        {
            // ХАРАКТЕРИЗАЦІЯ: підозріло — null неможливо передати навіть у параметр типу object,
            // бо тип визначається через param.GetType(); виняток загортається в загальний JsonException.
            var ex = Assert.Throws<JsonException>(() => JsonTypeReflector.CreateJsonConverterInstance(typeof(ConvWithOverloads), new object?[] { null }!));
            Assert.That(ex!.Message, Is.EqualTo("Error creating '" + typeof(ConvWithOverloads) + "'."));
            Assert.That(ex.InnerException, Is.InstanceOf<InvalidOperationException>());
            Assert.That(ex.InnerException!.Message, Is.EqualTo("Cannot pass a null parameter to the constructor."));
        }

        [Test]
        [Category("Suspicious")]
        public void NoMatchingConstructor_ThrowsJsonExceptionWrappedInAnotherJsonException()
        {
            // ХАРАКТЕРИЗАЦІЯ: підозріло — JsonException("No matching ...") кидається всередині try
            // і перехоплюється тим самим catch, тож причину видно лише у InnerException.
            var ex = Assert.Throws<JsonException>(() => JsonTypeReflector.CreateJsonConverterInstance(typeof(ConvStringIntOnly), new object[] { 1.5 }));
            Assert.That(ex!.Message, Is.EqualTo("Error creating '" + typeof(ConvStringIntOnly) + "'."));
            Assert.That(ex.InnerException, Is.TypeOf<JsonException>());
            Assert.That(ex.InnerException!.Message, Is.EqualTo("No matching parameterized constructor found for '" + typeof(ConvStringIntOnly) + "'."));
        }

        [Test]
        public void ArgumentsInWrongOrder_DoNotMatch()
        {
            var ex = Assert.Throws<JsonException>(() => JsonTypeReflector.CreateJsonConverterInstance(typeof(ConvStringIntOnly), new object[] { 1, "x" }));
            Assert.That(ex!.InnerException!.Message, Does.StartWith("No matching parameterized constructor"));
        }

        [Test]
        public void NoDefaultConstructor_AndNullArgs_Throws()
        {
            var ex = Assert.Throws<JsonException>(() => JsonTypeReflector.CreateJsonConverterInstance(typeof(ConvNoDefaultCtor), null));
            Assert.That(ex!.InnerException!.Message, Is.EqualTo("No parameterless constructor defined for '" + typeof(ConvNoDefaultCtor) + "'."));
        }

        [Test]
        [Category("Suspicious")]
        public void PrivateParameterlessConstructor_IsIgnored_ErrorSaysNoneDefined()
        {
            // ХАРАКТЕРИЗАЦІЯ: підозріло — HasDefaultConstructor(type, nonPublic: false) не бачить приватний
            // конструктор, але повідомлення стверджує, що конструктора без параметрів немає взагалі.
            var ex = Assert.Throws<JsonException>(() => JsonTypeReflector.CreateJsonConverterInstance(typeof(ConvPrivateDefaultCtor), null));
            Assert.That(ex!.InnerException!.Message, Is.EqualTo("No parameterless constructor defined for '" + typeof(ConvPrivateDefaultCtor) + "'."));
        }

        [Test]
        public void AbstractConverterType_ReportedAsNoParameterlessConstructor()
        {
            var ex = Assert.Throws<JsonException>(() => JsonTypeReflector.CreateJsonConverterInstance(typeof(JsonConverter), null));
            Assert.That(ex!.InnerException!.Message, Does.StartWith("No parameterless constructor defined"));
        }

        [Test]
        public void ConstructorThrows_ExceptionIsWrappedWithOriginalAsInner()
        {
            var ex = Assert.Throws<JsonException>(() => JsonTypeReflector.CreateJsonConverterInstance(typeof(ConvThrowingCtor), null));
            Assert.That(ex!.Message, Is.EqualTo("Error creating '" + typeof(ConvThrowingCtor) + "'."));
            Assert.That(ex.InnerException, Is.TypeOf<InvalidOperationException>().With.Message.EqualTo("boom"));
        }

        [Test]
        [Category("Suspicious")]
        public void TypeThatIsNotAConverter_ThrowsRawInvalidCastException()
        {
            // ХАРАКТЕРИЗАЦІЯ: підозріло — приведення до JsonConverter виконується поза try/catch,
            // тому замість JsonException користувач отримує «сирий» InvalidCastException.
            Assert.Throws<InvalidCastException>(() => JsonTypeReflector.CreateJsonConverterInstance(typeof(object), null));
            Assert.Throws<InvalidCastException>(() => JsonTypeReflector.GetJsonConverter(typeof(ConvAttributeNotAConverter)));
        }

        [Test]
        public void ParamsArrayConstructor_RequiresExplicitArray()
        {
            // params не розгортається: два рядки не відповідають сигнатурі (string[]) …
            var ex = Assert.Throws<JsonException>(() => JsonTypeReflector.CreateJsonConverterInstance(typeof(ConvParamsArray), new object[] { "a", "b" }));
            Assert.That(ex!.InnerException!.Message, Does.StartWith("No matching parameterized constructor"));

            // … а один аргумент-масив — відповідає.
            var converter = (ConvParamsArray)JsonTypeReflector.CreateJsonConverterInstance(typeof(ConvParamsArray), new object[] { new[] { "a", "b" } });
            Assert.That(converter.Count, Is.EqualTo(2));
        }

        [Test]
        public void NullConverterType_ThrowsArgumentNullExceptionFromCache()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => JsonTypeReflector.CreateJsonConverterInstance(null!, null));
            Assert.That(ex!.ParamName, Is.EqualTo("key"));
        }

        [Test]
        public void GetJsonConverter_AttributeWithUnconstructibleConverter_ThrowsJsonException()
        {
            var ex = Assert.Throws<JsonException>(() => JsonTypeReflector.GetJsonConverter(typeof(ConvAttributeNoDefaultCtor)));
            Assert.That(ex!.Message, Does.StartWith("Error creating"));
        }

        [Test]
        public void GetJsonConverter_NonReflectionProvider_ThrowsInvalidCastException()
        {
            Assert.Throws<InvalidCastException>(() => JsonTypeReflector.GetJsonConverter("not a provider"));
        }

        [Test]
        public void WorksIdenticallyWithLateBoundFactory()
        {
            bool original = ReflectionEnvironment.DynamicCodeGeneration;
            try
            {
                ReflectionEnvironment.SetDynamicCodeGeneration(false);
                // делегат із CreatorCache звертається до ReflectionDelegateFactory під час КОЖНОГО виклику
                // з параметрами, тож параметризований конструктор тут створює вже LateBound-фабрика
                var converter = (ConvParamsArray)JsonTypeReflector.CreateJsonConverterInstance(typeof(ConvParamsArray), new object[] { new[] { "x" } });
                Assert.That(converter.Count, Is.EqualTo(1));
            }
            finally
            {
                ReflectionEnvironment.SetDynamicCodeGeneration(original);
            }
        }
    }
}
