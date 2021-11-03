// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


namespace System.Text.Json.Serialization.Converters
{
    internal partial class CursorConverter : JsonResumableConverter<bool>
    {
        public struct JsonLevel
        {
            public const int DefaultLevelSize = 3;

            public JsonTokenType TokenType;
            public string? JsonPath;

            public int CurrentArrayLength;
            public string? CurrentPropertyName;

            public void Clear()
            {
                TokenType = JsonTokenType.None;
                JsonPath = null!;
                CurrentArrayLength = 0;
                CurrentPropertyName = null;
            }
        }
    }
}
