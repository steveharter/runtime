// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// Supports dynamic arrays.
    /// </summary>
    public class JsonArray : JsonNode, IList<JsonNode?>
    {
        private IList<JsonNode?>? _value;

        /// <summary>
        /// todo
        /// </summary>
        /// <param name="options"></param>
        public JsonArray(JsonSerializerOptions? options = null) : base(options) { }

        /// <summary>
        /// todo
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public override T GetValue<T>()
        {
            Type type = typeof(T);

            if (type == typeof(object) || type == typeof(IList<object>))
            {
                return (T)(object)this;
            }

            throw new NotImplementedException("GetValue<> currently not implemented");
        }

        internal IList<JsonNode?> List => _value ?? (_value = new List<JsonNode?>());

        /// <summary>
        /// todo
        /// </summary>
        /// <param name="returnType"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        internal override bool TryConvert(Type returnType, out object? result)
        {
            if (returnType.IsAssignableFrom(typeof(IList<object?>)))
            {
                result = _value;
                return true;
            }

            result = null;
            return false;
        }

        internal override JsonNode? GetItem(int index)
        {
            return List[index];
        }

        internal override void SetItem(int index, JsonNode? value)
        {
            if (value is JsonNode jNode)
            {
                jNode.UpdateOptions(this);
            }

            List[index] = value;
        }

        /// <summary>
        /// todo
        /// </summary>
        public int Count => List.Count;

        bool ICollection<JsonNode?>.IsReadOnly => List.IsReadOnly;

        /// <summary>
        /// todo
        /// </summary>
        /// <param name="item"></param>
        public void Add(JsonNode? item)
        {
            if (item != null)
            {
                item.UpdateOptions(this);
            }

            List.Add(item);
        }

        /// <summary>
        /// todo
        /// </summary>
        /// <param name="item"></param>
        public void Add(object? item)
        {
            JsonNode? node = Convert(item);

            if (node != null)
            {
                node.UpdateOptions(this);
            }

            List.Add(node);
        }

        /// <summary>
        /// todo
        /// </summary>
        public void Clear() => List.Clear();

        /// <summary>
        /// todo
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Contains(JsonNode? item) => List.Contains(item);

        /// <summary>
        /// todo
        /// </summary>
        /// <param name="array"></param>
        /// <param name="arrayIndex"></param>
        void ICollection<JsonNode?>.CopyTo(JsonNode?[] array, int arrayIndex) => List.CopyTo(array, arrayIndex);

        /// <summary>
        /// todo
        /// </summary>
        /// <returns></returns>
        public IEnumerator<JsonNode?> GetEnumerator() => List.GetEnumerator();

        /// <summary>
        /// todo
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public int IndexOf(JsonNode? item) => List.IndexOf(item);

        /// <summary>
        /// todo
        /// </summary>
        /// <param name="index"></param>
        /// <param name="item"></param>
        public void Insert(int index, JsonNode? item)
        {
            if (item != null)
            {
                item.UpdateOptions(this);
            }

            List.Insert(index, item);
        }

        /// <summary>
        /// todo
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Remove(JsonNode? item) => List.Remove(item);

        /// <summary>
        /// todo
        /// </summary>
        /// <param name="index"></param>
        public void RemoveAt(int index) => List.RemoveAt(index);
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)List).GetEnumerator();
    }
}
