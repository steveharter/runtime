// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json.Serialization.Converters;

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// Supports dynamic objects.
    /// </summary>
    public partial class JsonObject : JsonNode
    {
        private JsonElement? _jsonElement;
        private IDictionary<string, JsonNode?>? _value;
        private string? _lastKey;
        private JsonNode? _lastValue;
        internal JsonNodeConverterBase _nodeConverter;

        /// <summary>
        /// todo
        /// </summary>
        /// <param name="options"></param>
        public JsonObject(JsonSerializerOptions? options = null) : base(options)
        {
            _nodeConverter = JsonNodeConverterFactory.s_NodeConverter;
            ValueKind = JsonValueKind.Object;
        }

        internal JsonObject(in JsonElement jsonElement,
            JsonNodeConverterBase nodeConverter,
            JsonSerializerOptions? options = null) : base(options)
        {
            _jsonElement = jsonElement;
            _nodeConverter = nodeConverter;
            ValueKind = JsonValueKind.Object;
        }

        /// <summary>
        /// todo
        /// </summary>
        /// <typeparam name="TypeToReturn"></typeparam>
        /// <returns></returns>
        public override TypeToReturn To<TypeToReturn>()
        {
            Type type = typeof(TypeToReturn);

            if (type == typeof(object) || type == typeof(IDictionary<string, JsonNode?>))
            {
                return (TypeToReturn)Dictionary;
            }

            throw new NotImplementedException("GetValue<> currently not implemented");
        }

        /// <summary>
        /// todo
        /// </summary>
        internal IDictionary<string, JsonNode?> Dictionary
        {
            get
            {
                CreateNodes();
                Debug.Assert(_value != null);
                return _value;
            }
        }

        /// <summary>
        /// todo
        /// </summary>
        /// <param name="returnType"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        internal override bool TryConvert(Type returnType, out object? result)
        {
            if (returnType.IsAssignableFrom(typeof(IDictionary<string, JsonNode?>)))
            {
                result = this;
                return true;
            }

            result = null;
            return false;
        }

        /// <summary>
        /// todo
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool TryGetValue(string propertyName, out JsonNode? value) => Dictionary.TryGetValue(propertyName, out value);

        private void CreateNodes()
        {
            if (_value == null)
            {
                bool caseInsensitive = false;
                if (Options?.PropertyNameCaseInsensitive == true)
                {
                    caseInsensitive = true;
                }

                var dictionary = new Dictionary<string, JsonNode?>(
                    caseInsensitive ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);

                if (_jsonElement != null)
                {
                    JsonElement jElement = _jsonElement.Value;
                    Debug.Assert(jElement.ValueKind == JsonValueKind.Object);
                    Debug.Assert(_nodeConverter != null);
                    foreach (JsonProperty property in jElement.EnumerateObject())
                    {
                        JsonNode jNode = _nodeConverter.Create(property.Value, Options!);
                        dictionary.Add(property.Name, jNode);
                    }

                    // Clear these since no longer needed.
                    _jsonElement = null;
                }

                _value = dictionary;

            }
        }

        internal void Write(Utf8JsonWriter writer)
        {
            if (_jsonElement != null)
            {
                _jsonElement.Value.WriteTo(writer);
            }
            else
            {
                Debug.Assert(_nodeConverter != null);

                writer.WriteStartObject();

                foreach (KeyValuePair<string, JsonNode?> kvp in Dictionary)
                {
                    // todo: check for null against options and skip
                    writer.WritePropertyName(kvp.Key);
                    JsonSerializerOptions options = Options ?? JsonSerializerOptions.s_defaultOptions;
                    _nodeConverter.Write(writer, kvp.Value!, options);
                }

                writer.WriteEndObject();
            }
        }
    }
}
