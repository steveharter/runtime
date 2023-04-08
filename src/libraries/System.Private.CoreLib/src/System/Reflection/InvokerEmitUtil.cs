// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection.Emit;

namespace System.Reflection
{
    internal static class InvokerEmitUtil
    {
        // If changed, update native stack walking code that also uses this prefix to ignore reflection frames.
        private const string InvokeStubPrefix = "InvokeStub_";

        internal unsafe delegate void InvokeFunc_Ref(ByReference target, IntPtr* refArguments, ref ByReference returnValue);
        internal unsafe delegate object? InvokeFunc_Obj(object? target, ReadOnlySpan<object?> arguments);
        //internal unsafe delegate void InvokeFunc2(IntPtr* target, IntPtr* refArguments, IntPtr* returnValue);

        public static unsafe InvokeFunc_Obj CreateInvokeDelegate_Obj(MethodBase method)
        {
            Debug.Assert(!method.ContainsGenericParameters);

            bool emitNew = false;

            Type returnType;
            if (method is RuntimeMethodInfo rmi)
            {
                returnType = (RuntimeType)rmi.ReturnType;
            }
            else if (method is RuntimeConstructorInfo rci)
            {
                emitNew = true;
                returnType = (RuntimeType)rci.DeclaringType!;
            }
            else
            {
                Debug.Assert(method is DynamicMethod);
                returnType = (RuntimeType)((DynamicMethod)method).ReturnType;
            }

            bool hasThis = !emitNew && !method.IsStatic;

            // The first parameter is unused but supports treating the DynamicMethod as an instance method which is slightly faster than a static.
            Type[] delegateParameters = new Type[2] { typeof(object), typeof(ReadOnlySpan<object>) };

            string declaringTypeName = method.DeclaringType != null ? method.DeclaringType.Name + "." : string.Empty;
            var dm = new DynamicMethod(
                InvokeStubPrefix + declaringTypeName + method.Name,
                returnType: typeof(object),
                delegateParameters,
                typeof(object).Module, // Use system module to identify our DynamicMethods.
                skipVisibility: true);

            ILGenerator il = dm.GetILGenerator();

            Label throwNullRefReturn = il.DefineLabel();

            // Handle instance methods.
            if (hasThis)
            {
                il.Emit(OpCodes.Ldarg_0);
                if (method.DeclaringType!.IsValueType)
                {
                    il.Emit(OpCodes.Unbox, method.DeclaringType);
                }
            }

            // Push the arguments.
            ParameterInfo[] parameters = method.GetParametersNoCopy();
            for (int i = 0; i < parameters.Length; i++)
            {
                RuntimeType parameterType = (RuntimeType)parameters[i].ParameterType;

                il.Emit(OpCodes.Ldarga_S, 1);
                il.Emit(OpCodes.Ldc_I4, i);
                il.Emit(OpCodes.Call, Methods.ReadOnlySpan_get_Item());
                il.Emit(OpCodes.Ldind_Ref);
                if (parameterType.IsValueType)
                {
                    il.Emit(OpCodes.Unbox_Any, parameterType);
                }
            }

            // Invoke the method.
            // For CallStack reasons, don't inline target method.
#if MONO
            il.Emit(OpCodes.Call, Methods.DisableInline());
#else
            il.Emit(OpCodes.Call, Methods.NextCallReturnAddress());
            il.Emit(OpCodes.Pop);
#endif

            if (emitNew)
            {
                il.Emit(OpCodes.Newobj, (ConstructorInfo)method);
            }
            else if (method.IsStatic || method.DeclaringType!.IsValueType)
            {
                il.Emit(OpCodes.Call, (MethodInfo)method);
            }
            else
            {
                il.Emit(OpCodes.Callvirt, (MethodInfo)method);
            }

            // Handle the return.
            if (emitNew)
            {
                if (returnType.IsValueType)
                {
                    il.Emit(OpCodes.Box, returnType);
                }
            }
            else
            {
                if (returnType == typeof(void))
                {
                    il.Emit(OpCodes.Ldnull);
                }
                else if (returnType.IsValueType)
                {
                    il.Emit(OpCodes.Box, returnType);
                }
                else if (returnType.IsPointer)
                {
                    il.Emit(OpCodes.Ldtoken, returnType);
                    il.Emit(OpCodes.Call, Methods.Type_GetTypeFromHandle());
                    il.Emit(OpCodes.Call, Methods.Pointer_Box());
                }
            }

            il.Emit(OpCodes.Ret);

            // Create the delegate; it is also compiled at this point due to restrictedSkipVisibility=true.
            return (InvokeFunc_Obj)dm.CreateDelegate(typeof(InvokeFunc_Obj), target: null);
        }

