// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Dynamic;
using Xunit;

namespace System.Reflection.Emit.Extensions.Tests
{
    public static class First
    {
        [Fact]
        public static void Dummy()
        {
            Type t = typeof(TestAsm);
        }

        [Fact]
        public static void Hello()
        {
            Type t = typeof(TestAsm);
            Type runtimeModuleType = t.Module.GetType();
            int mt = t.MetadataToken;
            RuntimeTypeHandle h2 = t.Module.ModuleHandle.GetRuntimeTypeHandleFromMetadataToken(mt);
            PropertyInfo pi = runtimeModuleType.GetProperty("MetadataImport", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo mi = pi.GetGetMethod(nonPublic: true);
            MetadataImport import = (MetadataImport)mi.Invoke(t.Module, new object[] { });


            //int[] v1 = import.GetTypeRefs();
            int[] v2 = import.GetStrings();

            Console.WriteLine(".");
        }
    }

    internal class TestAsm
    {
        public void Hello()
        {
            Console.WriteLine("Steve");
        }
    }
}
