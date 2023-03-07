// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace System.Reflection
{
    public partial class MethodInvoker
    {
        private readonly Signature _signature;
        internal readonly MethodBase _method;
        private readonly bool[]? _isValueType;
        internal InvocationFlags _invocationFlags;
        private readonly int _argCount;
        private readonly bool _isConstructor;
        private readonly bool _needsCopyBack;

        internal MethodInvoker(MethodBase method, Signature signature)
        {
            _method = method;
            _signature = signature;
            _argTypes = signature.Arguments;

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

            _argCount = _argTypes.Length;

            if (_argCount != 0)
            {
                _isValueType = new bool[_argCount];
                for (int i = 0; i < _argCount; i++)
                {
                    RuntimeType type = _argTypes[i];
                    if (RuntimeTypeHandle.IsByRef(type))
                    {
                        RuntimeType elementType = (RuntimeType)type.GetElementType();
                        _isValueType[i] = RuntimeTypeHandle.IsValueType(elementType);
                        _needsCopyBack |= elementType.IsNullableOfT;
                    }
                    else if (RuntimeTypeHandle.IsPointer(type))
                    {
                        _isValueType[i] = true;
                        RuntimeType elementType = (RuntimeType)type.GetElementType();
                        _needsCopyBack |= elementType.IsNullableOfT;
                    }
                    else
                    {
                        _isValueType[i] = RuntimeTypeHandle.IsValueType(type);
                        _needsCopyBack |= type.IsNullableOfT;
                    }
                }
            }

            if (_method is RuntimeConstructorInfo rci)
            {
                _isConstructor = true;
                _returnType = (RuntimeType)rci.DeclaringType!;
                _hasThis = false;
            }
            else if (_method is RuntimeMethodInfo rmi)
            {
                _returnType = (RuntimeType) rmi.ReturnParameter.ParameterType;
                _hasThis = !rmi.IsStatic;
            }
            else
            {
                DynamicMethod dm = (DynamicMethod)_method;
                _returnType = (RuntimeType) dm.ReturnParameter.ParameterType;
                _hasThis = !dm.IsStatic;
            }
        }

        internal bool NeedsCopyBack => _needsCopyBack;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe object? InterpretedInvoke(object? obj, IntPtr* arguments)
        {
            return RuntimeMethodHandle.InvokeMethod(obj, (void**)arguments, _signature, _isConstructor);
        }
    }
}
