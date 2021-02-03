// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// The base class for all dynamic types supported by the serializer.
    /// </summary>
    public abstract partial class JsonNode
    {
        /// <summary>
        /// todo
        /// </summary>
        public JsonSerializerOptions? Options { get; private set; }

        internal JsonNode(JsonSerializerOptions? options = null)
        {
            Options = options;
        }

        internal JsonNode? Convert<T>(T? value)
        {
            if (value == null)
            {
                return null;
            }

            if (value is JsonNode node)
            {
                return node;
            }

            return new JsonValue<T>(value, Options);
        }

        /// <summary>
        /// todo
        /// </summary>
        /// <typeparam name="TypeToReturn"></typeparam>
        /// <returns></returns>
        public abstract TypeToReturn To<TypeToReturn>();

        // todo:
        // public abstract bool TryGetValue(Type type, out T value);
        // public abstract bool TryGetValue<T>(out T value);
        // public abstract bool TryGetValue(Type type, out object value);

        /// <summary>
        /// todo
        /// </summary>
        public JsonValueKind ValueKind { get; internal set; }

        ///// <summary>
        ///// todo; only works with JsonValue
        ///// </summary>
        //public virtual T GetValue<T>()
        //{
        //    get => throw new InvalidOperationException("todo");
        //    set => throw new InvalidOperationException("todo");
        //}

        /// <summary>
        /// todo; only works with JsonArray
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public JsonNode? this[int index]
        {
            get
            {
                return GetItem(index);
            }
            set
            {
                SetItem(index, value);
            }
        }

        internal virtual JsonNode? GetItem(int index)
        {
            throw new InvalidOperationException("todo");
        }

        internal virtual void SetItem(int index, JsonNode? value)
        {
            throw new InvalidOperationException("todo");
        }

        /// <summary>
        /// todo; only works with JsonObject
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public JsonNode? this[string key]
        {
            get
            {
                return GetItem(key);
            }

            set
            {
                SetItem(key, value);
            }
        }

        internal virtual JsonNode? GetItem(string key)
        {
            throw new InvalidOperationException("todo");
        }

        internal virtual void SetItem(string key, JsonNode? value)
        {
            throw new InvalidOperationException("todo");
        }

        internal void UpdateOptions(JsonNode node)
        {
            node.Options ??= Options;
        }

        ///// <summary>
        ///// todo
        ///// </summary>
        ///// <param name="returnType"></param>
        ///// <param name="result"></param>
        ///// <returns></returns>
        internal abstract bool TryConvert(Type returnType, out object? result);
    }
}
