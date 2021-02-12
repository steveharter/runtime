// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json.Serialization
{
    public partial class JsonNode
    {
        /// <summary>
        /// todo
        /// </summary>
        /// <param name="value"></param>
        public static implicit operator JsonNode(bool value) => new JsonValue<bool>(value);

        /// <summary>
        /// todo
        /// </summary>
        /// <param name="value"></param>
        public static implicit operator JsonNode(byte value) => new JsonValue<byte>(value);

        /// <summary>
        /// todo
        /// </summary>
        /// <param name="value"></param>
        public static implicit operator JsonNode(DateTime value) => new JsonValue<DateTime>(value);

        /// <summary>
        /// todo
        /// </summary>
        /// <param name="value"></param>
        public static implicit operator JsonNode(DateTimeOffset value) => new JsonValue<DateTimeOffset>(value);

        /// <summary>
        /// todo
        /// </summary>
        /// <param name="value"></param>
        public static implicit operator JsonNode(decimal value) => new JsonValue<decimal>(value);

        /// <summary>
        /// todo
        /// </summary>
        /// <param name="value"></param>
        public static implicit operator JsonNode(double value) => new JsonValue<double>(value);

        /// <summary>
        /// todo
        /// </summary>
        /// <param name="value"></param>
        public static implicit operator JsonNode(Guid value) => new JsonValue<Guid>(value);

        /// <summary>
        /// todo
        /// </summary>
        /// <param name="value"></param>
        public static implicit operator JsonNode(short value) => new JsonValue<short>(value);

        /// <summary>
        /// todo
        /// </summary>
        /// <param name="value"></param>
        public static implicit operator JsonNode(int value) => new JsonValue<int>(value);

        /// <summary>
        /// todo
        /// </summary>
        /// <param name="value"></param>
        public static implicit operator JsonNode(long value) => new JsonValue<long>(value);

        /// <summary>
        /// todo
        /// </summary>
        /// <param name="value"></param>
        [System.CLSCompliantAttribute(false)]
        public static implicit operator JsonNode(sbyte value) => new JsonValue<sbyte>(value);

        /// <summary>
        /// todo
        /// </summary>
        /// <param name="value"></param>
        public static implicit operator JsonNode(float value) => new JsonValue<float>(value);

        /// <summary>
        /// todo
        /// </summary>
        /// <param name="value"></param>
        public static implicit operator JsonNode?(string? value) =>
            (value == null ? null : new JsonValue<string>(value));

        /// <summary>
        /// todo
        /// </summary>
        /// <param name="value"></param>
        public static implicit operator JsonNode(char value) => new JsonValue<char>(value);

        /// <summary>
        /// todo
        /// </summary>
        /// <param name="value"></param>
        [System.CLSCompliantAttribute(false)]
        public static implicit operator JsonNode(ushort value) => new JsonValue<ushort>(value);

        /// <summary>
        /// todo
        /// </summary>
        /// <param name="value"></param>
        [System.CLSCompliantAttribute(false)]
        public static implicit operator JsonNode(uint value) => new JsonValue<uint>(value);

        /// <summary>
        /// todo
        /// </summary>
        /// <param name="value"></param>
        [System.CLSCompliantAttribute(false)]
        public static implicit operator JsonNode(ulong value) => new JsonValue<ulong>(value);


        /// <summary>
        /// todo
        /// </summary>
        /// <param name="value"></param>
        public static explicit operator bool(JsonNode value) => value.GetValue<bool>();

        /// <summary>
        /// todo
        /// </summary>
        /// <param name="value"></param>
        public static explicit operator byte(JsonNode value) => value.GetValue<byte>();

        /// <summary>
        /// todo
        /// </summary>
        /// <param name="value"></param>
        public static explicit operator DateTime(JsonNode value) => value.GetValue<DateTime>();

        /// <summary>
        /// todo
        /// </summary>
        /// <param name="value"></param>
        public static explicit operator DateTimeOffset(JsonNode value) => value.GetValue<DateTimeOffset>();

        /// <summary>
        /// todo
        /// </summary>
        /// <param name="value"></param>
        public static explicit operator decimal(JsonNode value) => value.GetValue<decimal>();

        /// <summary>
        /// todo
        /// </summary>
        /// <param name="value"></param>
        public static explicit operator double(JsonNode value) => value.GetValue<double>();

        /// <summary>
        /// todo
        /// </summary>
        /// <param name="value"></param>
        public static explicit operator Guid(JsonNode value) => value.GetValue<Guid>();

        /// <summary>
        /// todo
        /// </summary>
        /// <param name="value"></param>
        public static explicit operator short(JsonNode value) => value.GetValue<short>();

        /// <summary>
        /// todo
        /// </summary>
        /// <param name="value"></param>
        public static explicit operator int(JsonNode value) => value.GetValue<int>();

        /// <summary>
        /// todo
        /// </summary>
        /// <param name="value"></param>
        public static explicit operator long(JsonNode value) => value.GetValue<long>();

        /// <summary>
        /// todo
        /// </summary>
        /// <param name="value"></param>
        [System.CLSCompliantAttribute(false)]
        public static explicit operator sbyte(JsonNode value) => value.GetValue<sbyte>();

        /// <summary>
        /// todo
        /// </summary>
        /// <param name="value"></param>
        public static explicit operator float(JsonNode value) => value.GetValue<float>();

        /// <summary>
        /// todo
        /// </summary>
        /// <param name="value"></param>
        public static explicit operator string(JsonNode value) => value.GetValue<string>();

        /// <summary>
        /// todo
        /// </summary>
        /// <param name="value"></param>
        public static explicit operator char(JsonNode value) => value.GetValue<char>();

        /// <summary>
        /// todo
        /// </summary>
        /// <param name="value"></param>
        [System.CLSCompliantAttribute(false)]
        public static explicit operator ushort(JsonNode value) => value.GetValue<ushort>();

        /// <summary>
        /// todo
        /// </summary>
        /// <param name="value"></param>
        [System.CLSCompliantAttribute(false)]
        public static explicit operator uint(JsonNode value) => value.GetValue<uint>();

        /// <summary>
        /// todo
        /// </summary>
        /// <param name="value"></param>
        [System.CLSCompliantAttribute(false)]
        public static explicit operator ulong(JsonNode value) => value.GetValue<ulong>();
    }
}
