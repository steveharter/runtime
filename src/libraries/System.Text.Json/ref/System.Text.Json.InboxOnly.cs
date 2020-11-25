// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Text.Json.Serialization
{
    public sealed partial class DynamicJsonArray : System.Text.Json.Serialization.JsonArray, System.Dynamic.IDynamicMetaObjectProvider
    {
        public DynamicJsonArray(System.Text.Json.JsonSerializerOptions? options) : base(default(System.Text.Json.JsonSerializerOptions)) { }
        System.Dynamic.DynamicMetaObject System.Dynamic.IDynamicMetaObjectProvider.GetMetaObject(System.Linq.Expressions.Expression parameter) { throw null; }
    }
    public sealed partial class DynamicJsonObject : System.Text.Json.Serialization.JsonObject, System.Dynamic.IDynamicMetaObjectProvider
    {
        public DynamicJsonObject(System.Text.Json.JsonSerializerOptions? options) : base(default(System.Text.Json.JsonSerializerOptions)) { }
        System.Dynamic.DynamicMetaObject System.Dynamic.IDynamicMetaObjectProvider.GetMetaObject(System.Linq.Expressions.Expression parameter) { throw null; }
    }
    public sealed partial class DynamicJsonValue : System.Text.Json.Serialization.JsonValue, System.Dynamic.IDynamicMetaObjectProvider
    {
        public DynamicJsonValue(object? value, System.Text.Json.JsonSerializerOptions? options) : base(default(object), default(System.Text.Json.JsonSerializerOptions)) { }
        System.Dynamic.DynamicMetaObject System.Dynamic.IDynamicMetaObjectProvider.GetMetaObject(System.Linq.Expressions.Expression parameter) { throw null; }
    }
}
