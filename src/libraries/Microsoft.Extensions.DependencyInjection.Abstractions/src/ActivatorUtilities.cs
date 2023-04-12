// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Internal;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Helper code for the various activator services.
    /// </summary>
    public static class ActivatorUtilities
    {
        private static readonly MethodInfo GetServiceInfo =
            GetMethodInfo<Func<IServiceProvider, Type, Type, bool, object?>>((sp, t, r, c) => GetService(sp, t, r, c));

        /// <summary>
        /// Instantiate a type with constructor arguments provided directly and/or from an <see cref="IServiceProvider"/>.
        /// </summary>
        /// <param name="provider">The service provider used to resolve dependencies</param>
        /// <param name="instanceType">The type to activate</param>
        /// <param name="parameters">Constructor arguments not provided by the <paramref name="provider"/>.</param>
        /// <returns>An activated object of type instanceType</returns>
        public static object CreateInstance(
            IServiceProvider provider,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type instanceType,
            params object[] parameters)
        {
#pragma warning disable CA1510 // Use ArgumentNullException throw helper
            if (provider == null)
            {
                throw new ArgumentNullException(nameof(provider));
            }
#pragma warning restore CA1510 // Use ArgumentNullException throw helper

            if (instanceType.IsAbstract)
            {
                throw new InvalidOperationException(SR.CannotCreateAbstractClasses);
            }

            IServiceProviderIsService? serviceProviderIsService = provider.GetService<IServiceProviderIsService>();
            // if container supports using IServiceProviderIsService, we try to find the longest ctor that
            // (a) matches all parameters given to CreateInstance
            // (b) matches the rest of ctor arguments as either a parameter with a default value or as a service registered
            // if no such match is found we fallback to the same logic used by CreateFactory which would only allow creating an
            // instance if all parameters given to CreateInstance only match with a single ctor
            if (serviceProviderIsService != null)
            {
                int bestLength = -1;
                bool seenPreferred = false;

                ConstructorMatcher bestMatcher = default;
                bool multipleBestLengthFound = false;

                foreach (ConstructorInfo? constructor in instanceType.GetConstructors())
                {
                    var matcher = new ConstructorMatcher(constructor);
                    bool isPreferred = constructor.IsDefined(typeof(ActivatorUtilitiesConstructorAttribute), false);
                    int length = matcher.Match(parameters, serviceProviderIsService);

                    if (isPreferred)
                    {
                        if (seenPreferred)
                        {
                            ThrowMultipleCtorsMarkedWithAttributeException();
                        }

                        if (length == -1)
                        {
                            ThrowMarkedCtorDoesNotTakeAllProvidedArguments();
                        }
                    }

                    if (isPreferred || bestLength < length)
                    {
                        bestLength = length;
                        bestMatcher = matcher;
                        multipleBestLengthFound = false;
                    }
                    else if (bestLength == length)
                    {
                        multipleBestLengthFound = true;
                    }

                    seenPreferred |= isPreferred;
                }

                if (bestLength != -1)
                {
                    if (multipleBestLengthFound)
                    {
                        throw new InvalidOperationException(SR.Format(SR.MultipleCtorsFoundWithBestLength, instanceType, bestLength));
                    }

                    return bestMatcher.CreateInstance(provider);
                }
            }

            Type?[] argumentTypes = new Type[parameters.Length];
            for (int i = 0; i < argumentTypes.Length; i++)
            {
                argumentTypes[i] = parameters[i]?.GetType();
            }

            FindApplicableConstructor(instanceType, argumentTypes, out ConstructorInfo constructorInfo, out int?[] parameterMap);
            var constructorMatcher = new ConstructorMatcher(constructorInfo);
            constructorMatcher.MapParameters(parameterMap, parameters);
            return constructorMatcher.CreateInstance(provider);
        }

        /// <summary>
        /// Create a delegate that will instantiate a type with constructor arguments provided directly
        /// and/or from an <see cref="IServiceProvider"/>.
        /// </summary>
        /// <param name="instanceType">The type to activate</param>
        /// <param name="argumentTypes">
        /// The types of objects, in order, that will be passed to the returned function as its second parameter
        /// </param>
        /// <returns>
        /// A factory that will instantiate instanceType using an <see cref="IServiceProvider"/>
        /// and an argument array containing objects matching the types defined in argumentTypes
        /// </returns>
        public static ObjectFactory CreateFactory(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type instanceType,
            Type[] argumentTypes)
        {
            #if NETSTANDARD2_1_OR_GREATER || NETCOREAPP
            //if (!RuntimeFeature.IsDynamicCodeSupported)
            {
                // Create a reflection-based factory when dynamic code isn't supported, e.g. app is published with NativeAOT.
                // Reflection-based factory is faster than interpreted expressions and doesn't pull in System.Linq.Expressions dependency.
                return CreateFactoryReflection(instanceType, argumentTypes);
            }
            #endif

            //CreateFactoryInternal(instanceType, argumentTypes, out ParameterExpression provider, out ParameterExpression argumentArray, out Expression factoryExpressionBody);

            //var factoryLambda = Expression.Lambda<Func<IServiceProvider, object?[]?, object>>(
            //    factoryExpressionBody, provider, argumentArray);

            //Func<IServiceProvider, object?[]?, object>? result = factoryLambda.Compile();
            //return result.Invoke;
        }

        /// <summary>
        /// Create a delegate that will instantiate a type with constructor arguments provided directly
        /// and/or from an <see cref="IServiceProvider"/>.
        /// </summary>
        /// <typeparam name="T">The type to activate</typeparam>
        /// <param name="argumentTypes">
        /// The types of objects, in order, that will be passed to the returned function as its second parameter
        /// </param>
        /// <returns>
        /// A factory that will instantiate type T using an <see cref="IServiceProvider"/>
        /// and an argument array containing objects matching the types defined in argumentTypes
        /// </returns>
        public static ObjectFactory<T>
            CreateFactory<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(
                Type[] argumentTypes)
        {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP
            if (!RuntimeFeature.IsDynamicCodeSupported)
            {
                // Create a reflection-based factory when dynamic code isn't supported, e.g. app is published with NativeAOT.
                // Reflection-based factory is faster than interpreted expressions and doesn't pull in System.Linq.Expressions dependency.
                var factory = CreateFactoryReflection(typeof(T), argumentTypes);
                return (serviceProvider, arguments) => (T)factory(serviceProvider, arguments);
            }
#endif

            CreateFactoryInternal(typeof(T), argumentTypes, out ParameterExpression provider, out ParameterExpression argumentArray, out Expression factoryExpressionBody);

            var factoryLambda = Expression.Lambda<Func<IServiceProvider, object?[]?, T>>(
                factoryExpressionBody, provider, argumentArray);

            Func<IServiceProvider, object?[]?, T>? result = factoryLambda.Compile();
            return result.Invoke;
        }

        private static void CreateFactoryInternal([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type instanceType, Type[] argumentTypes, out ParameterExpression provider, out ParameterExpression argumentArray, out Expression factoryExpressionBody)
        {
            FindApplicableConstructor(instanceType, argumentTypes, out ConstructorInfo constructor, out int?[] parameterMap);

            provider = Expression.Parameter(typeof(IServiceProvider), "provider");
            argumentArray = Expression.Parameter(typeof(object[]), "argumentArray");
            factoryExpressionBody = BuildFactoryExpression(constructor, parameterMap, provider, argumentArray);
        }

        /// <summary>
        /// Instantiate a type with constructor arguments provided directly and/or from an <see cref="IServiceProvider"/>.
        /// </summary>
        /// <typeparam name="T">The type to activate</typeparam>
        /// <param name="provider">The service provider used to resolve dependencies</param>
        /// <param name="parameters">Constructor arguments not provided by the <paramref name="provider"/>.</param>
        /// <returns>An activated object of type T</returns>
        public static T CreateInstance<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(IServiceProvider provider, params object[] parameters)
        {
            return (T)CreateInstance(provider, typeof(T), parameters);
        }

        /// <summary>
        /// Retrieve an instance of the given type from the service provider. If one is not found then instantiate it directly.
        /// </summary>
        /// <typeparam name="T">The type of the service</typeparam>
        /// <param name="provider">The service provider used to resolve dependencies</param>
        /// <returns>The resolved service or created instance</returns>
        public static T GetServiceOrCreateInstance<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(IServiceProvider provider)
        {
            return (T)GetServiceOrCreateInstance(provider, typeof(T));
        }

        /// <summary>
        /// Retrieve an instance of the given type from the service provider. If one is not found then instantiate it directly.
        /// </summary>
        /// <param name="provider">The service provider</param>
        /// <param name="type">The type of the service</param>
        /// <returns>The resolved service or created instance</returns>
        public static object GetServiceOrCreateInstance(
            IServiceProvider provider,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type)
        {
            return provider.GetService(type) ?? CreateInstance(provider, type);
        }

        private static MethodInfo GetMethodInfo<T>(Expression<T> expr)
        {
            var mc = (MethodCallExpression)expr.Body;
            return mc.Method;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static object? GetService(IServiceProvider sp, Type type, Type requiredBy, bool isDefaultParameterRequired)
        {
            object? service = sp.GetService(type);
            if (service == null && !isDefaultParameterRequired)
            {
                throw new InvalidOperationException(SR.Format(SR.UnableToResolveService, type, requiredBy));
            }
            return service;
        }

        private static NewExpression BuildFactoryExpression(
            ConstructorInfo constructor,
            int?[] parameterMap,
            Expression serviceProvider,
            Expression factoryArgumentArray)
        {
            ParameterInfo[]? constructorParameters = constructor.GetParameters();
            var constructorArguments = new Expression[constructorParameters.Length];

            for (int i = 0; i < constructorParameters.Length; i++)
            {
                ParameterInfo? constructorParameter = constructorParameters[i];
                Type? parameterType = constructorParameter.ParameterType;
                bool hasDefaultValue = ParameterDefaultValue.TryGetDefaultValue(constructorParameter, out object? defaultValue);

                if (parameterMap[i] != null)
                {
                    constructorArguments[i] = Expression.ArrayAccess(factoryArgumentArray, Expression.Constant(parameterMap[i]));
                }
                else
                {
                    var parameterTypeExpression = new Expression[] { serviceProvider,
                        Expression.Constant(parameterType, typeof(Type)),
                        Expression.Constant(constructor.DeclaringType, typeof(Type)),
                        Expression.Constant(hasDefaultValue) };
                    constructorArguments[i] = Expression.Call(GetServiceInfo, parameterTypeExpression);
                }

                // Support optional constructor arguments by passing in the default value
                // when the argument would otherwise be null.
                if (hasDefaultValue)
                {
                    ConstantExpression? defaultValueExpression = Expression.Constant(defaultValue);
                    constructorArguments[i] = Expression.Coalesce(constructorArguments[i], defaultValueExpression);
                }

                constructorArguments[i] = Expression.Convert(constructorArguments[i], parameterType);
            }

            return Expression.New(constructor, constructorArguments);
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP
        private static ObjectFactory CreateFactoryReflection(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type instanceType,
            Type?[] argumentTypes)
        {
            FindApplicableConstructor(instanceType, argumentTypes, out ConstructorInfo constructor, out int?[] parameterMap);

            ParameterInfo[] constructorParameters = constructor.GetParameters();
            if (constructorParameters.Length == 0)
            {
                return (IServiceProvider serviceProvider, object?[]? arguments) =>
                    constructor.Invoke(BindingFlags.DoNotWrapExceptions, binder: null, parameters: null, culture: null);
            }

            FactoryParameterContext[] parameters = new FactoryParameterContext[constructorParameters.Length];
            bool hasAnyDefaultValues = false;
            int serviceLookupCount = 0;
            for (int i = 0; i < constructorParameters.Length; i++)
            {
                ParameterInfo constructorParameter = constructorParameters[i];
                bool hasDefaultValue = ParameterDefaultValue.TryGetDefaultValue(constructorParameter, out object? defaultValue);
                hasAnyDefaultValues |= hasDefaultValue;

                parameters[i] = new FactoryParameterContext(constructorParameter.ParameterType, hasDefaultValue, defaultValue, parameterMap[i] ?? -1);

                if (parameters[i].ArgumentIndex != -1)
                {
                    serviceLookupCount++;
                }
            }
            Type declaringType = constructor.DeclaringType!;

            //return DoIt(constructor, parameters, declaringType);
            if (hasAnyDefaultValues)
            {
                return DoIt(constructor, parameters, declaringType);
            }
            else
            {
                if (serviceLookupCount == 0)
                {
                    return DoIt3(constructor, parameters);
                }

                return DoIt2(constructor, parameters, declaringType);
            }
        }

        private static ObjectFactory DoIt(ConstructorInfo constructor, FactoryParameterContext[] parameters, Type declaringType)
        {
            return (IServiceProvider serviceProvider, object?[]? arguments) =>
            {
#if false //NETCOREAPP8_0_OR_GREATER
                unsafe
                {
                    int length = parameters.Length;
                    IntPtr* args = stackalloc IntPtr[length * 2];
                    ArgumentValues values = new(args, length);
                    using (InvokeContext context = new InvokeContext(ref values))
                    {
                        for (int i = 0; i < parameters.Length; i++)
                        {
                            FactoryParameterContext parameter = parameters[i];

                            int argumentIndex = parameter.ArgumentIndex;
                            if (argumentIndex != -1)
                            {
                                context.Set(i, arguments![argumentIndex]);
                            }
                            else if (parameter.HasDefaultValue)
                            {
                                context.Set(i, GetService(
                                        serviceProvider,
                                        parameter.ParameterType,
                                        declaringType,
                                        true) ?? parameter.DefaultValue);
                            }
                            else
                            {
                                context.Set(i, GetService(
                                        serviceProvider,
                                        parameter.ParameterType,
                                        declaringType,
                                        false));
                            }
                        }
                        return MethodInvoker.GetInvoker(constructor).InvokeDirect(obj: null, length)!;
                    }
                }
#else
                object?[] constructorArguments = new object?[parameters.Length];
                for (int i = 0; i < parameters.Length; i++)
                {
                    FactoryParameterContext parameter = parameters[i];

                    //if (i == 0)
                    //{
                    //    throw new Exception($"{parameters[0].ArgumentIndex} {parameters[1].ArgumentIndex} {parameters[2].ArgumentIndex}");
                    //}

                    int argumentIndex = parameter.ArgumentIndex;
                    if (argumentIndex != -1)
                    {
                        constructorArguments[i] = arguments![argumentIndex];
                    }
                    else if (parameter.HasDefaultValue)
                    {
                        constructorArguments[i] = GetService(
                                serviceProvider,
                                parameter.ParameterType,
                                declaringType,
                                true) ?? parameter.DefaultValue;
                    }
                    else
                    {
                        constructorArguments[i] = GetService(
                                serviceProvider,
                                parameter.ParameterType,
                                declaringType,
                                false);
                    }
                }

                //return constructor.Invoke(BindingFlags.DoNotWrapExceptions, binder: null, constructorArguments, culture: null);
                //return constructor.Invoke(BindingFlags.DoNotWrapExceptions, binder: null, constructorArguments, culture: null);
                return MethodInvoker.GetInvoker(constructor).InvokeDirect_Obj(null, constructorArguments)!;
#endif
            };
        }

#pragma warning disable IDE0060 // Remove unused parameter
        private static ObjectFactory DoIt2(ConstructorInfo constructor, FactoryParameterContext[] parameters, Type declaringType)
#pragma warning restore IDE0060 // Remove unused parameter
        {
            return (IServiceProvider serviceProvider, object?[]? arguments) =>
            {
#if true

                unsafe
                {
                    ArgumentValuesFixed values = new(
                        parameters![0].ArgumentIndex != -1 ? parameters![0] : GetService(serviceProvider, parameters![0].ParameterType, declaringType, false),
                        parameters![1].ArgumentIndex != -1 ? parameters![1] : GetService(serviceProvider, parameters![1].ParameterType, declaringType, false),
                        parameters![2].ArgumentIndex != -1 ? parameters![2] : GetService(serviceProvider, parameters![2].ParameterType, declaringType, false));
                    InvokeContext context = new(ref values);
                    context.InvokeDirect(System.Reflection.MethodInvoker.GetInvoker(constructor));
                    return context.GetReturn()!;
                    //bool HasArgumentIndex(int index, out FactoryParameterContext context)
                    //{
                    //    context = parameters[0];
                    //    return context.ArgumentIndex != -1;
                    //}

                    //int length = parameters.Length;

                    //ArgumentValue* values = stackalloc ArgumentValue[length];
                    //using InvokeContext context = new InvokeContext(values, length);

                    //ArgumentValuesFixed values = new(length);
                    //InvokeContext context = new InvokeContext(ref values);
                    //InvokeContext context2 = new InvokeContext(ref values);
                    //for (int i = 0; i < length; i++)
                    //{
                    //    FactoryParameterContext parameter = parameters[i];

                    //    int argumentIndex = parameter.ArgumentIndex;
                    //    if (argumentIndex != -1)
                    //    {
                    //        context.SetArgument(i, arguments![argumentIndex]);
                    //        //context2.SetArgument(i, arguments![argumentIndex]);
                    //    }
                    //    else
                    //    {
                    //        context.SetArgument(i, GetService(
                    //                serviceProvider,
                    //                parameter.ParameterType,
                    //                declaringType,
                    //                false));
                    //        //context2.SetArgument(i, GetService(
                    //        //        serviceProvider,
                    //        //        parameter.ParameterType,
                    //        //        declaringType,
                    //        //        false));
                    //    }
                    //}

                    //context.InvokeDirect(MethodInvoker.GetInvoker(constructor));
                    //return context.GetReturn()!;
                }
#else
                object?[] constructorArguments = new object?[parameters.Length];
                for (int i = 0; i < parameters.Length; i++)
                {
                    FactoryParameterContext parameter = parameters[i];

                    int argumentIndex = parameter.ArgumentIndex;
                    if (argumentIndex != -1)
                    {
                        constructorArguments[i] = arguments![argumentIndex];
                    }
                    else
                    {
                        constructorArguments[i] = GetService(
                                serviceProvider,
                                parameter.ParameterType,
                                declaringType,
                                false);
                    }
                }

                return MethodInvoker.GetInvoker(constructor).InvokeDirect(obj: null, constructorArguments)!;
                //return constructor.Invoke(BindingFlags.DoNotWrapExceptions, binder: null, constructorArguments, culture: null);
#endif
            };
        }

        private static ObjectFactory DoIt3(ConstructorInfo constructor, FactoryParameterContext[] parameters)
        {
            return (IServiceProvider serviceProvider, object?[]? arguments) =>
            {
                object?[] constructorArguments = new object?[parameters.Length];
                for (int i = 0; i < parameters.Length; i++)
                {
                    constructorArguments[i] = arguments![parameters[i].ArgumentIndex];
                }

#if NETCOREAPP8_0_OR_GREATER
    sdsdf
    return MethodInvoker.GetInvoker(constructor).InvokeDirect(obj: null, constructorArguments)!;
#else
                constructor.Invoke(BindingFlags.DoNotWrapExceptions, binder: null, constructorArguments, culture: null);
                return constructor.Invoke(BindingFlags.DoNotWrapExceptions, binder: null, constructorArguments, culture: null);
#endif
            };
        }

        private readonly struct FactoryParameterContext
        {
            public FactoryParameterContext(Type parameterType, bool hasDefaultValue, object? defaultValue, int argumentIndex)
            {
                ParameterType = parameterType;
                HasDefaultValue = hasDefaultValue;
                DefaultValue = defaultValue;
                ArgumentIndex = argumentIndex;
            }

            public Type ParameterType { get; }
            public bool HasDefaultValue { get; }
            public object? DefaultValue { get; }
            public int ArgumentIndex { get; }
        }
#endif

        private static void FindApplicableConstructor(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type instanceType,
            Type?[] argumentTypes,
            out ConstructorInfo matchingConstructor,
            out int?[] matchingParameterMap)
        {
            ConstructorInfo? constructorInfo = null;
            int?[]? parameterMap = null;

            if (!TryFindPreferredConstructor(instanceType, argumentTypes, ref constructorInfo, ref parameterMap) &&
                !TryFindMatchingConstructor(instanceType, argumentTypes, ref constructorInfo, ref parameterMap))
            {
                throw new InvalidOperationException(SR.Format(SR.CtorNotLocated, instanceType));
            }

            matchingConstructor = constructorInfo;
            matchingParameterMap = parameterMap;
        }

        // Tries to find constructor based on provided argument types
        private static bool TryFindMatchingConstructor(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type instanceType,
            Type?[] argumentTypes,
            [NotNullWhen(true)] ref ConstructorInfo? matchingConstructor,
            [NotNullWhen(true)] ref int?[]? parameterMap)
        {
            foreach (ConstructorInfo? constructor in instanceType.GetConstructors())
            {
                if (TryCreateParameterMap(constructor.GetParameters(), argumentTypes, out int?[] tempParameterMap))
                {
                    if (matchingConstructor != null)
                    {
                        throw new InvalidOperationException(SR.Format(SR.MultipleCtorsFound, instanceType));
                    }

                    matchingConstructor = constructor;
                    parameterMap = tempParameterMap;
                }
            }

            if (matchingConstructor != null)
            {
                Debug.Assert(parameterMap != null);
                return true;
            }

            return false;
        }

        // Tries to find constructor marked with ActivatorUtilitiesConstructorAttribute
        private static bool TryFindPreferredConstructor(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type instanceType,
            Type?[] argumentTypes,
            [NotNullWhen(true)] ref ConstructorInfo? matchingConstructor,
            [NotNullWhen(true)] ref int?[]? parameterMap)
        {
            bool seenPreferred = false;
            foreach (ConstructorInfo? constructor in instanceType.GetConstructors())
            {
                if (constructor.IsDefined(typeof(ActivatorUtilitiesConstructorAttribute), false))
                {
                    if (seenPreferred)
                    {
                        ThrowMultipleCtorsMarkedWithAttributeException();
                    }

                    if (!TryCreateParameterMap(constructor.GetParameters(), argumentTypes, out int?[] tempParameterMap))
                    {
                        ThrowMarkedCtorDoesNotTakeAllProvidedArguments();
                    }

                    matchingConstructor = constructor;
                    parameterMap = tempParameterMap;
                    seenPreferred = true;
                }
            }

            if (matchingConstructor != null)
            {
                Debug.Assert(parameterMap != null);
                return true;
            }

            return false;
        }

        // Creates an injective parameterMap from givenParameterTypes to assignable constructorParameters.
        // Returns true if each given parameter type is assignable to a unique; otherwise, false.
        private static bool TryCreateParameterMap(ParameterInfo[] constructorParameters, Type?[] argumentTypes, out int?[] parameterMap)
        {
            parameterMap = new int?[constructorParameters.Length];

            for (int i = 0; i < argumentTypes.Length; i++)
            {
                bool foundMatch = false;
                Type? givenParameter = argumentTypes[i];

                for (int j = 0; j < constructorParameters.Length; j++)
                {
                    if (parameterMap[j] != null)
                    {
                        // This ctor parameter has already been matched
                        continue;
                    }

                    if (constructorParameters[j].ParameterType.IsAssignableFrom(givenParameter))
                    {
                        foundMatch = true;
                        parameterMap[j] = i;
                        break;
                    }
                }

                if (!foundMatch)
                {
                    return false;
                }
            }

            return true;
        }

        private struct ConstructorMatcher
        {
            private readonly ConstructorInfo _constructor;
            private readonly ParameterInfo[] _parameters;
            private readonly object?[] _parameterValues;

            public ConstructorMatcher(ConstructorInfo constructor)
            {
                _constructor = constructor;
                _parameters = _constructor.GetParameters();
                _parameterValues = new object?[_parameters.Length];
            }

            public int Match(object[] givenParameters, IServiceProviderIsService serviceProviderIsService)
            {
                for (int givenIndex = 0; givenIndex < givenParameters.Length; givenIndex++)
                {
                    Type? givenType = givenParameters[givenIndex]?.GetType();
                    bool givenMatched = false;

                    for (int applyIndex = 0; applyIndex < _parameters.Length; applyIndex++)
                    {
                        if (_parameterValues[applyIndex] == null &&
                            _parameters[applyIndex].ParameterType.IsAssignableFrom(givenType))
                        {
                            givenMatched = true;
                            _parameterValues[applyIndex] = givenParameters[givenIndex];
                            break;
                        }
                    }

                    if (!givenMatched)
                    {
                        return -1;
                    }
                }

                // confirms the rest of ctor arguments match either as a parameter with a default value or as a service registered
                for (int i = 0; i < _parameters.Length; i++)
                {
                    if (_parameterValues[i] == null &&
                        !serviceProviderIsService.IsService(_parameters[i].ParameterType))
                    {
                        if (ParameterDefaultValue.TryGetDefaultValue(_parameters[i], out object? defaultValue))
                        {
                            _parameterValues[i] = defaultValue;
                        }
                        else
                        {
                            return -1;
                        }
                    }
                }

                return _parameters.Length;
            }

            public object CreateInstance(IServiceProvider provider)
            {
                for (int index = 0; index != _parameters.Length; index++)
                {
                    if (_parameterValues[index] == null)
                    {
                        object? value = provider.GetService(_parameters[index].ParameterType);
                        if (value == null)
                        {
                            if (!ParameterDefaultValue.TryGetDefaultValue(_parameters[index], out object? defaultValue))
                            {
                                throw new InvalidOperationException(SR.Format(SR.UnableToResolveService, _parameters[index].ParameterType, _constructor.DeclaringType));
                            }
                            else
                            {
                                _parameterValues[index] = defaultValue;
                            }
                        }
                        else
                        {
                            _parameterValues[index] = value;
                        }
                    }
                }

#if NETFRAMEWORK || NETSTANDARD2_0
                try
                {
                    return _constructor.Invoke(_parameterValues);
                }
                catch (TargetInvocationException ex) when (ex.InnerException != null)
                {
                    ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                    // The above line will always throw, but the compiler requires we throw explicitly.
                    throw;
                }
#else
                return _constructor.Invoke(BindingFlags.DoNotWrapExceptions, binder: null, parameters: _parameterValues, culture: null);
#endif
            }

            public void MapParameters(int?[] parameterMap, object[] givenParameters)
            {
                for (int i = 0; i < _parameters.Length; i++)
                {
                    if (parameterMap[i] != null)
                    {
                        _parameterValues[i] = givenParameters[(int)parameterMap[i]!];
                    }
                }
            }
        }

        private static void ThrowMultipleCtorsMarkedWithAttributeException()
        {
            throw new InvalidOperationException(SR.Format(SR.MultipleCtorsMarkedWithAttribute, nameof(ActivatorUtilitiesConstructorAttribute)));
        }

        private static void ThrowMarkedCtorDoesNotTakeAllProvidedArguments()
        {
            throw new InvalidOperationException(SR.Format(SR.MarkedCtorMissingArgumentTypes, nameof(ActivatorUtilitiesConstructorAttribute)));
        }
    }
}
