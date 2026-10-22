using System;
using System.Reflection;
using System.Reflection.Emit;
using Newtonsoft.Json.Serialization;
using NUnit.Framework;

namespace Newtonsoft.Json.CharacterizationTests.JsonTypeReflectorTests
{
    /// <summary>Пошук атрибутів через інтерфейси, базові класи та MetadataTypeAttribute; некоректні провайдери.</summary>
    [TestFixture]
    public class AttributeLookupEdgeCaseTests
    {
        [Test]
        public void Type_AttributeOnImplementedInterface_IsFound()
        {
            Assert.That(JsonTypeReflector.GetAttribute<JsonObjectAttribute>(typeof(AttrImplementsInterface))!.Title, Is.EqualTo("from-interface"));
        }

        [Test]
        public void Type_OwnAttribute_WinsOverInterface()
        {
            Assert.That(JsonTypeReflector.GetAttribute<JsonObjectAttribute>(typeof(AttrImplementsInterfaceWithOwn))!.Title, Is.EqualTo("own"));
        }

        [Test]
        public void Member_AttributeOnInterfaceMember_IsFound()
        {
            PropertyInfo property = typeof(AttrImplementsInterface).GetProperty(nameof(AttrImplementsInterface.Value))!;
            Assert.That(JsonTypeReflector.GetAttribute<JsonPropertyAttribute>(property)!.PropertyName, Is.EqualTo("iface-value"));
        }

        [Test]
        public void Member_InterfaceMemberWithoutAttribute_ReturnsNull()
        {
            PropertyInfo property = typeof(AttrImplementsInterface).GetProperty(nameof(AttrImplementsInterface.NoAttribute))!;
            Assert.That(JsonTypeReflector.GetAttribute<JsonPropertyAttribute>(property), Is.Null);
        }

        [Test]
        public void Member_OverrideWithoutAttribute_InheritsAttributeFromBaseProperty()
        {
            PropertyInfo property = typeof(AttrDerivedOverride).GetProperty(nameof(AttrDerivedOverride.X))!;
            Assert.That(JsonTypeReflector.GetAttribute<JsonPropertyAttribute>(property)!.PropertyName, Is.EqualTo("base-x"));
        }

        [Test]
        public void Type_MetadataTypeBuddyAttribute_IsFound()
        {
            Assert.That(JsonTypeReflector.GetAttribute<JsonObjectAttribute>(typeof(AttrWithMetadata))!.Title, Is.EqualTo("metadata-title"));
        }

        [Test]
        public void Member_MetadataTypeBuddyMemberAttribute_IsFound()
        {
            PropertyInfo property = typeof(AttrWithMetadata).GetProperty(nameof(AttrWithMetadata.Name))!;
            Assert.That(JsonTypeReflector.GetAttribute<JsonPropertyAttribute>(property)!.PropertyName, Is.EqualTo("metadata-name"));
        }

        [Test]
        public void Member_NotPresentInMetadataBuddy_FallsBackToOwnAttributes()
        {
            PropertyInfo property = typeof(AttrWithMetadata).GetProperty(nameof(AttrWithMetadata.NotInBuddy))!;
            Assert.That(JsonTypeReflector.GetAttribute<JsonPropertyAttribute>(property), Is.Null);
        }

        [Test]
        [Category("Suspicious")]
        public void MetadataBuddyAttributes_TakePriorityOverTypesOwnAttributes()
        {
            // ХАРАКТЕРИЗАЦІЯ: підозріло — атрибут із класу-метаданих перекриває атрибут, явно вказаний
            // на самому типі (і на його властивості). Очікувалося б навпаки або злиття атрибутів.
            Assert.That(JsonTypeReflector.GetAttribute<JsonObjectAttribute>(typeof(AttrWithMetadataAndOwn))!.Title, Is.EqualTo("metadata-title"));
            PropertyInfo property = typeof(AttrWithMetadataAndOwn).GetProperty(nameof(AttrWithMetadataAndOwn.Name))!;
            Assert.That(JsonTypeReflector.GetAttribute<JsonPropertyAttribute>(property)!.PropertyName, Is.EqualTo("metadata-name"));
        }

        [Test]
        public void EmptyMetadataBuddy_FallsBackToOwnAttributes()
        {
            Assert.That(JsonTypeReflector.GetAttribute<JsonObjectAttribute>(typeof(AttrWithEmptyMetadata))!.Title, Is.EqualTo("own-only"));
            PropertyInfo property = typeof(AttrWithEmptyMetadata).GetProperty(nameof(AttrWithEmptyMetadata.Name))!;
            Assert.That(JsonTypeReflector.GetAttribute<JsonPropertyAttribute>(property)!.PropertyName, Is.EqualTo("own-name"));
        }

