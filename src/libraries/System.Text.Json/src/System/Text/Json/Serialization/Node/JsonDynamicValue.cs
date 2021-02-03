// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if BUILDING_INBOX_LIBRARY

using System.Dynamic;
using System.Linq.Expressions;
using System.Reflection;

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// todo
    /// </summary>
    public sealed class JsonDynamicValue : JsonValue<object>, IJsonDynamicMetaObjectProvider
    {
        /// <summary>
        /// todo
        /// </summary>
        /// <param name="value"></param>
        /// <param name="options"></param>
        public JsonDynamicValue(object value, JsonSerializerOptions? options = null) : base(value, options) { }

        internal bool TryConvertCallback(ConvertBinder binder, out object? result)
        {
            return TryConvert(binder.ReturnType, out result);
        }

        DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter) =>
            new MetaDynamic(parameter, this);

        private static MethodInfo GetMethod(string name) => typeof(JsonDynamicValue).GetMethod(
            name, BindingFlags.Instance | BindingFlags.NonPublic)!;

        MethodInfo? IJsonDynamicMetaObjectProvider.TryGetIndexMethodInfo => null;

        MethodInfo? IJsonDynamicMetaObjectProvider.TrySetIndexMethodInfo => null;

        MethodInfo? IJsonDynamicMetaObjectProvider.TryGetMemberMethodInfo => null;

        MethodInfo? IJsonDynamicMetaObjectProvider.TrySetMemberMethodInfo => null;

        private static MethodInfo? s_TryConvert;
        MethodInfo IJsonDynamicMetaObjectProvider.TryConvertMethodInfo =>
            s_TryConvert ??
            (s_TryConvert = GetMethod(nameof(TryConvertCallback)));
    }
}
#endif
