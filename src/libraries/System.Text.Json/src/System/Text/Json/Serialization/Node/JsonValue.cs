// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// todo
    /// </summary>
    public abstract partial class JsonValue : JsonNode
    {
        /// <summary>
        /// todo
        /// </summary>
        /// <param name="options"></param>
        public JsonValue(JsonSerializerOptions? options = null) : base(options) { }
    }
}
