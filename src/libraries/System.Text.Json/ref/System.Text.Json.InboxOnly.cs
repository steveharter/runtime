// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Text.Json.Serialization
{
    public sealed partial class JsonDynamicArray : System.Text.Json.Serialization.JsonArray, System.Dynamic.IDynamicMetaObjectProvider
    {
        public JsonDynamicArray(System.Text.Json.JsonSerializerOptions? options = null) : base(default(System.Text.Json.JsonSerializerOptions)) { }
        public JsonDynamicArray(JsonSerializerOptions? options, params JsonNode[] items) { }
        public JsonDynamicArray(params JsonNode[] items) { }
        System.Dynamic.DynamicMetaObject System.Dynamic.IDynamicMetaObjectProvider.GetMetaObject(System.Linq.Expressions.Expression parameter) { throw null; }
    }
    public sealed partial class JsonDynamicObject : System.Text.Json.Serialization.JsonObject, System.Dynamic.IDynamicMetaObjectProvider
    {
        public JsonDynamicObject(System.Text.Json.JsonSerializerOptions? options = null) : base(default(System.Text.Json.JsonSerializerOptions)) { }
        System.Dynamic.DynamicMetaObject System.Dynamic.IDynamicMetaObjectProvider.GetMetaObject(System.Linq.Expressions.Expression parameter) { throw null; }
    }
    public sealed partial class JsonDynamicValue : System.Text.Json.Serialization.JsonValueBase<object>, System.Dynamic.IDynamicMetaObjectProvider
    {
        public JsonDynamicValue(object? value, System.Text.Json.JsonSerializerOptions? options = null) : base(default(object), default(System.Text.Json.JsonSerializerOptions)) { }
        System.Dynamic.DynamicMetaObject System.Dynamic.IDynamicMetaObjectProvider.GetMetaObject(System.Linq.Expressions.Expression parameter) { throw null; }
    }
}
