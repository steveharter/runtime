// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// TypedReference is basically only ever seen on the call stack, and in param arrays.
// These are blob that must be dealt with by the compiler.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System
{
    [System.Runtime.Versioning.NonVersionable] // This only applies to field layout
    public ref partial struct TypedReference
    {
        private readonly ref byte _value;
        private IntPtr _type;

        private TypedReference(ref byte value, Type type)
        {
            _value = ref value!;
            _type = type.TypeHandle.Value;
        }

        public static TypedReference Make<T>(ref T value)
        {
            return new TypedReference(ref Unsafe.As<T, byte>(ref value), typeof(T));
        }

        /// <summary>
        /// Create a TypedReference using the specified type.
        /// Supports boxing
        /// </summary>
        /// <param name="value">A reference to the value. If null for a value type, a default value will be created.</param>
        /// <param name="type"></param>
        /// <returns></returns>
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2067:UnrecognizedReflectionPattern",
            Justification = "Activator.CreateInstance() only called on value types which have a public parameterless constructor")]
        public static TypedReference Make(ref object? value, Type type)
        {
            RuntimeType rtType = (RuntimeType)type;
            if (RuntimeTypeHandle.IsValueType(rtType))
            {
                if (value is null)
                {
                    if (rtType.IsNullableOfT)
                    {
                        value = RuntimeMethodHandle.ReboxToNullable(null, rtType);
                    }
                    else
                    {
                        value = Activator.CreateInstance(type);
                    }
                }

                BoxObject boxObject = Unsafe.As<BoxObject>(value);
                return new TypedReference(ref boxObject.FirstByte!, type);
            }

            if (value is not null)
            {
                if (!value.GetType().IsAssignableTo(type))
                {
                    throw new ArgumentException("Value's type {value.GetType()} is not compatible with {type}", nameof(value));
                }
            }

            return new TypedReference(ref Unsafe.As<object, byte>(ref value!), type);
        }

        internal static TypedReference Make(ref byte value, Type type)
        {
            return new TypedReference(ref value, type);
        }

        public ref T GetValue<T>()
        {
            return ref Unsafe.As<byte, T>(ref _value);
        }

        public static unsafe object? ToObject(TypedReference value)
        {
            TypeHandle typeHandle = new((void*)value._type);

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
                result = RuntimeHelpers.Box(pMethodTable, ref value._value);
            }
            else
            {
                result = Unsafe.As<byte, object>(ref value._value);
            }

            return result;
        }
    }
}
