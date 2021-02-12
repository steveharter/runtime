// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if BUILDING_INBOX_LIBRARY

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Dynamic;

using static System.Text.Json.Serialization.CachedReflectionInfo;

namespace System.Text.Json.Serialization
{
    internal sealed class MetaDynamic : DynamicMetaObject
    {
        private static readonly ConstantExpression NullExpression = Expression.Constant(null);
        private static readonly DefaultExpression EmptyExpression = Expression.Empty();
        private static readonly ConstantExpression Int1Expression = Expression.Constant((object)1);

        private JsonNode Dynamic { get; }
        internal MetaDynamic(Expression expression, JsonNode dynamicObject)
            : base(expression, BindingRestrictions.Empty, dynamicObject)
        {
            Dynamic = dynamicObject;
        }

        public override IEnumerable<string> GetDynamicMemberNames() => Array.Empty<string>();

        public override DynamicMetaObject BindGetIndex(GetIndexBinder binder, DynamicMetaObject[] indexes)
        {
            MethodInfo? methodInfo = Dynamic.TryGetIndexMethodInfo;
            if (methodInfo == null)
            {
                return base.BindGetIndex(binder, indexes);
            }

            DynamicMetaObject[] localIndexes = indexes;

            return CallMethodWithResult(
                methodInfo,
                binder,
                GetExpressions(indexes),
                (MetaDynamic @this, GetIndexBinder b, DynamicMetaObject? e) => b.FallbackGetIndex(@this, localIndexes, e)
            );
        }

        public override DynamicMetaObject BindSetIndex(SetIndexBinder binder, DynamicMetaObject[] indexes, DynamicMetaObject value)
        {
            MethodInfo? methodInfo = Dynamic.TrySetIndexMethodInfo;
            if (methodInfo == null)
            {
                return base.BindSetIndex(binder, indexes, value);
            }

            DynamicMetaObject[] localIndexes = indexes;
            DynamicMetaObject localValue = value;

            return CallMethodReturnLast(
                methodInfo,
                binder,
                GetExpressions(indexes),
                value.Expression,
                (MetaDynamic @this, SetIndexBinder b, DynamicMetaObject? e) => b.FallbackSetIndex(@this, localIndexes, localValue, e)
            );
        }

        public override DynamicMetaObject BindGetMember(GetMemberBinder binder)
        {
            MethodInfo? methodInfo = Dynamic.TryGetMemberMethodInfo;
            if (methodInfo == null)
            {
                return base.BindGetMember(binder);
            }

            return CallMethodWithResult(
                methodInfo,
                binder,
                s_noArgs,
                (MetaDynamic @this, GetMemberBinder b, DynamicMetaObject? e) => b.FallbackGetMember(@this, e)
            );
        }

        public override DynamicMetaObject BindSetMember(SetMemberBinder binder, DynamicMetaObject value)
        {
            MethodInfo? methodInfo = Dynamic.TrySetMemberMethodInfo;
            if (methodInfo == null)
            {
                return base.BindSetMember(binder, value);
            }

            DynamicMetaObject localValue = value;

            return CallMethodReturnLast(
                methodInfo,
                binder,
                s_noArgs,
                value.Expression,
                (MetaDynamic @this, SetMemberBinder b, DynamicMetaObject? e) => b.FallbackSetMember(@this, localValue, e)
            );
        }

        public override DynamicMetaObject BindConvert(ConvertBinder binder)
        {
            MethodInfo? methodInfo = Dynamic.TryConvertMethodInfo;
            if (methodInfo == null)
            {
                return base.BindConvert(binder);
            }

            return CallMethodWithResult(
                methodInfo,
                binder,
                s_noArgs,
                (MetaDynamic @this, ConvertBinder b, DynamicMetaObject? e) => b.FallbackConvert(@this, e)
            );
        }

        private delegate DynamicMetaObject Fallback<TBinder>(MetaDynamic @this, TBinder binder, DynamicMetaObject? errorSuggestion);

#pragma warning disable CA1825 // used in reference comparison, requires unique object identity
        private static readonly Expression[] s_noArgs = new Expression[0];
#pragma warning restore CA1825

