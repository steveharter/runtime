// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if BUILDING_INBOX_LIBRARY

using System.Linq.Expressions;
using System.Dynamic;
using System.Reflection;
using System.Text.Json.Serialization.Converters;

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// todo
    /// </summary>
    public sealed class JsonDynamicObject : JsonObject, IJsonDynamicMetaObjectProvider
    {
        internal JsonDynamicObject(JsonElement jsonElement,
            JsonNodeConverterBase converter,
            JsonSerializerOptions? options = null) : base(jsonElement, converter, options) { }

        /// <summary>
        /// todo
        /// </summary>
        /// <param name="options"></param>
        public JsonDynamicObject(JsonSerializerOptions? options = null) : base(options)
        {
            _nodeConverter = JsonDynamicNodeConverterFactory.s_NodeConverter;
        }

        internal bool TrySetIndexCallback(SetIndexBinder binder, object[] indexes, object? value)
        {
            Dictionary[((string)indexes[0])] = Convert(value);
            return true;
        }

        internal bool TryGetMemberCallback(GetMemberBinder binder, out object? result)
        {
            if (Dictionary.TryGetValue(binder.Name, out JsonNode? node))
            {
                result = node;
                return true;
            }

            // Return null for missing properties.
            result = null;
            return true;
        }

        internal bool TrySetMemberCallback(SetMemberBinder binder, object? value)
        {
            JsonNode? node = null;
            if (value != null)
            {
                node = value as JsonNode;
                if (node == null)
                {
                    node = new JsonDynamicValue(value, Options);
                }
            }

            Dictionary[binder.Name] = node;
            return true;
        }

        DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter) =>
            new MetaDynamic(parameter, this);

        private static MethodInfo GetMethod(string name) => typeof(JsonDynamicObject).GetMethod(
            name, BindingFlags.Instance | BindingFlags.NonPublic)!;

        MethodInfo? IJsonDynamicMetaObjectProvider.TryGetIndexMethodInfo => null;

        private static MethodInfo? s_TrySetIndex;
        MethodInfo IJsonDynamicMetaObjectProvider.TrySetIndexMethodInfo =>
            s_TrySetIndex ??
            (s_TrySetIndex = GetMethod(nameof(TrySetIndexCallback)));

        private static MethodInfo? s_TryGetMember;
        MethodInfo IJsonDynamicMetaObjectProvider.TryGetMemberMethodInfo =>
            s_TryGetMember ??
            (s_TryGetMember = GetMethod(nameof(TryGetMemberCallback)));

        private static MethodInfo? s_TrySetMember;
        MethodInfo IJsonDynamicMetaObjectProvider.TrySetMemberMethodInfo =>
            s_TrySetMember ??
            (s_TrySetMember = GetMethod(nameof(TrySetMemberCallback)));

        MethodInfo? IJsonDynamicMetaObjectProvider.TryConvertMethodInfo => null;
    }
}
#endif
