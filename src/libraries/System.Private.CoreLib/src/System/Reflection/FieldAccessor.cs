// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Reflection
{
    internal sealed partial class FieldAccessor
    {
        private readonly RtFieldInfo _fieldInfo;
        public InvocationFlags _invocationFlags;
        private InvokerEmitUtil.GetField? _getField;
        private InvokerEmitUtil.SetField? _setField;

        public FieldAccessor(RtFieldInfo fieldInfo)
        {
            _fieldInfo = fieldInfo;
        }

        [DebuggerStepThrough]
        [DebuggerHidden]
        public object? GetValue(object? obj)
        {
            _getField ??= InvokerEmitUtil.CreateGetFieldDelegate(_fieldInfo);

            unsafe
            {
                return _getField(obj);
            }
        }

        [DebuggerStepThrough]
        [DebuggerHidden]
        public void SetValue(object? obj, object? value)
        {
            _setField ??= InvokerEmitUtil.CreateSetFieldDelegate(_fieldInfo);

            unsafe
            {
                if (_fieldInfo.FieldType.IsValueType)
                {
                    ByReference valueRef = ByReference.Create(ref value!.GetRawData());
                    _setField(obj, (IntPtr*)valueRef.Value);
                }
                else
                {
                    ByReference valueRef = ByReference.Create(ref value!);
                    _setField(obj, (IntPtr*)valueRef.Value);
                }

                //_fieldInfo.SetValueNonEmit(obj, value);
            }

        }
    }
}