        private static ReadOnlyCollection<Expression> GetConvertedArgs(params Expression[] args)
        {
            var paramArgs = new Expression[args.Length];

            for (int i = 0; i < args.Length; i++)
            {
                paramArgs[i] = Expression.Convert(args[i], typeof(object));
            }

            return new ReadOnlyCollection<Expression>(paramArgs);
        }

        /// <summary>
        /// Helper method for generating expressions that assign byRef call
        /// parameters back to their original variables.
        /// </summary>
        private static Expression ReferenceArgAssign(Expression callArgs, Expression[] args)
        {
            ReadOnlyCollectionBuilder<Expression>? block = null;

            for (int i = 0; i < args.Length; i++)
            {
                ParameterExpression variable = (ParameterExpression)args[i];

                if (variable.IsByRef)
                {
                    if (block == null)
                        block = new ReadOnlyCollectionBuilder<Expression>();

                    block.Add(
                        Expression.Assign(
                            variable,
                            Expression.Convert(
                                Expression.ArrayIndex(
                                    callArgs,
                                    Int1Expression
                                ),
                                variable.Type
                            )
                        )
                    );
                }
            }

            if (block != null)
                return Expression.Block(block);
            else
                return EmptyExpression;
        }

        /// <summary>
        /// Helper method for generating arguments for calling methods
        /// on DynamicObject.  parameters is either a list of ParameterExpressions
        /// to be passed to the method as an object[], or NoArgs to signify that
        /// the target method takes no object[] parameter.
        /// </summary>
        private static Expression[] BuildCallArgs<TBinder>(TBinder binder, Expression[] parameters, Expression arg0, Expression? arg1)
            where TBinder : DynamicMetaObjectBinder
        {
            if (!ReferenceEquals(parameters, s_noArgs))
                return arg1 != null ? new Expression[] { Constant(binder), arg0, arg1 } : new Expression[] { Constant(binder), arg0 };
            else
                return arg1 != null ? new Expression[] { Constant(binder), arg1 } : new Expression[] { Constant(binder) };
        }

        private static ConstantExpression Constant<TBinder>(TBinder binder)
        {
            return Expression.Constant(binder, typeof(TBinder));
        }

        /// <summary>
        /// Helper method for generating a MetaObject which calls a
        /// specific method on Dynamic that returns a result
        /// </summary>
        private DynamicMetaObject CallMethodWithResult<TBinder>(MethodInfo method, TBinder binder, Expression[] args, Fallback<TBinder> fallback)
            where TBinder : DynamicMetaObjectBinder
        {
            return CallMethodWithResult(method, binder, args, fallback, null);
        }

        /// <summary>
        /// Helper method for generating a MetaObject which calls a
        /// specific method on Dynamic that returns a result
        /// </summary>
        private DynamicMetaObject CallMethodWithResult<TBinder>(MethodInfo method, TBinder binder, Expression[] args, Fallback<TBinder> fallback, Fallback<TBinder>? fallbackInvoke)
            where TBinder : DynamicMetaObjectBinder
        {
            //
            // First, call fallback to do default binding
            // This produces either an error or a call to a .NET member
            //
            DynamicMetaObject fallbackResult = fallback(this, binder, null);

            DynamicMetaObject callDynamic = BuildCallMethodWithResult(method, binder, args, fallbackResult, fallbackInvoke);

            //
            // Now, call fallback again using our new MO as the error
            // When we do this, one of two things can happen:
            //   1. Binding will succeed, and it will ignore our call to
            //      the dynamic method, OR
            //   2. Binding will fail, and it will use the MO we created
            //      above.
            //
            return fallback(this, binder, callDynamic);
        }

