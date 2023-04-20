// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Helper code for the various activator services.
    /// </summary>
    public static partial class ActivatorUtilities
    {
#if NETCOREAPP_8_0_OR_GREATER
        private static ObjectFactory ReflectionFactory_Canonical(ConstructorInfo constructor, FactoryParameterContext[] parameters, Type declaringType)
        {
            return (IServiceProvider serviceProvider, object?[]? arguments) =>
            {
                object?[] constructorArguments = new object?[parameters.Length];
                for (int i = 0; i < parameters.Length; i++)
                {
                    ref FactoryParameterContext parameter = ref parameters[i];
                    constructorArguments[i] = ((parameter.ArgumentIndex != -1)
                        // Throws a NullReferenceException if arguments is null. Consistent with expression-based factory.
                        ? arguments![parameter.ArgumentIndex]
                        : GetService(
                            serviceProvider,
                            parameter.ParameterType,
                            declaringType,
                            parameter.HasDefaultValue)) ?? parameter.DefaultValue;
                }

                return constructor.Invoke(BindingFlags.DoNotWrapExceptions, binder: null, constructorArguments, culture: null);
            };
        }

        private static ObjectFactory ReflectionFactory_NoDefaultValues(ConstructorInfo constructor, FactoryParameterContext_Type_Index[] parameters, Type declaringType)
        {
            return (IServiceProvider serviceProvider, object?[]? arguments) =>
            {
                //    object?[] constructorArguments = new object?[parameters.Length];
                //    for (int i = 0; i < parameters.Length; i++)
                //    {
                //        ref FactoryParameterContext parameter = ref parameters[i];
                //        constructorArguments[i] = (parameter.ArgumentIndex != -1)
                //            // Throws a NullReferenceException if arguments is null. Consistent with expression-based factory.
                //            ? arguments![parameter.ArgumentIndex]
                //            : GetService(
                //                serviceProvider,
                //                parameter.ParameterType,
                //                declaringType,
                //                false);
                //    }

                //    return constructor.Invoke(BindingFlags.DoNotWrapExceptions, binder: null, constructorArguments, culture: null);
                //};

                unsafe
                {
                    //ArgumentValuesFixed values = new(
                    //    parameters![0].ArgumentIndex != -1 ? parameters![parameters![0].ArgumentIndex] : GetService(serviceProvider, parameters![0].ParameterType, declaringType, false),
                    //    parameters![1].ArgumentIndex != -1 ? parameters![parameters![1].ArgumentIndex] : GetService(serviceProvider, parameters![1].ParameterType, declaringType, false),
                    //    parameters![2].ArgumentIndex != -1 ? parameters![parameters![2].ArgumentIndex] : GetService(serviceProvider, parameters![2].ParameterType, declaringType, false));
                    //InvokeContext context = new(ref values);
                    //context.InvokeDirect(System.Reflection.MethodInvoker.GetInvoker(constructor));
                    //return context.GetReturn()!;

                    int length = parameters.Length;

                    //ArgumentValue* values = stackalloc ArgumentValue[length];
                    ArgumentValuesFixed values = new(3);
                    //using InvokeContext context = new InvokeContext(values, length);
                    using InvokeContext context = new InvokeContext(ref values);
                    //ArgumentValuesFixed values = new(length);
                    //InvokeContext context = new InvokeContext(ref values);
                    for (int i = 0; i < length; i++)
                    {
                        ref FactoryParameterContext_Type_Index parameter = ref parameters[i];

                        int argumentIndex = parameter.ArgumentIndex;
                        if (argumentIndex != -1)
                        {
                            context.SetArgument(i, arguments![argumentIndex]);
                        }
                        else
                        {
                            context.SetArgument(i, GetService(
                                    serviceProvider,
                                    parameter.ParameterType,
                                    declaringType,
                                    false));
                        }
                     }

                    context.InvokeDirect(MethodInvoker.GetInvoker(constructor));
                    return context.GetReturn()!;
                }
            };
        }

        private static ObjectFactory ReflectionFactory_NoDefaultValues_3(Func<object?, object?, object?, object?, object?> constructor, FactoryParameterContext_Type_Index[] parameters, Type declaringType)
        {
            return (IServiceProvider serviceProvider, object?[]? arguments) => constructor(null,
                parameters[0].ArgumentIndex != -1 ? arguments![parameters[0].ArgumentIndex] : GetService(serviceProvider, parameters[0].ParameterType, declaringType, false),
                parameters[1].ArgumentIndex != -1 ? arguments![parameters[1].ArgumentIndex] : GetService(serviceProvider, parameters[1].ParameterType, declaringType, false),
                parameters[2].ArgumentIndex != -1 ? arguments![parameters[2].ArgumentIndex] : GetService(serviceProvider, parameters[2].ParameterType, declaringType, false))!;
        }

        private static ObjectFactory ReflectionFactory_NoDefaultValues_AllMatches(ConstructorInfo constructor, int[] parameters)
        {
            return (IServiceProvider serviceProvider, object?[]? arguments) =>
            {
                unsafe
                {
                    ArgumentValue* values = stackalloc ArgumentValue[parameters.Length];
                    using InvokeContext context = new InvokeContext(values, parameters.Length);

                    for (int i = 0; i < parameters.Length; i++)
                    {
                        context.SetArgument(i, arguments![parameters[i]]);
                    }

                    context.InvokeDirect(MethodInvoker.GetInvoker(constructor));
                    return context.GetReturn()!;
                }
            };
        }

        private static ObjectFactory ReflectionFactory_NoDefaultValues_AllMatches_3(Func<object?, object?, object?, object?, object?> constructor, int[] parameters)
        {
            return (IServiceProvider serviceProvider, object?[]? arguments) =>
            {
                return constructor(null, arguments![parameters[0]], arguments![parameters[1]], arguments![parameters[2]])!;
            };
        }

        private static ObjectFactory ReflectionFactory_NoDefaultValues_NoMatches(ConstructorInfo constructor, ParameterInfo[] parameters, Type declaringType)
        {
            return (IServiceProvider serviceProvider, object?[]? arguments) =>
            {
                unsafe
                {
                    ArgumentValue* values = stackalloc ArgumentValue[parameters.Length];
                    using InvokeContext context = new InvokeContext(values, parameters.Length);
                    {
                        //object?[] constructorArguments = new object?[parameters.Length];
                        for (int i = 0; i < parameters.Length; i++)
                        {
                            context.SetArgument(i, GetService(
                                serviceProvider,
                                parameters[i].ParameterType,
                                declaringType,
                                false));
                        }
                    }

                    context.InvokeDirect(MethodInvoker.GetInvoker(constructor));
                    return context.GetReturn()!;
                    //return constructor.Invoke(BindingFlags.DoNotWrapExceptions, binder: null, constructorArguments, culture: null);
                }
            };
        }

        private static ObjectFactory ReflectionFactory_NoDefaultValues_NoMatches3(Func<object?, object?, object?, object?, object?> constructor, ParameterInfo[] parameters, Type declaringType)
        {
            return (IServiceProvider serviceProvider, object?[]? arguments) =>
            {
                return constructor(null,
                    GetService(serviceProvider, parameters![0].ParameterType, declaringType, false),
                    GetService(serviceProvider, parameters![1].ParameterType, declaringType, false),
                    GetService(serviceProvider, parameters![2].ParameterType, declaringType, false))!;

            };
        }
#elif NETSTANDARD2_1_OR_GREATER || NETCOREAPP
        private static ObjectFactory ReflectionFactory_Canonical(ConstructorInfo constructor, FactoryParameterContext[] parameters, Type declaringType)
        {
            return (IServiceProvider serviceProvider, object?[]? arguments) =>
            {
                object?[] constructorArguments = new object?[parameters.Length];
                for (int i = 0; i < parameters.Length; i++)
                {
                    ref FactoryParameterContext parameter = ref parameters[i];
                    constructorArguments[i] = ((parameter.ArgumentIndex != -1)
                        // Throws a NullReferenceException if arguments is null. Consistent with expression-based factory.
                        ? arguments![parameter.ArgumentIndex]
                        : GetService(
                            serviceProvider,
                            parameter.ParameterType,
                            declaringType,
                            parameter.HasDefaultValue)) ?? parameter.DefaultValue;
                }

                return constructor.Invoke(BindingFlags.DoNotWrapExceptions, binder: null, constructorArguments, culture: null);
            };
        }
#endif
    }
}
