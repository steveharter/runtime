// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Linq.Expressions;
using System.Reflection;

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

        /// <summary>
        ///   Transforms this instance into <see cref="JsonElement"/> representation.
        ///   Operations performed on this instance will modify the returned <see cref="JsonElement"/>.
        /// </summary>
        /// <returns>Mutable <see cref="JsonElement"/> with <see cref="JsonNode"/> underneath.</returns>
        public JsonElement AsJsonElement() => new JsonElement(this);

        /// <summary>
        /// Performs a deep copy operation on this instance.
        /// </summary>
        /// <returns><see cref="JsonNode"/> which is a clone of this instance.</returns>
        public abstract JsonNode Clone();

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
        ///   Gets the <see cref="JsonNode"/> represented by <paramref name="jsonElement"/>.
        ///   Operations performed on the returned <see cref="JsonNode"/> will modify the <paramref name="jsonElement"/>.
        ///   See also: <seealso cref="JsonElement.IsImmutable"/>.
        /// </summary>
        /// <param name="jsonElement"><see cref="JsonElement"/> to get the <see cref="JsonNode"/> from.</param>
        /// <returns><see cref="JsonNode"/> represented by <paramref name="jsonElement"/>.</returns>
        /// <exception cref="ArgumentException">
        ///   Provided <see cref="JsonElement"/> was not built from <see cref="JsonNode"/>.
        /// </exception>
        public static JsonNode GetNode(JsonElement jsonElement)
        {
            if (jsonElement.IsImmutable)
            {
                throw new ArgumentException(SR.NotNodeJsonElementParent);
            }

            Debug.Assert(jsonElement._parent != null);
            return (JsonNode)jsonElement._parent;
        }

        /// <summary>
        ///    Gets the <see cref="JsonNode"/> represented by the <paramref name="jsonElement"/>.
        ///    Operations performed on the returned <see cref="JsonNode"/> will modify the <paramref name="jsonElement"/>.
        ///    A return value indicates whether the operation succeeded.
        /// </summary>
        /// <param name="jsonElement"><see cref="JsonElement"/> to get the <see cref="JsonNode"/> from.</param>
        /// <param name="jsonNode"><see cref="JsonNode"/> represented by <paramref name="jsonElement"/>.</param>
        /// <returns>
        ///  <see langword="true"/> if the operation succeded;
        ///  otherwise, <see langword="false"/>
        /// </returns>
        public static bool TryGetNode(JsonElement jsonElement, [NotNullWhen(true)] out JsonNode? jsonNode)
        {
            if (!jsonElement.IsImmutable)
            {
                Debug.Assert(jsonElement._parent != null);
                jsonNode = (JsonNode)jsonElement._parent;
                return true;
            }

            jsonNode = null;
            return false;
        }

        /// <summary>
        /// todo
        /// </summary>
        public JsonValueKind ValueKind { get; internal set; }

        /// <summary>
        /// todo; only works with JsonValue
        /// </summary>
        public virtual TypeToReturn GetValue<TypeToReturn>() => throw new InvalidOperationException("only works with JsonValue");

        /// <summary>
        /// todo
        /// </summary>
        /// <typeparam name="TypeToReturn"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public virtual bool TryGetValue<TypeToReturn>(out TypeToReturn value) => throw new InvalidOperationException("only works with JsonValue");

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

        /// <summary>
        /// todo
        /// </summary>
        /// <param name="writer"></param>
        public abstract void WriteTo(Utf8JsonWriter writer);

        /// <summary>
        ///   Converts the current instance to string in JSON format.
        /// </summary>
        /// <returns>JSON representation of current instance.</returns>
        public string ToJsonString()
        {
            var output = new ArrayBufferWriter<byte>();
            using (var writer = new Utf8JsonWriter(output))
            {
                WriteTo(writer);
            }
            return JsonHelpers.Utf8GetString(output.WrittenSpan);
        }

        /// <summary>
        /// todo
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T? Deserialize<T>()
        {
            string str = ToJsonString();
            return JsonSerializer.Deserialize<T>(str, Options);
        }
    }
}
