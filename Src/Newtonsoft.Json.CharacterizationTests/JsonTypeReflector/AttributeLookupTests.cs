using System.Reflection;
using Newtonsoft.Json.Serialization;
using NUnit.Framework;

namespace Newtonsoft.Json.CharacterizationTests.JsonTypeReflectorTests
{
    /// <summary>GetAttribute&lt;T&gt; / GetCachedAttribute&lt;T&gt;: пошук атрибутів на типах, членах і інших провайдерах.</summary>
    [TestFixture]
    public class AttributeLookupTests
    {
        [Test]
        public void Type_WithOwnAttribute_ReturnsIt()
        {
            JsonObjectAttribute? attribute = JsonTypeReflector.GetAttribute<JsonObjectAttribute>(typeof(MsJsonObjectOptIn));
            Assert.That(attribute!.MemberSerialization, Is.EqualTo(MemberSerialization.OptIn));
        }

        [Test]
        public void Member_WithOwnAttribute_ReturnsIt()
        {
            PropertyInfo property = typeof(AttrImplementsInterfaceWithOwn).GetProperty(nameof(AttrImplementsInterfaceWithOwn.Value))!;
            Assert.That(JsonTypeReflector.GetAttribute<JsonPropertyAttribute>(property)!.PropertyName, Is.EqualTo("own-value"));
        }

        [Test]
        public void Member_WithoutAttribute_ReturnsNull()
        {
            PropertyInfo property = typeof(AttrPlain).GetProperty(nameof(AttrPlain.X))!;
            Assert.That(JsonTypeReflector.GetAttribute<JsonPropertyAttribute>(property), Is.Null);
        }

        [Test]
        public void GetCachedAttribute_ReturnsSameInstanceOnRepeatedCalls()
        {
            JsonObjectAttribute? first = JsonTypeReflector.GetCachedAttribute<JsonObjectAttribute>(typeof(MsJsonObjectFields));
            JsonObjectAttribute? second = JsonTypeReflector.GetCachedAttribute<JsonObjectAttribute>(typeof(MsJsonObjectFields));
            Assert.That(first, Is.Not.Null);
            Assert.That(second, Is.SameAs(first));
        }
    }
}
