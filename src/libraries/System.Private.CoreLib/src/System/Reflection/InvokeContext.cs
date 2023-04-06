// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime;
using System.Runtime.CompilerServices;

namespace System.Reflection
{
    public unsafe ref partial struct InvokeContext
    {
        private int _argCount;

        private readonly ref object? _firstObject;
        private readonly ref RuntimeType? _firstType;

        private ref object? _targetObj;
        private ByReference _targetRef;
        private RuntimeType? _targetType = null;

        private ref object? _returnObj;
        private ByReference _returnRef;
        private RuntimeType _returnType = (RuntimeType)typeof(void);

        private readonly IntPtr* _pByRefStorage;
        private readonly IntPtr* _pObjStorage;
        private readonly RuntimeImports.GCFrameRegistration* _pRegObjStorage;
        private readonly RuntimeImports.GCFrameRegistration* _pRegByRefStorage;

        public unsafe InvokeContext(ref ArgumentValues values)
        {
            _argCount = values._argCount;
            _pByRefStorage = values._byRefStorage;
            _pObjStorage = values._objStorage;
            _firstObject = ref Unsafe.As<IntPtr, object?>(ref *values._objStorage);
            _firstType = ref Unsafe.As<IntPtr, RuntimeType?>(ref *(values._objStorage + _argCount));

            _targetObj = ref values._targetObject;
            _returnObj = ref values._returnObject;

            _pRegObjStorage = (RuntimeImports.GCFrameRegistration*)Unsafe.AsPointer(ref values._regObjStorage);
            _pRegObjStorage->Reset();
            _pRegByRefStorage = (RuntimeImports.GCFrameRegistration*)Unsafe.AsPointer(ref values._regByRefStorage);
            _pRegByRefStorage->Reset();
            RuntimeImports.RhRegisterForGCReporting(_pRegObjStorage);
            RuntimeImports.RhRegisterForGCReporting(_pRegByRefStorage);
        }

        public InvokeContext(ref ArgumentValuesFixed values)
        {
            _argCount = values._argCount;
            _firstObject = ref values._obj0;
            _firstType = ref values._type0;
            _targetObj = ref values._targetObject;
            _returnObj = ref values._returnObject;

            _pByRefStorage = (IntPtr*)Unsafe.AsPointer(ref values._dummyRef) + 1;
            // this returns zero for the pointer:
            //_pByRefStorage = (IntPtr*)Unsafe.AsPointer(ref values._ref0);
        }

        #region Get\Set return

        public object? GetReturn()
        {
            Debug.Assert(_returnType != null);

            if (Unsafe.IsNullRef(ref _returnRef.Value))
            {
                return null;
            }

            if (_returnType == typeof(void))
            {
                throw new InvalidOperationException();
            }

            RuntimeType? elementType = (RuntimeType?)_returnType.GetElementType();
            if (elementType is not null)
            {
                return TypedReference.ToObject(elementType, ref _returnRef.Value);
            }

            return TypedReference.ToObject(_returnType, ref _returnRef.Value);
        }

        public ref T GetReturn<T>()
        {
            Debug.Assert(_returnType != null);

            if (_returnType == typeof(void))
            {
                throw new InvalidOperationException();
            }

            return ref Unsafe.As<byte, T>(ref _returnRef.Value);
        }

        [CLSCompliant(false)]
        public unsafe void SetReturn(void* value, Type type)
        {
            _returnType = (RuntimeType)type;
            _returnRef = ByReference.Create(ref Unsafe.AsRef<byte>(value));
        }

        public void SetReturn(object value)
        {
            if (value is ValueType)
            {
                SetReturn_ValueType(value);
            }
            else
            {
                SetReturn_ReferenceType(value);
            }
        }

        private void SetReturn_ValueType(object value)
        {
            _returnObj = value;

            Debug.Assert(value is ValueType);
            _returnRef = ByReference.Create(ref _returnObj.GetRawData());
        }

        private void SetReturn_ReferenceType(object? value)
        {
            _returnObj = value;

            Debug.Assert(value is not ValueType);
            _returnRef = ByReference.Create(ref _returnObj);
        }

        // todo:
        public void SetReturn<T>(ref T value)
        {
#pragma warning disable CS9094
            _returnRef = ByReference.Create(ref value);
#pragma warning restore CS9094
            _returnObj = null;
        }
        #endregion

        #region Get\Set target
        public object? GetTarget()
        {
            if (Unsafe.IsNullRef(ref _targetRef.Value))
            {
                return null;
            }

            return TypedReference.ToObject(_targetType!, ref _targetRef.Value);
        }

        public ref T GetTarget<T>()
        {
            if (Unsafe.IsNullRef(ref _targetRef.Value))
            {
                throw new InvalidOperationException("todo: no target");
            }

            Debug.Assert(_targetType is not null);
            return ref Unsafe.As<byte, T>(ref _targetRef.Value);
        }

        public void SetTarget(object value)
        {
            if (value is ValueType)
            {
                SetTarget_ValueType(value);
            }
            else
            {
                SetTarget_ReferenceType(value);
            }
        }

        private void SetTarget_ValueType(object value)
        {
            Debug.Assert(value is ValueType);
            _targetObj = value;
            _targetType = (RuntimeType)value.GetType();
            _targetRef = ByReference.Create(ref _targetObj.GetRawData());
        }

        private void SetTarget_ReferenceType(object? value)
        {
            Debug.Assert(value is not ValueType);
            _targetObj = value;
            _targetType = (RuntimeType?)value?.GetType();
            _targetRef = ByReference.Create(ref _targetObj);
        }

        // todo: safety?
        public void SetTarget<T>(ref T value)
        {
#pragma warning disable CS9094
            _targetRef = ByReference.Create(ref value);
#pragma warning restore CS9094
        }

        [CLSCompliant(false)]
        public unsafe void SetTarget(void* value, Type type)
        {
            _targetType = (RuntimeType)type;
            _targetRef = ByReference.Create(ref Unsafe.AsRef<byte>(value));
        }

        #endregion

        #region Get\Set arg

        public object? GetArgument(int index)
        {
            if (index < 0 || index >= _argCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

#pragma warning disable CS8500
            ByReference br = *(ByReference*)(_pByRefStorage + index);
#pragma warning restore CS8500
            RuntimeType? type = Unsafe.Add(ref _firstType, index);

            if (type is null)
            {
                throw new InvalidOperationException("todo - arg not set");
            }

            return TypedReference.ToObject(type, ref br.Value);
        }

        public ref T GetArgument<T>(int index)
        {
            if (index < 0 || index >= _argCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return ref Unsafe.AsRef<T>((void*)*(_pByRefStorage + index));
            // or:
            // ByReference br = *(ByReference*)(_pByRefStorage + index);
            // return ref Unsafe.As<byte, T>(ref br.Value);
        }

        public void SetArgument(int index, object? value)
        {
            if (index < 0 || index >= _argCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            if (value is ValueType)
            {
                SetValueTypeInternal(index, value);
            }
            else
            {
                SetReferenceTypeInternal(index, value);
            }
        }

        private void SetValueTypeInternal(int index, object? value)
        {
            Unsafe.Add(ref _firstObject, index) = value;
            Debug.Assert(value is ValueType);
            Unsafe.Add(ref _firstType, index) = value is null ? (RuntimeType)typeof(object) : (RuntimeType)value.GetType();
#pragma warning disable CS8500
            *(ByReference*)(_pByRefStorage + index) = ByReference.Create(ref Unsafe.Add(ref _firstObject, index)!.GetRawData());
#pragma warning restore CS8500
        }

        private void SetReferenceTypeInternal(int index, object? value)
        {
            Unsafe.Add(ref _firstObject, index) = value;
#pragma warning disable CS8500
            Debug.Assert(value is not ValueType);
            Unsafe.Add(ref _firstType, index) = value is null ? (RuntimeType)typeof(object) : (RuntimeType)value.GetType();
            *(ByReference*)(_pByRefStorage + index) = ByReference.Create(ref Unsafe.Add(ref _firstObject, index));
#pragma warning restore CS8500
        }

        // todo (gc-safe capture of ref types; value types assumed OK since on stack previously -- can the compiler re-use same slot in > 1 place?):
        public void SetArgument<T>(int index, ref T value)
        {
            Unsafe.Add(ref _firstType, index) = (RuntimeType)typeof(T);
#pragma warning disable CS9094
#pragma warning disable CS8500
            *(ByReference*)(_pByRefStorage + index) = ByReference.Create(ref value);
#pragma warning restore CS8500
#pragma warning restore CS9094
        }

        [CLSCompliant(false)]
        public unsafe void SetArgument(int index, void* value, Type type)
        {
            if (index < 0 || index >= _argCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            Unsafe.Add(ref _firstType, index) = (RuntimeType)type;
            *(_pByRefStorage + index) = (IntPtr)value;

            // Don't bother clearing out any previous value in _firstObject.
        }

        #endregion

        public void Dispose()
        {
            // Throw next time Get()\Set() are called.
            _argCount = 0;

            if (_pRegObjStorage != null)
            {
                RuntimeImports.RhUnregisterForGCReporting(_pRegByRefStorage);
                RuntimeImports.RhUnregisterForGCReporting(_pRegObjStorage);
            }
        }

        public unsafe void InvokeDirect(MethodInvoker invoker)
        {
            RuntimeType[] argTypes = invoker._argTypes;

            if (!invoker._hasThis)
            {
                _targetRef = default;
            }
            else if (Unsafe.IsNullRef(ref _targetRef.Value))
            {
                throw new InvalidOperationException("todo: need to set target");
            }

            if (argTypes.Length != _argCount)
            {
                throw new InvalidOperationException($"todo: The provided argument count of {_argCount} is not equal to the expected value of {argTypes.Length} which includes any return value.");
            }

            for (int i = 0; i < _argCount; i++)
            {
                // Provide default values for missing parameters
                if (*(_pByRefStorage + i) == IntPtr.Zero)
                {
                    RuntimeType parameterType = argTypes[i];
                    if (parameterType.IsValueType)
                    {
                        SetValueTypeInternal(i, RuntimeType.AllocateValueType(parameterType, value: null));
                    }
                    else if (parameterType.IsByRef)
                    {
                        RuntimeType elementType = (RuntimeType)parameterType.GetElementType()!;
                        if (elementType.IsValueType)
                        {
                            SetValueTypeInternal(i, RuntimeType.AllocateValueType(elementType, value: null));
                        }
                        else
                        {
                            SetReferenceTypeInternal(i, null);
                        }
                    }
                    else
                    {
                        SetReferenceTypeInternal(i, null);
                    }
                    // pointer?
                }
            }

            _returnType = invoker._returnType;
            if (_returnType == typeof(void))
            {
                _returnRef = default;
            }
            else
            {
                if (Unsafe.IsNullRef(ref _returnRef.Value))
                {
                    if (_returnType.IsValueType)
                    {
                        SetReturn_ValueType(RuntimeType.AllocateValueType(_returnType, value: null));
                    }
                    else if (_returnType.IsByRef)
                    {
                        RuntimeType elementType = (RuntimeType)_returnType.GetElementType()!;
                        if (elementType.IsValueType)
                        {
                            SetReturn_ValueType(RuntimeType.AllocateValueType(elementType, value: null));
                        }
                        else
                        {
                            SetReturn_ReferenceType(null);
                        }
                    }
                    else
                    {
                        SetReturn_ReferenceType(null);
                    }
                    // pointer?
                }
            }

            invoker.InvokeDirect(_targetRef, _pByRefStorage, ref _returnRef);
        }

        public void Invoke(MethodInvoker invoker)
        {
            // todo - this overload validates and fixes up parameters
            throw new NotImplementedException();
        }
    }
}