        public static unsafe InvokeFunc_Ref CreateInvokeDelegate_Ref(MethodBase method)
        {
            Debug.Assert(!method.ContainsGenericParameters);

            bool emitNew = false;

            Type returnType;
            if (method is RuntimeMethodInfo rmi)
            {
                returnType = (RuntimeType)rmi.ReturnType;
            }
            else if (method is RuntimeConstructorInfo rci)
            {
                emitNew = true;
                returnType = (RuntimeType)rci.DeclaringType!;
            }
            else
            {
                Debug.Assert(method is DynamicMethod);
                returnType = (RuntimeType)((DynamicMethod)method).ReturnType;
            }

            bool hasThis = !emitNew && !method.IsStatic;

            // The first parameter is unused but supports treating the DynamicMethod as an instance method which is slightly faster than a static.
            Type[] delegateParameters = new Type[4] { typeof(object), typeof(ByReference), typeof(IntPtr*), typeof(ByReference).MakeByRefType() };

            string declaringTypeName = method.DeclaringType != null ? method.DeclaringType.Name + "." : string.Empty;
            var dm = new DynamicMethod(
                InvokeStubPrefix + declaringTypeName + method.Name,
                returnType: null,
                delegateParameters,
                typeof(object).Module, // Use system module to identify our DynamicMethods.
                skipVisibility: true);

            ILGenerator il = dm.GetILGenerator();

            Label throwNullRefReturn = il.DefineLabel();

            ParameterInfo[] parameters = method.GetParametersNoCopy();

            // Set the return ref address up now so we can set it from the value returned from invoke.
            if (returnType != typeof(void))
            {
                if (returnType.IsByRef)
                {
                    il.Emit(OpCodes.Ldarg_3);
                    il.Emit(OpCodes.Ldflda, Methods.ByReferenceOfByte_Value());
                }
                else
                {
                    il.Emit(OpCodes.Ldarg_3);
                    il.Emit(OpCodes.Ldfld, Methods.ByReferenceOfByte_Value());
                }
                //il.Emit(OpCodes.Ldind_I);
            }

            // Handle instance methods.
            if (hasThis)
            {
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldfld, Methods.ByReferenceOfByte_Value());
            }

            // Push the arguments.
            for (int i = 0; i < parameters.Length; i++)
            {
                il.Emit(OpCodes.Ldarg_2);
                if (i != 0)
                {
                    il.Emit(OpCodes.Ldc_I4, i * IntPtr.Size);
                    il.Emit(OpCodes.Add);
                }

                il.Emit(OpCodes.Ldfld, Methods.ByReferenceOfByte_Value());

                RuntimeType parameterType = (RuntimeType)parameters[i].ParameterType;
                if (!parameterType.IsByRef)
                {
                    il.Emit(OpCodes.Ldobj, parameterType.IsPointer ? typeof(IntPtr) : parameterType);
                }
            }

            // Invoke the method.
            // For CallStack reasons, don't inline target method.
#if MONO
            il.Emit(OpCodes.Call, Methods.DisableInline());
#else
            il.Emit(OpCodes.Call, Methods.NextCallReturnAddress());
            il.Emit(OpCodes.Pop);
#endif

            if (emitNew)
            {
                il.Emit(OpCodes.Newobj, (ConstructorInfo)method);
            }
            else if (method.IsStatic || method.DeclaringType!.IsValueType)
            {
                il.Emit(OpCodes.Call, (MethodInfo)method);
            }
            else
            {
                il.Emit(OpCodes.Callvirt, (MethodInfo)method);
            }

