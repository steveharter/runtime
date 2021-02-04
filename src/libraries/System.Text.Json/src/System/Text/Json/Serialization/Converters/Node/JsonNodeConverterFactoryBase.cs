// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Text.Json.Serialization.Converters
{
    /// <summary>
    /// Shared code for JsonNodeConverterFactory and JsonDynamicNodeConverterFactory
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
    }
}
