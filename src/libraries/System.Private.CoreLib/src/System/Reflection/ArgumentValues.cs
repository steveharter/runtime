// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime;
using System.Runtime.InteropServices;

namespace System.Reflection
{
    public unsafe ref struct ArgumentValues
    {
        internal readonly int _argCount;
        internal readonly IntPtr* _byRefStorage;
        internal readonly IntPtr* _objStorage;
        internal readonly IntPtr* _typeStorage;
        internal RuntimeImports.GCFrameRegistration _regByRefStorage;
        internal RuntimeImports.GCFrameRegistration _regObjStorage; // Includes type storage
        internal object? _targetObject;
        internal object? _returnObject;

        [CLSCompliant(false)]
        public ArgumentValues(ArgumentValue* argumentStorage, int argCount)
        {
            //NativeMemory.Clear(argumentStorage, (nuint)argCount * (nuint)sizeof(TypedArgument));
            _argCount = argCount;

#pragma warning disable 8500
            _byRefStorage = (IntPtr*)(ByReference*)argumentStorage;
#pragma warning restore
            _objStorage = (IntPtr*)argumentStorage + argCount;
            _typeStorage = (IntPtr*)(argumentStorage + (argCount * 2));

            _regObjStorage = new RuntimeImports.GCFrameRegistration((void**)_objStorage, (uint)argCount * 2, areByRefs: false);
            _regByRefStorage = new RuntimeImports.GCFrameRegistration((void**)_byRefStorage, (uint)argCount, areByRefs: true);
        }
    }

    // This represents the storage requirement, not the actual layout for a single argument.
    public struct ArgumentValue
    {
#pragma warning disable CA1823, CS0169, IDE0051
        private IntPtr _1;
        private IntPtr _2;
        private IntPtr _3;
#pragma warning restore CA1823, CS0169, IDE0051
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe ref struct ArgumentValuesFixed
    {
        internal object? _obj0;
#pragma warning disable CA1823, CS0169, IDE0051 // accessed via ref arithmetic
        private object? _obj1;
        private object? _obj2;
        private object? _obj3;
        private object? _obj4;
        private object? _obj5;
        private object? _obj6;
        private object? _obj7;
#pragma warning restore CA1823, CS0169, IDE0051
        internal RuntimeType? _type0;
#pragma warning disable CA1823, CS0169, IDE0051 // accessed via ref arithmetic
        private RuntimeType? _type1;
        private RuntimeType? _type2;
        private RuntimeType? _type3;
        private RuntimeType? _type4;
        private RuntimeType? _type5;
        private RuntimeType? _type6;
        private RuntimeType? _type7;
#pragma warning restore CA1823, CS0169, IDE0051
        internal IntPtr _dummyRef; // needed to obtain pointer to _ref0
        internal ref byte _ref0;
#pragma warning disable CA1823, CS0169, IDE0051 // accessed via ref arithmetic
        private ref byte _ref1;
        private ref byte _ref2;
        private ref byte _ref3;
        private ref byte _ref4;
        private ref byte _ref5;
        private ref byte _ref6;
        private ref byte _ref7;
#pragma warning restore CA1823, CS0169, IDE0051

        internal object? _targetObject;
        internal object? _returnObject;

        internal readonly int _argCount;

        public ArgumentValuesFixed(int argCount)
        {
            if (argCount > 8)
                throw new ArgumentException("todo", nameof(argCount));

            _argCount = argCount;
        }
    }
}
