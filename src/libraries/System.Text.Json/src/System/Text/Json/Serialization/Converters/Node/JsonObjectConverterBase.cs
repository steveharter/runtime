// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Text.Json.Serialization.Converters
{
    internal abstract class JsonObjectConverterBase : JsonConverter<JsonObject>
    {
        internal abstract JsonObject Create(JsonElement jsonElement, JsonSerializerOptions options);
        protected abstract JsonNodeConverterBase NodeConverter { get; }

        public override void Write(Utf8JsonWriter writer, JsonObject value, JsonSerializerOptions options)
        {
            Debug.Assert(value != null);
            JsonNodeConverterFactoryBase.VerifyOptions(value, options);
            value.Write(writer);
        }

        public override JsonObject? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.StartObject:
                    return ReadObject(ref reader, options);
                case JsonTokenType.Null:
                    return null;
                default:
                    throw new JsonException("todo:Unexpected token type.");
            }
        }

        public JsonObject ReadObject(ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            JsonElement jElement = JsonElement.ParseValue(ref reader);
            JsonObject jObject = Create(jElement, options);
            return jObject;
        }
    }
}
