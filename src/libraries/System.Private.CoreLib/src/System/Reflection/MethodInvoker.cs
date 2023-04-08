// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection.Emit;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static System.Runtime.CompilerServices.RuntimeHelpers;

namespace System.Reflection
{
    public sealed partial class MethodInvoker
    {
        internal const int MaxStackAllocArgCount = 4;

        private bool _invoked;
        private bool _strategyDetermined; private bool _strategyDetermined2;
        internal InvokerEmitUtil.InvokeFunc? _invokeFunc; // todo:remove this once we get InvokeFunc2 working
        internal InvokerEmitUtil.InvokeFunc_Ref? _invokeFunc_Ref;
        internal InvokerEmitUtil.InvokeFunc_Obj? _invokeFunc_Obj;
        internal readonly RuntimeType[] _argTypes;
        internal readonly RuntimeType _returnType;
        internal readonly bool _hasThis;

        public static MethodInvoker GetInvoker(MethodBase method)
        {
            ArgumentNullException.ThrowIfNull(method);

            // todo: add the virtual and make public

            if (method is RuntimeConstructorInfo rci)
            {
                return rci.Invoker;
            }

            if (method is RuntimeMethodInfo rmi)
            {
                return rmi.Invoker;
            }

            Debug.Assert(method is DynamicMethod);
            return ((DynamicMethod)method).Invoker;
        }

        private void DetermineStrategy()
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

        private void DetermineStrategy2()
        {
            if (!_strategyDetermined2)
            {
                // todo: remove this once done with testing
                _invokeFunc_Ref = InvokerEmitUtil.CreateInvokeDelegate_Ref(_method);
                _invokeFunc_Obj = InvokerEmitUtil.CreateInvokeDelegate_Obj(_method);

                _strategyDetermined2 = true;
            }
        }

        //[DebuggerStepThrough]
        //[DebuggerHidden]
        internal unsafe object? Invoke(object? obj, Span<object?> args, BindingFlags invokeAttr)
        {
            if (!_strategyDetermined)
            {
                DetermineStrategy();
            }

            int argCount = args.Length;
            object? ret;

            if (argCount < MaxStackAllocArgCount)
            {
                scoped StackAllocatedByRefs byrefs = default;
#pragma warning disable CS8500
                IntPtr* pByRefStorage = (IntPtr*)&byrefs._arg0;
#pragma warning restore CS8500

                for (int i = 0; i < argCount; i++)
                {
#pragma warning disable CS8500
                    *(ByReference*)(pByRefStorage + i) = _isValueType![i] ?
#pragma warning restore CS8500
                        ByReference.Create(ref args[i]!.GetRawData()) :
                        ByReference.Create(ref args[i]);
                }

                if ((invokeAttr & BindingFlags.DoNotWrapExceptions) == 0)
                {
                    try
                    {
                        if (_invokeFunc is not null)
                        {
                            ret = _invokeFunc(obj, pByRefStorage);
                        }
                        else
                        {
                            ret = InterpretedInvoke(obj, pByRefStorage);
                        }
                    }
                    catch (Exception e)
                    {
                        throw new TargetInvocationException(e);
                    }
                }
                else if (_invokeFunc is not null)
                {
                    ret = _invokeFunc(obj, pByRefStorage);
                }
                else
                {
                    ret = InterpretedInvoke(obj, pByRefStorage);
                }
            }
            else
            {
                // We don't check a max stack size since we are invoking a method which
                // naturally requires a stack size that is dependent on the arg count\size.
                IntPtr* pByRefStorage = stackalloc IntPtr[argCount];
                NativeMemory.Clear(pByRefStorage, (uint)(argCount * sizeof(IntPtr)));
                RuntimeImports.GCFrameRegistration reg = new((void**)pByRefStorage, (uint)argCount, areByRefs: true);

                try
                {
                    RuntimeImports.RhRegisterForGCReporting(&reg);

                    for (int i = 0; i < argCount; i++)
                    {
                        ByReference v = _isValueType![i] ?
                            ByReference.Create(ref args[i]!.GetRawData()) :
                            ByReference.Create(ref args[i]!);

#pragma warning disable CS8500
                        *(ByReference*)(pByRefStorage + i) = v;
#pragma warning restore CS8500
                    }

                    if ((invokeAttr & BindingFlags.DoNotWrapExceptions) == 0)
                    {
                        try
                        {
                            if (_invokeFunc is not null)
                            {
                                ret = _invokeFunc(obj, pByRefStorage);
                            }
                            else
                            {
                                ret = InterpretedInvoke(obj, pByRefStorage);
                            }
                        }
                        catch (Exception e)
                        {
                            throw new TargetInvocationException(e);
                        }
                    }
                    else
                    {
                        if (_invokeFunc is not null)
                        {
                            ret = _invokeFunc(obj, pByRefStorage);
                        }
                        else
                        {
                            ret = InterpretedInvoke(obj, pByRefStorage);
                        }
                    }
                }
                finally
                {
                    RuntimeImports.RhUnregisterForGCReporting(&reg);
                }
            }

