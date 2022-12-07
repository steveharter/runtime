// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection.Emit;

namespace System.Reflection
{
    internal static class InvokerEmitUtil
    {
        // If changed, update native stack walking code that also uses this prefix to ignore reflection frames.
        public const string InvokeStubPrefix = "InvokeStub_";

        internal unsafe delegate void InvokeFunc(IntPtr functionPointer, TypedReference target, TypedReference result, IntPtr* arguments);

        public static unsafe Func<object, TValue> CreateGetter<TValue>(MethodInfo method)
        {
            Type[] delegateParameters = new Type[1]
            {
                typeof(object)
            };

            Type? targetType = method.DeclaringType;
            string declaringTypeName = targetType != null ? targetType.Name + "." : string.Empty;
            var dm = new DynamicMethod(
                InvokeStubPrefix + declaringTypeName + method.Name + "_O_T",
                returnType: typeof(TValue),
                delegateParameters,
                targetType == null ? typeof(object).Module : targetType.Module,
                skipVisibility: true);

            Emit(dm,
                method,
                keepExceptionCallStack: false,
                fnPtrIndex: -1, // Since user-cached delegates, function pointer sharing doesn't make as much sense.
                targetKind: ParameterKind.Object,
                targetArgIndex: 0,
                returnKind: ParameterKind.Typed,
                returnArgIndex: -1,
                parametersKind: ParameterKind.NotUsed,
                parametersArgIndex: -1);

            return (Func<object, TValue>)dm.CreateDelegate(typeof(Func<object, TValue>), target: null);
        }

        public static unsafe Action<object, TValue> CreateSetter<TValue>(MethodInfo method)
        {
            Type[] delegateParameters = new Type[2]
            {
                typeof(object),
                typeof(TValue)
            };

            Type? targetType = method.DeclaringType;
            string declaringTypeName = targetType != null ? targetType.Name + "." : string.Empty;
            var dm = new DynamicMethod(
                InvokeStubPrefix + declaringTypeName + method.Name + "_O_T",
                returnType: typeof(void),
                delegateParameters,
                targetType == null ? typeof(object).Module : targetType.Module,
                skipVisibility: true);

            Emit(dm,
                method,
                keepExceptionCallStack: false,
                fnPtrIndex: -1, // Since user-cached delegates, function pointer sharing doesn't make as much sense.
                targetKind: ParameterKind.Object,
                targetArgIndex: 0,
                returnKind : ParameterKind.NotUsed,
                returnArgIndex: -1,
                parametersKind: ParameterKind.Typed,
                parametersArgIndex: 1);

            return (Action<object, TValue>)dm.CreateDelegate(typeof(Action<object, TValue>), target: null);
        }

        public static unsafe InvokeFunc CreateInvokeDelegate(MethodBase method)
        {
            Type[] delegateParameters = new Type[5] {
                typeof(object), // A dummy parameter that treats the DynamicMethod as an instance method which is slightly faster than a static.
                typeof(IntPtr), // Function pointer
                typeof(TypedReference), // Target
                typeof(TypedReference), // Result
                typeof(IntPtr*) // Arguments
            };

            Type? targetType = method.DeclaringType;
            string declaringTypeName = targetType != null ? targetType.Name + "." : string.Empty;
            var dm = new DynamicMethod(
                InvokeStubPrefix + declaringTypeName + method.Name,
                returnType: null,
                delegateParameters,
                targetType == null ? typeof(object).Module : targetType.Module,
                skipVisibility: true);

            Emit(dm,
                method,
                keepExceptionCallStack: true,
                fnPtrIndex: 1,
                targetKind: ParameterKind.TypedReference,
                targetArgIndex: 2,
                returnKind: ParameterKind.TypedReference,
                returnArgIndex: 3,
                parametersKind: ParameterKind.TypedReference,
                parametersArgIndex: 4);

            // Create the delegate; it is also compiled at this point due to restrictedSkipVisibility=true.
            return (InvokeFunc)dm.CreateDelegate(typeof(InvokeFunc), target: null);
        }

