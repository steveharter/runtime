// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Dynamic;
using System.Reflection;

namespace System.Text.Json.Serialization
{
    internal interface IJsonDynamicMetaObjectProvider : IDynamicMetaObjectProvider
    {
        MethodInfo? TryGetMemberMethodInfo { get; }
        MethodInfo? TrySetMemberMethodInfo { get; }
        MethodInfo? TryGetIndexMethodInfo { get; }
        MethodInfo? TrySetIndexMethodInfo { get; }
        MethodInfo? TryConvertMethodInfo { get; }
    }
}
