// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace System.Text.Json.Serialization.Metadata
{
    internal abstract class MemberAccessor
    {
        public abstract Func<object>? CreateConstructor(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type classType);

        public abstract Func<object[], T> CreateParameterizedConstructor<T>(ConstructorInfo constructor);

        public abstract JsonTypeInfo.ParameterizedConstructorDelegate<T, TArg0, TArg1, TArg2, TArg3>?
            CreateParameterizedConstructor<T, TArg0, TArg1, TArg2, TArg3>(ConstructorInfo constructor);

        public abstract Action<TCollection, object?> CreateAddMethodDelegate<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] TCollection>();

        [RequiresUnreferencedCode(IEnumerableConverterFactoryHelpers.ImmutableConvertersUnreferencedCodeMessage)]
        [RequiresDynamicCode(IEnumerableConverterFactoryHelpers.ImmutableConvertersUnreferencedCodeMessage)]
        public abstract Func<IEnumerable<TElement>, TCollection> CreateImmutableEnumerableCreateRangeDelegate<TCollection, TElement>();

        [RequiresUnreferencedCode(IEnumerableConverterFactoryHelpers.ImmutableConvertersUnreferencedCodeMessage)]
        [RequiresDynamicCode(IEnumerableConverterFactoryHelpers.ImmutableConvertersUnreferencedCodeMessage)]
        public abstract Func<IEnumerable<KeyValuePair<TKey, TValue>>, TCollection> CreateImmutableDictionaryCreateRangeDelegate<TCollection, TKey, TValue>();

        public abstract JsonPropertyInfo<TProperty>.StjGetter CreatePropertyGetter<TProperty>(PropertyInfo propertyInfo);

        public abstract JsonPropertyInfo<TProperty>.StjSetter CreatePropertySetter<TProperty>(PropertyInfo propertyInfo);

        public abstract JsonPropertyInfo<TProperty>.StjGetter CreateFieldGetter<TProperty>(FieldInfo fieldInfo);

        public abstract JsonPropertyInfo<TProperty>.StjSetter CreateFieldSetter<TProperty>(FieldInfo fieldInfo);
    }
}
