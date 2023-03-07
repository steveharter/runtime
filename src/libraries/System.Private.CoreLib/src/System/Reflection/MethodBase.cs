// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Reflection
{
    public abstract partial class MethodBase : MemberInfo
    {
        protected MethodBase() { }

        public abstract ParameterInfo[] GetParameters();
        public abstract MethodAttributes Attributes { get; }
        public virtual MethodImplAttributes MethodImplementationFlags => GetMethodImplementationFlags();
        public abstract MethodImplAttributes GetMethodImplementationFlags();

        [RequiresUnreferencedCode("Trimming may change method bodies. For example it can change some instructions, remove branches or local variables.")]
        public virtual MethodBody? GetMethodBody() { throw new InvalidOperationException(); }

        public virtual CallingConventions CallingConvention => CallingConventions.Standard;

        public bool IsAbstract => (Attributes & MethodAttributes.Abstract) != 0;
        public bool IsConstructor =>
            // To be backward compatible we only return true for instance RTSpecialName ctors.
            this is ConstructorInfo &&
            !IsStatic &&
            (Attributes & MethodAttributes.RTSpecialName) == MethodAttributes.RTSpecialName;
        public bool IsFinal => (Attributes & MethodAttributes.Final) != 0;
        public bool IsHideBySig => (Attributes & MethodAttributes.HideBySig) != 0;
        public bool IsSpecialName => (Attributes & MethodAttributes.SpecialName) != 0;
        public bool IsStatic => (Attributes & MethodAttributes.Static) != 0;
        public bool IsVirtual => (Attributes & MethodAttributes.Virtual) != 0;

        public bool IsAssembly => (Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Assembly;
        public bool IsFamily => (Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Family;
        public bool IsFamilyAndAssembly => (Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.FamANDAssem;
        public bool IsFamilyOrAssembly => (Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.FamORAssem;
        public bool IsPrivate => (Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Private;
        public bool IsPublic => (Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Public;

        public virtual bool IsConstructedGenericMethod => IsGenericMethod && !IsGenericMethodDefinition;
        public virtual bool IsGenericMethod => false;
        public virtual bool IsGenericMethodDefinition => false;
        public virtual Type[] GetGenericArguments() { throw new NotSupportedException(SR.NotSupported_SubclassOverride); }
        public virtual bool ContainsGenericParameters => false;

        [DebuggerHidden]
        [DebuggerStepThrough]
        public object? Invoke(object? obj, object?[]? parameters) => Invoke(obj, BindingFlags.Default, binder: null, parameters: parameters, culture: null);
        public abstract object? Invoke(object? obj, BindingFlags invokeAttr, Binder? binder, object?[]? parameters, CultureInfo? culture);

        public abstract RuntimeMethodHandle MethodHandle { get; }

        public virtual bool IsSecurityCritical => throw NotImplemented.ByDesign;
        public virtual bool IsSecuritySafeCritical => throw NotImplemented.ByDesign;
        public virtual bool IsSecurityTransparent => throw NotImplemented.ByDesign;

        public override bool Equals(object? obj) => base.Equals(obj);
        public override int GetHashCode() => base.GetHashCode();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(MethodBase? left, MethodBase? right)
        {
            // Test "right" first to allow branch elimination when inlined for null checks (== null)
            // so it can become a simple test
            if (right is null)
            {
                return left is null;
            }

            // Try fast reference equality and opposite null check prior to calling the slower virtual Equals
            if (ReferenceEquals(left, right))
            {
                return true;
            }

            return (left is null) ? false : left.Equals(right);
        }

        public static bool operator !=(MethodBase? left, MethodBase? right) => !(left == right);

        internal const int MethodNameBufferSize = 100;

        internal static void AppendParameters(ref ValueStringBuilder sbParamList, Type[] parameterTypes, CallingConventions callingConvention)
        {
            string comma = "";

            for (int i = 0; i < parameterTypes.Length; i++)
            {
                Type t = parameterTypes[i];

                sbParamList.Append(comma);

                string typeName = t.FormatTypeName();

                // Legacy: Why use "ByRef" for by ref parameters? What language is this?
                // VB uses "ByRef" but it should precede (not follow) the parameter name.
                // Why don't we just use "&"?
                if (t.IsByRef)
                {
                    sbParamList.Append(typeName.AsSpan().TrimEnd('&'));
                    sbParamList.Append(" ByRef");
                }
                else
                {
                    sbParamList.Append(typeName);
                }

                comma = ", ";
            }

            if ((callingConvention & CallingConventions.VarArgs) == CallingConventions.VarArgs)
            {
                sbParamList.Append(comma);
                sbParamList.Append("...");
            }
        }

        internal virtual Type[] GetParameterTypes()
        {
            ParameterInfo[] paramInfo = GetParametersNoCopy();
            if (paramInfo.Length == 0)
            {
                return Type.EmptyTypes;
            }

            Type[] parameterTypes = new Type[paramInfo.Length];
            for (int i = 0; i < paramInfo.Length; i++)
                parameterTypes[i] = paramInfo[i].ParameterType;

            return parameterTypes;
        }

#if !NATIVEAOT
        private protected void ValidateInvokeTarget(object? target)
        {
            // Confirm member invocation has an instance and is of the correct type
            if (!IsStatic)
            {
                if (target == null)
                {
                    throw new TargetException(SR.RFLCT_Targ_StatMethReqTarg);
                }

                if (!DeclaringType!.IsInstanceOfType(target))
                {
                    throw new TargetException(SR.RFLCT_Targ_ITargMismatch);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private protected unsafe bool CheckArguments(
            ReadOnlySpan<object?> parameters,
            Span<object?> copyOfParameters,
            RuntimeType[] sigTypes,
            Binder? binder,
            CultureInfo? culture,
            BindingFlags invokeAttr
        )
        {
            bool hasTypeMissing = false;
            ParameterInfo[]? paramInfos = null;
            for (int i = 0; i < parameters.Length; i++)
            {
                object? arg = parameters[i];
                RuntimeType sigType = sigTypes[i];

                // Convert a Type.Missing to the default value.
                if (ReferenceEquals(arg, Type.Missing))
                {
                    hasTypeMissing = true;
                    paramInfos ??= GetParametersNoCopy();
                    arg = HandleTypeMissing(paramInfos[i], sigType);
                }

                // Convert the type if necessary.
                if (arg is null)
                {
                    if (RuntimeTypeHandle.IsValueType(sigType) || RuntimeTypeHandle.IsByRef(sigType))
                    {
                        sigType.CheckValue(ref arg, binder, culture, invokeAttr);
                    }
                }
                else if (!ReferenceEquals(arg.GetType(), sigType))
                {
                    // Determine if we can use the fast path for byref types
                    if (RuntimeType.TryGetByRefElementType(sigType, out RuntimeType? sigElementType) &&
                        ReferenceEquals(sigElementType, arg.GetType()))
                    {
                        if (RuntimeTypeHandle.IsValueType(sigElementType))
                        {
                            // Make a copy to prevent the boxed instance from being directly modified by the method.
                            arg = RuntimeType.AllocateValueType(sigElementType, arg);
                        }
                    }
                    else
                    {
                        sigType.CheckValue(ref arg, binder, culture, invokeAttr);
                    }
                }

                copyOfParameters[i] = arg;
            }

            return hasTypeMissing;
        }

        internal static object? HandleTypeMissing(ParameterInfo paramInfo, RuntimeType sigType)
        {
            if (paramInfo.DefaultValue == DBNull.Value)
            {
                throw new ArgumentException(SR.Arg_VarMissNull, "parameters");
            }

            object? arg = paramInfo.DefaultValue;

            if (sigType.IsNullableOfT)
            {
                if (arg is not null)
                {
                    // For nullable Enum types, the ParameterInfo.DefaultValue returns a raw value which
                    // needs to be parsed to the Enum type, for more info: https://github.com/dotnet/runtime/issues/12924
                    Type argumentType = sigType.GetGenericArguments()[0];
                    if (argumentType.IsEnum)
                    {
                        arg = Enum.ToObject(argumentType, arg);
                    }
                }
            }

            return arg;
        }

        internal static bool IsElementTypeNullableOfT(RuntimeType type)
        {
            if (type.IsPointer || type.IsByRef)
            {
                type = (RuntimeType)type.GetElementType()!;
            }

            return type.IsNullableOfT;
        }

        // Copy modified values out. This should be done only with ByRef or Type.Missing parameters.
        internal static void CopyBack(object?[] parameters, Span<object?> copyOfParameters, RuntimeType[] argTypes)
        {
            for (int i = 0; i < parameters.Length; i++)
            {
                RuntimeType type = argTypes[i];
                if (IsElementTypeNullableOfT(type))
                {
                    Debug.Assert(copyOfParameters[i] != null);
                    Debug.Assert(((RuntimeType)copyOfParameters[i]!.GetType()).IsNullableOfT);
                    parameters![i] = RuntimeMethodHandle.ReboxFromNullable(copyOfParameters[i]);
                }
                else if (type.IsByRef || ReferenceEquals(parameters![i], Type.Missing))
                {
                    parameters![i] = copyOfParameters[i];
                }
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        internal ref struct StackAllocatedObjects
        {
            internal object? _arg0;
            private object? _arg1;
            private object? _arg2;
            private object? _arg3;
        }
#endif
    }
}
