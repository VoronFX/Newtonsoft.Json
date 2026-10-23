#region License
// Copyright (c) 2007 James Newton-King
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
#endregion

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Security;
#if HAVE_CAS
using System.Security.Permissions;
#endif
using Newtonsoft.Json.Utilities;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;

using System.Runtime.Serialization;

namespace Newtonsoft.Json.Serialization
{
    internal static class JsonTypeReflector
    {
        private static bool? _dynamicCodeGeneration;
        private static bool? _fullyTrusted;

        public const string IdPropertyName = "$id";
        public const string RefPropertyName = "$ref";
        public const string TypePropertyName = "$type";
        public const string ValuePropertyName = "$value";
        public const string ArrayValuesPropertyName = "$values";

        public const string ShouldSerializePrefix = "ShouldSerialize";
        public const string SpecifiedPostfix = "Specified";

        public const string ConcurrentDictionaryTypeName = "System.Collections.Concurrent.ConcurrentDictionary`2";

        [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
        [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
        private static class CreatorCache
        {
            internal static readonly ThreadSafeStore<Type, Func<object[]?, object>> Instance = 
                new ThreadSafeStore<Type, Func<object[]?, object>>(GetCreator);
        }

        [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
        [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
        public static T? GetCachedAttribute<T>(object attributeProvider) where T : Attribute
        {
            return CachedAttributeGetter<T>.GetAttribute(attributeProvider);
        }

#if HAVE_TYPE_DESCRIPTOR
        [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
        public static bool CanTypeDescriptorConvertString(Type type, out TypeConverter typeConverter)
        {
            typeConverter = TypeDescriptor.GetConverter(type);

            // use the objectType's TypeConverter if it has one and can convert to a string
            if (typeConverter != null)
            {
                Type converterType = typeConverter.GetType();
                string? converterTypeName = converterType.FullName;

                // these converters turn an object into a reference (component/data source name), not into its value
                bool isReferenceConverter = string.Equals(converterTypeName, "System.ComponentModel.ComponentConverter", StringComparison.Ordinal)
                    || string.Equals(converterTypeName, "System.ComponentModel.ReferenceConverter", StringComparison.Ordinal)
                    || string.Equals(converterTypeName, "System.Windows.Forms.Design.DataSourceConverter", StringComparison.Ordinal);
                bool isBaseTypeConverter = converterType == typeof(TypeConverter);

                if (!isReferenceConverter && !isBaseTypeConverter)
                {
                    return typeConverter.CanConvertTo(typeof(string));
                }
            }

            return false;
        }
#endif

#if HAVE_DATA_CONTRACTS
        [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
        [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
        public static DataContractAttribute? GetDataContractAttribute(Type type)
        {
            // DataContractAttribute does not have inheritance
            Type? currentType = type;

            while (currentType != null)
            {
                DataContractAttribute? result = CachedAttributeGetter<DataContractAttribute>.GetAttribute(currentType);
                if (result != null)
                {
                    return result;
                }

                currentType = currentType.BaseType();
            }

            return null;
        }

        [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
        [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
        public static DataMemberAttribute? GetDataMemberAttribute(MemberInfo memberInfo)
        {
            // DataMemberAttribute does not have inheritance

            // can't override a field
            if (memberInfo.MemberType() == MemberTypes.Field)
            {
                return CachedAttributeGetter<DataMemberAttribute>.GetAttribute(memberInfo);
            }

            // search property and then search base properties if nothing is returned and the property is virtual
            PropertyInfo propertyInfo = (PropertyInfo)memberInfo;
            DataMemberAttribute? result = CachedAttributeGetter<DataMemberAttribute>.GetAttribute(propertyInfo);
            if (result == null)
            {
                if (propertyInfo.IsVirtual())
                {
                    Type? currentType = propertyInfo.DeclaringType;

                    while (result == null && currentType != null)
                    {
                        PropertyInfo? baseProperty = (PropertyInfo?)ReflectionUtils.GetMemberInfoFromType(currentType, propertyInfo);
                        if (baseProperty != null && baseProperty.IsVirtual())
                        {
                            result = CachedAttributeGetter<DataMemberAttribute>.GetAttribute(baseProperty);
                        }

                        currentType = currentType.BaseType();
                    }
                }
            }

            return result;
        }
#endif

        [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
        [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
        public static MemberSerialization GetObjectMemberSerialization(Type objectType, bool ignoreSerializableAttribute)
        {
            JsonObjectAttribute? objectAttribute = GetCachedAttribute<JsonObjectAttribute>(objectType);
            if (objectAttribute != null)
            {
                return objectAttribute.MemberSerialization;
            }

#if HAVE_DATA_CONTRACTS
            DataContractAttribute? dataContractAttribute = GetDataContractAttribute(objectType);
            if (dataContractAttribute != null)
            {
                return MemberSerialization.OptIn;
            }
#endif

#if HAVE_BINARY_SERIALIZATION
            if (!ignoreSerializableAttribute && IsSerializable(objectType))
            {
                return MemberSerialization.Fields;
            }
#endif

            // the default
            return MemberSerialization.OptOut;
        }

        [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
        [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
        public static JsonConverter? GetJsonConverter(object attributeProvider)
        {
            JsonConverterAttribute? converterAttribute = GetCachedAttribute<JsonConverterAttribute>(attributeProvider);

            if (converterAttribute != null)
            {
                return CreateJsonConverterInstance(converterAttribute.ConverterType, converterAttribute.ConverterParameters);
            }

            return null;
        }

        /// <summary>
        /// Lookup and create an instance of the <see cref="JsonConverter"/> type described by the argument.
        /// </summary>
        /// <param name="converterType">The <see cref="JsonConverter"/> type to create.</param>
        /// <param name="args">Optional arguments to pass to an initializing constructor of the JsonConverter.
        /// If <c>null</c>, the default constructor is used.</param>
        [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
        [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
        public static JsonConverter CreateJsonConverterInstance(Type converterType, object[]? args)
        {
            Func<object[]?, object> converterCreator = CreatorCache.Instance.Get(converterType);
            return (JsonConverter)converterCreator(args);
        }

        [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
        [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
        public static NamingStrategy CreateNamingStrategyInstance(Type namingStrategyType, object[]? args)
        {
            Func<object[]?, object> converterCreator = CreatorCache.Instance.Get(namingStrategyType);
            return (NamingStrategy)converterCreator(args);
        }

        [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
        [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
        public static NamingStrategy? GetContainerNamingStrategy(JsonContainerAttribute containerAttribute)
        {
            if (containerAttribute.NamingStrategyInstance == null)
            {
                if (containerAttribute.NamingStrategyType == null)
                {
                    return null;
                }

                containerAttribute.NamingStrategyInstance = CreateNamingStrategyInstance(containerAttribute.NamingStrategyType, containerAttribute.NamingStrategyParameters);
            }

            return containerAttribute.NamingStrategyInstance;
        }

        [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
        private static Func<object[]?, object> GetCreator(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
            Type type)
        {
            Func<object>? defaultConstructor = (ReflectionUtils.HasDefaultConstructor(type, false))
                ? ReflectionDelegateFactory.CreateDefaultConstructor<object>(type)
                : null;

            return (parameters) =>
            {
                try
                {
                    if (parameters != null)
                    {
                        return CreateWithParameterizedConstructor(type, parameters);
                    }

                    if (defaultConstructor == null)
                    {
                        throw new JsonException("No parameterless constructor defined for '{0}'.".FormatWith(CultureInfo.InvariantCulture, type));
                    }

                    return defaultConstructor();
                }
                catch (Exception ex)
                {
                    throw new JsonException("Error creating '{0}'.".FormatWith(CultureInfo.InvariantCulture, type), ex);
                }
            };
        }

        [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
        private static object CreateWithParameterizedConstructor(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
            Type type,
            object[] parameters)
        {
            Type[] parameterTypes = GetConstructorParameterTypes(parameters);
            ConstructorInfo constructorInfo = type.GetConstructor(parameterTypes)
                ?? throw new JsonException("No matching parameterized constructor found for '{0}'.".FormatWith(CultureInfo.InvariantCulture, type));

            ObjectConstructor<object> parameterizedConstructor = ReflectionDelegateFactory.CreateParameterizedConstructor(constructorInfo);
            return parameterizedConstructor(parameters);
        }

        private static Type[] GetConstructorParameterTypes(object[] parameters)
        {
            Type[] parameterTypes = new Type[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                object parameter = parameters[i] ?? throw new InvalidOperationException("Cannot pass a null parameter to the constructor.");
                parameterTypes[i] = parameter.GetType();
            }

            return parameterTypes;
        }

        [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
        [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
        private static T? GetAttribute<T>([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type type) where T : Attribute
        {
            T? attribute;

#if !(NET20 || DOTNET)
            Type? metadataType = AssociatedMetadataTypeResolver.GetAssociatedMetadataType(type);
            if (metadataType != null)
            {
                attribute = ReflectionUtils.GetAttribute<T>(metadataType, true);
                if (attribute != null)
                {
                    return attribute;
                }
            }
#endif

            attribute = ReflectionUtils.GetAttribute<T>(type, true);
            if (attribute != null)
            {
                return attribute;
            }

            foreach (Type typeInterface in type.GetInterfaces())
            {
                attribute = ReflectionUtils.GetAttribute<T>(typeInterface, true);
                if (attribute != null)
                {
                    return attribute;
                }
            }

            return null;
        }

        [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
        [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
        private static T? GetAttribute<T>(MemberInfo memberInfo) where T : Attribute
        {
            T? attribute;

#if !(NET20 || DOTNET)
            Type? metadataType = AssociatedMetadataTypeResolver.GetAssociatedMetadataType(memberInfo.DeclaringType!);
            if (metadataType != null)
            {
                MemberInfo? metadataTypeMemberInfo = ReflectionUtils.GetMemberInfoFromType(metadataType, memberInfo);

                if (metadataTypeMemberInfo != null)
                {
                    attribute = ReflectionUtils.GetAttribute<T>(metadataTypeMemberInfo, true);
                    if (attribute != null)
                    {
                        return attribute;
                    }
                }
            }
#endif

            attribute = ReflectionUtils.GetAttribute<T>(memberInfo, true);
            if (attribute != null)
            {
                return attribute;
            }

            if (memberInfo.DeclaringType != null)
            {
                foreach (Type typeInterface in memberInfo.DeclaringType.GetInterfaces())
                {
                    MemberInfo? interfaceTypeMemberInfo = ReflectionUtils.GetMemberInfoFromType(typeInterface, memberInfo);

                    if (interfaceTypeMemberInfo != null)
                    {
                        attribute = ReflectionUtils.GetAttribute<T>(interfaceTypeMemberInfo, true);
                        if (attribute != null)
                        {
                            return attribute;
                        }
                    }
                }
            }

            return null;
        }

#if HAVE_NON_SERIALIZED_ATTRIBUTE
        public static bool IsNonSerializable(object provider)
        {
#if HAVE_FULL_REFLECTION
            // no inheritance
            return (ReflectionUtils.GetAttribute<NonSerializedAttribute>(provider, false) != null);
#else
            if (provider is FieldInfo fieldInfo && (fieldInfo.Attributes & FieldAttributes.NotSerialized) == FieldAttributes.NotSerialized)
            {
                return true;
            }

            return false;
#endif
        }
#endif

#if HAVE_BINARY_SERIALIZATION
        public static bool IsSerializable(object provider)
        {
#if HAVE_FULL_REFLECTION
            // no inheritance
            return (ReflectionUtils.GetAttribute<SerializableAttribute>(provider, false) != null);
#else
            if (provider is Type type && (type.GetTypeInfo().Attributes & TypeAttributes.Serializable) == TypeAttributes.Serializable)
            {
                return true;
            }

            return false;
#endif
        }
#endif

        [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
        [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
        public static T? GetAttribute<T>(object provider) where T : Attribute
        {
            if (provider is Type type)
            {
                return GetAttribute<T>(type);
            }

            if (provider is MemberInfo memberInfo)
            {
                return GetAttribute<T>(memberInfo);
            }

            return ReflectionUtils.GetAttribute<T>(provider, true);
        }

#if DEBUG
        internal static void SetFullyTrusted(bool? fullyTrusted)
        {
            _fullyTrusted = fullyTrusted;
        }

        internal static void SetDynamicCodeGeneration(bool dynamicCodeGeneration)
        {
            _dynamicCodeGeneration = dynamicCodeGeneration;
        }
#endif

        public static bool DynamicCodeGeneration
        {
#if HAVE_SECURITY_SAFE_CRITICAL_ATTRIBUTE
            [SecuritySafeCritical]
#endif
            get
            {
                if (_dynamicCodeGeneration == null)
                {
#if HAVE_DYNAMIC_CODE_COMPILED
                    _dynamicCodeGeneration = RuntimeFeature.IsDynamicCodeCompiled;
#elif HAVE_CAS
                    try
                    {
                        new ReflectionPermission(ReflectionPermissionFlag.MemberAccess).Demand();
                        new ReflectionPermission(ReflectionPermissionFlag.RestrictedMemberAccess).Demand();
                        new SecurityPermission(SecurityPermissionFlag.SkipVerification).Demand();
                        new SecurityPermission(SecurityPermissionFlag.UnmanagedCode).Demand();
                        new SecurityPermission(PermissionState.Unrestricted).Demand();
                        _dynamicCodeGeneration = true;
                    }
                    catch (Exception)
                    {
                        _dynamicCodeGeneration = false;
                    }
#else
                    _dynamicCodeGeneration = false;
#endif
                }

                return _dynamicCodeGeneration.GetValueOrDefault();
            }
        }

        public static bool FullyTrusted
        {
            get
            {
                if (_fullyTrusted == null)
                {
#if (DOTNET || PORTABLE || PORTABLE40)
                    _fullyTrusted = true;
#elif !(NET20 || NET35 || PORTABLE40)
                    AppDomain appDomain = AppDomain.CurrentDomain;

                    _fullyTrusted = appDomain.IsHomogenous && appDomain.IsFullyTrusted;
#else
                    try
                    {
                        new SecurityPermission(PermissionState.Unrestricted).Demand();
                        _fullyTrusted = true;
                    }
                    catch (Exception)
                    {
                        _fullyTrusted = false;
                    }
#endif
                }

                return _fullyTrusted.GetValueOrDefault();
            }
        }

        public static ReflectionDelegateFactory ReflectionDelegateFactory
        {
            get
            {
#if !(PORTABLE40 || PORTABLE || DOTNET || NETSTANDARD2_0)
                if (DynamicCodeGeneration)
                {
                    return DynamicReflectionDelegateFactory.Instance;
                }

                return LateBoundReflectionDelegateFactory.Instance;
#else
                return ExpressionReflectionDelegateFactory.Instance;
#endif
            }
        }
    }
}