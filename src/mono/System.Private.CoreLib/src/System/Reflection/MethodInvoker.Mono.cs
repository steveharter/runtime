// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Reflection
{
    public partial class MethodInvoker
    {
        internal readonly MethodBase _method;
        private readonly bool[]? _isValueType;
        internal InvocationFlags _invocationFlags;
        internal readonly int _argCount;
        private readonly bool _isConstructor;
        private readonly bool _needsCopyBack;
        internal readonly bool _needsRefs;

        public static MethodInvoker GetInvoker(MethodBase method)
        {
            ArgumentNullException.ThrowIfNull(method);

            // todo: add the virtual and make public

            if (method is RuntimeConstructorInfo rci)
            {
                return rci.Invoker;
            }

            Debug.Assert(method is RuntimeMethodInfo);
            return ((RuntimeMethodInfo)method).Invoker;
        }

        internal MethodInvoker(MethodBase method, RuntimeType[] argTypes)
        {
            _method = method;
            _argTypes = argTypes;

            if (LocalAppContextSwitches.ForceInterpretedInvoke && !LocalAppContextSwitches.ForceEmitInvoke)
            {
                // Always use the native invoke; useful for testing.
                _strategyDetermined = true;
            }
            else if (LocalAppContextSwitches.ForceEmitInvoke && !LocalAppContextSwitches.ForceInterpretedInvoke)
            {
                // Always use emit invoke (if IsDynamicCodeSupported == true); useful for testing.
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
                        _needsRefs = true;
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
                    else if (RuntimeTypeHandle.IsByRefLike(type))
                    {
                        _needsRefs = true;
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
                throw new NotSupportedException("todo");
            }

            _needsRefs |= _returnType.IsByRef;
        }

        internal bool NeedsCopyBack => _needsCopyBack;

        private unsafe object? InterpretedInvoke(object? obj, IntPtr *args)
        {
            Exception? exc;
            object? o;

            if (_method is RuntimeConstructorInfo rci)
            {
                o = rci.InternalInvoke(obj, args, out exc);
            }
            else
            {
                o = ((RuntimeMethodInfo)_method).InternalInvoke(obj, args, out exc);
            }

            if (exc != null)
                throw exc;

            return o;
        }
    }
}
