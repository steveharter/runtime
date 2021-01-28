// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Text.Json.Serialization.Converters
{
    /// <summary>
    /// Shared code for JsonNodeConverterFactory and DynamicJsonNodeConverterFactory
    /// </summary>
    internal abstract class JsonNodeConverterFactoryBase : JsonConverterFactory
    {
        protected abstract JsonNodeConverterBase NodeConverter { get; }

        public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            if (typeof(JsonValue).IsAssignableFrom(typeToConvert))
            {
                return NodeConverter.ValueConverter;
            }

            if (typeof(JsonObject).IsAssignableFrom(typeToConvert))
            {
                return NodeConverter.ObjectConverter;
            }

            if (typeof(JsonArray).IsAssignableFrom(typeToConvert))
            {
                return NodeConverter.ArrayConverter;
            }

            Debug.Assert(typeof(JsonNode).IsAssignableFrom(typeToConvert) || typeToConvert == typeof(object));
            return NodeConverter;
        }

        private static void VerifyOptions(object value, JsonSerializerOptions options)
        {
            if (value is JsonNode node)
            {
                if (node.Options != null && options != node.Options)
                {
                    throw new InvalidOperationException("todo");
                }
            }
            else
            {
                throw new InvalidOperationException("todo");
            }
        }

        /// <summary>
        /// todo
        /// </summary>
        public abstract class JsonNodeConverterBase : JsonConverter<object>
        {
            public abstract JsonArrayConverterBase ArrayConverter { get; }
            public abstract JsonObjectConverterBase ObjectConverter { get; }
            public abstract JsonValueConverterBase ValueConverter { get; }

            public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
            {
                if (value == null)
                {
                    writer.WriteNullValue();
                }
                else
                {
                    VerifyOptions(value, options);

                    if (value is JsonValue jsonValue)
                    {
                        ValueConverter.Write(writer, jsonValue, options);
                    }
                    else if (value is JsonObject jsonObject)
                    {
                        ObjectConverter.Write(writer, jsonObject, options);
                    }
                    else
                    {
                        Debug.Assert(value is JsonArray);
                        ArrayConverter.Write(writer, (JsonArray)value, options);
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
        }

        public abstract class JsonArrayConverterBase : JsonConverter<JsonArray>
        {
            protected abstract JsonArray Create(JsonSerializerOptions options);
            protected abstract JsonNodeConverterBase NodeConverter { get; }

            public override void Write(Utf8JsonWriter writer, JsonArray value, JsonSerializerOptions options)
            {
                if (value == null)
                {
                    writer.WriteNullValue();
                }
                else
                {
                    VerifyOptions(value, options);

                    writer.WriteStartArray();

                    for (int i = 0; i < value.Count; i++)
                    {
                        NodeConverter.Write(writer, ((JsonNode)value)[i]!, options);
                    }

                    writer.WriteEndArray();
                }
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
                JsonArray jArray = Create(options);
                jArray.ValueKind = JsonValueKind.Array;

                while (true)
                {
                    reader.Read();
                    if (reader.TokenType == JsonTokenType.EndArray)
                    {
                        break;
                    }

                    var value = NodeConverter.Read(ref reader, typeof(object), options);
                    jArray.Add(value);
                }

                return jArray;
            }
        }

        public abstract class JsonObjectConverterBase : JsonConverter<JsonObject>
        {
            protected abstract JsonObject Create(JsonSerializerOptions options);
            protected abstract JsonNodeConverterBase NodeConverter { get; }

            public override void Write(Utf8JsonWriter writer, JsonObject value, JsonSerializerOptions options)
            {
                if (value == null)
                {
                    writer.WriteNullValue();
                }
                else
                {
                    VerifyOptions(value, options);

                    writer.WriteStartObject();

                    foreach (KeyValuePair<string, JsonNode?> kvp in value)
                    {
                        // todo: check for null against options and skip
                        writer.WritePropertyName(kvp.Key);
                        NodeConverter.Write(writer, kvp.Value!, options);
                    }

                    writer.WriteEndObject();
                }
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
                JsonObject jObject = Create(options);
                jObject.ValueKind = JsonValueKind.Object;

                while (true)
                {
                    reader.Read();
                    if (reader.TokenType == JsonTokenType.EndObject)
                    {
                        break;
                    }

                    if (reader.TokenType != JsonTokenType.PropertyName)
                    {
                        throw new JsonException();
                    }

                    string? key = reader.GetString();
                    if (key == null)
                    {
                        throw new JsonException();
                    }

                    reader.Read();
                    var value = NodeConverter.Read(ref reader, typeof(JsonNode), options);
                    jObject.Add(key, (JsonNode?)value);
                }

                return jObject;
            }
        }

        public abstract class JsonValueConverterBase : JsonConverter<JsonValue>
        {
            protected abstract JsonValue Create(object? value, JsonSerializerOptions options);
            protected abstract JsonNodeConverterBase NodeConverter { get; }

            public override void Write(Utf8JsonWriter writer, JsonValue value, JsonSerializerOptions options)
            {
                VerifyOptions(value, options);
                JsonSerializer.Serialize(writer, value.Value, JsonClassInfo.ObjectType, options);
            }

            public override JsonValue? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                JsonValue? value;

                switch (reader.TokenType)
                {
                    case JsonTokenType.String:
                        value = Create(reader.GetString(), options);
                        value.ValueKind = JsonValueKind.String;
                        break;
                    case JsonTokenType.False:
                        value = Create(false, options);
                        value.ValueKind = JsonValueKind.False;
                        break;
                    case JsonTokenType.True:
                        value = Create(true, options);
                        value.ValueKind = JsonValueKind.True;
                        break;
                    case JsonTokenType.Number:
                        value = Create(JsonElement.ParseValue(ref reader), options);
                        value.ValueKind = JsonValueKind.Number;
                        break;
                    case JsonTokenType.Null:
                        value = null;
                        break;
                    default:
                        throw new JsonException("Unexpected token type.");
                }

                return value;
            }
        }
    }
}
