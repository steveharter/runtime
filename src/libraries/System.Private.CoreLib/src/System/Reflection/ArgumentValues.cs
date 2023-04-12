// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Reflection
{
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
        // private object? _obj5;
        // private object? _obj6;
        // private object? _obj7;
#pragma warning restore CA1823, CS0169, IDE0051
        internal RuntimeType? _type0;
#pragma warning disable CA1823, CS0169, IDE0051 // accessed via ref arithmetic
        private RuntimeType? _type1;
        private RuntimeType? _type2;
        private RuntimeType? _type3;
        private RuntimeType? _type4;
        // private RuntimeType? _type5;
        // private RuntimeType? _type6;
        // private RuntimeType? _type7;
#pragma warning restore CA1823, CS0169, IDE0051
        internal ref byte _ref0;
#pragma warning disable CA1823, CS0169, IDE0051 // accessed via ref arithmetic
        private ref byte _ref1;
        private ref byte _ref2;
        private ref byte _ref3;
        private ref byte _ref4;
        // private ref byte _ref5;
        // private ref byte _ref6;
        // private ref byte _ref7;
#pragma warning restore CA1823, CS0169, IDE0051

        internal readonly int _argCount;

        public ArgumentValuesFixed(int argCount)
        {
            if (argCount > 8)
                throw new ArgumentException("todo", nameof(argCount));

            _argCount = argCount;
        }

        public ArgumentValuesFixed(object? o1)
        {
            _argCount = 1;
            _obj0 = o1;
        }

        public ArgumentValuesFixed(object? o1, object? o2)
        {
            _argCount = 2;
            _obj0 = o1;
            _obj1 = o2;
        }

        public ArgumentValuesFixed(object? o1, object? o2, object? o3)
        {
            _argCount = 3;
            _obj0 = o1;
            _obj1 = o2;
            _obj2 = o3;
        }

        public ArgumentValuesFixed(object? o1, object? o2, object? o3, object? o4)
        {
            _argCount = 4;
            _obj0 = o1;
            _obj1 = o2;
            _obj2 = o3;
            _obj3 = o4;
        }

        public ArgumentValuesFixed(object? o1, object? o2, object? o3, object? o4, object? o5)
        {
            _argCount = 5;
            _obj0 = o1;
            _obj1 = o2;
            _obj2 = o3;
            _obj3 = o4;
            _obj4 = o5;
        }

        // public ArgumentValuesFixed(object? o1, object? o2, object? o3, object? o4, object? o5, object? o6)
        // {
        //     _argCount = 6;
        //     _obj0 = o1;
        //     _obj1 = o2;
        //     _obj2 = o3;
        //     _obj3 = o4;
        //     _obj4 = o5;
        //     _obj5 = o6;
        // }

        // public ArgumentValuesFixed(object? o1, object? o2, object? o3, object? o4, object? o5, object? o6, object? o7)
        // {
        //     _argCount = 7;
        //     _obj0 = o1;
        //     _obj1 = o2;
        //     _obj2 = o3;
        //     _obj3 = o4;
        //     _obj4 = o5;
        //     _obj5 = o6;
        //     _obj6 = o7;
        // }

        // public ArgumentValuesFixed(object? o1, object? o2, object? o3, object? o4, object? o5, object? o6, object? o7, object? o8)
        // {
        //     _argCount = 8;
        //     _obj0 = o1;
        //     _obj1 = o2;
        //     _obj2 = o3;
        //     _obj3 = o4;
        //     _obj4 = o5;
        //     _obj5 = o6;
        //     _obj6 = o7;
        //     _obj7 = o8;
        // }
    }
}