            return ret;
        }

        //[DebuggerStepThrough]
        //[DebuggerHidden]
        //        public unsafe object? InvokeOneParameter(object? obj, object? parameter, BindingFlags invokeAttr)
        //        {
        //            if (!_strategyDetermined)
        //            {
        //                DetermineStrategy();
        //            }

        //            object? ret;

        //            ByReference byRef = _isValueType![0] ?
        //                ByReference.Create(ref parameter!.GetRawData()) :
        //                ByReference.Create(ref parameter);

        //#pragma warning disable CS8500
        //            IntPtr* pByRefStorage = (IntPtr*)&byRef;
        //#pragma warning restore CS8500

        //            if ((invokeAttr & BindingFlags.DoNotWrapExceptions) == 0)
        //            {
        //                try
        //                {
        //                    if (_invokeFunc is not null)
        //                    {
        //                        ret = _invokeFunc(obj, pByRefStorage);
        //                    }
        //                    else
        //                    {
        //                        ret = InterpretedInvoke(obj, pByRefStorage);
        //                    }
        //                }
        //                catch (Exception e)
        //                {
        //                    throw new TargetInvocationException(e);
        //                }
        //            }
        //            else if (_invokeFunc is not null)
        //            {
        //                ret = _invokeFunc(obj, pByRefStorage);
        //            }
        //            else
        //            {
        //                ret = InterpretedInvoke(obj, pByRefStorage);
        //            }

        //            return ret;
        //        }

        // For the rarely used scenario of calling the constructor directly through MethodBase.Invoke()
        // with a non-null 'obj', we always use the slower InterpretedInvoke path to avoid having two emit-based delegates.
        // We also keep as a separate method to avoid having to special case the standard Invoke() above.
        internal unsafe object? InvokeConstructorWithoutAlloc(object? obj, Span<object?> args, BindingFlags invokeAttr)
        {
            Debug.Assert(_method is RuntimeConstructorInfo);

            int argCount = args.Length;
            object? ret;
            if (argCount < MaxStackAllocArgCount)
            {
                scoped StackAllocatedByRefs byrefs = default;
#pragma warning disable CS8500
                IntPtr* pByRefStorage = (IntPtr*)&byrefs._arg0;
#pragma warning restore CS8500

                for (int i = 0; i < argCount; i++)
                {
#pragma warning disable CS8500
                    *(ByReference*)(pByRefStorage + i) = _isValueType![i] ?
#pragma warning restore CS8500
                        ByReference.Create(ref args[i]!.GetRawData()) :
                        ByReference.Create(ref args[i]);
                }

                if ((invokeAttr & BindingFlags.DoNotWrapExceptions) == 0)
                {
                    try
                    {
                        ret = InterpretedInvoke(obj, pByRefStorage);
                    }
                    catch (Exception e)
                    {
                        throw new TargetInvocationException(e);
                    }
                }
                else if (_invokeFunc is not null)
                {
                    ret = _invokeFunc(obj, pByRefStorage);
                }
                else
                {
                    ret = InterpretedInvoke(obj, pByRefStorage);
                }
            }
            else
            {
                // We don't check a max stack size since we are invoking a method which
                // naturally requires a stack size that is dependent on the arg count\size.
                IntPtr* pByRefStorage = stackalloc IntPtr[argCount];
                NativeMemory.Clear(pByRefStorage, (uint)(argCount * sizeof(IntPtr)));
                RuntimeImports.GCFrameRegistration reg = new((void**)pByRefStorage, (uint)argCount, areByRefs: true);

                try
                {
                    RuntimeImports.RhRegisterForGCReporting(&reg);

                    for (int i = 0; i < argCount; i++)
                    {
                        ByReference v = _isValueType![i] ?
                            ByReference.Create(ref args[i]!.GetRawData()) :
                            ByReference.Create(ref args[i]!);

#pragma warning disable CS8500
                        *(ByReference*)(pByRefStorage + i) = v;
#pragma warning restore CS8500
                    }

                    if ((invokeAttr & BindingFlags.DoNotWrapExceptions) == 0)
                    {
                        try
                        {
                            ret = InterpretedInvoke(obj, pByRefStorage);
                        }
                        catch (Exception e)
                        {
                            throw new TargetInvocationException(e);
                        }
                    }
                    else
                    {
                        ret = InterpretedInvoke(obj, pByRefStorage);
                    }
                }
                finally
                {
                    RuntimeImports.RhUnregisterForGCReporting(&reg);
                }
            }

