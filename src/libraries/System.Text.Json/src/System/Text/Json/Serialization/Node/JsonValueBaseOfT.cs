// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization.Converters;

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// Supports dynamic numbers.
    /// </summary>
    public abstract class JsonValueBase<T> : JsonValue
    {
        internal T _value; // keep a field for direct access to avoid copies

        /// <summary>
        /// todo
        /// </summary>
        public T Value
        {
            get
            {
                return _value;
            }
            set
            {
                _value = value;
            }
        }

        /// <summary>
        /// todo
        /// </summary>
        /// <param name="value"></param>
        /// <param name="options"></param>
        public JsonValueBase(T value, JsonSerializerOptions? options = null) : base(options)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (value is JsonNode)
            {
                throw new ArgumentException("todo", nameof(value));
            }

            _value = value;
        }

        /// <summary>
        /// todo
        /// </summary>
        /// <typeparam name="TypeToReturn"></typeparam>
        /// <returns></returns>
        public override TypeToReturn To<TypeToReturn>()
        {
            if (TryConvert(out TypeToReturn result))
            {
                return result!;
            }

            throw new InvalidOperationException($"Cannot change type {_value!.GetType()} to {typeof(TypeToReturn)}.");
        }

        /// <summary>
        /// todo
        /// </summary>
        /// <param name="returnType"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        internal override bool TryConvert(Type returnType, out object? result)
        {
            // If no conversion is needed, just return the raw value.
            if (returnType == _value?.GetType())
            {
                result = _value;
                return true;
            }

            if (_value is JsonElement jsonElement)
            {
                if (TryConvertJsonElement(jsonElement, returnType, out result))
                {
                    return true;
                }

                // Use the raw bytes which may be recognized by converters such as the Enum converter than can process numbers.
                ReadOnlySpan<byte> span = jsonElement.GetRawUtf8();
                result = JsonSerializer.Deserialize(span, returnType, Options)!;
                return true;
            }

            try
            {
                if (_value == null)
                {
                    result = JsonSerializer.Deserialize($"null", returnType, Options);
                }
                else if (_value is string strValue)
                {
                    result = JsonSerializer.Deserialize($"\"{strValue}\"", returnType, Options);
                }
                else if (_value.GetType() == typeof(ReadOnlySpan<byte>))
                {
                    // todo: cannot cast to a ref struct
                    throw new NotImplementedException("ref struct");
                }
                else if (_value.GetType() == typeof(ReadOnlySpan<char>))
                {
                    // todo: cannot cast to a ref struct
                    throw new NotImplementedException("ref struct");
                }
                else
                {
                    throw new JsonException("Unsupported conversion");
                }
            }
            catch (JsonException)
            {
                result = default;
                return false;
            }

            return true;
        }

        private bool TryConvert<TypeToConvert>(out TypeToConvert result)
        {
            Type returnType = typeof(TypeToConvert);

            // If no conversion is needed, just return the raw value.
            if (_value is TypeToConvert)
            {
                result = (TypeToConvert)(object)_value;
                return true;
            }

            try
            {
                if (_value is JsonElement jsonElement)
                {
                    if (TryConvertJsonElement<TypeToConvert>(out result))
                    {
                        return true;
                    }

                    // Use the raw bytes which may be recognized by converters such as the Enum converter than can process numbers.
                    ReadOnlySpan<byte> span = jsonElement.GetRawUtf8();
                    result = JsonSerializer.Deserialize<TypeToConvert>(span, Options)!;
                    return true;
                }

                if (_value == null)
                {
                    result = JsonSerializer.Deserialize<TypeToConvert>($"null", Options)!;
                }
                else if (_value is string strValue)
                {
                    result = JsonSerializer.Deserialize<TypeToConvert>($"\"{strValue}\"", Options)!;
                }
                else if (_value.GetType() == typeof(ReadOnlySpan<byte>))
                {
                    // todo: cannot cast to a ref struct
                    throw new NotImplementedException("ref struct");
                }
                else if (_value.GetType() == typeof(ReadOnlySpan<char>))
                {
                    // todo: cannot cast to a ref struct
                    throw new NotImplementedException("ref struct");
                }
                else
                {
                    throw new InvalidOperationException("Unsupported conversion");
                }
            }
            catch (JsonException)
            {
                result = default!;
                return false;
            }

            return true;
        }

        private bool TryConvertJsonElement(in JsonElement jsonElement, Type returnType, out object? result)
        {
            bool success = false;
            result = default;

            switch (jsonElement.ValueKind)
            {
                case JsonValueKind.Number:
                    if (returnType == typeof(int))
                    {
                        success = jsonElement.TryGetInt32(out int value);
                        result = value;
                    }
                    else if (returnType == typeof(long))
                    {
                        success = jsonElement.TryGetInt64(out long value);
                        result = value;
                    }
                    else if (returnType == typeof(double))
                    {
                        success = jsonElement.TryGetDouble(out double value);
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

                    break;
                case JsonValueKind.String:
                    if (returnType == typeof(string))
                    {
                        result = jsonElement.GetString();
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

                    break;
                case JsonValueKind.True:
                case JsonValueKind.False:
                    if (returnType == typeof(bool))
                    {
                        result = jsonElement.GetBoolean();
                        success = true;
                    }
                    break;

                default:
                    break;
            }

            if (success)
            {
                return success;
            }

            // Use the raw test which may be recognized by converters such as the Enum converter than can process numbers.
            string strValue = jsonElement.GetRawText();

            try
            {
                result = JsonSerializer.Deserialize($"{strValue}", returnType, Options);
            }
            catch (JsonException)
            {
                return false;
            }

            return true;
        }

        internal bool TryConvertJsonElement<TypeToConvert>(out TypeToConvert result)
        {
            bool success;

            JsonElement element = (JsonElement)(object)_value!;
            Type returnType = typeof(TypeToConvert);

            switch (element.ValueKind)
            {
                case JsonValueKind.Number:
                    if (returnType == typeof(int))
                    {
                        success = element.TryGetInt32(out int value);
                        result = (TypeToConvert)(object)value;
                        return success;
                    }

                    if (returnType == typeof(long))
                    {
                        success = element.TryGetInt64(out long value);
                        result = (TypeToConvert)(object)value;
                        return success;
                    }

                    if (returnType == typeof(double))
                    {
                        success = element.TryGetDouble(out double value);
                        result = (TypeToConvert)(object)value;
                        return success;
                    }

                    if (returnType == typeof(short))
                    {
                        success = element.TryGetInt16(out short value);
                        result = (TypeToConvert)(object)value;
                        return success;
                    }

                    if (returnType == typeof(decimal))
                    {
                        success = element.TryGetDecimal(out decimal value);
                        result = (TypeToConvert)(object)value;
                        return success;
                    }

                    if (returnType == typeof(byte))
                    {
                        success = element.TryGetByte(out byte value);
                        result = (TypeToConvert)(object)value;
                        return success;
                    }

                    if (returnType == typeof(float))
                    {
                        success = element.TryGetSingle(out float value);
                        result = (TypeToConvert)(object)value;
                        return success;
                    }

                    else if (returnType == typeof(uint))
                    {
                        success = element.TryGetUInt32(out uint value);
                        result = (TypeToConvert)(object)value;
                        return success;
                    }

                    if (returnType == typeof(ushort))
                    {
                        success = element.TryGetUInt16(out ushort value);
                        result = (TypeToConvert)(object)value;
                        return success;
                    }

                    if (returnType == typeof(ulong))
                    {
                        success = element.TryGetUInt64(out ulong value);
                        result = (TypeToConvert)(object)value;
                        return success;
                    }

                    if (returnType == typeof(sbyte))
                    {
                        success = element.TryGetSByte(out sbyte value);
                        result = (TypeToConvert)(object)value;
                        return success;
                    }
                    break;

                case JsonValueKind.String:
                    if (returnType == typeof(string))
                    {
                        result = (TypeToConvert)(object)element.GetString()!; // todo: nullability
                        return true;
                    }

                    if (returnType == typeof(DateTime))
                    {
                        result = (TypeToConvert)(object)element.GetDateTime();
                        return true;
                    }

                    if (returnType == typeof(DateTimeOffset))
                    {
                        result = (TypeToConvert)(object)element.GetDateTimeOffset();
                        return true;
                    }

                    if (returnType == typeof(Guid))
                    {
                        result = (TypeToConvert)(object)element.GetGuid();
                        return true;
                    }
                    break;

                case JsonValueKind.True:
                case JsonValueKind.False:
                    if (returnType == typeof(bool))
                    {
                        result = (TypeToConvert)(object)element.GetBoolean();
                        return true;
                    }
                    break;
            }

            result = default!;
            return false;
        }

        /// <summary>
        /// todo
        /// </summary>
        /// <param name="writer"></param>
        public override void Serialize(Utf8JsonWriter writer)
        {
            Write(writer);
        }

        internal override void Write(Utf8JsonWriter writer)
        {
            if (_value is JsonElement jsonElement)
            {
                jsonElement.WriteTo(writer);
            }
            else
            {
                JsonSerializer.Serialize(writer, _value, _value!.GetType(), Options);
            }
        }
    }
}