        private DynamicMetaObject BuildCallMethodWithResult<TBinder>(MethodInfo method, TBinder binder, Expression[] args, DynamicMetaObject fallbackResult, Fallback<TBinder>? fallbackInvoke)
            where TBinder : DynamicMetaObjectBinder
        {
            ParameterExpression result = Expression.Parameter(typeof(object), null);
            ParameterExpression callArgs = Expression.Parameter(typeof(object[]), null);
            ReadOnlyCollection<Expression> callArgsValue = GetConvertedArgs(args);

            var resultMO = new DynamicMetaObject(result, BindingRestrictions.Empty);

            // Need to add a conversion if calling TryConvert
            if (binder.ReturnType != typeof(object))
            {
                Debug.Assert(binder is ConvertBinder && fallbackInvoke == null);

                UnaryExpression convert = Expression.Convert(resultMO.Expression, binder.ReturnType);
                // will always be a cast or unbox
                Debug.Assert(convert.Method == null);

                // Prepare a good exception message in case the convert will fail
                string convertFailed = "todo";
                //    System.Linq.Expressions.Strings.DynamicObjectResultNotAssignable(
                //    "{0}",
                //    this.Value.GetType(),
                //    binder.GetType(),
                //    binder.ReturnType
                //);

                Expression condition;
                // If the return type can not be assigned null then just check for type assignability otherwise allow null.
                if (binder.ReturnType.IsValueType && Nullable.GetUnderlyingType(binder.ReturnType) == null)
                {
                    condition = Expression.TypeIs(resultMO.Expression, binder.ReturnType);
                }
                else
                {
                    condition = Expression.OrElse(
                                    Expression.Equal(resultMO.Expression, NullExpression),
                                    Expression.TypeIs(resultMO.Expression, binder.ReturnType));
                }

                Expression checkedConvert = Expression.Condition(
                    condition,
                    convert,
                    Expression.Throw(
                        Expression.New(
                            InvalidCastException_Ctor_String,
                            new TrueReadOnlyCollection<Expression>(
                                Expression.Call(
                                    String_Format_String_ObjectArray,
                                    Expression.Constant(convertFailed),
                                    Expression.NewArrayInit(
                                        typeof(object),
                                        new TrueReadOnlyCollection<Expression>(
                                            Expression.Condition(
                                                Expression.Equal(resultMO.Expression, NullExpression),
                                                Expression.Constant("null"),
                                                Expression.Call(
                                                    resultMO.Expression,
                                                    Object_GetType
                                                ),
                                                typeof(object)
                                            )
                                        )
                                    )
                                )
                            )
                        ),
                        binder.ReturnType
                    ),
                    binder.ReturnType
                );

                resultMO = new DynamicMetaObject(checkedConvert, resultMO.Restrictions);
            }

            if (fallbackInvoke != null)
            {
                resultMO = fallbackInvoke(this, binder, resultMO);
            }

            var callDynamic = new DynamicMetaObject(
                Expression.Block(
                    new TrueReadOnlyCollection<ParameterExpression>(result, callArgs),
                    new TrueReadOnlyCollection<Expression>(
                        Expression.Assign(callArgs, Expression.NewArrayInit(typeof(object), callArgsValue)),
                        Expression.Condition(
                            Expression.Call(
                                GetLimitedSelf(),
                                method,
                                BuildCallArgs(
                                    binder,
                                    args,
                                    callArgs,
                                    result
                                )
                            ),
                            Expression.Block(
                                ReferenceArgAssign(callArgs, args),
                                resultMO.Expression
                            ),
                            fallbackResult.Expression,
                            binder.ReturnType
                        )
                    )
                ),
                GetRestrictions().Merge(resultMO.Restrictions).Merge(fallbackResult.Restrictions)
            );
            return callDynamic;
        }