            return ret;
        }

        public unsafe object? InvokeDirect(object? obj, Span<object?> parameters)
        {
            if (!_strategyDetermined)
            {
                DetermineStrategy();
            }

            // todo: fix up nullables (and pointers?)

            int argCount = parameters.Length;
            object? ret;

            if (argCount < MaxStackAllocArgCount)
            {
                scoped StackAllocatedByRefs byrefs = default;
#pragma warning disable CS8500
                IntPtr* pByRefStorage = (IntPtr*)&byrefs._arg0;
#pragma warning restore CS8500

                for (int i = 0; i < argCount; i++)
                {
#pragma warning disable CS8500
                    *(ByReference*)(pByRefStorage + i) = _isValueType![i] ?
#pragma warning restore CS8500
                        ByReference.Create(ref parameters[i]!.GetRawData()) :
                        ByReference.Create(ref parameters[i]);
                }

                if (_invokeFunc is not null)
                {
                    ret = _invokeFunc(obj, pByRefStorage);
                }
                else
                {
                    ret = InterpretedInvoke(obj, pByRefStorage);
                }
            }
            else
            {
                // We don't check a max stack size since we are invoking a method which
                // naturally requires a stack size that is dependent on the arg count\size.
                IntPtr* pByRefStorage = stackalloc IntPtr[argCount];
                NativeMemory.Clear(pByRefStorage, (uint)(argCount * sizeof(IntPtr)));
                RuntimeImports.GCFrameRegistration reg = new((void**)pByRefStorage, (uint)argCount, areByRefs: true);

                try
                {
                    RuntimeImports.RhRegisterForGCReporting(&reg);

                    for (int i = 0; i < argCount; i++)
                    {
                        ByReference v = _isValueType![i] ?
                            ByReference.Create(ref parameters[i]!.GetRawData()) :
                            ByReference.Create(ref parameters[i]!);

#pragma warning disable CS8500
                        *(ByReference*)(pByRefStorage + i) = v;
#pragma warning restore CS8500
                    }

                    if (_invokeFunc is not null)
                    {
                        ret = _invokeFunc(obj, pByRefStorage);
                    }
                    else
                    {
                        ret = InterpretedInvoke(obj, pByRefStorage);
                    }
                }
                finally
                {
                    RuntimeImports.RhUnregisterForGCReporting(&reg);
                }
            }

            return ret;
        }

        internal unsafe object? InvokeNoParams(object? obj, BindingFlags invokeAttr)
        {
            if (!_strategyDetermined)
            {
                DetermineStrategy();
            }

            object? ret;
            if ((invokeAttr & BindingFlags.DoNotWrapExceptions) == 0)
            {
                try
                {
                    if (_invokeFunc is not null)
                    {
                        ret = _invokeFunc(obj, default);
                    }
                    else
                    {
                        ret = InterpretedInvoke(obj, default);
                    }
                }
                catch (Exception e)
                {
                    throw new TargetInvocationException(e);
                }
            }
            else if (_invokeFunc is not null)
            {
                ret = _invokeFunc(obj, default);
            }
            else
            {
                ret = InterpretedInvoke(obj, default);
            }

            return ret;
        }

        internal unsafe void InvokeDirect_Ref(ByReference objRef, IntPtr* args, ref ByReference refReturn)
        {
            if (!_strategyDetermined2)
            {
                DetermineStrategy2();
            }

            if (_invokeFunc_Ref is not null)
            {
                _invokeFunc_Ref(objRef, args, ref refReturn);
            }
            else
            {
                throw new NotImplementedException();
                //ret = InterpretedInvoke(obj, args); // todo: support byrefs
            }
        }

        internal unsafe object? InvokeDirect_Obj(object? obj, ReadOnlySpan<object?> args)
        {
            if (!_strategyDetermined2)
            {
                DetermineStrategy2();
            }

            if (_invokeFunc_Obj is not null)
            {
                return _invokeFunc_Obj(obj, args);
            }
            else
            {
                throw new NotImplementedException();
                // todo: InterpretedInvoke(obj, args);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private ref struct StackAllocatedByRefs
        {
            internal ByReference _arg0;
            private ByReference _arg1;
            private ByReference _arg2;
            private ByReference _arg3;
        }
    }
}
