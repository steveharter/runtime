// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class JsonNodeConverterFactory : JsonConverterFactory
    {
        public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            if (JsonClassInfo.ObjectType == typeToConvert)
            {
                if (options.UnknownTypeHandling == JsonUnknownTypeHandling.JsonNode)
                {
                    return JsonNodeConverter.Default;
                }

                // Return the converter for System.Object which uses JsonElement.
                return JsonNodeConverter.Default.ElementConverter;
            }

            if (typeof(JsonValue).IsAssignableFrom(typeToConvert))
            {
                return JsonNodeConverter.Default.ValueConverter;
            }

            if (typeof(JsonObject) == typeToConvert)
            {
                return JsonNodeConverter.Default.ObjectConverter;
            }

            if (typeof(JsonArray) == typeToConvert)
            {
                return JsonNodeConverter.Default.ArrayConverter;
            }

            Debug.Assert(typeof(JsonNode) == typeToConvert);
            return JsonNodeConverter.Default;
        }

        internal static void VerifyOptions(object value, JsonSerializerOptions options)
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

        public override bool CanConvert(Type typeToConvert) =>
            typeToConvert == JsonClassInfo.ObjectType ||
            typeof(JsonNode).IsAssignableFrom(typeToConvert);
    }
}
