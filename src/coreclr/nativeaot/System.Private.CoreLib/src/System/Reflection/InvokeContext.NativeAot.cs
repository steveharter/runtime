// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Reflection
{
    public unsafe ref partial struct InvokeContext
    {
    }

    internal static class InvokerEmitUtil
    {
#pragma warning disable IDE0060
        public static unsafe Func<object?, object?, object?, object?, object?> CreateInvokeDelegate_Obj3(MethodBase method) => throw new NotSupportedException();
#pragma warning restore IDE0060
    }
}
