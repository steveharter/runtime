// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Reflection
{
    public unsafe ref partial struct InvokeContext
    {
        private int _argCount;
        private bool _needsRefs;

        private readonly ref object? _firstObject;
        private readonly ref RuntimeType? _firstType;

        private object? _targetObj;
        private ByReference _targetRef;
        private RuntimeType? _targetType = null;

        private object? _returnObj;
        private ByReference _returnRef;
        private RuntimeType? _returnType;

        private readonly IntPtr* _pByRefStorage;
        private readonly IntPtr* _pObjStorage;

        private RuntimeImports.GCFrameRegistration _regByRefStorage;
        private RuntimeImports.GCFrameRegistration _regObjStorage; // Includes type storage
        private bool _disposeNeeded;

        // zero-arg case:
        public unsafe InvokeContext()
        {
        }

        [CLSCompliant(false)]
        public unsafe InvokeContext(ArgumentValue* argumentStorage, int argCount)
        {
            NativeMemory.Clear(argumentStorage, (nuint)argCount * (nuint)sizeof(ArgumentValue));

            _argCount = argCount;
#pragma warning disable 8500
            _pByRefStorage = (IntPtr*)(ByReference*)argumentStorage;
            _pObjStorage = (IntPtr*)(object*)argumentStorage + argCount;
#pragma warning restore

            _firstObject = ref Unsafe.As<IntPtr, object?>(ref *_pObjStorage);
            _firstType = ref Unsafe.As<IntPtr, RuntimeType?>(ref *(_pObjStorage + _argCount));

            _regObjStorage = new RuntimeImports.GCFrameRegistration((void**)_pObjStorage, (uint)argCount * 2, areByRefs: false);
            RuntimeImports.RhRegisterForGCReporting((RuntimeImports.GCFrameRegistration*)Unsafe.AsPointer(ref _regObjStorage));

            _regByRefStorage = new RuntimeImports.GCFrameRegistration((void**)_pByRefStorage, (uint)argCount, areByRefs: true);
            RuntimeImports.RhRegisterForGCReporting((RuntimeImports.GCFrameRegistration*)Unsafe.AsPointer(ref _regByRefStorage));

            _disposeNeeded = true;
        }

        public InvokeContext(ref ArgumentValuesFixed values)
        {
            _argCount = values._argCount;
            _firstObject = ref values._obj0;
            _firstType = ref values._type0;
            _pByRefStorage = (IntPtr*)Unsafe.AsPointer(ref values._dummyRef) + 1;
            // this returns zero for the pointer:
            //_pByRefStorage = (IntPtr*)Unsafe.AsPointer(ref values._ref0);
        }

        public void Dispose()
        {
            _argCount = 0;

            _returnRef = default;
            _returnObj = null;

            _targetRef = default;
            _targetObj = null;

            _firstObject = default;
            _firstType = default;

            if (_disposeNeeded)
            {
                RuntimeImports.RhUnregisterForGCReporting((RuntimeImports.GCFrameRegistration*)Unsafe.AsPointer(ref _regByRefStorage));
                RuntimeImports.RhUnregisterForGCReporting((RuntimeImports.GCFrameRegistration*)Unsafe.AsPointer(ref _regObjStorage));
                _disposeNeeded = false;
            }
        }

        #region Get\Set return

        public object? GetReturn()
        {
            Debug.Assert(_returnType != null);

            if (_returnType == typeof(void))
            {
                throw new InvalidOperationException();
            }

            if (Unsafe.IsNullRef(ref _returnRef.Value))
            {
                return _returnObj;
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
            if (_returnType is null || _returnType == typeof(void))
            {
                throw new InvalidOperationException();
            }

            if (Unsafe.IsNullRef(ref _returnRef.Value))
            {
                if (typeof(T).IsValueType)
                {
                    if (_returnObj is null)
                    {
                        throw new InvalidOperationException("todo: method return value not set.");
                    }

                    _returnRef = ByReference.Create(ref _returnObj.GetRawData());
                }
                else
                {
#pragma warning disable CS9084 // Struct member returns 'this' or other instance members by reference
                    _returnRef = ByReference.Create(ref _returnObj);
#pragma warning restore CS9084 // Struct member returns 'this' or other instance members by reference
                }
            }

            return ref Unsafe.As<byte, T>(ref _returnRef.Value);
        }

        public void SetReturn(object value)
        {
            _returnObj = value;
            _returnRef = default;
        }

        [CLSCompliant(false)]
        public unsafe void SetReturn(void* value, Type type)
        {
            _needsRefs = true;
            _returnType = (RuntimeType)type;
            _returnRef = ByReference.Create(ref Unsafe.AsRef<byte>(value));
            _returnObj = null;
        }

        public void SetReturn<T>(ref T value)
        {
            _needsRefs = true;
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
                if (typeof(T).IsValueType)
                {
                    if (_targetObj is null)
                    {
                        throw new InvalidOperationException("todo: target value not set.");
                    }

                    _targetRef = ByReference.Create(ref _targetObj.GetRawData());
                }
                else
                {
#pragma warning disable CS9084 // Struct member returns 'this' or other instance members by reference
                    _targetRef = ByReference.Create(ref _targetObj);
#pragma warning restore CS9084 // Struct member returns 'this' or other instance members by reference
                }
            }

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
#pragma warning disable CS9084 // Struct member returns 'this' or other instance members by reference
            _targetRef = ByReference.Create(ref _targetObj);
#pragma warning restore CS9084 // Struct member returns 'this' or other instance members by reference
        }

        // todo: safety?
        public void SetTarget<T>(ref T value)
        {
            _needsRefs = true;
#pragma warning disable CS9094
            _targetRef = ByReference.Create(ref value);
#pragma warning restore CS9094
        }

        [CLSCompliant(false)]
        public unsafe void SetTarget(void* value, Type type)
        {
            _needsRefs = true;
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

            object? obj = Unsafe.Add(ref _firstObject, index);
            if (obj is not null)
            {
                return obj;
            }

#pragma warning disable CS8500
            ByReference br = *(ByReference*)(_pByRefStorage + index);
#pragma warning restore CS8500

            if (Unsafe.IsNullRef(ref br.Value))
            {
                return null;
            }

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

            return ref Unsafe.AsRef<T>(GetRef<T>(index));
        }

        public void SetArgument(int index, object? value)
        {
            if (index < 0 || index >= _argCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            Unsafe.Add(ref _firstObject, index) = value;
            *(_pByRefStorage + index) = IntPtr.Zero;
        }

        // todo (gc-safe capture of ref types; value types assumed OK since on stack previously -- can the compiler re-use same slot in > 1 place?):
        public void SetArgument<T>(int index, ref T value)
        {
            _needsRefs = true;
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

            _needsRefs = true;
            Unsafe.Add(ref _firstObject, index) = null;
            Unsafe.Add(ref _firstType, index) = (RuntimeType)type;
            *(_pByRefStorage + index) = (IntPtr)value;
        }

        #endregion

        private void UpdateRef(int index)
        {
            RuntimeType t = Unsafe.Add(ref _firstType, index)!;
            if (t.IsValueType)
            {
#pragma warning disable CS8500
                *(ByReference*)(_pByRefStorage + index) = ByReference.Create(ref Unsafe.Add(ref _firstObject, index)!.GetRawData());
#pragma warning restore CS8500
            }
            else
            {
#pragma warning disable CS8500
                *(ByReference*)(_pByRefStorage + index) = ByReference.Create(ref Unsafe.Add(ref _firstObject, index));
#pragma warning restore CS8500
            }
        }

        private IntPtr* GetRef<T>(int index)
        {
#pragma warning disable CS8500
            ByReference* pbr = (ByReference*)(_pByRefStorage + index);
#pragma warning restore CS8500
            if (Unsafe.IsNullRef(ref pbr->Value))
            {
                if (typeof(T).IsValueType)
                {
                    *pbr = ByReference.Create(ref Unsafe.Add(ref _firstObject, index)!.GetRawData());
                }
                else
                {
                    *pbr = ByReference.Create(ref Unsafe.Add(ref _firstObject, index));
                }
            }
            return *(IntPtr**)pbr;
        }

        private void NormalizeForRefs(MethodInvoker invoker)
        {
            // Target.
            if (!invoker._hasThis)
            {
                _targetRef = default;
                _targetObj = null;
            }
            else if (Unsafe.IsNullRef(ref _targetRef.Value))
            {
                if (_targetObj is null)
                {
                    throw new InvalidOperationException("todo: need a target");
                }

                Debug.Assert(_targetType is not null);

#pragma warning disable CS9084 // Struct member returns 'this' or other instance members by reference
                _targetRef = _targetType.IsValueType ?
                    ByReference.Create(ref _targetObj.GetRawData()) :
                    ByReference.Create(ref _targetObj);
#pragma warning restore CS9084 // Struct member returns 'this' or other instance members by reference
            }

            // Return.
            if (invoker._returnType == typeof(void))
            {
                _returnRef = default;
                _returnObj = null;
            }
            else if (Unsafe.IsNullRef(ref _returnRef.Value))
            {
                RuntimeType returnType = invoker._returnType;

                if (returnType.IsValueType && _returnObj is null)
                {
                    _returnObj = RuntimeType.AllocateValueType(returnType, value: null);
                }

#pragma warning disable CS9084 // Struct member returns 'this' or other instance members by reference
                _returnRef = returnType.IsValueType ?
                    ByReference.Create(ref _returnObj!.GetRawData()) :
                    ByReference.Create(ref _returnObj);
#pragma warning restore CS9084 // Struct member returns 'this' or other instance members by reference
            }

            // Args.
            if (_argCount > 0)
            {
                RuntimeType[] argTypes = invoker._argTypes;
                for (int i = 0; i < _argCount; i++)
                {
                    // Provide default values for missing parameters
                    if (*(_pByRefStorage + i) == IntPtr.Zero)
                    {
                        RuntimeType parameterType = argTypes[i];
                        if (parameterType.IsByRef)
                        {
                            parameterType = (RuntimeType)parameterType.GetElementType()!;
                        }

                        ref object? arg = ref Unsafe.Add(ref _firstObject, i);
                        if (parameterType.IsValueType)
                        {
                            arg ??= RuntimeType.AllocateValueType(parameterType, value: null);
#pragma warning disable CS8500
                            *(ByReference*)(_pByRefStorage + i) = ByReference.Create(ref arg!.GetRawData());
#pragma warning restore CS8500
                        }
                        else
                        {
#pragma warning disable CS8500
                            *(ByReference*)(_pByRefStorage + i) = ByReference.Create(ref arg);
#pragma warning restore CS8500
                        }
                    }
                }
            }
        }

        public unsafe void InvokeDirect(MethodInvoker invoker)
        {
            if (_argCount != invoker._argCount)
            {
                throw new InvalidOperationException($"todo: The provided argument count of {_argCount} is not equal to the expected value of {invoker._argCount}.");
            }

            if (_returnType != invoker._returnType)
            {
                if (_returnType is not null)
                {
                    throw new InvalidOperationException($"todo: the return type {_returnType} is not correct. Expected: {invoker._returnType}.");
                }
                _returnType = invoker._returnType;
            }

            if (invoker._needsRefs || _needsRefs)
            {
                NormalizeForRefs(invoker);
                invoker.InvokeDirect_Ref(_targetRef, _pByRefStorage, ref _returnRef);
            }
            else
            {
                // Perform minimal validation on target and return.
                if (!invoker._hasThis)
                {
                    _targetRef = default;
                    _targetObj = null;
                }
                else if (Unsafe.IsNullRef(ref _targetRef.Value))
                {
                    throw new InvalidOperationException("todo: need to set target");
                }

                if (_returnType == typeof(void))
                {
                    _returnRef = default;
                }

                _returnObj = invoker.InvokeDirect_Obj(_targetObj, new ReadOnlySpan<object?>(ref _firstObject!, _argCount));
            }
        }

        public void Invoke(MethodInvoker invoker)
        {
            // todo - this overload validates and fixes up parameters
            throw new NotImplementedException();
        }
    }
}
