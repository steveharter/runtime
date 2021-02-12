// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Text.Json.Serialization.Converters
{
    internal class JsonArrayConverter : JsonConverter<JsonArray>
    {
        public override void Write(Utf8JsonWriter writer, JsonArray value, JsonSerializerOptions options)
        {
            Debug.Assert(value != null);
            JsonNodeConverterFactory.VerifyOptions(value, options);
            value.WriteTo(writer);
        }

        public override JsonArray? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.StartArray:
                    return ReadList(ref reader, options);
                case JsonTokenType.Null:
                    return null;
                default:
                    throw new JsonException("todo:Unexpected token type.");
            }
        }

        public JsonArray ReadList(ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            JsonElement jElement = JsonElement.ParseValue(ref reader);
            JsonArray jArray = new JsonArray(jElement, options);
            return jArray;
        }
    }
}
