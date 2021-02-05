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
    public sealed class JsonDynamicArray : JsonArray, IJsonDynamicMetaObjectProvider
    {
        internal JsonDynamicArray(JsonElement jsonElement,
            JsonNodeConverterBase converter,
            JsonSerializerOptions? options = null) : base(jsonElement, converter, options) { }

        /// <summary>
        /// todo
        /// </summary>
        /// <param name="options"></param>
        public JsonDynamicArray(JsonSerializerOptions? options = null) : base(options)
        {
            _converter = JsonDynamicNodeConverterFactory.s_NodeConverter;
        }

        /// <summary>
        /// todo
        /// </summary>
        /// <param name="options"></param>
        /// <param name="items"></param>
        public JsonDynamicArray(JsonSerializerOptions options, params JsonNode[] items) : base(options, items)
        {
            _converter = JsonNodeConverterFactory.s_NodeConverter;
        }

        /// <summary>
        /// todo
        /// </summary>
        /// <param name="items"></param>
        public JsonDynamicArray(params JsonNode[] items) : base(items)
        {
            _converter = JsonNodeConverterFactory.s_NodeConverter;
        }

        internal bool TryGetIndexCallback(GetIndexBinder binder, object[] indexes, out object? result)
        {
            result = List[(int)indexes[0]];
            return true;
        }

        internal bool TrySetIndexCallback(SetIndexBinder binder, object[] indexes, object? value)
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

            List[(int)indexes[0]] = node;
            return true;
        }

        DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter) =>
            new MetaDynamic(parameter, this);

        private static MethodInfo GetMethod(string name) => typeof(JsonDynamicArray).GetMethod(
            name, BindingFlags.Instance | BindingFlags.NonPublic)!;

        private static MethodInfo? s_TryGetIndex;
        MethodInfo IJsonDynamicMetaObjectProvider.TryGetIndexMethodInfo =>
            s_TryGetIndex ??
            (s_TryGetIndex = GetMethod(nameof(TryGetIndexCallback)));

        private static MethodInfo? s_TrySetIndex;
        MethodInfo? IJsonDynamicMetaObjectProvider.TrySetIndexMethodInfo =>
            s_TrySetIndex ??
            (s_TrySetIndex = GetMethod(nameof(TrySetIndexCallback)));

        MethodInfo? IJsonDynamicMetaObjectProvider.TryGetMemberMethodInfo => null;

        MethodInfo? IJsonDynamicMetaObjectProvider.TrySetMemberMethodInfo => null;

        MethodInfo? IJsonDynamicMetaObjectProvider.TryConvertMethodInfo => null;
    }
}
#endif