            // Handle the return.
            if (returnType != typeof(void))
            {
                if (returnType.IsValueType)
                {
                    il.Emit(OpCodes.Stobj, returnType);
                }
                else if (returnType.IsPointer)
                {
                    il.Emit(OpCodes.Stobj, typeof(IntPtr));
                }
                else if (returnType.IsByRef)
                {
                    //il.Emit(OpCodes.Dup);
                    //il.Emit(OpCodes.Brfalse_S, throwNullRefReturn);
                    il.Emit(OpCodes.Stfld, Methods.ByReferenceOfByte_Value());
                    //il.Emit(OpCodes.Stobj, returnType);
                }
                else
                {
                    il.Emit(OpCodes.Stind_Ref);
                }
            }

            il.Emit(OpCodes.Ret);

            //if (returnType.IsByRef)
            //{
            //    il.MarkLabel(throwNullRefReturn);
            //    il.Emit(OpCodes.Call, Methods.ThrowHelper_Throw_NullReference_InvokeNullRefReturned());
            //}

            // Create the delegate; it is also compiled at this point due to restrictedSkipVisibility=true.
            return (InvokeFunc_Ref)dm.CreateDelegate(typeof(InvokeFunc_Ref), target: null);
        }

    //EXISTING:
        internal unsafe delegate object? InvokeFunc(object? target, IntPtr* arguments);

        public static unsafe InvokeFunc CreateInvokeDelegate(MethodBase method)
        {
            Debug.Assert(!method.ContainsGenericParameters);

            bool emitNew = method is RuntimeConstructorInfo;
            bool hasThis = !(emitNew || method.IsStatic);

            // The first parameter is unused but supports treating the DynamicMethod as an instance method which is slightly faster than a static.
            Type[] delegateParameters = new Type[3] { typeof(object), typeof(object), typeof(IntPtr*) };

            string declaringTypeName = method.DeclaringType != null ? method.DeclaringType.Name + "." : string.Empty;
            var dm = new DynamicMethod(
                InvokeStubPrefix + declaringTypeName + method.Name,
                returnType: typeof(object),
                delegateParameters,
                typeof(object).Module, // Use system module to identify our DynamicMethods.
                skipVisibility: true);

            ILGenerator il = dm.GetILGenerator();

            // Handle instance methods.
            if (hasThis)
            {
                il.Emit(OpCodes.Ldarg_1);
                if (method.DeclaringType!.IsValueType)
                {
                    il.Emit(OpCodes.Unbox, method.DeclaringType);
                }
            }

            // Push the arguments.
            ParameterInfo[] parameters = method.GetParametersNoCopy();
            for (int i = 0; i < parameters.Length; i++)
            {
                il.Emit(OpCodes.Ldarg_2);
                if (i != 0)
                {
                    il.Emit(OpCodes.Ldc_I4, i * IntPtr.Size);
                    il.Emit(OpCodes.Add);
                }

                il.Emit(OpCodes.Ldfld, Methods.ByReferenceOfByte_Value());

                RuntimeType parameterType = (RuntimeType)parameters[i].ParameterType;
                if (!parameterType.IsByRef)
                {
                    il.Emit(OpCodes.Ldobj, parameterType.IsPointer ? typeof(IntPtr) : parameterType);
                }
            }

            // Invoke the method.
            // For CallStack reasons, don't inline target method.
#if MONO
            il.Emit(OpCodes.Call, Methods.DisableInline());
#else
            il.Emit(OpCodes.Call, Methods.NextCallReturnAddress());
            il.Emit(OpCodes.Pop);
#endif

            if (emitNew)
            {
                il.Emit(OpCodes.Newobj, (ConstructorInfo)method);
            }
            else if (method.IsStatic || method.DeclaringType!.IsValueType)
            {
                il.Emit(OpCodes.Call, (MethodInfo)method);
            }
            else
            {
                il.Emit(OpCodes.Callvirt, (MethodInfo)method);
            }

            // Handle the return.
            if (emitNew)
            {
                Type returnType = method.DeclaringType!;
                if (returnType.IsValueType)
                {
                    il.Emit(OpCodes.Box, returnType);
                }
            }
            else
            {
                RuntimeType returnType;
                if (method is RuntimeMethodInfo rmi)
                {
                    returnType = (RuntimeType)rmi.ReturnType;
                }
                else
                {
                    Debug.Assert(method is DynamicMethod);
                    returnType = (RuntimeType)((DynamicMethod)method).ReturnType;
                }

                if (returnType == typeof(void))
                {
                    il.Emit(OpCodes.Ldnull);
                }
                else if (returnType.IsValueType)
                {
                    il.Emit(OpCodes.Box, returnType);
                }
                else if (returnType.IsPointer)
                {
                    il.Emit(OpCodes.Ldtoken, returnType);
                    il.Emit(OpCodes.Call, Methods.Type_GetTypeFromHandle());
                    il.Emit(OpCodes.Call, Methods.Pointer_Box());
                }
                else if (returnType.IsByRef)
                {
                    // Check for null ref return.
                    Type elementType = returnType.GetElementType()!;
                    Label retValueOk = il.DefineLabel();
                    il.Emit(OpCodes.Dup);
                    il.Emit(OpCodes.Brtrue_S, retValueOk);
                    il.Emit(OpCodes.Call, Methods.ThrowHelper_Throw_NullReference_InvokeNullRefReturned());
                    il.MarkLabel(retValueOk);

                    // Handle per-type differences.
                    if (elementType.IsValueType)
                    {
                        il.Emit(OpCodes.Ldobj, elementType);
                        il.Emit(OpCodes.Box, elementType);
                    }
                    else if (elementType.IsPointer)
                    {
                        il.Emit(OpCodes.Ldind_Ref);
                        il.Emit(OpCodes.Conv_U);
                        il.Emit(OpCodes.Ldtoken, elementType);
                        il.Emit(OpCodes.Call, Methods.Type_GetTypeFromHandle());
                        il.Emit(OpCodes.Call, Methods.Pointer_Box());
                    }
                    else
                    {
                        il.Emit(OpCodes.Ldobj, elementType);
                    }
                }
            }

            il.Emit(OpCodes.Ret);

            // Create the delegate; it is also compiled at this point due to restrictedSkipVisibility=true.
            return (InvokeFunc)dm.CreateDelegate(typeof(InvokeFunc), target: null);
        }

