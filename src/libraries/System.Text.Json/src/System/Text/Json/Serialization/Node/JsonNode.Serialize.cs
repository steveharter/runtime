// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Text.Json.Serialization
{
    public abstract partial class JsonNode
    {
        /// <summary>
        /// todo
        /// </summary>
        /// <returns></returns>
        public byte[] SerializeToUtf8Bytes()
        {
            return JsonSerializer.SerializeToUtf8Bytes(this, this.GetType(), Options);
        }

        /// <summary>
        /// todo
        /// </summary>
        /// <param name="utf8Json"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task SerializeAsync(Stream utf8Json, CancellationToken cancellationToken = default)
        {
            return JsonSerializer.SerializeAsync(utf8Json, this, this.GetType(), Options, cancellationToken);
        }

        /// <summary>
        /// todo
        /// </summary>
        /// <returns></returns>
        public string Serialize()
        {
            return JsonSerializer.Serialize(this, this.GetType(), Options);
        }

        /// <summary>
        /// todo
        /// </summary>
        /// <param name="writer"></param>
        public virtual void Serialize(Utf8JsonWriter writer)
        {
            JsonSerializer.Serialize(writer, this, this.GetType(), Options);
        }
    }
}
