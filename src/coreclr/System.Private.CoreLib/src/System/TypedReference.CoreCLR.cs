// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// TypedReference is basically only ever seen on the call stack, and in param arrays.
// These are blob that must be dealt with by the compiler.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System
{
    [System.Runtime.Versioning.NonVersionable] // This only applies to field layout
    public ref partial struct TypedReference
    {
        private readonly ref byte _value;
        private readonly IntPtr _type;

        internal unsafe TypedReference(Type type, ref byte value)
        {
            if (type is RuntimeType rtType)
            {
                _value = ref value;
                _type = rtType.m_handle;
            }
            else
            {
                throw new ArgumentException("todo");
            }
        }

        public static unsafe object? ToObject(TypedReference value) => ToObject(value._type, ref value._value);

        internal static unsafe object? ToObject(RuntimeType rtType, ref byte value) => ToObject(rtType.m_handle, ref value);

        private static unsafe object? ToObject(IntPtr type, ref byte value)
        {
            TypeHandle typeHandle = new((void*)type);

            if (typeHandle.IsNull)
            {
                ThrowHelper.ThrowArgumentException_ArgumentNull_TypedRefType();
            }

            // The only case where a type handle here might be a type desc is when the type is either a
            // pointer or a function pointer. In those cases, just always return the method table pointer
            // for System.UIntPtr without inspecting the type desc any further. Otherwise, the type handle
            // is just wrapping a method table pointer, so return that directly with a reinterpret cast.
            MethodTable* pMethodTable = typeHandle.IsTypeDesc
                ? TypeHandle.TypeHandleOf<UIntPtr>().AsMethodTable()
                : typeHandle.AsMethodTable();

            Debug.Assert(pMethodTable is not null);

            object? result;

            if (pMethodTable->IsValueType)
            {
                result = RuntimeHelpers.Box(pMethodTable, ref value);
            }
            else
            {
                result = Unsafe.As<byte, object>(ref value);
            }

            return result;
        }
    }
}