        public static unsafe void Emit(
            DynamicMethod dm,
            MethodBase method,
            bool keepExceptionCallStack, // slightly faster if false
            int fnPtrIndex, // -1 if not used
            ParameterKind targetKind,
            int targetArgIndex,
            ParameterKind returnKind,
            int returnArgIndex,
            ParameterKind parametersKind,
            int parametersArgIndex // -1 if not used
        )
        {
            Debug.Assert(!method.ContainsGenericParameters);

            bool emitNew = method is RuntimeConstructorInfo;
            bool hasThis = !(emitNew || method.IsStatic);
            bool useCalli = fnPtrIndex != -1 && IsTargetMethodSealed();
            RuntimeType returnType = GetReturnType();
            RuntimeType? refReturnType = returnType.IsByRef ? (RuntimeType)returnType.GetElementType() : null;

            ILGenerator il = dm.GetILGenerator();

            // If there is a return, push the TypedReference's internal address.
            if (returnKind == ParameterKind.TypedReference && returnType != typeof(void))
            {
                EmitLdArg(returnArgIndex);
                il.Emit(OpCodes.Ldfld, Methods.TypedReference_value());
            }

            // Handle instance methods.
            if (hasThis)
            {
                EmitLdArg(targetArgIndex);

                if (targetKind == ParameterKind.TypedReference)
                {
                    il.Emit(OpCodes.Ldfld, Methods.TypedReference_value());
                    il.Emit(OpCodes.Ldobj, method.DeclaringType!);
                }
                else if (targetKind == ParameterKind.Object)
                {
                    if (method.DeclaringType!.IsValueType)
                    {
                        il.Emit(OpCodes.Unbox, method.DeclaringType);
                    }
                    else
                    {
                        // Castclass to throw on invalid type
                        il.Emit(OpCodes.Castclass, method.DeclaringType!);
                    }
                }
                else
                {
                    Debug.Assert(false, "Not implemented");
                }
            }

            // Initialize the Type[] for CallI
            ParameterInfo[] parameters = method.GetParametersNoCopy();
            Type[]? parameterTypes = null;
            int instanceMethodOffset = 0;
            if (useCalli)
            {
                if (!method.IsStatic)
                {
                    instanceMethodOffset = 1;
                    parameterTypes = new Type[parameters.Length + 1];
                    parameterTypes[0] = GetParameterTypeForCallI(method.DeclaringType!);
                }
                else if (parameters.Length > 0)
                {
                    parameterTypes = new Type[parameters.Length];
                }
            }

            // Push the arguments.
            for (int i = 0; i < parameters.Length; i++)
            {
                Type parameterType = parameters[i].ParameterType;
                if (useCalli)
                {
                    parameterTypes![i + instanceMethodOffset] = GetParameterTypeForCallI(parameterType);
                }

                EmitLdArg(parametersArgIndex);
                switch (parametersKind)
                {
                    case ParameterKind.TypedReference:
                        if (i != 0)
                        {
                            il.Emit(OpCodes.Ldc_I4, i * IntPtr.Size);
                            il.Emit(OpCodes.Add);
                        }

                        il.Emit(OpCodes.Ldfld, Methods.ByReferenceOfByte_Value());

                        if (!parameterType.IsByRef)
                        {
                            il.Emit(OpCodes.Ldobj, parameterType.IsPointer ? typeof(IntPtr) : parameterType);
                        }
                        break;
                    case ParameterKind.Object:
                        Debug.Assert(i == 0); // Assume a single parameter only
                        if (parameterType.IsValueType)
                        {
                            il.Emit(OpCodes.Unbox, parameterType);
                        }
                        break;
                    default:
                        Debug.Assert(parametersKind == ParameterKind.Typed);
                        Debug.Assert(i == 0); // Assume a single parameter only
                        break;
                }
            }

            // For CallStack reasons, don't inline target method.
            if (keepExceptionCallStack && !useCalli) // Calli does not require this hack.
            {
#if MONO
                il.Emit(OpCodes.Call, Methods.DisableInline());
#else
                il.Emit(OpCodes.Call, Methods.NextCallReturnAddress());
                il.Emit(OpCodes.Pop);
#endif
            }

            // Invoke the method.
            if (emitNew)
            {
                il.Emit(OpCodes.Newobj, (ConstructorInfo)method);
            }
            else if (useCalli)
            {
                EmitLdArg(fnPtrIndex);
                il.EmitCalli(OpCodes.Calli, CallingConventions.Standard, returnType, parameterTypes, null);
            }
            else
            {
                if (IsTargetMethodSealed())
                {
                    il.Emit(OpCodes.Call, (MethodInfo)method);
                }
                else
                {
                    il.Emit(OpCodes.Callvirt, (MethodInfo)method);
                }
            }

            if (emitNew)
            {
                if (returnKind == ParameterKind.Object && returnType.IsValueType)
                {
                    il.Emit(OpCodes.Box, returnType);
                    // else castclass for ref type?
                }
                else if (returnKind == ParameterKind.TypedReference)
                {
                    // Handle the return by updating the TypedReference's internal reference.
                    il.Emit(OpCodes.Stobj, returnType);
                }
            }
            else if (returnType.IsPointer)
            {
                il.Emit(OpCodes.Ldtoken, returnType);
                il.Emit(OpCodes.Call, Methods.Type_GetTypeFromHandle());
                il.Emit(OpCodes.Call, Methods.Pointer_Box());

                if (returnKind == ParameterKind.TypedReference)
                {
                    il.Emit(OpCodes.Stobj, typeof(Pointer)); // todo: correct?
                }
            }
            else if (returnType.IsByRef)
            {
                Debug.Assert(refReturnType != null);

                // Check for null ref return.
                Label retValueOk = il.DefineLabel();
                il.Emit(OpCodes.Dup);
                il.Emit(OpCodes.Brtrue_S, retValueOk);
                il.Emit(OpCodes.Call, Methods.ThrowHelper_Throw_NullReference_InvokeNullRefReturned());
                il.MarkLabel(retValueOk);

                // Handle per-type differences.
                if (refReturnType.IsPointer)
                {
                    il.Emit(OpCodes.Ldind_Ref);
                    il.Emit(OpCodes.Conv_U);
                    il.Emit(OpCodes.Ldtoken, refReturnType);
                    il.Emit(OpCodes.Call, Methods.Type_GetTypeFromHandle());
                    il.Emit(OpCodes.Call, Methods.Pointer_Box());

                    if (returnKind == ParameterKind.TypedReference)
                    {
                        il.Emit(OpCodes.Stobj, typeof(Pointer)); // todo: correct?
                    }
                }
                else
                {   // todo: is this necessary?
                    if (returnKind == ParameterKind.TypedReference)
                    {
                        il.Emit(OpCodes.Ldobj, refReturnType);
                        il.Emit(OpCodes.Stobj, refReturnType);
                    }
                }
            }
            else if (returnType != typeof(void))
            {
                if (parametersKind == ParameterKind.TypedReference)
                {
                    il.Emit(OpCodes.Stobj, returnType);
                }
            }

            il.Emit(OpCodes.Ret);

            return;

            Type GetParameterTypeForCallI(Type type)
            {
                return type;

                // todo: normalize types for caching
                // if (type.IsValueType)
                // {
                //     return type;
                // }

                // if (type.IsByRef || type.IsPointer)
                // {
                //     return type; // ref type here as &object ?
                // }

                // return typeof(object);
            }

            RuntimeType GetReturnType()
            {
                if (emitNew)
                {
                    return (RuntimeType)method.DeclaringType!;
                }

                if (method is RuntimeMethodInfo rmi)
                {
                    return (RuntimeType)rmi.ReturnType;
                }

                Debug.Assert(method is DynamicMethod);
                return (RuntimeType)((DynamicMethod)method).ReturnType;
            }

            void EmitLdArg(int arg)
            {
                switch (arg)
                {
                    case 1:
                        il.Emit(OpCodes.Ldarg_1);
                        break;
                    case 2:
                        il.Emit(OpCodes.Ldarg_2);
                        break;
                    case 3:
                        il.Emit(OpCodes.Ldarg_3);
                        break;
                    default:
                        // Arg 0 is the dummy 'object' parameter.
                        Debug.Assert(arg != 0);

                        il.Emit(OpCodes.Ldarg, arg);
                        break;
                }
            }

            bool IsTargetMethodSealed() => !(method.IsVirtual && !method.IsFinal) || method.DeclaringType!.IsSealed;
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

            private static FieldInfo? s_TypedReference_value;
            public static FieldInfo TypedReference_value() =>
                s_TypedReference_value ??= typeof(TypedReference).GetField("_value", BindingFlags.NonPublic | BindingFlags.Instance)!;

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

        private static Type TypeFromKind(ParameterKind kind, Type type)
        {
            switch (kind)
            {
                case ParameterKind.Object:
                    return type.IsValueType ? typeof(object) : type;
                case ParameterKind.Typed:
                    return type;
                default:
                    Debug.Assert(kind == ParameterKind.TypedReference);
                    return typeof(TypedReference);
            }
        }

        public enum ParameterKind
        {
            NotUsed = 0,
            Object = 1,
            Typed = 2,
            TypedReference = 3,
        }
    }
}
