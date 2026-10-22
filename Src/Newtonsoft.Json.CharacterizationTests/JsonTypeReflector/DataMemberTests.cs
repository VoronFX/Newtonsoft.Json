using System;
using System.Reflection;
using System.Runtime.Serialization;
using Newtonsoft.Json.Serialization;
using NUnit.Framework;

namespace Newtonsoft.Json.CharacterizationTests.JsonTypeReflectorTests
{
    /// <summary>GetDataContractAttribute (пошук у базових класах) і GetDataMemberAttribute (пошук у базових властивостях).</summary>
    [TestFixture]
    public class DataMemberTests
    {
        private static MemberInfo Member(Type t, string name) => t.GetMember(name)[0];

        [Test]
        public void DataContract_IsSearchedInBaseClasses()
        {
            Assert.That(JsonTypeReflector.GetDataContractAttribute(typeof(DcSecondLevel))!.Name, Is.EqualTo("base-contract"));
        }

        [Test]
        public void DataContract_OwnAttributeWinsOverBase()
        {
            Assert.That(JsonTypeReflector.GetDataContractAttribute(typeof(DcDerivedWithOwnContract))!.Name, Is.EqualTo("own-contract"));
        }

        [Test]
        public void DataContract_NoneInHierarchy_ReturnsNull()
        {
            Assert.That(JsonTypeReflector.GetDataContractAttribute(typeof(DcNone)), Is.Null);
        }

        [Test]
        public void DataMember_OnField_IsReturned()
        {
            Assert.That(JsonTypeReflector.GetDataMemberAttribute(Member(typeof(DcBase), nameof(DcBase.Field)))!.Name, Is.EqualTo("field"));
        }

        [Test]
        public void DataMember_OnProperty_IsReturned()
        {
            Assert.That(JsonTypeReflector.GetDataMemberAttribute(Member(typeof(DcBase), nameof(DcBase.Prop)))!.Name, Is.EqualTo("prop"));
        }

        [Test]
        public void DataMember_InheritedField_IsReturned()
        {
            // поле не можна перевизначити, тож береться атрибут поля базового класу напряму
            Assert.That(JsonTypeReflector.GetDataMemberAttribute(typeof(DcDerived).GetField(nameof(DcBase.Field))!)!.Name, Is.EqualTo("field"));
        }

        [Test]
        public void DataMember_VirtualOverride_TakesAttributeFromBaseProperty()
        {
            Assert.That(JsonTypeReflector.GetDataMemberAttribute(typeof(DcDerived).GetProperty(nameof(DcBase.Virtual))!)!.Name, Is.EqualTo("virtual"));
        }

        [Test]
        public void DataMember_VirtualOverrideTwoLevelsDeep_TakesAttributeFromRoot()
        {
            Assert.That(JsonTypeReflector.GetDataMemberAttribute(typeof(DcSecondLevel).GetProperty(nameof(DcBase.Virtual))!)!.Name, Is.EqualTo("virtual"));
        }

        [Test]
        public void DataMember_PropertyHiddenWithNew_DoesNotInheritAttribute()
        {
            PropertyInfo hidden = typeof(DcDerived).GetProperty(nameof(DcDerived.Hidden), BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)!;
            Assert.That(JsonTypeReflector.GetDataMemberAttribute(hidden), Is.Null);
        }

        [Test]
        public void DataMember_VirtualWithoutAnyAttribute_ReturnsNull()
        {
            Assert.That(JsonTypeReflector.GetDataMemberAttribute(typeof(DcNone).GetProperty(nameof(DcNone.Virtual))!), Is.Null);
        }

        [Test]
        public void DataMember_PropertyWithoutAttribute_ReturnsNull()
        {
            Assert.That(JsonTypeReflector.GetDataMemberAttribute(Member(typeof(DcBase), nameof(DcBase.NoAttribute))), Is.Null);
        }

        [Test]
        [Category("Suspicious")]
        public void DataMember_MethodInfo_ThrowsInvalidCastException()
        {
            // ХАРАКТЕРИЗАЦІЯ: підозріло — будь-який член, що не є полем, без перевірки приводиться до PropertyInfo.
            Assert.Throws<InvalidCastException>(() => JsonTypeReflector.GetDataMemberAttribute(Member(typeof(DcBase), nameof(DcBase.Method))));
        }

        [Test]
        public void DataMember_Null_ThrowsNullReferenceException()
        {
            Assert.Throws<NullReferenceException>(() => JsonTypeReflector.GetDataMemberAttribute(null!));
        }
    }
}
