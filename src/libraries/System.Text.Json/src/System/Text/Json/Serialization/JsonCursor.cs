// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Text.Json.Serialization.Converters;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// todo
    /// </summary>
    public abstract class JsonCursor : IDisposable //IAsyncDisposable?
    {
        internal ReadStack _readStack;
        internal ReadBufferState _bufferState;
        internal JsonReaderState _readerState;
        internal CancellationToken _cancellationToken;
        private bool _disposedValue;

        internal CursorConverter Converter { get; private init; }
        internal JsonTypeInfo JsonTypeInfo { get; private init; }

        internal JsonCursor(
            JsonSerializerOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            Options = options;
            _cancellationToken = cancellationToken;
            Converter = new CursorConverter(this);
            JsonSerializerOptions actualOptions = Options ??= JsonSerializerOptions.s_defaultOptions;
            JsonTypeInfo = new JsonTypeInfo(typeof(bool), Converter, typeof(bool), actualOptions);
            _readStack = default;
            _readStack.Initialize(JsonTypeInfo, supportContinuation: true);
            _bufferState = new ReadBufferState(actualOptions.DefaultBufferSize);
            _readerState = new JsonReaderState(actualOptions.GetReaderOptions());
        }

        /// <summary>
        /// todo
        /// </summary>
        /// <param name="utf8Json"></param>
        /// <param name="options"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static JsonCursor Create(
            Stream utf8Json,
            JsonSerializerOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            if (utf8Json == null)
            {
                throw new ArgumentNullException(nameof(utf8Json));
            }

            return new StreamCursor(utf8Json, options, cancellationToken);
        }

        /// <summary>
        /// todo; must be superset of current
        /// </summary>
        /// <param name="jsonPath"></param>
        public async ValueTask MoveToStart(string jsonPath)
        {
            bool success = await TryMoveToStartAsync(jsonPath).ConfigureAwait(false);
            if (!success)
            {
                throw new JsonException("todo - node not found");
            }
        }

        /// <summary>
        /// todo
        /// </summary>
        /// <param name="jsonPath"></param>
        /// <returns></returns>
        public async ValueTask<bool> TryMoveToStartAsync(string jsonPath)
        {
            if (jsonPath == "$")
            {
                return true;
            }

            return await TryMoveToStartAsyncImpl(new JsonPath(jsonPath)).ConfigureAwait(false);
        }

        internal abstract ValueTask<bool> TryMoveToStartAsyncImpl(JsonPath jsonPath);

        /// <summary>
        /// todo; must be subset of existing
        /// </summary>
        /// <param name="jsonPath"></param>
        public void MoveToEnd(string jsonPath)
        {
            if (!TryMoveToEnd(jsonPath))
            {
                throw new JsonException("todo - node not found");
            }
        }

        /// <summary>
        /// todo
        /// </summary>
        /// <param name="jsonPath"></param>
        /// <returns></returns>
        public bool TryMoveToEnd(string jsonPath)
        {
            bool success = TryMoveToEndImpl(new JsonPath(jsonPath));
            //if (success)
            //{
            //    JsonPath = jsonPath;
            //}

            return success;
        }

        internal abstract bool TryMoveToEndImpl(in JsonPath jsonPath);

        /// <summary>
        /// todo
        /// </summary>
        public JsonSerializerOptions? Options { get; private set; }

        /// <summary>
        /// todo
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _bufferState.Dispose();
                }

                _disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~JsonCursor()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }


        /// <summary>
        /// todo
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        ///// <summary>
        ///// todo
        ///// </summary>
        //public JsonPath GetJsonPath()
        //{
        //}

        //public void ReadOnlySpan<byte>GetUnprocessedBytes();
        //public static ref Utf8JsonReader CreateReader()
        //public static bool UpdateReader(ref Utf8JsonReader reader)
    }
}
