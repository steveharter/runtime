// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Emit;

namespace System.Reflection
{
    internal static class InvokerEmitUtil
    {
        // If changed, update native stack walking code that also uses this prefix to ignore reflection frames.
        private const string InvokeStubPrefix = "InvokeStub_";

        internal unsafe delegate void InvokeFunc(IntPtr functionPointer, TypedReference target, TypedReference result, IntPtr* arguments);

        public static unsafe InvokeFunc CreateInvokeDelegate(MethodBase method)
        {
            Debug.Assert(!method.ContainsGenericParameters);

            bool emitNew = method is RuntimeConstructorInfo;
            bool useCalli = !(method.IsVirtual && !method.IsFinal) || method.DeclaringType!.IsSealed;
            bool hasThis = !(emitNew || method.IsStatic);
            RuntimeType returnType = GetReturnType();
            RuntimeType? refReturnType = returnType.IsByRef ? (RuntimeType)returnType.GetElementType() : null;

            Type[] delegateParameters = new Type[5] {
                typeof(object), // A dummy parameter that treats the DynamicMethod as an instance method which is slightly faster than a static.
                typeof(IntPtr), // Function pointer
                typeof(TypedReference), // Target
                typeof(TypedReference), // Result
                typeof(IntPtr*) // Arguments
            };

            string declaringTypeName = method.DeclaringType != null ? method.DeclaringType.Name + "." : string.Empty;
            var dm = new DynamicMethod(
                InvokeStubPrefix + declaringTypeName + method.Name,
                returnType: null,
                delegateParameters,
                typeof(object).Module, // Use system module to identify our DynamicMethods.
                skipVisibility: true);

            ILGenerator il = dm.GetILGenerator();

            // If there is a return, push the TypedReference's internal address.
            if (returnType != typeof(void))
            {
                il.Emit(OpCodes.Ldarg_3);
                il.Emit(OpCodes.Ldfld, Methods.TypedReference_value());
            }

            // Handle instance methods.
            if (hasThis)
            {
                il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Ldfld, Methods.TypedReference_value());
                il.Emit(OpCodes.Ldobj, method.DeclaringType!);
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

                il.Emit(OpCodes.Ldarg, 4);
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
            }

#if !MONO
            if (!useCalli)
            {
                il.Emit(OpCodes.Call, Methods.NextCallReturnAddress()); // For CallStack reasons, don't inline target method.
                il.Emit(OpCodes.Pop);
            }
#endif

            // Invoke the method.
            if (emitNew)
            {
                il.Emit(OpCodes.Newobj, (ConstructorInfo)method);
            }
            else if (useCalli)
            {
                il.Emit(OpCodes.Ldarg_1);
                il.EmitCalli(OpCodes.Calli, CallingConventions.Standard, returnType, parameterTypes, null);
            }
            else
            {
                il.Emit(OpCodes.Callvirt, (MethodInfo)method);
            }

            // Handle the return by updating the TypedReference's internal reference.
            if (emitNew)
            {
                if (returnType.IsValueType)
                {
                    il.Emit(OpCodes.Box, returnType); // todo:not necessary if caller does this through TR
                    il.Emit(OpCodes.Stobj, typeof(object));
                }
                else
                {
                    il.Emit(OpCodes.Castclass, typeof(object)); // todo:not necessary if caller does this through TR
                    il.Emit(OpCodes.Stobj, typeof(object));
                }
            }
            else if (returnType.IsPointer)
            {
                il.Emit(OpCodes.Ldtoken, returnType);
                il.Emit(OpCodes.Call, Methods.Type_GetTypeFromHandle());
                il.Emit(OpCodes.Call, Methods.Pointer_Box());
                il.Emit(OpCodes.Stobj, typeof(Pointer)); //todo
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
                    il.Emit(OpCodes.Stobj, typeof(Pointer));
                }
                else
                {   // todo: is this necessary?
                    il.Emit(OpCodes.Ldobj, refReturnType);
                    il.Emit(OpCodes.Stobj, refReturnType);
                }
            }
            else if (returnType != typeof(void))
            {
                il.Emit(OpCodes.Stobj, returnType);
            }

            il.Emit(OpCodes.Ret);

            // Create the delegate; it is also compiled at this point due to restrictedSkipVisibility=true.
            return (InvokeFunc)dm.CreateDelegate(typeof(InvokeFunc), target: null);

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

#if !MONO
            private static MethodInfo? s_NextCallReturnAddress;
            public static MethodInfo NextCallReturnAddress() =>
                s_NextCallReturnAddress ??= typeof(System.StubHelpers.StubHelpers).GetMethod(nameof(System.StubHelpers.StubHelpers.NextCallReturnAddress), BindingFlags.NonPublic | BindingFlags.Static)!;
#endif
        }
    }
}
