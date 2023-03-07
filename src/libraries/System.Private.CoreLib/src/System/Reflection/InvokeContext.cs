// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime;
using System.Runtime.CompilerServices;

namespace System.Reflection
{
    public unsafe ref partial struct InvokeContext
    {
        internal int _argCount;

        internal readonly ref object? _firstObject;

        internal ref object? _targetObj;
        internal ByReference _targetRef;

        internal ref object? _returnObj;
        internal ByReference _returnRef;

        internal readonly IntPtr* _pByRefStorage;
        internal readonly RuntimeImports.GCFrameRegistration* _pRegObjectStorage;
        internal readonly RuntimeImports.GCFrameRegistration* _pRegByRefStorage;

        public InvokeContext(ref ArgumentValues values)
        {
            _argCount = values._argCount;
            _pByRefStorage = values._byRefStorage;
            _firstObject = ref Unsafe.As<IntPtr, object?>(ref *values._objectStorage);

            _targetObj = ref values._targetObject;
            _returnObj = ref values._returnObject;

            _pRegObjectStorage = (RuntimeImports.GCFrameRegistration*)Unsafe.AsPointer(ref values._regObjectStorage);
            _pRegByRefStorage = (RuntimeImports.GCFrameRegistration*)Unsafe.AsPointer(ref values._regByRefStorage);
            _pRegByRefStorage->Reset();
            _pRegObjectStorage->Reset();
            RuntimeImports.RhRegisterForGCReporting(_pRegObjectStorage);
            RuntimeImports.RhRegisterForGCReporting(_pRegByRefStorage);
        }

        public InvokeContext(ref ArgumentValuesFixed values)
        {
            _argCount = values._argCount;
            _firstObject = ref values._obj0;
            _targetObj = ref values._targetObject;
            _returnObj = ref values._returnObject;

            _pByRefStorage = (IntPtr*)Unsafe.AsPointer(ref values._dummyRef) + 1;
            // this returns zero for the pointer:
            //_pByRefStorage = (IntPtr*)Unsafe.AsPointer(ref values._ref0);
        }

        #region Get\Set return

        public object? GetReturn()
        {
            return Unsafe.As<object>(_returnRef.Value);
        }

        public ref T GetReturn<T>()
        {
            // todo: this is not GC safe; need ByReference.GetValue<T>()
             return ref Unsafe.AsRef<T>(Unsafe.AsPointer(ref _returnRef.Value));
        }

        [CLSCompliant(false)]
        public void SetReturn(void* value)
        {
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
            _returnRef = ByReference.Create(ref Unsafe.Add(ref _returnObj, 1));
        }

        private void SetReturn_ReferenceType(object? value)
        {
            _returnObj = value;

            Debug.Assert(value is not ValueType);
            _returnRef = ByReference.Create(ref _returnObj);
        }

        public void SetReturn<T>(ref T value)
        {
#pragma warning disable CS9094 // This returns a parameter by reference through a ref parameter; but it can only safely be returned in a return statement
            _returnRef = ByReference.Create(ref value);
#pragma warning restore CS9094 // This returns a parameter by reference through a ref parameter; but it can only safely be returned in a return statement
            _returnObj = null;
        }
        #endregion

        #region Get\Set target
        public object? GetTarget()
        {
            return Unsafe.As<object>(_targetRef.Value);
        }

        public ref T GetTarget<T>()
        {
            // todo: this is not GC safe
            return ref Unsafe.AsRef<T>(Unsafe.AsPointer(ref _targetRef.Value));
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
            _targetObj = value;

            Debug.Assert(value is ValueType);
            _targetRef = ByReference.Create(ref Unsafe.Add(ref _targetObj, 1));
        }

        private void SetTarget_ReferenceType(object? value)
        {
            _targetObj = value;

            Debug.Assert(value is not ValueType);
            _targetRef = ByReference.Create(ref _targetObj);
        }

        public void SetTarget<T>(ref T value)
        {
#pragma warning disable CS9094 // This returns a parameter by reference through a ref parameter; but it can only safely be returned in a return statement
            _targetRef = ByReference.Create(ref value);
#pragma warning restore CS9094 // This returns a parameter by reference through a ref parameter; but it can only safely be returned in a return statement
        }

        [CLSCompliant(false)]
        public void SetTarget(void* value)
        {
            _targetRef = ByReference.Create(ref Unsafe.AsRef<byte>(value));
        }

        #endregion

        #region Get\Set arg
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
#pragma warning disable CS8500
            Debug.Assert(value is ValueType);
            *(ByReference*)(_pByRefStorage + index) = ByReference.Create(ref Unsafe.Add(ref _firstObject, index)!.GetRawData());
#pragma warning restore CS8500
        }

        private void SetReferenceTypeInternal(int index, object? value)
        {
            Unsafe.Add(ref _firstObject, index) = value;
#pragma warning disable CS8500
            Debug.Assert(value is not ValueType);
            *(ByReference*)(_pByRefStorage + index) = ByReference.Create(ref Unsafe.Add(ref _firstObject, index)); //AccessViolationException?
#pragma warning restore CS8500
        }

        public object? GetArgument(int index)
        {
            if (index < 0 || index >= _argCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            // todo: either remove this method, or use the ref to get and box the value
            return Unsafe.Add(ref _firstObject, index);
        }

        public ref T GetArgument<T>(int index)
        {
            if (index < 0 || index >= _argCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return ref Unsafe.AsRef<T>((void*)*(_pByRefStorage + index));
        }


        [CLSCompliant(false)]
        public void SetArgument(int index, void* value)
        {
            if (index < 0 || index >= _argCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            *(_pByRefStorage + index) = (IntPtr)value;
        }

        public void SetArgument<T>(int index, ref T value)
        {
            if (index < 0 || index >= _argCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            // For ArgumentValues, this will cause the reference to be tracked by _pRegByRefStorage.
            // For ArgumentValuesFixed, this will set a `ref byte` field which will allows GC to track.
            // todo: is this always GC-safe?
            *(_pByRefStorage + index) = (IntPtr)Unsafe.AsPointer(ref value);
            // ref byte v = ref Unsafe.As<T, byte>(ref value);
            // *(_pByRefStorage + index) = (IntPtr)Unsafe.AsPointer(ref v);
        }

        #endregion

        public void Dispose()
        {
            // Throw next time Get()\Set() are called.
            _argCount = 0;

            // Release the interior pointers that we re-set.
            //_targetObj = null;
            //_returnObj = null;

            if (_pRegObjectStorage != null)
            {
                RuntimeImports.RhUnregisterForGCReporting(_pRegByRefStorage);
                RuntimeImports.RhUnregisterForGCReporting(_pRegObjectStorage);
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

            RuntimeType returnType = invoker._returnType;
            if (returnType == typeof(void))
            {
                _returnRef = default;
            }
            else if (Unsafe.IsNullRef(ref _returnRef.Value))
            {
                if (returnType.IsValueType)
                {
                    SetReturn_ValueType(RuntimeType.AllocateValueType(returnType, value: null));
                }
                else if (returnType.IsByRef)
                {
                    RuntimeType elementType = (RuntimeType)returnType.GetElementType()!;
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

            invoker.InvokeDirect(_targetRef, _pByRefStorage, ref _returnRef);
        }

        public void Invoke(MethodInvoker invoker)
        {
            // todo - this overload validates and fixes up parameters
            throw new NotImplementedException();
        }
    }
}
