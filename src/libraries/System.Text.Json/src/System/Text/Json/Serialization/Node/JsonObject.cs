// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// Supports dynamic objects.
    /// </summary>
    public class JsonObject : JsonNode, IDictionary<string, JsonNode?>
    {
        private IDictionary<string, JsonNode?>? _value;
        private string? _lastKey;
        private JsonNode? _lastValue;

        /// <summary>
        /// todo
        /// </summary>
        /// <param name="options"></param>
        public JsonObject(JsonSerializerOptions? options = null) : base(options) { }

        /// <summary>
        /// todo
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public override T GetValue<T>()
        {
            Type type = typeof(T);

            if (type == typeof(object) || type == typeof(IDictionary<string, JsonNode?>))
            {
                return (T)Dictionary;
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
                if (_value == null)
                {
                    bool caseInsensitive = false;
                    if (Options?.PropertyNameCaseInsensitive == true)
                    {
                        caseInsensitive = true;
                    }

                    _value = new Dictionary<string, JsonNode?>(caseInsensitive ?
                        StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
                }

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
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void Add(string key, JsonNode? value)
        {
            if (value is JsonNode jNode)
            {
                jNode.UpdateOptions(this);
            }

            Dictionary.Add(key, value);
        }

        void ICollection<KeyValuePair<string, JsonNode?>>.Add(KeyValuePair<string, JsonNode?> item)
        {
            JsonNode? value = item.Value;

            if (value != null)
            {
                value.UpdateOptions(this);
            }

            Dictionary.Add(item);
        }

        /// <summary>
        /// todo
        /// </summary>
        public void Clear() => Dictionary.Clear();

        /// <summary>
        /// todo
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        bool ICollection<KeyValuePair<string, JsonNode?>>.Contains(KeyValuePair<string, JsonNode?> item) => Dictionary.Contains(item);

        /// <summary>
        /// todo
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool ContainsKey(string key) => Dictionary.ContainsKey(key);

        /// <summary>
        /// todo
        /// </summary>
        /// <param name="array"></param>
        /// <param name="arrayIndex"></param>
        void ICollection<KeyValuePair<string, JsonNode?>>.CopyTo(KeyValuePair<string, JsonNode?>[] array, int arrayIndex) => Dictionary.CopyTo(array, arrayIndex);

        /// <summary>
        /// todo
        /// </summary>
        /// <returns></returns>
        public IEnumerator<KeyValuePair<string, JsonNode?>> GetEnumerator() => Dictionary.GetEnumerator();

        /// <summary>
        /// todo
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool Remove(string key) => Dictionary.Remove(key);

        bool ICollection<KeyValuePair<string, JsonNode?>>.Remove(KeyValuePair<string, JsonNode?> item) => Dictionary.Remove(item);

        internal override JsonNode? GetItem(string key)
        {
            if (key == _lastKey)
            {
                // Optimize for repeating sections in code:
                // obj.Foo.Bar.One
                // obj.Foo.Bar.Two
                return _lastValue;
            }

            if (TryGetValue(key, out JsonNode? value))
            {
                _lastKey = key;
                _lastValue = value;
                return value;
            }

            // Return null for missing properties.
            return null;
        }

        internal override void SetItem(string key, JsonNode? value)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (value != null)
            {
                value.UpdateOptions(this);
            }

            Dictionary[key] = value;
        }

        ICollection<string> IDictionary<string, JsonNode?>.Keys => Dictionary.Keys;
        ICollection<JsonNode?> IDictionary<string, JsonNode?>.Values => Dictionary.Values;

        /// <summary>
        /// todo
        /// </summary>
        public int Count => Dictionary.Count;

        bool ICollection<KeyValuePair<string, JsonNode?>>.IsReadOnly => Dictionary.IsReadOnly;
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)Dictionary).GetEnumerator();

        /// <summary>
        /// todo
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool TryGetValue(string key, out JsonNode? value) => Dictionary.TryGetValue(key, out value);
    }
}
