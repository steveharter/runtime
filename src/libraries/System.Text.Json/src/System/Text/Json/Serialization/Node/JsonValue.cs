// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// Supports dynamic numbers.
    /// </summary>
    public class JsonValue : JsonNode
    {
        private Type? _type;
        private object? _value;
        private object? _lastValue;

        /// <summary>
        /// todo
        /// </summary>
        /// <param name="value"></param>
        /// <param name="options"></param>
        public JsonValue(object? value, JsonSerializerOptions? options = null) : base(options)
        {
            Value = value;
        }

        /// <summary>
        /// todo
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public override T GetValue<T>()
        {
            if (TryConvert(typeof(T), out object? result))
            {
                return (T)result!;
            }

            throw new InvalidOperationException($"Cannot change type {_value!.GetType()} to {typeof(T)}.");
        }

        /// <summary>
        /// todo
        /// </summary>
        /// <param name="returnType"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        internal override bool TryConvert(Type returnType, out object? result)
        {
            if (returnType == _type || returnType == typeof(object))
            {
                result = _lastValue;
                return true;
            }

            string? strValue;

            if (!(_value is JsonElement jsonElement))
            {
                if (_value == null)
                {
                    strValue = "null";
                }
                else
                {
                    strValue = _value is string ? $"\"{_value!}\"" : _value!.ToString();
                }
            }
            else
            {
                bool success = false;
                result = null;

                if (returnType == typeof(long))
                {
                    success = jsonElement.TryGetInt64(out long value);
                    result = value;
                }
                else if (returnType == typeof(double))
                {
                    success = jsonElement.TryGetDouble(out double value);
                    result = value;
                }
                else if (returnType == typeof(int))
                {
                    success = jsonElement.TryGetInt32(out int value);
                    result = value;
                }
                else if (returnType == typeof(short))
                {
                    success = jsonElement.TryGetInt16(out short value);
                    result = value;
                }
                else if (returnType == typeof(decimal))
                {
                    success = jsonElement.TryGetDecimal(out decimal value);
                    result = value;
                }
                else if (returnType == typeof(byte))
                {
                    success = jsonElement.TryGetByte(out byte value);
                    result = value;
                }
                else if (returnType == typeof(float))
                {
                    success = jsonElement.TryGetSingle(out float value);
                    result = value;
                }
                else if (returnType == typeof(uint))
                {
                    success = jsonElement.TryGetUInt32(out uint value);
                    result = value;
                }
                else if (returnType == typeof(ushort))
                {
                    success = jsonElement.TryGetUInt16(out ushort value);
                    result = value;
                }
                else if (returnType == typeof(ulong))
                {
                    success = jsonElement.TryGetUInt64(out ulong value);
                    result = value;
                }
                else if (returnType == typeof(sbyte))
                {
                    success = jsonElement.TryGetSByte(out sbyte value);
                    result = value;
                }
                else if (returnType == typeof(string))
                {
                    result = jsonElement.GetString();
                    success = true;
                }
                else if (returnType == typeof(bool))
                {
                    result = jsonElement.GetBoolean();
                    success = true;
                }
                else if (returnType == typeof(DateTime))
                {
                    result = jsonElement.GetDateTime();
                    success = true;
                }
                else if (returnType == typeof(DateTimeOffset))
                {
                    result = jsonElement.GetDateTimeOffset();
                    success = true;
                }
                else if (returnType == typeof(Guid))
                {
                    result = jsonElement.GetGuid();
                    success = true;
                }

                if (success)
                {
                    _lastValue = result;
                    _type = result?.GetType();
                    return true;
                }
                else
                {
                    // Use the raw test which may be recognized by converters such as the Enum converter than can process numbers.
                    strValue = jsonElement.GetRawText();
                }
            }

            try
            {
                result = _lastValue = JsonSerializer.Deserialize($"{strValue}", returnType, Options);
            }
            catch (JsonException)
            {
                result = default;
                return false;
            }

            _type = _lastValue?.GetType();
            return true;

        }

        /// <summary>
        /// todo
        /// </summary>
        /// <param name="value"></param>
        public override object? Value
        {
            get
            {
                return _value;
            }
            set
            {
                _value = _lastValue = value;
                _type = value?.GetType();
            }
        }
    }
}
