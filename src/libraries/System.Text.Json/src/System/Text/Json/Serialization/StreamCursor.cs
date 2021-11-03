// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Text.Json.Serialization
{
    internal sealed class StreamCursor : JsonCursor
    {
        public Stream Stream { get; private init; }

        public StreamCursor(
            Stream utf8Json,
            JsonSerializerOptions? options = null,
            CancellationToken cancellationToken = default)
            : base(options, cancellationToken)
        {
            Stream = utf8Json;
        }

        internal async override ValueTask<bool> TryMoveToStartAsyncImpl(JsonPath jsonPath)
        {
            return await JsonSerializer.TrySkipAsync(this).ConfigureAwait(false);
        }

        internal override bool TryMoveToEndImpl(in JsonPath jsonPath)
        {
            return false;
        }
    }
}
