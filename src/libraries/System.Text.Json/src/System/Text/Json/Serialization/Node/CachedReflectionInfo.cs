// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if BUILDING_INBOX_LIBRARY

using System.Reflection;

namespace System.Text.Json.Serialization
{
    internal static partial class CachedReflectionInfo
    {
        private static MethodInfo? s_String_Format_String_ObjectArray;
        public static MethodInfo String_Format_String_ObjectArray =>
                                  s_String_Format_String_ObjectArray ??
                                 (s_String_Format_String_ObjectArray = typeof(string).GetMethod(nameof(string.Format), new Type[] { typeof(string), typeof(object[]) })!);

        private static ConstructorInfo? s_InvalidCastException_Ctor_String;
        public static ConstructorInfo InvalidCastException_Ctor_String =>
                                       s_InvalidCastException_Ctor_String ??
                                      (s_InvalidCastException_Ctor_String = typeof(InvalidCastException).GetConstructor(new Type[] { typeof(string) })!);

        private static MethodInfo? s_Object_GetType;
        public static MethodInfo Object_GetType =>
                                  s_Object_GetType ??
                                 (s_Object_GetType = typeof(object).GetMethod(nameof(object.GetType))!);
    }
}

#endif
