// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace System.Reflection
{
    public partial class MethodInvoker
    {
        private readonly Signature _signature;
        internal InvocationFlags _invocationFlags;

        internal MethodInvoker()
        {
            throw new NotSupportedException();
        }

        internal MethodInvoker(MethodBase method, Signature signature)
        {
            _method = method;
            _signature = signature;
            _functionPointer = method.MethodHandle.GetFunctionPointer();

            if (method is RuntimeMethodInfo rmi)
            {
                _returnType = rmi.ReturnType;
            }
            else
            {
                Debug.Assert(method is DynamicMethod);
                _returnType = ((DynamicMethod)method).ReturnType;
            }

            if (LocalAppContextSwitches.ForceInterpretedInvoke && !LocalAppContextSwitches.ForceEmitInvoke)
            {
                // Always use the native invoke; useful for testing.
                _strategyDetermined = true;
            }
            else if (LocalAppContextSwitches.ForceEmitInvoke && !LocalAppContextSwitches.ForceInterpretedInvoke)
            {
                // Always use emit invoke (if IsDynamicCodeCompiled == true); useful for testing.
                _invoked = true;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe object? InterpretedInvoke(object? obj, IntPtr* arguments)
        {
            return RuntimeMethodHandle.InvokeMethod(obj, (void**)arguments, _signature, isConstructor: false);
        }
    }
}
