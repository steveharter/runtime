// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class CursorConverter : JsonResumableConverter<bool> // bool is a dummy
    {
        private readonly JsonCursor _cursor;

        private bool _continuation_readObject;
        private bool _continuation_readArray;
        private bool _continuation_skipping;
        private readonly JsonPath _destinationPath;
        private int _arrayIndex;

        public CursorConverter(JsonCursor jsonCursor, JsonPath jsonPath)
        {
            _cursor = jsonCursor;
            _destinationPath = jsonPath;
        }

        internal sealed override ConverterStrategy ConverterStrategy => ConverterStrategy.JsonCursor;

        internal override bool OnTryRead(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options,
            ref ReadStack state,
            out bool value)
        {
        TryRead:
            if (_continuation_skipping)
            {
                if (!reader.TrySkip())
                {
                    value = false;
                    return false;
                }

                _continuation_skipping = false;
            }

        TryReadProperty:
            if (_continuation_readObject)
            {
                if (!reader.Read())
                {
                    value = false;
                    return false;
                }

                string? propertyName = reader.GetString();
                Debug.Assert(propertyName != null);

                if (_destinationPath._elements[reader.CurrentDepth].Name == propertyName)
                {
                    // The current property matches; see if we are done.
                    if (_destinationPath._elements.Length == reader.CurrentDepth)
                    {
                        value = true;
                        return true;
                    }

                    // We need to read the value and recurse.
                    _continuation_readObject = false;
                    goto ReadNext;
                }
                else
                {
                    // Skip the property value.
                    if (!reader.TrySkip())
                    {
                        _continuation_skipping = true;
                        goto TryRead;
                    }
                }

                while (reader.Read())
                {
                    JsonTokenType jsonTokenType = reader.TokenType;
                    switch (jsonTokenType)
                    {
                        case JsonTokenType.PropertyName:
                            goto TryReadProperty;
                        case JsonTokenType.EndObject:
                            // Property was not found.
                            value = false;
                            return true;
                        default:
                            Debug.Assert(false, $"Token {jsonTokenType} not expected");
                            value = false;
                            return true;
                    }
                }

                value = false;
                return false;
            }

        TryReadArray:
            if (_continuation_readArray)
            {
                if (_destinationPath._elements[reader.CurrentDepth].Name != null)
                {
                    // Expected an array
                    value = false;
                    return true;
                }

                // Position reader on element
                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndArray)
                    {
                        // Element was not found.
                        value = false;
                        return true;
                    }

                    if (_destinationPath._elements[reader.CurrentDepth].Index == _arrayIndex)
                    {
                        // The current index matches; see if we are done.
                        if (_destinationPath._elements.Length == reader.CurrentDepth)
                        {
                            value = true;
                            return true;
                        }

                        // Read the value and recurse.
                        _continuation_readArray = false;
                        goto ReadNext;
                    }

                    // Skip if object\array
                    if (!reader.TrySkip())
                    {
                        _continuation_skipping = true;
                        goto TryRead;
                    }

                    _arrayIndex++;
                }

                value = false;
                return false;
            }


        ReadNext:
            while (reader.Read())
            {
                JsonTokenType jsonTokenType = reader.TokenType;
                switch (jsonTokenType)
                {
                    case JsonTokenType.PropertyName:
                        // First property for an object.
                        _continuation_readObject = true;
                        goto TryReadProperty;

                    case JsonTokenType.StartArray:
                        _continuation_readArray = true;
                        _arrayIndex = 0;
                        goto TryReadArray;
                }
            }

            value = false;
            return false;
        }

        internal override bool OnTryWrite(
            Utf8JsonWriter writer,
            bool value,
            JsonSerializerOptions options,
            ref WriteStack state)
        {
            throw new InvalidOperationException("todo");
        }

        // todo: clear() method fo reuse
    }
}
