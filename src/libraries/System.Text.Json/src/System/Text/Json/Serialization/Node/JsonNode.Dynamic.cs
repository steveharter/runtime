// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if BUILDING_INBOX_LIBRARY

using System.Dynamic;
using System.Linq.Expressions;
using System.Reflection;

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// The base class for all dynamic types supported by the serializer.
    /// </summary>
    public partial class JsonNode : IDynamicMetaObjectProvider
    {
        internal virtual MethodInfo? TryGetMemberMethodInfo => null;
        internal virtual MethodInfo? TrySetMemberMethodInfo => null;
        internal virtual MethodInfo? TryGetIndexMethodInfo => null;
        internal virtual MethodInfo? TrySetIndexMethodInfo => null;
        internal virtual MethodInfo? TryConvertMethodInfo => null;

        DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter) =>
            new MetaDynamic(parameter, this);
    }
}

#endif