        [Test]
        public void Member_PresentInBuddyWithoutAttribute_FallsBackToOwnAttribute()
        {
            PropertyInfo property = typeof(AttrWithMetadataPartial).GetProperty(nameof(AttrWithMetadataPartial.Other))!;
            Assert.That(JsonTypeReflector.GetAttribute<JsonPropertyAttribute>(property)!.PropertyName, Is.EqualTo("own-other"));
        }

        [Test]
        [Category("Suspicious")]
        public void MetadataTypeAttribute_MatchedByName_ButAccessorIsCachedForFirstType()
        {
            // ХАРАКТЕРИЗАЦІЯ: підозріло — можливий дефект. Атрибут розпізнається за рядком повного імені
            // ("через type forwarding"), але ReflectionObject для читання MetadataClassType зберігається
            // в статичному полі для ПЕРШОГО знайденого типу атрибута. Інший тип з тим самим іменем
            // (інша збірка) далі ламається з InvalidCastException.
            // 1) спершу справжній System.ComponentModel.DataAnnotations.MetadataTypeAttribute
            Assert.That(JsonTypeReflector.GetAttribute<JsonObjectAttribute>(typeof(AttrWithMetadataForSpoofTest))!.Title, Is.EqualTo("metadata-title"));

            // 2) потім тип з тим самим повним іменем, згенерований у динамічній збірці
            Type spoofed = SpoofedMetadataFactory.CreateTypeWithSpoofedMetadata(typeof(AttrMetadataBuddy));
            var ex = Assert.Throws<InvalidCastException>(() => JsonTypeReflector.GetAttribute<JsonObjectAttribute>(spoofed));
            Assert.That(ex!.Message, Does.Contain("System.ComponentModel.DataAnnotations.MetadataTypeAttribute cannot be cast to"));
        }

        [Test]
        public void ParameterInfoProvider_UsesDirectReflectionLookup()
        {
            ParameterInfo parameter = typeof(AttrCtorParameter).GetConstructors()[0].GetParameters()[0];
            Assert.That(JsonTypeReflector.GetAttribute<JsonPropertyAttribute>(parameter)!.PropertyName, Is.EqualTo("param-name"));
        }

        [Test]
        public void AssemblyProvider_UsesDirectReflectionLookup()
        {
            Assembly assembly = typeof(AttributeLookupEdgeCaseTests).Assembly;
            Assert.That(JsonTypeReflector.GetAttribute<AssemblyProductAttribute>(assembly), Is.Not.Null);
        }

        [Test]
        public void NonReflectionProvider_ThrowsInvalidCastException()
        {
            Assert.Throws<InvalidCastException>(() => JsonTypeReflector.GetAttribute<JsonObjectAttribute>("just a string"));
        }

        [Test]
        public void NullProvider_ThrowsArgumentNullException()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => JsonTypeReflector.GetAttribute<JsonObjectAttribute>(null!));
            Assert.That(ex!.ParamName, Is.EqualTo("attributeProvider"));
        }
    }

    /// <summary>Будує в пам'яті тип, позначений «підробленим» MetadataTypeAttribute з чужої збірки.</summary>
    internal static class SpoofedMetadataFactory
    {
        public static Type CreateTypeWithSpoofedMetadata(Type buddy)
        {
            var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("SpoofedMetadata" + Guid.NewGuid().ToString("N")), AssemblyBuilderAccess.Run);
            ModuleBuilder module = assembly.DefineDynamicModule("Main");

            // public sealed class System.ComponentModel.DataAnnotations.MetadataTypeAttribute : Attribute
            TypeBuilder attr = module.DefineType("System.ComponentModel.DataAnnotations.MetadataTypeAttribute",
                TypeAttributes.Public | TypeAttributes.Sealed, typeof(Attribute));
            FieldBuilder field = attr.DefineField("_type", typeof(Type), FieldAttributes.Private);
            ConstructorBuilder ctor = attr.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new[] { typeof(Type) });
            ILGenerator il = ctor.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, typeof(Attribute).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null)!);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Stfld, field);
            il.Emit(OpCodes.Ret);
            PropertyBuilder prop = attr.DefineProperty("MetadataClassType", PropertyAttributes.None, typeof(Type), null);
            MethodBuilder getter = attr.DefineMethod("get_MetadataClassType", MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, typeof(Type), Type.EmptyTypes);
            ILGenerator gil = getter.GetILGenerator();
            gil.Emit(OpCodes.Ldarg_0);
            gil.Emit(OpCodes.Ldfld, field);
            gil.Emit(OpCodes.Ret);
            prop.SetGetMethod(getter);
            Type attrType = attr.CreateType()!;

            TypeBuilder target = module.DefineType("SpoofedTarget", TypeAttributes.Public | TypeAttributes.Class);
            target.SetCustomAttribute(new CustomAttributeBuilder(attrType.GetConstructor(new[] { typeof(Type) })!, new object[] { buddy }));
            return target.CreateType()!;
        }
    }
}
