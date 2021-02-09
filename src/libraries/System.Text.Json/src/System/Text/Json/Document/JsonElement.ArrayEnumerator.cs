// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace System.Text.Json
{
    public partial struct JsonElement
    {
        /// <summary>
        ///   An enumerable and enumerator for the contents of a JSON array.
        /// </summary>
        [DebuggerDisplay("{Current,nq}")]
        public struct ArrayEnumerator : IEnumerable<JsonElement>, IEnumerator<JsonElement>
        {
            private readonly JsonElement _target;
            private int _curIdx;
            private readonly int _endIdxOrVersion;
            private IEnumerator<JsonNode?>? _current;

            internal ArrayEnumerator(JsonElement target)
            {
                _target = target;
                _curIdx = -1;
                _current = null;

                Debug.Assert(target._parent != null);

                if (target._parent is JsonDocument document)
                {
                    Debug.Assert(target.TokenType == JsonTokenType.StartArray);
                    _endIdxOrVersion = document.GetEndIndex(_target._idx, includeEndElement: false);
                }
                else
                {
                    JsonArray jsonArray = (JsonArray)target._parent;
                    _current = jsonArray.List.GetEnumerator();
                    _endIdxOrVersion = 0; // Not used.
                }
            }

            /// <inheritdoc />
            public JsonElement Current
            {
                get
                {
                    if (_target._parent is JsonArray jsonArray)
                    {
                        if (_current == null)
                        {
                            return default;
                        }

                        JsonNode? node = _current.Current;
                        return node == null ? s_nullLiteral : node.AsJsonElement();
                    }

                    if (_curIdx < 0)
                    {
                        return default;
                    }

                    var document = (JsonDocument)_target._parent;
                    return new JsonElement(document, _curIdx);
                }
            }

            /// <summary>
            ///   Returns an enumerator that iterates through a collection.
            /// </summary>
            /// <returns>
            ///   An <see cref="ArrayEnumerator"/> value that can be used to iterate
            ///   through the array.
            /// </returns>
            public ArrayEnumerator GetEnumerator()
            {
                if (_target._parent is JsonArray jsonArray)
                {
                    _current = jsonArray.List.GetEnumerator();
                }

                ArrayEnumerator ator = this;
                ator._curIdx = -1;
                return ator;
            }

            /// <inheritdoc />
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            /// <inheritdoc />
            IEnumerator<JsonElement> IEnumerable<JsonElement>.GetEnumerator() => GetEnumerator();

            /// <inheritdoc />
            public void Dispose()
            {
                _curIdx = _endIdxOrVersion;
            }

            /// <inheritdoc />
            public void Reset()
            {
                _curIdx = -1;
            }

            /// <inheritdoc />
            object IEnumerator.Current => Current;

            /// <inheritdoc />
            public bool MoveNext()
            {
                if (_target._parent is JsonArray jsonArray)
                {
                    if (_current == null)
                    {
                        return false;
                    }

                    return _current.MoveNext();
                }

                if (_curIdx >= _endIdxOrVersion)
                {
                    return false;
                }

                if (_curIdx < 0)
                {
                    _curIdx = _target._idx + JsonDocument.DbRow.Size;
                }
                else
                {
                    Debug.Assert(_target._parent != null);
                    var document = (JsonDocument)_target._parent;
                    _curIdx = document.GetEndIndex(_curIdx, includeEndElement: true);
                }

                return _curIdx < _endIdxOrVersion;
            }
        }
    }
}
