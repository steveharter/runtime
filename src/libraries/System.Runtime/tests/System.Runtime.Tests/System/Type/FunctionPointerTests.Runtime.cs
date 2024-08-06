// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Tests.Types
{
    // These are runtime-specific tests not shared with MetadataLoadContext.
    // Using arrays in the manner below allows for use of the "is" keyword.
    // The use of 'object' will call into the runtime to compare but using a strongly-typed
    // function pointer without 'object' causes C# to hard-code the result.
    public partial class FunctionPointerTests
    {
        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/90308", TestRuntimes.Mono)]
        public static unsafe void CompileTimeIdentity_Managed()
        {
            object obj = new delegate*<int>[1];
            Assert.True(obj is delegate*<int>[]);
            Assert.False(obj is delegate*<bool>[]);

            var fn = new delegate*<int>[1];
            Assert.True(fn is delegate*<int>[]);
#pragma warning disable CS0184 // 'is' expression's given expression is never of the provided type
            Assert.False(fn is delegate*<bool>[]);
#pragma warning restore CS0184
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/90308", TestRuntimes.Mono)]
        public static unsafe void CompileTimeIdentity_ManagedWithMods()
        {
            object obj = new delegate*<ref int, void>[1];
            Assert.True(obj is delegate*<out int, void>[]);
            Assert.True(obj is delegate*<in int, void>[]);

            var fn = new delegate*<ref int, void>[1];
#pragma warning disable CS0184
            Assert.False(fn is delegate*<out int, void>[]);
            Assert.False(fn is delegate*<in int, void>[]);
#pragma warning restore CS0184
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/90308", TestRuntimes.Mono)]
        public static unsafe void CompileTimeIdentity_Unmanaged()
        {
            object obj = new delegate* unmanaged[MemberFunction]<void>[1];
            Assert.True(obj is delegate* unmanaged<void>[]);
            Assert.True(obj is delegate* unmanaged[SuppressGCTransition]<void>[]);
            Assert.True(obj is delegate* unmanaged[MemberFunction, SuppressGCTransition]<void>[]);

            var fn = new delegate* unmanaged[MemberFunction]<void>[1];
#pragma warning disable CS0184
            Assert.False(fn is delegate* unmanaged<void>[]);
            Assert.False(fn is delegate* unmanaged[SuppressGCTransition]<void>[]);
            Assert.False(fn is delegate* unmanaged[MemberFunction, SuppressGCTransition]<void>[]);
#pragma warning restore CS0184
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/90308", TestRuntimes.Mono)]
        public static unsafe void CompileTimeIdentity_UnmanagedIsPartOfIdentity()
        {
            object obj = new delegate* unmanaged[MemberFunction]<void>[1];
            Assert.False(obj is delegate*<void>[]);

            var fn = new delegate* unmanaged[MemberFunction]<void>[1];
#pragma warning disable CS0184
            Assert.False(fn is delegate*<void>[]);
#pragma warning restore CS0184

            object obj2 = new delegate* unmanaged[Cdecl]<void>[1];
            Assert.False(obj2 is delegate*<void>[]);

            var fn2 = new delegate* unmanaged[Cdecl]<void>[1];
#pragma warning disable CS0184
            Assert.False(fn2 is delegate*<void>[]);
#pragma warning restore CS0184
        }

        [Fact]
        public static unsafe void EmitSupport_OpCodesCall()
        {
            ILGenerator il = CreateTestMethod(out TypeBuilder type, out AssemblyBuilder assembly);

            MethodInfo m1 = typeof(MethodHolder).GetMethod(nameof(MethodHolder.CallFunctionPointer))!;
            MethodInfo m2 = typeof(MethodHolder).GetMethod(nameof(MethodHolder.CallMeWithFunctionPointer))!;

            il.Emit(OpCodes.Ldftn, m2);
            // todo: this worked previously; add a test:
            //il.EmitCalli(OpCodes.Calli, CallingConvention.StdCall, typeof(int), Type.EmptyTypes);
            il.Emit(OpCodes.Call, m1);
            il.Emit(OpCodes.Ret);

            // Get the generated method and invoke it.
            Type runtimeType = type.CreateType();
            MethodInfo runtimeMethod = runtimeType.GetMethod("TestMethod")!;
            object ret = runtimeMethod.Invoke(null, null);
            Assert.Equal(42, ret);
        }

        [Fact]
        public static unsafe void EmitSupport_Generic_OpCodesCall()
        {
            ILGenerator il = CreateTestMethod(out TypeBuilder type, out AssemblyBuilder assembly);

            MethodInfo m1 = typeof(MethodHolder).GetMethod(nameof(MethodHolder.CallFunctionPointerWithGeneric))!;
            MethodInfo m1_generic = m1.MakeGenericMethod(typeof(int));
            MethodInfo m2 = typeof(MethodHolder).GetMethod(nameof(MethodHolder.CallMeWithFunctionPointer))!;

            il.Emit(OpCodes.Ldftn, m2);

            // Currently function pointers can't be passed to generic methods.
            Assert.Throws<NotSupportedException>(() => il.Emit(OpCodes.Call, m1_generic));
        }

        private static ILGenerator CreateTestMethod(out TypeBuilder type, out AssemblyBuilder assembly)
        {
            // Generate a method that calls a function pointer.
            assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("test"), AssemblyBuilderAccess.Run);

            ModuleBuilder module = assembly.DefineDynamicModule("test");
            type = module.DefineType("TestType", TypeAttributes.Class | TypeAttributes.Public);
            MethodBuilder method = type.DefineMethod(
                "TestMethod",
                MethodAttributes.Public | MethodAttributes.Static,
                returnType: typeof(int),
                parameterTypes: null);

            return method.GetILGenerator();
        }

        public static class MethodHolder
        {
            public static unsafe T CallFunctionPointerWithGeneric<T>(delegate*<T> fptr) => fptr();
            public static unsafe int CallFunctionPointer(delegate*<int> fptr) => fptr();
            public static int CallMeWithFunctionPointer() => 42;
        }
    }
}


