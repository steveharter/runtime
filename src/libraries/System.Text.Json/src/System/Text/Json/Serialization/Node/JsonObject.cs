// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
        /// Creates a new JSON object that is a copy of the current instance.
        /// </summary>
        /// <returns>A new JSON object that is a copy of this instance.</returns>
        public override JsonNode Clone()
        {
            var jsonObject = new JsonObject();

            foreach (KeyValuePair<string, JsonNode?> property in this)
            {
                jsonObject.Add(property.Key, property.Value?.Clone());
            }

            return jsonObject;
        }

        /// <summary>
        /// todo
        /// </summary>
        /// <typeparam name="TypeToReturn"></typeparam>
        /// <returns></returns>
        public override TypeToReturn To<TypeToReturn>()
        {
            if (TryTo(out TypeToReturn value))
            {
                return value;
            }

            throw new NotImplementedException("GetValue<> currently not implemented");
        }

        /// <summary>
        /// todo
        /// </summary>
        /// <typeparam name="TypeToReturn"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public override bool TryTo<TypeToReturn>(out TypeToReturn value)
        {
            Type type = typeof(TypeToReturn);

            if (type == typeof(object) || type == typeof(IDictionary<string, JsonNode?>))
            {
                value = (TypeToReturn)(object)this;
                return true;
            }

            value = default!;
            return false;
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
        ///   Returns the value of a property with the specified name.
        /// </summary>
        /// <param name="propertyName">Name of the property to return.</param>
        /// <param name="jsonNode">The JSON value of the property with the specified name.</param>
        /// <returns>
        ///  <see langword="true"/> if a property with the specified name was found;
        ///  otherwise, <see langword="false"/>
        /// </returns>
        public bool TryGetPropertyValue(string propertyName, out JsonNode? jsonNode)
        {
            if (propertyName == _lastKey)
            {
                // Optimize for repeating sections in code:
                // obj.Foo.Bar.One
                // obj.Foo.Bar.Two
                jsonNode = _lastValue;
                return true;
            }

            bool rc = Dictionary.TryGetValue(propertyName, out jsonNode);
            _lastKey = propertyName;
            _lastValue = jsonNode;
            return rc;
        }

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

        /// <summary>
        /// todo
        /// </summary>
        /// <param name="writer"></param>
        public override void WriteTo(Utf8JsonWriter writer)
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
