// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if BUILDING_INBOX_LIBRARY

using System.Dynamic;
using System.Reflection;

namespace System.Text.Json.Serialization
{
    public partial class JsonArray
    {
        internal bool TryGetIndexCallback(GetIndexBinder binder, object[] indexes, out object? result)
        {
            result = List[(int)indexes[0]];
            return true;
        }

        internal bool TrySetIndexCallback(SetIndexBinder binder, object[] indexes, object? value)
        {
            JsonNode? node = null;
            if (value != null)
            {
                node = value as JsonNode;
                if (node == null)
                {
                    node = new JsonValue<object>(value, Options);
                }
            }

            List[(int)indexes[0]] = node;
            return true;
        }

        private static MethodInfo GetMethod(string name) => typeof(JsonArray).GetMethod(
            name, BindingFlags.Instance | BindingFlags.NonPublic)!;

        private static MethodInfo? s_TryGetIndex;
        internal override MethodInfo? TryGetIndexMethodInfo =>
            s_TryGetIndex ??
            (s_TryGetIndex = GetMethod(nameof(TryGetIndexCallback)));

        private static MethodInfo? s_TrySetIndex;
        internal override MethodInfo? TrySetIndexMethodInfo =>
            s_TrySetIndex ??
            (s_TrySetIndex = GetMethod(nameof(TrySetIndexCallback)));
    }
}

#endif