        private static class ThrowHelper
        {
            public static void Throw_NullReference_InvokeNullRefReturned()
            {
                throw new NullReferenceException(SR.NullReference_InvokeNullRefReturned);
            }
        }

        private static class Methods
        {
            private static FieldInfo? s_ByReferenceOfByte_Value;
            public static FieldInfo ByReferenceOfByte_Value() =>
                s_ByReferenceOfByte_Value ??= typeof(ByReference).GetField("Value")!;

            private static MethodInfo? s_ReadOnlySpan_get_Item;
            public static MethodInfo ReadOnlySpan_get_Item() =>
                s_ReadOnlySpan_get_Item ??= typeof(ReadOnlySpan<object>).GetProperty("Item")!.GetGetMethod()!;

            private static MethodInfo? s_ThrowHelper_Throw_NullReference_InvokeNullRefReturned;
            public static MethodInfo ThrowHelper_Throw_NullReference_InvokeNullRefReturned() =>
                s_ThrowHelper_Throw_NullReference_InvokeNullRefReturned ??= typeof(ThrowHelper).GetMethod(nameof(ThrowHelper.Throw_NullReference_InvokeNullRefReturned))!;

            private static MethodInfo? s_Pointer_Box;
            public static MethodInfo Pointer_Box() =>
                s_Pointer_Box ??= typeof(Pointer).GetMethod(nameof(Pointer.Box), new[] { typeof(void*), typeof(Type) })!;

            private static MethodInfo? s_Type_GetTypeFromHandle;
            public static MethodInfo Type_GetTypeFromHandle() =>
                s_Type_GetTypeFromHandle ??= typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle), new[] { typeof(RuntimeTypeHandle) })!;

#if MONO
            private static MethodInfo? s_DisableInline;
            public static MethodInfo DisableInline() =>
                s_DisableInline ??= typeof(System.Runtime.CompilerServices.JitHelpers).GetMethod(nameof(System.Runtime.CompilerServices.JitHelpers.DisableInline), BindingFlags.NonPublic | BindingFlags.Static)!;
#else
            private static MethodInfo? s_NextCallReturnAddress;
            public static MethodInfo NextCallReturnAddress() =>
                s_NextCallReturnAddress ??= typeof(System.StubHelpers.StubHelpers).GetMethod(nameof(System.StubHelpers.StubHelpers.NextCallReturnAddress), BindingFlags.NonPublic | BindingFlags.Static)!;
#endif
        }
    }
}
