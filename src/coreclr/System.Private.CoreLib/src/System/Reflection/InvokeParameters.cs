// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Reflection
{
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe ref struct InvokeParameters
    {
        public const int MaxStackAllocArgCount = 4;

        internal object? _arg0;
        internal object? _arg1;
        internal object? _arg2;
        internal object? _arg3;

        // We need to perform type safety validation against the incoming arguments, but we also need
        // to be resilient against the possibility that some other thread (or even the binder itself!)
        // may mutate the array after we've validated the arguments but before we've properly invoked
        // the method. The solution is to copy the arguments to a different, not-user-visible buffer
        // as we validate them. n.b. This disallows use of ArrayPool, as ArrayPool-rented arrays are
        // considered user-visible to threads which may still be holding on to returned instances.
        // This separate array is also used to hold default values when 'null' is specified for value
        // types, and also used to hold the results from conversions such as from Int16 to Int32. For
        // compat, these default values and conversions are not applied to the incoming arguments.
        internal readonly object?[]? _heapAllocatedObjects;
        private readonly int _parameterCount;

        private readonly ReadOnlySpan<object?> _span;

        public InvokeParameters(object? parameter)
        {
            _parameterCount = 1;
            _arg0 = parameter;
        }

        public InvokeParameters(int parameterCount)
        {
            _parameterCount = parameterCount;

            if (parameterCount > MaxStackAllocArgCount)
            {
                _heapAllocatedObjects = new object[parameterCount];
            }
        }

        public int ParameterCount => _parameterCount;
    }
}
