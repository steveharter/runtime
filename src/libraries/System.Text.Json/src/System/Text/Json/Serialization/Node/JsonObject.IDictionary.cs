// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;

namespace System.Text.Json.Serialization
{
    public partial class JsonObject : IDictionary<string, JsonNode?>
    {
        /// <summary>
        /// todo
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="value"></param>
        public void Add(string propertyName, JsonNode? value)
        {
            if (value is JsonNode jNode)
            {
                jNode.UpdateOptions(this);
            }

            Dictionary.Add(propertyName, value);
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
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public bool ContainsKey(string propertyName) => Dictionary.ContainsKey(propertyName);

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
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public bool Remove(string propertyName) => Dictionary.Remove(propertyName);

        bool ICollection<KeyValuePair<string, JsonNode?>>.Remove(KeyValuePair<string, JsonNode?> item) => Dictionary.Remove(item);

        internal override JsonNode? GetItem(string propertyName)
        {
            if (TryGetPropertyValue(propertyName, out JsonNode? value))
            {
                return value;
            }

            // Return null for missing properties.
            return null;
        }

        internal override void SetItem(string propertyName, JsonNode? value)
        {
            if (propertyName == null)
            {
                throw new ArgumentNullException(nameof(propertyName));
            }

            if (value != null)
            {
                value.UpdateOptions(this);
            }

            Dictionary[propertyName] = value;
            _lastKey = propertyName;
            _lastValue = value;
        }

        ICollection<string> IDictionary<string, JsonNode?>.Keys => Dictionary.Keys;
        ICollection<JsonNode?> IDictionary<string, JsonNode?>.Values => Dictionary.Values;

        bool IDictionary<string, JsonNode?>.TryGetValue(string propertyName, out JsonNode? jsonNode) =>
            Dictionary.TryGetValue(propertyName, out jsonNode);

        /// <summary>
        /// todo
        /// </summary>
        public int Count => Dictionary.Count;

        bool ICollection<KeyValuePair<string, JsonNode?>>.IsReadOnly => Dictionary.IsReadOnly;
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)Dictionary).GetEnumerator();
    }
}
