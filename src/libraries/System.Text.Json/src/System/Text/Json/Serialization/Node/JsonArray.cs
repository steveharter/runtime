// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json.Serialization.Converters;

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// Supports dynamic arrays.
    /// </summary>
    public class JsonArray : JsonNode, IList<JsonNode?>
    {
        private JsonElement? _jsonElement;
        private IList<JsonNode?>? _value;
        internal JsonNodeConverterBase? _converter;

        /// <summary>
        /// todo
        /// </summary>
        /// <param name="options"></param>
        public JsonArray(JsonSerializerOptions? options = null) : base(options)
        {
            _converter = JsonNodeConverterFactory.s_NodeConverter;
            ValueKind = JsonValueKind.Array;
        }

        internal JsonArray(in JsonElement jsonElement,
            JsonNodeConverterBase converter,
            JsonSerializerOptions? options = null) : base(options)
        {
            _jsonElement = jsonElement;
            _converter = converter;
            ValueKind = JsonValueKind.Array;
        }

        /// <summary>
        /// todo
        /// </summary>
        /// <typeparam name="TypeToReturn"></typeparam>
        /// <returns></returns>
        public override TypeToReturn To<TypeToReturn>()
        {
            Type type = typeof(TypeToReturn);

            if (type == typeof(object) || type == typeof(IList<object>))
            {
                return (TypeToReturn)(object)this;
            }

            throw new NotImplementedException("GetValue<> currently not implemented");
        }

        internal IList<JsonNode?> List
        {
            get
            {
                CreateNodes();
                Debug.Assert(_value != null);
                return _value;
            }
        }

        ///// <summary>
        ///// todo
        ///// </summary>
        ///// <param name="returnType"></param>
        ///// <param name="result"></param>
        ///// <returns></returns>
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

        private void CreateNodes()
        {
            if (_value == null)
            {
                List<JsonNode?> list;

                if (_jsonElement == null)
                {
                    list = new List<JsonNode?>();
                }
                else
                {
                    JsonElement jElement = _jsonElement.Value;
                    Debug.Assert(jElement.ValueKind == JsonValueKind.Array);

                    list = new List<JsonNode?>(jElement.GetArrayLength());

                    Debug.Assert(_converter != null);
                    foreach (JsonElement element in jElement.EnumerateArray())
                    {
                        JsonNode jNode = _converter.Create(element, Options!);
                        list.Add(jNode);
                    }

                    // Clear since no longer needed.
                    _jsonElement = null;
                }

                _value = list;
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
                Debug.Assert(_value != null);
                Debug.Assert(_converter != null);

                JsonSerializerOptions options = Options ?? JsonSerializerOptions.s_defaultOptions;

                writer.WriteStartArray();

                for (int i = 0; i < _value.Count; i++)
                {
                    _converter.Write(writer, _value[i]!, options);
                }

                writer.WriteEndArray();
            }
        }
    }
}
