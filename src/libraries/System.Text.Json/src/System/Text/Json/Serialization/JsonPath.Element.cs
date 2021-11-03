// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json.Serialization
{
    internal partial struct JsonPath
    {
        internal readonly struct Element
        {
            public readonly string? Name { get; init; }
            public readonly int Index { get; init; }
            public bool StringWithSubscript { get; init; }
        }
    }
}
