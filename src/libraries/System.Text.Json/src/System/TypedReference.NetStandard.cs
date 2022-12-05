// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json.System
{
    internal static class TypedReferenceExtensions
    {
        public static ref byte TargetRef
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        //public static TypedReference FromObject(ref object? target, Type type)
        //{
        //    throw new NotSupportedException();
        //}

        public static Type GetTargetType(TypedReference value)
        {
            throw new NotSupportedException();
        }

        //public static TypedReference Make<T>(ref T value)
        //{
        //    throw new NotSupportedException();
        //}

        //public static TypedReference Make(ref object value, Type type)
        //{
        //    throw new NotSupportedException();
        //}
    }
}
