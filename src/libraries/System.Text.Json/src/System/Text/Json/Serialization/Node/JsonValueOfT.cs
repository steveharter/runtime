// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization.Converters;

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// Supports dynamic numbers.
    /// </summary>
    public class JsonValue<T> : JsonValueBase<T>
    {
        /// <summary>
        /// todo
        /// </summary>
        /// <param name="value"></param>
        /// <param name="options"></param>
        public JsonValue(T value, JsonSerializerOptions? options = null) : base(value, options) { }

        /// <summary>
        /// todo
        /// </summary>
        /// <param name="value"></param>
        public static implicit operator JsonValue<T>(T value) => new JsonValue<T>(value);

        /// <summary>
        /// todo
        /// </summary>
        /// <param name="value"></param>
        public static implicit operator T(JsonValue<T> value) => value._value;
    }
}
