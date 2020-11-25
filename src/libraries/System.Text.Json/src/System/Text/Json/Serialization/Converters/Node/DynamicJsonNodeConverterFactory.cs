// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if BUILDING_INBOX_LIBRARY

namespace System.Text.Json.Serialization.Converters
{
    /// <summary>
    /// Supports deserialization of all <see cref="object"/>-declared types, supporting <see langword="dynamic"/>.
    /// Supports serialization of all <see cref="JsonNode"/>-derived types.
    /// </summary>
    internal sealed class DynamicJsonNodeConverterFactory : JsonNodeConverterFactoryBase
    {
        private static readonly JsonArrayConverterBase s_ArrayConverter = new DynamicJsonArrayConverter();
        private static readonly JsonObjectConverterBase s_ObjectConverter = new DynamicJsonObjectConverter();
        private static readonly JsonValueConverterBase s_ValueConverter = new DynamicJsonValueConverter();
        private static readonly JsonNodeConverterBase s_NodeConverter = new DynamicJsonNodeConverter();

        public override bool CanConvert(Type typeToConvert)
        {
            if (typeToConvert == JsonClassInfo.ObjectType)
            {
                return true;
            }

            if (typeof(JsonNode).IsAssignableFrom(typeToConvert))
            {
                if (typeToConvert == typeof(DynamicJsonValue) ||
                    typeToConvert == typeof(DynamicJsonObject) ||
                    typeToConvert == typeof(DynamicJsonArray))
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

            protected override JsonArray Create(JsonSerializerOptions? options) =>
                new DynamicJsonArray(options);
        }

        public class DynamicJsonObjectConverter : JsonObjectConverterBase
        {
            protected override JsonNodeConverterBase NodeConverter => s_NodeConverter;

            protected override JsonObject Create(JsonSerializerOptions? options) =>
                new DynamicJsonObject(options);
        }

        public class DynamicJsonValueConverter : JsonValueConverterBase
        {
            protected override JsonNodeConverterBase NodeConverter => s_NodeConverter;

            protected override JsonValue Create(object? value, JsonSerializerOptions? options) =>
                new DynamicJsonValue(value, options);
        }
    }
}
#endif
