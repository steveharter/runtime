// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json.Serialization.Converters
{
    /// <summary>
    /// todo
    /// </summary>
    internal class JsonNodeConverter : JsonConverter<object>
    {
        public static JsonNodeConverter Default { get; } = new JsonNodeConverter();
        public JsonArrayConverter ArrayConverter { get; } = new JsonArrayConverter();
        public JsonObjectConverter ObjectConverter { get; } = new JsonObjectConverter();
        public JsonValueConverter ValueConverter { get; } = new JsonValueConverter();
        public ObjectConverter ElementConverter { get; } = new ObjectConverter();

        public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
            }
            else
            {
                JsonNodeConverterFactory.VerifyOptions(value, options);

                if (value is JsonObject jsonObject)
                {
                    ObjectConverter.Write(writer, jsonObject, options);
                }
                else if (value is JsonArray jsonArray)
                {
                    ArrayConverter.Write(writer, (JsonArray)value, options);
                }
                else
                {
                    // todo: add internal virtual Write method on JsonNode and forward
                    ValueConverter.Write(writer, (JsonValue)value, options);
                    //throw new Exception("TODO");
                }
            }
        }

        public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.Null:
                    return null;
                case JsonTokenType.String:
                case JsonTokenType.False:
                case JsonTokenType.True:
                case JsonTokenType.Number:
                    return ValueConverter.Read(ref reader, typeToConvert, options);
                case JsonTokenType.StartArray:
                    return ArrayConverter.Read(ref reader, typeToConvert, options);
                case JsonTokenType.StartObject:
                    return ObjectConverter.Read(ref reader, typeToConvert, options);
                default:
                    throw new JsonException("todo:Unexpected token type.");
            }
        }

        public JsonNode Create(JsonElement element, JsonSerializerOptions options)
        {
            JsonNode node;

            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    node = new JsonObject(element, options);
                    break;
                case JsonValueKind.Array:
                    node = new JsonArray(element, options);
                    break;
                default:
                    node = new JsonValue<JsonElement>(element, options);
                    break;
            }

            node.ValueKind = element.ValueKind;
            return node;
        }
    }
}
