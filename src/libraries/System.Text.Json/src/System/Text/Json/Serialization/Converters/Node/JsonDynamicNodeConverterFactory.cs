// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if BUILDING_INBOX_LIBRARY

namespace System.Text.Json.Serialization.Converters
{
    /// <summary>
    /// Supports deserialization of all <see cref="object"/>-declared types, supporting <see langword="dynamic"/>.
    /// Supports serialization of all <see cref="JsonNode"/>-derived types.
    /// </summary>
    internal sealed class JsonDynamicNodeConverterFactory : JsonNodeConverterFactoryBase
    {
        private static readonly JsonArrayConverterBase s_ArrayConverter = new DynamicJsonArrayConverter();
        private static readonly JsonObjectConverterBase s_ObjectConverter = new DynamicJsonObjectConverter();
        private static readonly JsonValueConverterBase s_ValueConverter = new DynamicJsonValueConverter();
        public static readonly JsonNodeConverterBase s_NodeConverter = new DynamicJsonNodeConverter();

        public override bool CanConvert(Type typeToConvert)
        {
            if (typeToConvert == JsonClassInfo.ObjectType)
            {
                return true;
            }

            if (typeof(JsonNode).IsAssignableFrom(typeToConvert))
            {
                if (typeToConvert == typeof(JsonDynamicValue) ||
                    typeToConvert == typeof(JsonDynamicObject) ||
                    typeToConvert == typeof(JsonDynamicArray))
                {
                    return true;
                }
            }

            // Fallback to non-dynamic JsonNode converter.
            return false;
        }

        protected override JsonNodeConverterBase NodeConverter => s_NodeConverter;

        public sealed class DynamicJsonNodeConverter : JsonNodeConverterBase
        {
            public override JsonArrayConverterBase ArrayConverter => s_ArrayConverter;
            public override JsonObjectConverterBase ObjectConverter => s_ObjectConverter;
            public override JsonValueConverterBase ValueConverter => s_ValueConverter;
        }

        public class DynamicJsonArrayConverter : JsonArrayConverterBase
        {
            protected override JsonNodeConverterBase NodeConverter => s_NodeConverter;

            internal override JsonArray Create(JsonElement jsonElement, JsonSerializerOptions? options) =>
                new JsonDynamicArray(jsonElement, NodeConverter, options);
        }

        public class DynamicJsonObjectConverter : JsonObjectConverterBase
        {
            protected override JsonNodeConverterBase NodeConverter => s_NodeConverter;

            internal override JsonObject Create(JsonElement jsonElement, JsonSerializerOptions? options) =>
                new JsonDynamicObject(jsonElement, NodeConverter, options);
        }

        public class DynamicJsonValueConverter : JsonValueConverterBase
        {
            protected override JsonNodeConverterBase NodeConverter => s_NodeConverter;

            internal override JsonValue Create(JsonElement jsonElement, JsonSerializerOptions? options) =>
                new JsonDynamicValue(jsonElement, NodeConverter, options);
        }
    }
}
#endif
