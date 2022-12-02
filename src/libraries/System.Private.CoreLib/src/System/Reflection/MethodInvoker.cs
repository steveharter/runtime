// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static System.Reflection.InvokerEmitUtil;

namespace System.Reflection
{
    public sealed partial class MethodInvoker
    {
        private readonly MethodBase _method;
        private readonly Type _returnType;
        private readonly IntPtr _functionPointer;

        public static MethodInvoker GetInvoker(MethodInfo method)
        {
            if (method is RuntimeMethodInfo rmi)
            {
                return rmi.Invoker;
            }

            Debug.Assert(method is DynamicMethod);
            return ((DynamicMethod)method).Invoker;
        }

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
#if MONO // Temporary until Mono is updated.
        internal unsafe object? InlinedInvoke(object? obj, Span<object?> args, BindingFlags invokeAttr) => InterpretedInvoke(obj, args, invokeAttr);
#else
        internal unsafe object? InlinedInvoke(object? obj, IntPtr* args, BindingFlags invokeAttr)
        {
// todo: fast path

            return Invoke(obj, args, invokeAttr);
        }

        [CLSCompliant(false)]
        public void Invoke(TypedReference obj, TypedReference result)
        {
            unsafe
            {
                if (_invokeFunc != null)
                {
                    _invokeFunc(_functionPointer, obj, result, null);
                }
                else
                {
                    Invoke(obj, args: null, result);
                }
            }
        }

        [CLSCompliant(false)]
        public void Invoke(TypedReference obj, TypedReference arg1, TypedReference result)
        {
            // ParameterInfo[] parameters = _method.GetParametersNoCopy();
            // if (parameters.Length > 1)
            // {
            //     throw new InvalidOperationException("todo - not enough arguments");
            // }

            unsafe
            {
                StackAllocatedByRef byrefStorage = new(ref arg1.TargetRef);

#pragma warning disable 8500
                IntPtr* pByRefStorage = (IntPtr*)&byrefStorage;
#pragma warning restore 8500

                if (_invokeFunc != null)
                {
                    _invokeFunc(_functionPointer, obj, result, pByRefStorage);
                }
                else
                {
                    Invoke(obj, pByRefStorage, result);
                }
            }

            // todo: co- and contra variant
            //if (!_method.IsStatic && __reftype(obj) != _method.DeclaringType)
            //{
            //    throw new ArgumentException($"todo - wrong obj {__reftype(obj)} {_method.DeclaringType}");
            //}

            //if (parameters.Length != 0)
            //{
            //    // todo: co- and contra variant
            //    //if (__reftype(arg1) != parameters[0].ParameterType)
            //    //{
            //    //    throw new ArgumentException($"todo - wrong obj {__reftype(arg1)} {parameters[0].ParameterType}");
            //    //}
            //}

            //if (_returnType != typeof(void) && __reftype(result) != _returnType)
            //{
            //    throw new ArgumentException($"todo - wrong obj {__reftype(result)} {_returnType}");
            //}

        }

#if !MONO // Temporary until Mono is updated.
        private bool _invoked;
        private bool _strategyDetermined;
        private InvokerEmitUtil.InvokeFunc? _invokeFunc;

        //[DebuggerStepThrough]
        //[DebuggerHidden]
        private unsafe void Invoke(TypedReference obj, IntPtr* args, TypedReference result)
        {
            if (!_strategyDetermined)
            {   //todo: changeInterpretedInvoke to take refs
                //if (!_invoked)
                //{
                //    // The first time, ignoring race conditions, use the slow path.
                //    _invoked = true;
                //}
                //else
                //{
                //    if (RuntimeFeature.IsDynamicCodeCompiled)
                //    {
                        _invokeFunc = InvokerEmitUtil.CreateInvokeDelegate(_method);
                //    }
                    _strategyDetermined = true;
                //}
            }

            if (_invokeFunc != null)
            {
                _invokeFunc(_functionPointer, obj, result, args);
            }
            else
            {
                throw new NotSupportedException("here");
                //InterpretedInvoke(TypedReference.ToObject(obj), args);
            }
        }

        //[DebuggerStepThrough]
        //[DebuggerHidden]
        private unsafe object? Invoke(object? obj, IntPtr* args, BindingFlags invokeAttr)
        {
            if (!_strategyDetermined)
            {
                if (!_invoked)
                {
                    // The first time, ignoring race conditions, use the slow path.
                    _invoked = true;
                }
                else
                {
                    if (RuntimeFeature.IsDynamicCodeCompiled)
                    {
                        _invokeFunc = InvokerEmitUtil.CreateInvokeDelegate(_method);
                    }
                    _strategyDetermined = true;
                }
            }

            TypedReference obj_tr = default;
            ref object? obj_local = ref Unsafe.AsRef(obj);
            if (obj != null)
            {
                obj_tr = TypedReference.FromObject(ref obj_local, _method.DeclaringType!);
            }

            object? ret = null;
            TypedReference ret_tr = default;
            ref object? ret_local = ref Unsafe.AsRef(ret);
            if (_returnType != typeof(void))
            {
                ret_tr = TypedReference.FromObject(ref ret_local, _returnType);
            }

            if ((invokeAttr & BindingFlags.DoNotWrapExceptions) == 0)
            {
                try
                {
                    if (_invokeFunc != null)
                    {
                        _invokeFunc(_functionPointer, obj_tr, ret_tr, args);
                    }
                    else
                    {
                        ret = InterpretedInvoke(obj, args);
                    }
                }
                catch (Exception e)
                {
                    throw new TargetInvocationException(e);
                }
            }
            // else if (_invokeFunc != null)
            // {
            //     //_invokeFunc(TypedReference.FromObject(ref obj, _method.DeclaringType!), __makeref(ret), args);
            //     _invokeFunc(_functionPointer, obj_tr, ret_tr, args);
            // }
            else
            {
                ret = InterpretedInvoke(obj, args);
            }

            return ret;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal readonly ref struct StackAllocatedByRef
    {
        public StackAllocatedByRef(ref byte arg)
        {
            _arg = ref arg;
        }

        internal readonly ref byte _arg;
    }
}
