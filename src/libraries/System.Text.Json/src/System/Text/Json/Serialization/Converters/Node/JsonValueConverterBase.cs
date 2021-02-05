// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Text.Json.Serialization.Converters
{
    internal abstract class JsonValueConverterBase : JsonConverter<JsonValue>
    {
        internal abstract JsonValue Create<T>(T value, JsonSerializerOptions? options);
        protected abstract JsonNodeConverterBase NodeConverter { get; }

        public override void Write(Utf8JsonWriter writer, JsonValue value, JsonSerializerOptions options)
        {
            Debug.Assert(value != null);
            JsonNodeConverterFactoryBase.VerifyOptions(value, options);
            value.Write(writer);
        }

        public override JsonValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            JsonElement element = JsonElement.ParseValue(ref reader);
            JsonValue value = Create(element, options);
            value.ValueKind = element.ValueKind;
            return value;
        }
    }
}
