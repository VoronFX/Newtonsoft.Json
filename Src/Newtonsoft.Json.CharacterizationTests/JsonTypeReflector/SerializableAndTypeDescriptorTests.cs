using System;
using System.ComponentModel;
using Newtonsoft.Json.Serialization;
using NUnit.Framework;
using CategoryAttribute = NUnit.Framework.CategoryAttribute;

namespace Newtonsoft.Json.CharacterizationTests.JsonTypeReflectorTests
{
    /// <summary>IsSerializable / IsNonSerializable / CanTypeDescriptorConvertString.</summary>
    [TestFixture]
    public class SerializableAndTypeDescriptorTests
    {
        [Test]
        public void IsSerializable_MarkedType_True()
        {
            Assert.That(JsonTypeReflector.IsSerializable(typeof(SerMarked)), Is.True);
        }

        [Test]
        public void IsSerializable_UnmarkedType_False()
        {
            Assert.That(JsonTypeReflector.IsSerializable(typeof(SerPlain)), Is.False);
        }

        [Test]
        public void IsSerializable_DerivedFromMarkedType_False_NoInheritance()
        {
            Assert.That(JsonTypeReflector.IsSerializable(typeof(SerDerived)), Is.False);
        }

        [Test]
        public void IsNonSerializable_MarkedField_True_UnmarkedField_False()
        {
            Assert.That(JsonTypeReflector.IsNonSerializable(typeof(SerMarked).GetField(nameof(SerMarked.Skipped))!), Is.True);
            Assert.That(JsonTypeReflector.IsNonSerializable(typeof(SerMarked).GetField(nameof(SerMarked.Kept))!), Is.False);
        }

        [Test]
        public void IsSerializable_InvalidProviders_ThrowDifferentExceptions()
        {
            Assert.Throws<InvalidCastException>(() => JsonTypeReflector.IsSerializable("string"));
            Assert.Throws<ArgumentNullException>(() => JsonTypeReflector.IsSerializable(null!));
            Assert.Throws<ArgumentNullException>(() => JsonTypeReflector.IsNonSerializable(null!));
        }

        [TestCase(typeof(int), true, "Int32Converter")]
        [TestCase(typeof(string), true, "StringConverter")]
        [TestCase(typeof(Guid), true, "GuidConverter")]
        [TestCase(typeof(DateTime), true, "DateTimeConverter")]
        [TestCase(typeof(TimeSpan), true, "TimeSpanConverter")]
        [TestCase(typeof(Version), true, "VersionConverter")]
        [TestCase(typeof(Uri), true, "UriTypeConverter")]
        [TestCase(typeof(DayOfWeek), true, "EnumConverter")]
        [TestCase(typeof(int?), true, "NullableConverter")]
        [TestCase(typeof(object), false, "TypeConverter")]
        [TestCase(typeof(TdPlain), false, "TypeConverter")]
        public void CanTypeDescriptorConvertString_StandardTypes(Type type, bool expected, string converterName)
        {
            bool result = JsonTypeReflector.CanTypeDescriptorConvertString(type, out TypeConverter converter);
            Assert.That(result, Is.EqualTo(expected));
            Assert.That(converter.GetType().Name, Is.EqualTo(converterName));
        }

        [Test]
        public void CanTypeDescriptorConvertString_ComponentConverterIsExcluded()
        {
            Assert.That(JsonTypeReflector.CanTypeDescriptorConvertString(typeof(Component), out TypeConverter converter), Is.False);
            Assert.That(converter.GetType().Name, Is.EqualTo("ComponentConverter"));
        }

        [Test]
        public void CanTypeDescriptorConvertString_CustomConverters()
        {
            Assert.That(JsonTypeReflector.CanTypeDescriptorConvertString(typeof(TdWithStringConverter), out _), Is.True);
            Assert.That(JsonTypeReflector.CanTypeDescriptorConvertString(typeof(TdWithNonStringConverter), out _), Is.False);
            Assert.That(JsonTypeReflector.CanTypeDescriptorConvertString(typeof(TdWithBaseTypeConverter), out _), Is.False);
        }

        [Test]
        [Category("Suspicious")]
        public void CanTypeDescriptorConvertString_ByteArray_IsReportedConvertible()
        {
            // ХАРАКТЕРИЗАЦІЯ: підозріло — ArrayConverter «уміє» перетворювати в рядок, але результатом є
            // назва типу, а не вміст масиву; метод повертає true без застережень.
            Assert.That(JsonTypeReflector.CanTypeDescriptorConvertString(typeof(byte[]), out TypeConverter converter), Is.True);
            Assert.That(converter.ConvertToInvariantString(new byte[] { 1, 2, 3 }), Is.EqualTo("Byte[] Array"));
        }
    }
}
