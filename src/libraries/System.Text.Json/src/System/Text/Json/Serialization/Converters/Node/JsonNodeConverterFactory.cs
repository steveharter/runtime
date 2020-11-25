// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class JsonNodeConverterFactory : JsonNodeConverterFactoryBase
    {
        private static readonly JsonArrayConverterBase s_ArrayConverter = new JsonArrayConverter();
        private static readonly JsonObjectConverterBase s_ObjectConverter = new JsonObjectConverter();
        private static readonly JsonValueConverterBase s_ValueConverter = new JsonValueConverter();
        private static readonly JsonNodeConverterBase s_NodeConverter = new JsonNodeConverter();

        public override bool CanConvert(Type typeToConvert)
        {
            if (typeof(JsonNode).IsAssignableFrom(typeToConvert))
            {
                if (typeToConvert == typeof(JsonValue) ||
                    typeToConvert == typeof(JsonObject) ||
                    typeToConvert == typeof(JsonArray) ||
                    typeToConvert == typeof(JsonNode))
                {
                    return true;
                }

                throw new InvalidOperationException("todo: need to enable dynamic types");
            }

            return false;
        }

        protected override JsonNodeConverterBase NodeConverter => s_NodeConverter;

        /// <summary>
        /// Supports deserialization of all <see cref="object"/>-declared types.
        /// Supports serialization of all <see cref="JsonNode"/>-derived types.
        /// </summary>
        public sealed class JsonNodeConverter : JsonNodeConverterBase
        {
            public override JsonArrayConverterBase ArrayConverter => s_ArrayConverter;
            public override JsonObjectConverterBase ObjectConverter => s_ObjectConverter;
            public override JsonValueConverterBase ValueConverter => s_ValueConverter;
        }

        public class JsonArrayConverter : JsonArrayConverterBase
        {
            protected override JsonNodeConverterBase NodeConverter => s_NodeConverter;

            protected override JsonArray Create(JsonSerializerOptions? options) =>
                new JsonArray(options);
        }

        public class JsonObjectConverter : JsonObjectConverterBase
        {
            protected override JsonNodeConverterBase NodeConverter => s_NodeConverter;

            protected override JsonObject Create(JsonSerializerOptions? options) =>
                new JsonObject(options);
        }

        public class JsonValueConverter : JsonValueConverterBase
        {
            protected override JsonNodeConverterBase NodeConverter => s_NodeConverter;

            protected override JsonValue Create(object? value, JsonSerializerOptions? options) =>
                new JsonValue(value, options);
        }
    }
}
