// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if BUILDING_INBOX_LIBRARY

using System.Dynamic;
using System.Reflection;

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// Supports dynamic numbers.
    /// </summary>
    public partial class JsonValue
    {
        internal bool TryConvertCallback(ConvertBinder binder, out object? result)
        {
            return TryConvert(binder.ReturnType, out result);
        }

        private static MethodInfo GetMethod(string name) => typeof(JsonValue).GetMethod(
            name, BindingFlags.Instance | BindingFlags.NonPublic)!;

        private static MethodInfo? s_TryConvert;
        internal override MethodInfo? TryConvertMethodInfo =>
            s_TryConvert ??
            (s_TryConvert = GetMethod(nameof(TryConvertCallback)));
    }
}

#endif