        private DynamicMetaObject CallMethodReturnLast<TBinder>(MethodInfo method, TBinder binder, Expression[] args, Expression value, Fallback<TBinder> fallback)
            where TBinder : DynamicMetaObjectBinder
        {
            //
            // First, call fallback to do default binding
            // This produces either an error or a call to a .NET member
            //
            DynamicMetaObject fallbackResult = fallback(this, binder, null);

            //
            // Build a new expression like:
            // {
            //   object result;
            //   TrySetMember(payload, result = value) ? result : fallbackResult
            // }
            //

            ParameterExpression result = Expression.Parameter(typeof(object), null);
            ParameterExpression callArgs = Expression.Parameter(typeof(object[]), null);
            ReadOnlyCollection<Expression> callArgsValue = GetConvertedArgs(args);

            var callDynamic = new DynamicMetaObject(
                Expression.Block(
                    new TrueReadOnlyCollection<ParameterExpression>(result, callArgs),
                    new TrueReadOnlyCollection<Expression>(
                        Expression.Assign(callArgs, Expression.NewArrayInit(typeof(object), callArgsValue)),
                        Expression.Condition(
                            Expression.Call(
                                GetLimitedSelf(),
                                method,
                                BuildCallArgs(
                                    binder,
                                    args,
                                    callArgs,
                                    Expression.Assign(result, Expression.Convert(value, typeof(object)))
                                )
                            ),
                            Expression.Block(
                                ReferenceArgAssign(callArgs, args),
                                result
                            ),
                            fallbackResult.Expression,
                            typeof(object)
                        )
                    )
                ),
                GetRestrictions().Merge(fallbackResult.Restrictions)
            );

            //
            // Now, call fallback again using our new MO as the error
            // When we do this, one of two things can happen:
            //   1. Binding will succeed, and it will ignore our call to
            //      the dynamic method, OR
            //   2. Binding will fail, and it will use the MO we created
            //      above.
            //
            return fallback(this, binder, callDynamic);
        }

        /// <summary>
        /// Returns a Restrictions object which includes our current restrictions merged
        /// with a restriction limiting our type
        /// </summary>
        private BindingRestrictions GetRestrictions()
        {
            Debug.Assert(Restrictions == BindingRestrictions.Empty, "We don't merge, restrictions are always empty");

            return GetTypeRestriction(this);
        }

        /// <summary>
        /// Returns our Expression converted to DynamicObject
        /// </summary>
        private Expression GetLimitedSelf()
        {
            // Convert to DynamicObject rather than LimitType, because
            // the limit type might be non-public.
            if (AreEquivalent(Expression.Type, Value.GetType()))
            {
                return Expression;
            }
            return Expression.Convert(Expression, Value.GetType());
        }

        private static bool AreEquivalent(Type? t1, Type? t2) => t1 != null && t1.IsEquivalentTo(t2);

        private new object Value => base.Value!;

        // It is okay to throw NotSupported from this binder. This object
        // is only used by DynamicObject.GetMember--it is not expected to
        // (and cannot) implement binding semantics. It is just so the DO
        // can use the Name and IgnoreCase properties.
        private sealed class GetBinderAdapter : GetMemberBinder
        {
            internal GetBinderAdapter(InvokeMemberBinder binder)
                : base(binder.Name, binder.IgnoreCase)
            {
            }

            public override DynamicMetaObject FallbackGetMember(DynamicMetaObject target, DynamicMetaObject? errorSuggestion)
            {
                throw new NotSupportedException();
            }
        }

        private sealed class TrueReadOnlyCollection<T> : ReadOnlyCollection<T>
        {
            /// <summary>
            /// Creates instance of TrueReadOnlyCollection, wrapping passed in array.
            /// !!! DOES NOT COPY THE ARRAY !!!
            /// </summary>
            public TrueReadOnlyCollection(params T[] list)
                : base(list)
            {
            }
        }

        internal static BindingRestrictions GetTypeRestriction(DynamicMetaObject obj)
        {
            Debug.Assert(obj != null);
            if (obj.Value == null && obj.HasValue)
            {
                return BindingRestrictions.GetInstanceRestriction(obj.Expression, null);
            }
            else
            {
                return BindingRestrictions.GetTypeRestriction(obj.Expression, obj.LimitType);
            }
        }

        /// <summary>
        /// Returns the list of expressions represented by the <see cref="DynamicMetaObject"/> instances.
        /// </summary>
        /// <param name="objects">An array of <see cref="DynamicMetaObject"/> instances to extract expressions from.</param>
        /// <returns>The array of expressions.</returns>
        internal static Expression[] GetExpressions(DynamicMetaObject[] objects)
        {
            Debug.Assert(objects != null);

            Expression[] res = new Expression[objects.Length];
            for (int i = 0; i < objects.Length; i++)
            {
                DynamicMetaObject mo = objects[i];
                Debug.Assert(mo != null);
                Expression expr = mo.Expression;
                Debug.Assert(expr != null, "Unexpected null expression; ctor should have caught this.");
                res[i] = expr;
            }

            return res;
        }
    }
}
#endif
