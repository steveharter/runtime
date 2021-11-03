// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed partial class CursorConverter : JsonResumableConverter<bool> // bool is a dummy
    {
        private readonly JsonCursor _cursor;

        private bool _continuation_propertyName;
        private bool _continuation_skipping;

        private int _levelCount;
        private JsonLevel[]? _levels;

        public CursorConverter(JsonCursor jsonCursor)
        {
            _cursor = jsonCursor;
        }

        internal sealed override ConverterStrategy ConverterStrategy => ConverterStrategy.JsonCursor;

        internal override bool OnTryRead(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options,
            ref ReadStack state,
            out bool value)
        {
            if (_continuation_skipping)
            {
                if (!reader.TrySkip())
                {
                    value = false;
                    return false;
                }

                _continuation_skipping = false;
            }
            else if (_continuation_propertyName)
            {
                if (!reader.Read())
                {
                    value = false;
                    return false;
                }

                ReadPropertyName(ref reader);
                if (IsAtDestination())
                {
                    value = true;
                    return true;
                }
            }

            while (reader.Read())
            {
                JsonTokenType jsonTokenType = reader.TokenType;
                switch (jsonTokenType)
                {
                    case JsonTokenType.PropertyName:
                        Debug.Assert(GetLevels()[_levelCount].TokenType == JsonTokenType.StartObject);

                        if (!reader.Read())
                        {
                            _continuation_propertyName = true;
                            value = false;
                            return false;
                        }

                        ReadPropertyName(ref reader);
                        if (IsAtDestination())
                        {
                            value = true;
                            return true;
                        }

                        if (ShouldSkip())
                        {
                            if (reader.TrySkip())
                            {
                                _continuation_skipping = true;
                                value = false;
                                return false;
                            }
                        }

                        break;

                    case JsonTokenType.StartObject:
                    case JsonTokenType.StartArray:
                        {
                            JsonLevel[] levels = GetLevels();
                            levels[_levelCount].TokenType = jsonTokenType;
                            if (_levelCount == 0)
                            {
                                levels[_levelCount].JsonPath = "$";
                            }
                            else if (jsonTokenType == JsonTokenType.StartObject)
                            {
                                // todo: avoid this if dest doesn't contain properties
                                levels[_levelCount].JsonPath = levels[_levelCount - 1].JsonPath + "." + levels[_levelCount].CurrentPropertyName;
                            }
                            else
                            {
                                // todo: avoid this if dest doesn't contain []
                                Debug.Assert(jsonTokenType == JsonTokenType.StartArray);
                                levels[_levelCount].JsonPath = levels[_levelCount - 1].JsonPath + "[" + levels[_levelCount].CurrentArrayLength + "]";
                            }

                            if (IsAtDestination())
                            {
                                value = true;
                                return true;
                            }

                            _levelCount++;
                            break;
                        }

                    case JsonTokenType.EndObject:
                    case JsonTokenType.EndArray:
                        _levelCount--;

                        // todo: path for SkipToEnd

                        break;

                    default:
                        {
                            Debug.Assert(
                                jsonTokenType == JsonTokenType.False ||
                                jsonTokenType == JsonTokenType.True ||
                                jsonTokenType == JsonTokenType.Null ||
                                jsonTokenType == JsonTokenType.Number ||
                                jsonTokenType == JsonTokenType.String);

                            JsonLevel[] levels = GetLevels();
                            JsonTokenType currentParentTokenType = levels[_levelCount - 1].TokenType;
                            Debug.Assert(
                                currentParentTokenType == JsonTokenType.StartArray ||
                                currentParentTokenType == JsonTokenType.StartObject);

                            if (currentParentTokenType == JsonTokenType.StartArray)
                            {
                                _levels![_levelCount - 1].CurrentArrayLength = _levels[_levelCount - 1].CurrentArrayLength + 1;
                            }

                            break;
                        }
                }
            }

            value = false;
            return false;

            void ReadPropertyName(ref Utf8JsonReader reader)
            {
                Debug.Assert(_continuation_propertyName == false);

                string? propertyName = reader.GetString();
                if (propertyName == null)
                {
                    throw new JsonException("todo; null property name not supported");
                }

                GetLevels()[_levelCount -1].CurrentPropertyName = propertyName;
            }

            bool IsAtDestination()
            {
                return GetLevels()[_levelCount - 1].JsonPath!.Equals(_destJsonPath, StringComparison.OrdinalIgnoreCase);
            }

            bool ShouldSkip()
            {
                JsonLevel level = GetLevels()[_levelCount - 1];
                string jsonPath = level.JsonPath!;
                int i = _destJsonPath.IndexOf(jsonPath);
                if (i < 0)
                {
                    return true;
                }

                if (level.TokenType == JsonTokenType.StartObject)
                {
                    Debug.Assert(level.CurrentPropertyName != null);
                    // Skip if current property does not match.
                    return _destJsonPath.IndexOf(level.CurrentPropertyName, i) == i;
                }
                else
                {
                    Debug.Assert(level.TokenType == JsonTokenType.StartArray);
                    // Skip if current index does not match.
                    return level.CurrentArrayLength != _levelCount;
                }
            }

            JsonLevel[] GetLevels()
            {
                if (_levels == null)
                {
                    _levels = new JsonLevel[JsonLevel.DefaultLevelSize];
                }
                else if (_levels.Length == _levelCount)
                {
                    Array.Resize(ref _levels, _levels.Length * 2);
                }
                else
                {
                    // Initalize in case re-used in a different tree branch.
                    _levels[_levelCount - 1].Clear();
                }

                return _levels;
            }
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
