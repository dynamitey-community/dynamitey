// 
//  Copyright 2010  Ekon Benefits
// 
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
// 
//        http://www.apache.org/licenses/LICENSE-2.0
// 
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using Dynamitey.Internal;
using Dynamitey.Internal.Optimization;
using Microsoft.CSharp.RuntimeBinder;
using System.Text.RegularExpressions;
using Dynamitey.Internal.Compat;

namespace Dynamitey
{
    using System;




    /// <summary>
    /// Main API
    /// </summary>
    /// <remarks>
    /// <para>
    /// A deliberate note on this class's use of <c>dynamic</c> under nullable reference types:
    /// <c>dynamic</c> is <c>object</c> with a compiler flag, so annotating a <c>dynamic</c>
    /// <em>parameter</em> as <c>dynamic?</c> would say almost nothing - every member access on it
    /// is unchecked by the compiler regardless, dynamic dispatch happens at runtime either way.
    /// Parameters that feed the DLR directly (<c>target</c>, <c>args</c>, operator operands) are
    /// therefore left as plain, non-nullable <c>object</c>/<c>dynamic</c> - matching this library's
    /// actual behavior, since a <see langword="null"/> target already fails inside
    /// <see cref="Internal.Optimization.Util.GetTargetContext"/> before any binder runs.
    /// </para>
    /// <para>
    /// <c>dynamic</c> <em>return values</em> are different: they are whatever the resolved member
    /// itself produced, and a property or method can legitimately return <see langword="null"/>.
    /// Marking those <c>dynamic?</c> is not empty noise - it gives a real, actionable warning the
    /// first time a caller uses the result in a non-dynamic context (e.g. passes it to a
    /// non-nullable <c>object</c> parameter) before ever reaching a member access on it. So the
    /// methods below that hand back "whatever the target returned" (<see cref="InvokeMember"/>,
    /// <see cref="Invoke"/>, <see cref="InvokeGet"/>, <see cref="InvokeConvert"/>, etc.) are
    /// annotated <c>dynamic?</c>, while ones whose result is always a Dynamitey-constructed proxy
    /// (<see cref="Curry(object,int?)"/>, <see cref="InvokeSetAll"/>) stay non-nullable <c>dynamic</c>.
    /// </para>
    /// </remarks>
    public static class Dynamic
    {
        /// <summary>
        /// Clears the dynamic binding caches.
        /// </summary>
        public static void ClearCaches()
        {
            InvokeHelper.ClearAllCaches();
        }

    

        // Lazily constructed (not a field initializer): a LateType instantiates a DynamicObject, which
        // unconditionally requires the DLR. Dynamic has an explicit static constructor, so a field
        // initializer here would run - and pay that cost - the moment ANY member of Dynamic is first
        // touched, regardless of whether ComBinder itself is ever used. Behind a property instead,
        // [RequiresDynamicCode] lands only on callers who actually read ComBinder (e.g. GetMemberNames).
        private static dynamic? _comBinder;

        private static dynamic ComBinder
        {
            [RequiresUnreferencedCode("Constructs a LateType(string), which resolves the optional System.Dynamic.ComBinder type by name and reports absence via IsAvailable rather than throwing; the type may legitimately be missing (e.g. System.Dynamic not referenced).")]
            [RequiresDynamicCode("Constructing a LateType instantiates System.Dynamic.DynamicObject, whose default constructor requires the DLR's runtime code generation; not supported when AOT-compiled.")]
            get => _comBinder ?? (_comBinder = new DynamicObjects.LateType("System.Dynamic.ComBinder, System.Dynamic, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"));
        }

        // ReSharper disable once MemberCanBePrivate.Global
        // See ComBinder above for why this is a lazy property rather than a field initializer.
        private static dynamic? _impromptu;

        internal static dynamic Impromptu
        {
            [RequiresUnreferencedCode("Constructs a LateType(string), which resolves the optional ImpromptuInterface.Impromptu type by name and reports absence via IsAvailable rather than throwing; ImpromptuInterface may legitimately not be referenced.")]
            [RequiresDynamicCode("Constructing a LateType instantiates System.Dynamic.DynamicObject, whose default constructor requires the DLR's runtime code generation; not supported when AOT-compiled.")]
            get => _impromptu ?? (_impromptu = new DynamicObjects.LateType("ImpromptuInterface.Impromptu, ImpromptuInterface, PublicKeyToken=0b1781c923b2975b"));
        }

        // ReSharper disable once MemberCanBePrivate.Global
        // See ComBinder above for why this is a lazy property rather than a field initializer.
        private static dynamic? _typeDescriptor;

        internal static dynamic TypeDescriptor
        {
            [RequiresUnreferencedCode("Constructs a LateType(string), which resolves the optional System.ComponentModel.TypeDescriptor type by name and reports absence via IsAvailable rather than throwing; TypeDescriptor may legitimately not be available (e.g. on netstandard2.0).")]
            [RequiresDynamicCode("Constructing a LateType instantiates System.Dynamic.DynamicObject, whose default constructor requires the DLR's runtime code generation; not supported when AOT-compiled.")]
            get => _typeDescriptor ?? (_typeDescriptor = new DynamicObjects.LateType("System.ComponentModel.TypeDescriptor, System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"));
        }

        private static readonly Type? ComObjectType;
        // ReSharper disable once MemberCanBePrivate.Global
        internal static readonly Type? TypeConverterAttributeSL;

        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification =
            "The two Assembly.GetType/Type.GetType calls below resolve an optional type by name and " +
            "are wrapped in try/catch specifically because the type may legitimately be absent. A " +
            "static constructor has no caller to warn at and runs unconditionally regardless of " +
            "whether these optional features are ever used.")]
        static Dynamic()
        {
            try
            {
                ComObjectType = typeof(object).GetTypeInfo().Assembly.GetType("System.__ComObject");
            }
            catch
            {
                // Deliberately broad (cs/catch-of-all-exceptions): this is a name-based probe for an
                // optional type, and Assembly.GetType/Type.GetType can throw several different
                // exceptions (ArgumentException, FileNotFoundException, BadImageFormatException, ...)
                // for "can't resolve this", not just "not found" - any of them means "treat as absent".
                ComObjectType = null;
            }
            try
            {
                TypeConverterAttributeSL
                    = Type.GetType("System.ComponentModel.TypeConverter, System, Version=5.0.5.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e", false);
            }
            catch
            {
                // Same reasoning as the ComObjectType probe above.
                TypeConverterAttributeSL = null;
            }
        }
        
        /// <summary>
        /// Creates a cached call site at runtime.
        /// </summary>
        /// <param name="delegateType">Type of the delegate.</param>
        /// <param name="binder">The CallSite binder.</param>
        /// <param name="name">Member Name</param>
        /// <param name="context">Permissions Context type</param>
        /// <param name="argNames">The arg names.</param>
        /// <param name="staticContext">if set to <c>true</c> [static context].</param>
        /// <param name="isEvent">if set to <c>true</c> [is event].</param>
        /// <returns>The CallSite</returns>
        /// <remarks>
        /// Advanced usage only for serious custom dynamic invocation.
        /// </remarks>
        /// <seealso cref="CreateCallSite{T}"/>
        [RequiresUnreferencedCode("Builds a raw DLR CallSite from a caller-supplied binder; the binder resolves its target member by name at each call, and trimming can remove that member. Advanced/low-level API - prefer InvokeMember/InvokeGet/etc.")]
        [RequiresDynamicCode("Creating a CallSite - and, for delegate shapes with more than 14 parameters, emitting the delegate type itself via Reflection.Emit - requires the DLR's runtime code generation and is not supported when AOT-compiled.")]
        [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters",
            Justification = "Pre-existing public surface inherited from upstream 3.0.3. The rule guards against a "
                + "later version adding optional parameters to an overload set, which silently breaks binary "
                + "compatibility for already-compiled callers. Removing the optional parameters now would itself "
                + "be that break. Frozen instead: the PublicAPI declaration files record this shape, so any "
                + "future change to it has to be written down before it can build.")]
        public static CallSite CreateCallSite(Type delegateType, CallSiteBinder binder, String_OR_InvokeMemberName name,
                                              Type context, string?[]? argNames = null, bool staticContext = false,
                                              bool isEvent = false) =>
            binder is null ? throw new ArgumentNullException(nameof(binder)) :
            InvokeHelper.CreateCallSite(delegateType, binder.GetType(), InvokeHelper.Unknown,
                () => binder, (InvokeMemberName)name, context, argNames, staticContext, isEvent);

        /// <summary>
        /// Creates the call site.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="binder">The binder.</param>
        /// <param name="name">The name.</param>
        /// <param name="context">The context.</param>
        /// <param name="argNames">The arg names.</param>
        /// <param name="staticContext">if set to <c>true</c> [static context].</param>
        /// <param name="isEvent">if set to <c>true</c> [is event].</param>
        /// <returns></returns>
        /// /// 
        /// <example>
        /// Unit test that exhibits usage
        /// <code><![CDATA[
        /// string tResult = String.Empty;
        /// var tPoco = new MethOutPoco();
        /// var tBinder =
        /// Binder.InvokeMember(BinderFlags.None, "Func", null, GetType(),
        /// new[]
        /// {
        /// Info.Create(
        /// InfoFlags.None, null),
        /// Info.Create(
        /// InfoFlags.IsOut |
        /// InfoFlags.UseCompileTimeType, null)
        /// });
        /// var tSite = Impromptu.CreateCallSite<DynamicTryString>(tBinder);
        /// tSite.Target.Invoke(tSite, tPoco, out tResult);
        /// Assert.AreEqual("success", tResult);
        /// ]]></code>
        /// </example>
        /// <seealso cref="CreateCallSite"/>
        [RequiresUnreferencedCode("Builds a raw DLR CallSite from a caller-supplied binder; the binder resolves its target member by name at each call, and trimming can remove that member. Advanced/low-level API - prefer InvokeMember/InvokeGet/etc.")]
        [RequiresDynamicCode("Creating a CallSite<T> requires the DLR's runtime code generation to produce the binding rule; not supported when AOT-compiled.")]
        [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters",
            Justification = "Pre-existing public surface inherited from upstream 3.0.3. The rule guards against a "
                + "later version adding optional parameters to an overload set, which silently breaks binary "
                + "compatibility for already-compiled callers. Removing the optional parameters now would itself "
                + "be that break. Frozen instead: the PublicAPI declaration files record this shape, so any "
                + "future change to it has to be written down before it can build.")]
        public static CallSite<T> CreateCallSite<T>(CallSiteBinder binder, String_OR_InvokeMemberName name, Type context,
                                                    string?[]? argNames = null, bool staticContext = false,
                                                    bool isEvent = false) where T : class
            => binder is null ? throw new ArgumentNullException(nameof(binder)) :
               InvokeHelper.CreateCallSite<T>(binder.GetType(), InvokeHelper.Unknown,
                () => binder, (InvokeMemberName) name, context, argNames, staticContext, isEvent);


        /// <summary>
        /// Puts a dynamic linq proxy around the specified enumerable.
        /// </summary>
        /// <param name="enumerable">The enumerable.</param>
        /// <returns></returns>
        [RequiresUnreferencedCode("Wraps enumerable in a dynamic LINQ proxy that resolves Enumerable/Queryable extension methods by name and generic signature via reflection at each call; trimming can remove the method being resolved.")]
        [RequiresDynamicCode("Each LINQ-style call through the returned proxy binds through the DLR and, for multi-generic-argument methods, may build the closed generic method at runtime; not supported when AOT-compiled.")]
        public static dynamic Linq(object enumerable)
        {
            Guard.NotNull(enumerable);

            if (enumerable
                .GetType()
                .GetTypeInfo()
                .GetInterfaces()
                .Where(it => it.GetTypeInfo().IsGenericType)
                .Any(it => it.GetGenericTypeDefinition() == typeof(IEnumerable<>)))
            {
                return new DynamicObjects.LinqInstanceProxy(enumerable);
            }

            if (enumerable is IEnumerable tempEnumerable)
            {
                enumerable = tempEnumerable.Cast<object>();
            }

            return new DynamicObjects.LinqInstanceProxy(enumerable);
        }

    

        /// <summary>
        /// Dynamically Invokes a member method using the DLR
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="name">The name. Can be a string it will be implicitly converted</param>
        /// <param name="args">The args.</param>
        /// <returns> The result</returns>
        /// <example>   
        /// Unit test that exhibits usage:
        /// <code>
        /// <![CDATA[
        ///    dynamic tExpando = new ExpandoObject();
        ///    tExpando.Func = new Func<int, string>(it => it.ToString());
        ///
        ///    var tValue = 1;
        ///    var tOut = Impromptu.InvokeMember(tExpando, "Func", tValue);
        ///
        ///    Assert.AreEqual(tValue.ToString(), tOut);
        /// ]]>
        /// </code>
        /// </example>
        /// <remarks>
        /// If the invoked member's result is a <see cref="Task{TResult}"/> whose <c>TResult</c> is not
        /// visible to the calling assembly (see <see cref="Type.IsVisible"/>), the result is wrapped in an
        /// <see cref="AwaitableResult"/> instead of being returned directly, so that
        /// <c>await Dynamic.InvokeMember(...)</c> succeeds instead of throwing
        /// <see cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException"/>. Every other result -
        /// including a plain <see cref="Task"/>, or a <see cref="Task{TResult}"/> whose <c>TResult</c> is
        /// visible - is returned unchanged. See <see cref="AwaitableResult"/> for why this is safe.
        /// </remarks>
        [RequiresUnreferencedCode("Resolves 'name' on target's runtime type via the DLR binder; if trimming has removed the member, this throws RuntimeBinderException reporting the member as entirely absent, even when the untrimmed source plainly declares it.")]
        [RequiresDynamicCode("Every call binds through Microsoft.CSharp.RuntimeBinder, which requires the DLR's runtime code generation; not supported when AOT-compiled.")]
        public static dynamic? InvokeMember(object target, String_OR_InvokeMemberName name, params object?[] args)
        {
            target = target.GetTargetContext(out var context, out var staticContext);
            args = Util.GetArgsAndNames(args, out var argNames);
            CallSite? callSite = null;

            var result = InvokeHelper.InvokeMemberCallSite(target, (InvokeMemberName)name, args, argNames, context, staticContext,
                                                     ref callSite);

            return WrapIfResultTypeInaccessible(result);
        }

        /// <summary>
        /// Wraps <paramref name="result"/> in an <see cref="AwaitableResult"/> when it is a
        /// <see cref="Task{TResult}"/> whose <c>TResult</c> is not visible to callers outside its declaring
        /// assembly; otherwise returns it unchanged. <c>TResult</c>'s visibility is read off the runtime
        /// type's public <c>Result</c> property rather than assumed to be <see cref="Task{TResult}"/>
        /// itself, so this also catches a <see cref="Task"/> subclass that declares its own <c>Result</c>.
        /// A plain, non-generic <see cref="Task"/> has no <c>Result</c> property and is never wrapped.
        /// </summary>
        [RequiresUnreferencedCode("Reads the task's 'Result' property via Type.GetProperty(nameof(Result)) reflection; trimming can remove that property from the task's concrete type.")]
        private static object? WrapIfResultTypeInaccessible(object? result)
        {
            if (result is Task task)
            {
                var resultProperty = task.GetType().GetProperty(nameof(Task<object>.Result));
                if (resultProperty != null && !resultProperty.PropertyType.IsVisible)
                {
                    return new AwaitableResult(task);
                }
            }

            return result;
        }

        /// <summary>
        /// Dynamically invokes a member method using the DLR, the same as <see cref="InvokeMember"/>, then
        /// awaits its result without going through dynamic binding. Since <see cref="InvokeMember"/> now
        /// wraps an inaccessible-<c>TResult</c> task in an <see cref="AwaitableResult"/> automatically,
        /// <c>await Dynamic.InvokeMember(...)</c> works directly and this method is no longer required to
        /// make that case succeed. It remains supported for callers who prefer awaiting inside a single
        /// non-dynamic <see cref="Task{TResult}"/>-returning call, without an intermediate <c>dynamic</c>
        /// await expression.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="name">The name. Can be a string; it will be implicitly converted.</param>
        /// <param name="args">The args.</param>
        /// <returns>
        /// A task that completes with the invoked member's result, boxed to <see cref="object"/>
        /// (<see langword="null"/> if the invoked member returns a non-generic <see cref="Task"/>).
        /// </returns>
        /// <exception cref="InvalidOperationException">The invoked member did not return a <see cref="Task"/>.</exception>
        /// <remarks>
        /// This method invokes the member exactly as <see cref="InvokeMember"/> does, then hands the result
        /// to <see cref="AwaitResult"/>, which accepts either the raw <see cref="Task"/> or the
        /// <see cref="AwaitableResult"/> wrapper <see cref="InvokeMember"/> may have returned, and awaits it
        /// through reflection instead of the DLR.
        /// </remarks>
        [RequiresUnreferencedCode("Calls InvokeMember, which resolves 'name' via the DLR binder and can fail against a trimmed target.")]
        [RequiresDynamicCode("Calls InvokeMember, which requires the DLR's runtime code generation; not supported when AOT-compiled.")]
        [SuppressMessage("AsyncUsage", "AsyncFixer01:Unnecessary async/await usage",
            Justification = "Not unnecessary here, and applying it would change observable behaviour twice over. "
                + "InvokeMember runs synchronously before the await and can throw RuntimeBinderException; because "
                + "this method is async that exception is captured into the returned Task and surfaces when the "
                + "caller awaits. Returning the inner task directly would instead throw at the call site, before "
                + "anyone awaits it. It would also discard the ConfigureAwait(false), handing context capture to "
                + "the caller's await. The rule is about allocation overhead and does not account for either.")]
        public static async Task<object?> InvokeMemberAsync(object target, String_OR_InvokeMemberName name, params object?[] args)
        {
            object? result = InvokeMember(target, name, args);
            return await AwaitResult(result).ConfigureAwait(false);
        }

        /// <summary>
        /// Awaits a <see cref="Task"/> (or <see cref="Task{TResult}"/>) obtained dynamically - typically the
        /// un-awaited result of <see cref="InvokeMember"/>, which may be the raw <see cref="Task"/> or an
        /// <see cref="AwaitableResult"/> wrapping one - reading its result through reflection instead of
        /// dynamic binding, so an inaccessible <c>TResult</c> does not trip the C# runtime binder.
        /// </summary>
        /// <param name="task">
        /// The task to await, or an <see cref="AwaitableResult"/> wrapping one, or <see langword="null"/>.
        /// </param>
        /// <returns>
        /// The task's result, boxed to <see cref="object"/> (<see langword="null"/> for a non-generic
        /// <see cref="Task"/>, or if <paramref name="task"/> itself is <see langword="null"/>).
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// <paramref name="task"/> is not a <see cref="Task"/> or an <see cref="AwaitableResult"/>.
        /// </exception>
        [RequiresUnreferencedCode("Reads the completed task's 'Result' property via Type.GetProperty(nameof(Result)) reflection; trimming can remove that property from the task's concrete type.")]
        public static async Task<object?> AwaitResult(object? task)
        {
            if (task is null)
                return null;

            if (task is AwaitableResult awaitableResult)
                task = awaitableResult.Task;

            if (!(task is Task actualTask))
                throw new InvalidOperationException(
                    $"{nameof(AwaitResult)} requires a {nameof(Task)}, a {nameof(Task)}<T>, or an {nameof(AwaitableResult)}, but got {task.GetType()}.");

            // Statically typed await against the base Task class: the compiler binds GetAwaiter/GetResult
            // at compile time from Task itself, never from the object's actual runtime type, so an internal
            // TResult on the real Task<TResult> instance never enters into it. This only waits for
            // completion (and rethrows on fault/cancellation, exactly like a normal await); it does not,
            // and cannot, produce the result value - TaskAwaiter.GetResult() is void.
            await actualTask.ConfigureAwait(false);

            // The result, if any, is read via reflection, which is not subject to the caller's accessibility
            // context - PropertyInfo.GetValue can read a public property whose declared type is internal.
            return actualTask.GetType().GetProperty("Result")?.GetValue(actualTask);
        }


        /// <summary>
        /// Invokes the binary operator.
        /// </summary>
        /// <param name="leftArg">The left arg.</param>
        /// <param name="op">The op.</param>
        /// <param name="rightArg">The right Arg.</param>
        /// <returns></returns>
        [RequiresUnreferencedCode("Every operator case dispatches through 'dynamic' operands, resolved by the DLR binder against the runtime types of leftArg/rightArg; trimming can remove the operator overload being resolved.")]
        [RequiresDynamicCode("Dynamic operator dispatch requires the DLR's runtime code generation; not supported when AOT-compiled.")]
        public static dynamic InvokeBinaryOperator(dynamic leftArg, ExpressionType op, dynamic rightArg)
        {
            switch (op)
            {
                case ExpressionType.Add:
                    return leftArg + rightArg;
                case ExpressionType.AddAssign:
                    leftArg += rightArg;
                    return leftArg;
                case ExpressionType.AndAssign:
                    leftArg &= rightArg;
                    return leftArg;
                case ExpressionType.Divide:
                    return leftArg/rightArg;
                case ExpressionType.DivideAssign:
                    leftArg /= rightArg;
                    return leftArg;
                case ExpressionType.Equal:
                    return leftArg == rightArg;
                case ExpressionType.ExclusiveOr:
                    return leftArg ^ rightArg;
                case ExpressionType.ExclusiveOrAssign:
                    leftArg ^= rightArg;
                    return leftArg;
                case ExpressionType.GreaterThan:
                    return leftArg > rightArg;
                case ExpressionType.GreaterThanOrEqual:
                    return leftArg >= rightArg;
                case ExpressionType.LeftShift:
                    return leftArg << rightArg;
                case ExpressionType.LeftShiftAssign:
                    leftArg <<= rightArg;
                    return leftArg;
                case ExpressionType.LessThan:
                    return leftArg < rightArg;
                case ExpressionType.LessThanOrEqual:
                    return leftArg <= rightArg;
                case ExpressionType.Modulo:
                    return leftArg%rightArg;
                case ExpressionType.ModuloAssign:
                    leftArg %= rightArg;
                    return leftArg;
                case ExpressionType.Multiply:
                    return leftArg*rightArg;
                case ExpressionType.MultiplyAssign:
                    leftArg *= rightArg;
                    return leftArg;
                case ExpressionType.NotEqual:
                    return leftArg != rightArg;
                case ExpressionType.OrAssign:
                    leftArg |= rightArg;
                    return leftArg;
                case ExpressionType.RightShift:
                    return leftArg >> rightArg;
                case ExpressionType.RightShiftAssign:
                    leftArg >>= rightArg;
                    return leftArg;
                case ExpressionType.Subtract:
                    return leftArg - rightArg;
                case ExpressionType.SubtractAssign:
                    leftArg -= rightArg;
                    return leftArg;
                case ExpressionType.Or:
                    return leftArg | rightArg;
                case ExpressionType.And:
                    return leftArg & rightArg;
                case ExpressionType.OrElse:
                    return leftArg || rightArg;
                case ExpressionType.AndAlso:
                    return leftArg && rightArg;
                default:
                    throw new ArgumentException("Unsupported Operator", nameof(op));
            }
        }


        [Obsolete("Use `InvokeUnaryOperator` instead.")]
        // ReSharper disable once IdentifierTypo
        [RequiresUnreferencedCode("Forwards to InvokeUnaryOperator, which dispatches through a 'dynamic' operand resolved by the DLR binder.")]
        [RequiresDynamicCode("Forwards to InvokeUnaryOperator, which requires the DLR's runtime code generation; not supported when AOT-compiled.")]
        public static dynamic InvokeUnaryOpartor(ExpressionType op, dynamic arg)
            => InvokeUnaryOperator(op, (object)arg);
        
        /// <summary>
        /// Invokes the unary operator.
        /// </summary>
        /// <param name="arg">The arg.</param>
        /// <param name="op">The op.</param>
        /// <returns></returns>
        [RequiresUnreferencedCode("Every operator case dispatches through a 'dynamic' operand, resolved by the DLR binder against arg's runtime type; trimming can remove the operator overload being resolved.")]
        [RequiresDynamicCode("Dynamic operator dispatch requires the DLR's runtime code generation; not supported when AOT-compiled.")]
        public static dynamic InvokeUnaryOperator(ExpressionType op, dynamic arg)
        {
            switch (op)
            {
                case ExpressionType.Not:
                    return !arg;
                case ExpressionType.Negate:
                    return -arg;
                case ExpressionType.Decrement:
                    return --arg;
                case ExpressionType.Increment:
                    return ++arg;
                default:
                    throw new ArgumentException("Unsupported Operator", nameof(op));
            }
        }

        /// <summary>
        /// Invokes the specified target using the DLR;
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="args">The args.</param>
        /// <returns></returns>
        [RequiresUnreferencedCode("Resolves target's invoke/call operator via the DLR binder; trimming can remove the member being resolved.")]
        [RequiresDynamicCode("Binds through Microsoft.CSharp.RuntimeBinder, which requires the DLR's runtime code generation; not supported when AOT-compiled.")]
        public static dynamic? Invoke(object target, params object?[] args)
        {
            target = target.GetTargetContext(out var context, out var staticContext);
            args = Util.GetArgsAndNames(args, out var argNames);
            CallSite? callSite = null;

            return InvokeHelper.InvokeDirectCallSite(target, args, argNames, context, staticContext, ref callSite);
        }


        /// <summary>
        /// Dynamically Invokes indexer using the DLR.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="indexes">The indexes.</param>
        /// <returns></returns>
        [RequiresUnreferencedCode("Resolves target's indexer via the DLR binder; trimming can remove the indexer being resolved.")]
        [RequiresDynamicCode("Binds through Microsoft.CSharp.RuntimeBinder, which requires the DLR's runtime code generation; not supported when AOT-compiled.")]
        public static dynamic? InvokeGetIndex(object target, params object?[] indexes)
        {
            target = target.GetTargetContext(out var tContext, out var tStaticContext);
            indexes = Util.GetArgsAndNames(indexes, out var tArgNames);
            CallSite? tCallSite = null;

            return InvokeHelper.InvokeGetIndexCallSite(target, indexes, tArgNames, tContext, tStaticContext,
                                                       ref tCallSite);
        }


        /// <summary>
        /// Convenience version of InvokeSetIndex that separates value and indexes.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="value">The value</param>
        /// <param name="indexes">The indexes </param>
        /// <returns></returns>
        [RequiresUnreferencedCode("Forwards to InvokeSetIndex, which resolves target's indexer via the DLR binder.")]
        [RequiresDynamicCode("Forwards to InvokeSetIndex, which requires the DLR's runtime code generation; not supported when AOT-compiled.")]
        public static object? InvokeSetValueOnIndexes(object target, object? value, params object?[] indexes)
        {
            var tList = new List<object?>(indexes) {value};
            return InvokeSetIndex(target, indexesThenValue: tList.ToArray());
        }

        /// <summary>
        /// Invokes setindex.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="indexesThenValue">The indexes then value.</param>
        [RequiresUnreferencedCode("Resolves target's indexer setter via the DLR binder; trimming can remove the indexer being resolved.")]
        [RequiresDynamicCode("Binds through Microsoft.CSharp.RuntimeBinder, which requires the DLR's runtime code generation; not supported when AOT-compiled.")]
        public static object? InvokeSetIndex(object target, params object?[] indexesThenValue)
        {
            Guard.NotNull(indexesThenValue);
            if (indexesThenValue.Length < 2)
            {
                throw new ArgumentException("Requires at least one index and one value", nameof(indexesThenValue));
            }

            target = target.GetTargetContext(out var tContext, out var tStaticContext);
            indexesThenValue = Util.GetArgsAndNames(indexesThenValue, out var tArgNames);

            CallSite? tCallSite = null;
            return InvokeHelper.InvokeSetIndexCallSite(target, indexesThenValue, tArgNames, tContext, tStaticContext,
                                                ref tCallSite);
        }

        /// <summary>
        /// Dynamically Invokes a member method which returns void using the DLR
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="name">The name.</param>
        /// <param name="args">The args.</param>
        /// <example>
        /// Unit test that exhibits usage:
        /// <code>
        /// <![CDATA[
        ///    var tTest = "Wrong";
        ///    var tValue = "Correct";
        ///
        ///    dynamic tExpando = new ExpandoObject();
        ///    tExpando.Action = new Action<string>(it => tTest = it);
        ///
        ///    Impromptu.InvokeMemberAction(tExpando, "Action", tValue);
        ///
        ///    Assert.AreEqual(tValue, tTest);
        /// ]]>
        /// </code>
        /// </example>
        [RequiresUnreferencedCode("Resolves 'name' on target's runtime type via the DLR binder; if trimming has removed the member, this throws RuntimeBinderException reporting the member as entirely absent.")]
        [RequiresDynamicCode("Binds through Microsoft.CSharp.RuntimeBinder, which requires the DLR's runtime code generation; not supported when AOT-compiled.")]
        public static void InvokeMemberAction(object target, String_OR_InvokeMemberName name, params object?[] args)
        {
            target = target.GetTargetContext(out var tContext, out var tStaticContext);
            args = Util.GetArgsAndNames(args, out var tArgNames);

            CallSite? tCallSite = null;
            InvokeHelper.InvokeMemberActionCallSite(target, (InvokeMemberName)name, args, tArgNames, tContext, tStaticContext,
                                                    ref tCallSite);
        }

        /// <summary>
        /// Invokes the action using the DLR
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="args">The args.</param>
        [RequiresUnreferencedCode("Resolves target's invoke/call operator via the DLR binder; trimming can remove the member being resolved.")]
        [RequiresDynamicCode("Binds through Microsoft.CSharp.RuntimeBinder, which requires the DLR's runtime code generation; not supported when AOT-compiled.")]
        public static void InvokeAction(object target, params object?[] args)
        {
            target = target.GetTargetContext(out var tContext, out var tStaticContext);
            args = Util.GetArgsAndNames(args, out var tArgNames);

            CallSite? tCallSite = null;
            InvokeHelper.InvokeDirectActionCallSite(target, args, tArgNames, tContext, tStaticContext, ref tCallSite);
        }


        /// <summary>
        /// Dynamically Invokes a set member using the DLR.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        /// <example>
        /// Unit test that exhibits usage:
        /// <code>
        /// <![CDATA[
        ///    dynamic tExpando = new ExpandoObject();
        ///
        ///    var tSetValue = "1";
        ///
        ///    Impromptu.InvokeSet(tExpando, "Test", tSetValue);
        ///
        ///    Assert.AreEqual(tSetValue, tExpando.Test);
        /// ]]>
        /// </code>
        /// </example>
        /// <remarks>
        /// if you call a static property off a type with a static context the csharp dlr binder won't do it, so this method reverts to reflection
        /// </remarks>
        [RequiresUnreferencedCode("Resolves 'name' on target's runtime type via the DLR binder (falling back to reflection for a static context); trimming can remove the member being resolved.")]
        [RequiresDynamicCode("The DLR binder path requires the DLR's runtime code generation; not supported when AOT-compiled.")]
        public static object? InvokeSet(object target, string name, object? value)
        {
            target = target.GetTargetContext(out var tContext, out var tStaticContext);
            tContext = tContext.FixContext();


            CallSite? tCallSite = null;
            return InvokeHelper.InvokeSetCallSite(target, name, value, tContext, tStaticContext, ref tCallSite);
        }

        /// <summary>
        /// Invokes the set on the end of a property chain.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="propertyChain">The property chain.</param>
        /// <param name="value">The value.</param>
        [RequiresUnreferencedCode("Walks propertyChain by calling InvokeGet/InvokeGetIndex/InvokeSet, each of which resolves a member via the DLR binder; trimming can remove any member along the chain.")]
        [RequiresDynamicCode("Each step in the chain binds through the DLR; not supported when AOT-compiled.")]
        public static object InvokeSetChain(object target, string propertyChain, object value)
        {
            var tProperties = _chainRegex.FluentMatches(propertyChain).ToList();
            var tGetProperties = tProperties.Take(tProperties.Count - 1);

       
            var tTarget = target;
            foreach (var tProperty in tGetProperties)
            {
                var tGetter = tProperty.Getter;
                var tIntIndexer = tProperty.IntIndexer;
                var tStringIndexer = tProperty.StringIndexer;

                if (tGetter != null)
                    tTarget = InvokeGet(tTarget, tGetter);
                else if (tIntIndexer != null)
                    tTarget = InvokeGetIndex(tTarget, Dynamic.CoerceConvert(tIntIndexer, typeof(int)));
                else if (tStringIndexer != null)
                    tTarget = InvokeGetIndex(tTarget, tStringIndexer);
                else
                {
                    throw new Exception($"Could Not Parse :'{propertyChain}'");
                }
            }

            var tSetProperty = tProperties.Last();

            var tSetGetter = tSetProperty.Getter;
            var tSetIntIndexer = tSetProperty.IntIndexer;
            var tSetStringIndexer = tSetProperty.StringIndexer;

            if (tSetGetter != null)
                return InvokeSet(tTarget, tSetGetter, value);
            if (tSetIntIndexer != null)
                return InvokeSetIndex(tTarget, Dynamic.CoerceConvert(tSetIntIndexer, typeof(int)), value);
            if (tSetStringIndexer != null)
                return InvokeSetIndex(tTarget, tSetStringIndexer, value);
            
            throw new Exception($"Could Not Parse :'{propertyChain}'");
        }

           




        // Lazily constructed (not a field initializer) for the same reason as ComBinder above:
        // constructing an InvokeSetters (a DynamicObject) unconditionally requires the DLR, and Dynamic's
        // explicit static constructor would otherwise pay that cost the moment ANY member of Dynamic is
        // first touched, regardless of whether InvokeSetAll is ever used.
        private static dynamic? _invokeSetAll;

        /// <summary>
        /// Call Like method invokes set on target and a list of property/value. Invoke with dictionary, anonymous type or named arguments.
        /// </summary>
        /// <value>The invoke set all.</value>
        public static dynamic InvokeSetAll
        {
            [RequiresUnreferencedCode("Constructs an InvokeSetters, and the returned object's Invoke resolves each supplied property name against target via InvokeSetChain/the DLR binder; trimming can remove the property being set.")]
            [RequiresDynamicCode("Constructing an InvokeSetters instantiates System.Dynamic.DynamicObject, and invoking the returned object, both require the DLR's runtime code generation; not supported when AOT-compiled.")]
            get => _invokeSetAll ?? (_invokeSetAll = new InvokeSetters());
        }

        /// <summary>
        /// Wraps a target to partial apply a method (or target if you can invoke target directly eg delegate).
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="totalArgCount">The total arg count.</param>
        /// <returns></returns>
        [RequiresUnreferencedCode("The returned proxy resolves target's invoke/call operator (or, for a named member, InvokeMember) via the DLR binder on each call; trimming can remove the member being resolved.")]
        [RequiresDynamicCode("Invoking the returned proxy requires the DLR's runtime code generation; not supported when AOT-compiled.")]
        public static dynamic Curry(object target, int? totalArgCount = null)
        {
            if (target is Delegate && !totalArgCount.HasValue)
                return Curry((Delegate) target);
            return new Curry(target, totalArgCount);
        }

        /// <summary>
        /// Wraps a delegate to partially apply it.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <returns></returns>
        [RequiresUnreferencedCode("The returned proxy resolves the delegate's invoke operator via the DLR binder on each call; trimming can remove the member being resolved.")]
        [RequiresDynamicCode("Invoking the returned proxy requires the DLR's runtime code generation; not supported when AOT-compiled.")]
        public static dynamic Curry(Delegate target)
        {
            return new Curry(target, target.GetMethodInfo().GetParameters().Length);
        }



        /// <summary>
        /// Dynamically Invokes a get member using the DLR.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="name">The name.</param>
        /// <returns>The result.</returns>
        /// <example>
        /// Unit Test that describes usage
        /// <code>
        /// <![CDATA[
        ///    var tSetValue = "1";
        ///    var tAnon = new { Test = tSetValue };
        ///
        ///    var tOut =Impromptu.InvokeGet(tAnon, "Test");
        ///
        ///    Assert.AreEqual(tSetValue, tOut);
        /// ]]>
        /// </code>
        /// </example>
        [RequiresUnreferencedCode("Resolves 'name' on target's runtime type via the DLR binder; if trimming has removed the member, this throws RuntimeBinderException reporting the member as entirely absent, even when the untrimmed source plainly declares it.")]
        [RequiresDynamicCode("Binds through Microsoft.CSharp.RuntimeBinder, which requires the DLR's runtime code generation; not supported when AOT-compiled.")]
        public static dynamic? InvokeGet(object target, string name)
        {
            target = target.GetTargetContext(out var tContext, out var tStaticContext);            CallSite? tSite = null;
            return InvokeHelper.InvokeGetCallSite(target, name, tContext, tStaticContext, ref tSite);
        }


  private static readonly Regex _chainRegex
           = new Regex(@"((\.?(?<Getter>\w+))|(\[(?<IntIndexer>\d+)\])|(\['(?<StringIndexer>\w+)'\]))");

        /// <summary>
        /// Invokes the getter property chain.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="propertyChain">The property chain.</param>
        /// <returns></returns>
        [RequiresUnreferencedCode("Walks propertyChain by calling InvokeGet/InvokeGetIndex, each of which resolves a member via the DLR binder; trimming can remove any member along the chain.")]
        [RequiresDynamicCode("Each step in the chain binds through the DLR; not supported when AOT-compiled.")]
        public static dynamic? InvokeGetChain(object target, string propertyChain)
        {
            var tProperties = _chainRegex.FluentMatches(propertyChain);
            var tTarget = target;
            foreach (var tProperty in tProperties)
            {
                var tGetter = tProperty.Getter;
                var tIntIndexer = tProperty.IntIndexer;
                var tStringIndexer = tProperty.StringIndexer;

                if (tGetter != null)
                    tTarget = InvokeGet(tTarget, tGetter);
                else if (tIntIndexer != null)
                    tTarget = InvokeGetIndex(tTarget, Dynamic.CoerceConvert(tIntIndexer,typeof(int)));
                else if (tStringIndexer != null)
                    tTarget = InvokeGetIndex(tTarget, tStringIndexer);
                else
                {
                    throw new Exception($"Could Not Parse :'{propertyChain}'");
                }
            }
            return tTarget;
        }

        /// <summary>
        /// Determines whether the specified name on target is event. This allows you to know whether to InvokeMemberAction
        ///  add_{name} or a combo of {invokeGet, +=, invokeSet} and the corresponding remove_{name} 
        /// or a combo of {invokeGet, -=, invokeSet}
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="name">The name.</param>
        /// <returns>
        /// 	<c>true</c> if the specified target is event; otherwise, <c>false</c>.
        /// </returns>
        [RequiresUnreferencedCode("Resolves 'name' on target's runtime type via the DLR binder to determine whether it is an event; trimming can remove the member being resolved.")]
        [RequiresDynamicCode("Binds through Microsoft.CSharp.RuntimeBinder, which requires the DLR's runtime code generation; not supported when AOT-compiled.")]
        public static bool InvokeIsEvent(object target, string name)
        {
            target = target.GetTargetContext(out var tContext, out var tStaticContext);
            tContext = tContext.FixContext();
            CallSite? tCallSite = null;
            return InvokeHelper.InvokeIsEventCallSite(target, name, tContext, ref tCallSite);
        }

        /// <summary>
        /// Invokes add assign with correct behavior for events.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        [RequiresUnreferencedCode("Resolves the add accessor for 'name' via the DLR binder; trimming can remove the event being resolved.")]
        [RequiresDynamicCode("Binds through Microsoft.CSharp.RuntimeBinder, which requires the DLR's runtime code generation; not supported when AOT-compiled.")]
        public static void InvokeAddAssignMember(object target, string name, object? value)
        {
            CallSite? callSiteAdd = null;
            CallSite? callSiteGet = null;
            CallSite? callSiteSet = null;
            CallSite? callSiteIsEvent = null;
            target = target.GetTargetContext(out var context, out var staticContext);

            object?[] args = new[] { value };
            args = Util.GetArgsAndNames(args, out var argNames);

            InvokeHelper.InvokeAddAssignCallSite(target, name, args, argNames, context, staticContext, ref callSiteIsEvent, ref callSiteAdd, ref callSiteGet, ref callSiteSet);
        }

        /// <summary>
        /// Invokes subtract assign with correct behavior for events.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        [RequiresUnreferencedCode("Resolves the remove accessor for 'name' via the DLR binder; trimming can remove the event being resolved.")]
        [RequiresDynamicCode("Binds through Microsoft.CSharp.RuntimeBinder, which requires the DLR's runtime code generation; not supported when AOT-compiled.")]
        public static void InvokeSubtractAssignMember(object target, string name, object? value)
        {
            target = target.GetTargetContext(out var context, out var staticContext);

            object?[] args = new[] { value };

            args = Util.GetArgsAndNames(args, out var argNames);


            CallSite? callSiteIsEvent = null;
            CallSite? callSiteRemove = null;
            CallSite? callSiteGet = null;
            CallSite? callSiteSet = null;


            InvokeHelper.InvokeSubtractAssignCallSite(target, name, args, argNames, context, staticContext, ref callSiteIsEvent, ref callSiteRemove, ref callSiteGet,ref  callSiteSet);
        }

        /// <summary>
        /// Invokes  convert using the DLR.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="type">The type.</param>
        /// <param name="explicit">if set to <c>true</c> [explicit].</param>
        /// <returns></returns>
        [RequiresUnreferencedCode("Resolves the conversion operator to 'type' via the DLR binder; trimming can remove the conversion being resolved.")]
        [RequiresDynamicCode("Binds through Microsoft.CSharp.RuntimeBinder, which requires the DLR's runtime code generation; not supported when AOT-compiled.")]
        public static dynamic? InvokeConvert(object target, Type type, bool @explicit =false)
        {
            target = target.GetTargetContext(out var tContext, out var tDummy);

            CallSite? tCallSite = null;
            return InvokeHelper.InvokeConvertCallSite(target, @explicit, type, tContext, ref tCallSite);

        }

        internal static readonly IDictionary<Type, Delegate> CompiledExpressions = new Dictionary<Type, Delegate>();

        /// <summary>
        /// Coerces any invokable to specified delegate type.
        /// </summary>
        /// <param name="invokeableObject">The invokeable object.</param>
        /// <param name="delegateType">Type of the delegate.</param>
        /// <returns></returns>
        [RequiresUnreferencedCode("Falls back to Expression.Lambda(...).Compile() and, for a plain Action/Func-shaped delegate whose parameters are all reference types, to invoking invokeableObject through the DLR; trimming can remove the member the compiled expression or DLR call resolves.")]
        [RequiresDynamicCode("Expression.Lambda(...).Compile() and the DLR invocation path both generate code at runtime; not supported when AOT-compiled.")]
        public static dynamic? CoerceToDelegate(object? invokeableObject, Type delegateType)
            {
                var delegateTypeInfo = delegateType.GetTypeInfo();
                if (!typeof(Delegate).GetTypeInfo().IsAssignableFrom(delegateTypeInfo.BaseType))
                {
                    return null;
                }
                var tDelMethodInfo = delegateTypeInfo.GetMethod("Invoke");
                if (tDelMethodInfo is null)
                {
                    throw new Exception("This Delegate Didn't have and Invoke method! Impossible!");
                }
                var tReturnType = tDelMethodInfo.ReturnType;
                var tAction = tReturnType == typeof(void);
                var tParams = tDelMethodInfo.GetParameters();
                var tLength = tDelMethodInfo.GetParameters().Length;
                // invokeableObject is genuinely allowed to be null here (see its nullable
                // parameter annotation above); WrapAction/WrapFunc only close over it and hand it
                // to a delegate the caller may never invoke, so a null flows through unharmed. The
                // `!`s below match every other deliberate-null forward in this file rather than
                // widening WrapAction/WrapFunc's own parameter types.
                Delegate tBaseDelegate = tAction
                                             ? InvokeHelper.WrapAction(invokeableObject!, tLength)
                                             : InvokeHelper.WrapFunc(tReturnType, invokeableObject!, tLength);


                if (InvokeHelper.IsActionOrFunc(delegateType) &&
                    !tParams.Any(it => it.ParameterType.GetTypeInfo().IsValueType))
                {
                    return tBaseDelegate;
                }

                if (CompiledExpressions.TryGetValue(delegateType, out var tGetResult))
                {
                    return tGetResult.DynamicInvoke(tBaseDelegate);
                }

                var tParamTypes = tParams.Select(it => it.ParameterType).ToArray();
                var tDelParam = Expression.Parameter(tBaseDelegate.GetType());
                var tInnerParams = tParamTypes.Select(Expression.Parameter).ToArray();

                var tI = Expression.Invoke(tDelParam,
                    tInnerParams.Select(it => (Expression)Expression.Convert(it, typeof(object))));
                var tL = Expression.Lambda(delegateType, tI, tInnerParams);

                tGetResult =
                    Expression.Lambda(Expression.GetFuncType(tBaseDelegate.GetType(), delegateType), tL,
                        tDelParam).Compile();
                CompiledExpressions[delegateType] = tGetResult;

                return tGetResult.DynamicInvoke(tBaseDelegate);

            }

        // Lazily constructed (not a field initializer) for the same reason as ComBinder above:
        // constructing a LateType (a DynamicObject) unconditionally requires the DLR, and Dynamic's
        // explicit static constructor would otherwise pay that cost the moment ANY member of Dynamic is
        // first touched, regardless of whether IsDBNull is ever called.
        private static dynamic? _lateConvert;

        private static dynamic LateConvert
        {
            [RequiresDynamicCode("Constructing a LateType instantiates System.Dynamic.DynamicObject, whose default constructor requires the DLR's runtime code generation; not supported when AOT-compiled.")]
            get => _lateConvert ?? (_lateConvert = new DynamicObjects.LateType(typeof(Convert)));
        }


        /// <summary>
        /// Determines whether value is DBNull dynamically (Useful for PCL)
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        ///   <c>true</c> if [is DBNull]; otherwise, <c>false</c>.
        /// </returns>
        [RequiresUnreferencedCode("Resolves System.Convert.IsDBNull dynamically (via a late-bound Convert reference) rather than calling it directly; trimming Convert's public surface breaks this.")]
        [RequiresDynamicCode("Constructing the underlying LateType and making the late-bound call both require the DLR's runtime code generation; not supported when AOT-compiled.")]
        public static bool IsDBNull(object? value)
        {

            try
            {
                
                return LateConvert.IsDBNull(value);
            }
            catch
            {
                // Deliberately broad (cs/catch-of-all-exceptions): the expected failure is
                // RuntimeBinderException when trimming has removed Convert.IsDBNull (see the
                // [RequiresUnreferencedCode] above), but this is a boolean probe with a safe
                // default either way - "can't tell" and "not DBNull" collapse to the same false.
                return false;
            }
        }

        /// <summary>
        /// Applies the equivalent type hint to dynamic object
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="types">The types.</param>
        [RequiresUnreferencedCode("For more than one type, converts each with ConvertEach<FauxType>, which resolves a conversion via the DLR binder per item; trimming can remove the conversion being resolved.")]
        [RequiresDynamicCode("ConvertEach's DLR conversion path requires runtime code generation; not supported when AOT-compiled.")]
        public static void ApplyEquivalentType(DynamicObjects.IEquivalentType target, params Type[] types)
        {
            Guard.NotNull(target);
            Guard.NotNull(types);

            target.EquivalentType = types.Length == 1
                ? types.First()
                : new DynamicObjects.AggreType(types.ConvertEach<DynamicObjects.FauxType>().ToArray());
          
        }



        /// <summary>
        /// Implicit or Explicit Converts the items of the specified enumerable.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerable">The enumerable.</param>
        /// <param name="explicit">if set to <c>true</c> [explicit].</param>
        /// <returns></returns>
        [Obsolete("Use ConvertEach.")]
        [RequiresUnreferencedCode("Forwards to ConvertEach, which resolves a conversion via the DLR binder per item.")]
        [RequiresDynamicCode("Forwards to ConvertEach, which requires the DLR's runtime code generation; not supported when AOT-compiled.")]
        public static IEnumerable<T> ConvertAll<T>(this System.Collections.IEnumerable enumerable, bool explict = false)
            => ConvertEach<T>(enumerable, explict);
        
        /// <summary>
        /// Implicit or Explicit Converts the items of the specified enumerable.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerable">The enumerable.</param>
        /// <param name="explicit">if set to <c>true</c> [explicit].</param>
        /// <returns></returns>
        [RequiresUnreferencedCode("Calls InvokeConvert per item, which resolves a conversion via the DLR binder; trimming can remove the conversion being resolved.")]
        [RequiresDynamicCode("InvokeConvert's DLR path requires runtime code generation; not supported when AOT-compiled.")]
        public static IEnumerable<T> ConvertEach<T>(this System.Collections.IEnumerable enumerable, bool @explicit =false)
        {
            return enumerable.Cast<object>().Select(it => InvokeConvert(it, typeof (T), @explicit)).Cast<T>();
        } 

        /// <summary>
        /// Goes the extra mile to convert target to type.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        [RequiresUnreferencedCode("Falls through CoerceToDelegate, a late-bound Impromptu.DynamicActLike call, InvokeConvert, and TypeDescriptor/TypeConverter reflection - each resolves a member or type by name; trimming can remove any of them.")]
        [RequiresDynamicCode("The CoerceToDelegate and Impromptu.DynamicActLike paths require the DLR's runtime code generation; not supported when AOT-compiled.")]
        public static dynamic? CoerceConvert(object? target, Type type)
        {
            Guard.NotNull(type);

            var typeInfo = type.GetTypeInfo();
            if (target != null && !typeInfo.IsInstanceOfType(target) && !IsDBNull(target))
            {

                var delegateConversion = CoerceToDelegate(target, type);

                if (delegateConversion != null)
                    return delegateConversion;


                if (typeInfo.IsInterface && Impromptu.IsAvailable)
                {


                
                    if (target is IDictionary<string, object> tDict && !(tDict is DynamicObjects.BaseObject))
                    {
                        target = new DynamicObjects.Dictionary(tDict);
                    }
                    else if(!(target is DynamicObjects.BaseObject))
                    {
                        target = new DynamicObjects.Get(target);
                    }


                    target = Impromptu.DynamicActLike(target, type);
                }
                else
                {
                

                    try
                    {
                        object? tResult = Dynamic.InvokeConvert(target, type, @explicit: true);

                        target = tResult;
                    }
                    catch (RuntimeBinderException)
                    {
                        Type tReducedType = type;
                        if (typeInfo.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                        {
                            tReducedType = typeInfo.GetGenericArguments().First();
                        }

                        if (typeof (Enum).GetTypeInfo().IsAssignableFrom(tReducedType) && target is string sVal)
                        {
                            target = Enum.Parse(tReducedType, sVal, true);

                        }
                        else if (target is IConvertible && typeof (IConvertible).GetTypeInfo().IsAssignableFrom(tReducedType))
                        {

                            target = Convert.ChangeType(target, tReducedType, Net40.GetDefaultThreadCurrentCulture());

                        }
                        else
                        {
                            try
                            {
                                dynamic? converter = null;
                                if (TypeDescriptor.IsAvailable)
                                {
                                    converter = TypeDescriptor.GetConverter(tReducedType);
                                }
                                else if (TypeConverterAttributeSL != null) 
                                {
                                        var tAttributes =
                                            tReducedType.GetTypeInfo().GetCustomAttributes(TypeConverterAttributeSL, false);
                                        dynamic? attribute = tAttributes.FirstOrDefault();
                                        if (attribute != null)
                                        {
                                            converter =
                                                Impromptu.InvokeConstructor(Type.GetType(attribute.ConverterTypeName));
                                        }
                                }
                                

                                // target is still the non-null value the outer `if (target != null
                                // && ...)` proved: the try block's only reassignment
                                // (`target = tResult;`, above) never ran, or this catch wouldn't
                                // have been reached - it's the last statement in that try.
                                // Flow narrowing from `converter != null` doesn't carry into
                                // dynamic-typed member access, hence the `!`s below.
                                if (converter != null && converter!.CanConvertFrom(target!.GetType()))
                                {
                                    target = converter!.ConvertFrom(target!);
                                }
                            }
                            catch (RuntimeBinderException)
                            {
                                //This runtime converter block is a hail mary
                                //lgtm [cs/empty-catch-block]
                            }
                        }

                    }
                }
            }
            else if (((target == null) || IsDBNull(target )) && typeInfo.IsValueType)
            {
                target = Dynamic.InvokeConstructor(type);
            }
            else if (!typeInfo.IsInstanceOfType(target) && IsDBNull(target))
            {
                return null;
            }
            return target;
        }

        /// <summary>
        /// Invokes the constructor.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="args">The args.</param>
        /// <returns></returns>
        [RequiresUnreferencedCode("Resolves type's constructor via the DLR binder; if trimming has removed the constructor, this throws RuntimeBinderException reporting the type as having no constructors at all, even when the untrimmed source plainly declares one.")]
        [RequiresDynamicCode("For more than 14 arguments, building the call site emits a delegate type via Reflection.Emit; for 14 or fewer, it binds through Microsoft.CSharp.RuntimeBinder. Both require the DLR's runtime code generation. On a runtime without Reflection.Emit (AOT-compiled, trimmed, or mobile/WebAssembly), more than 14 arguments throws PlatformNotSupportedException instead - see issue #27; 14 or fewer still requires the DLR itself and is not supported when AOT-compiled.")]
        public static dynamic? InvokeConstructor(Type type, params object?[] args)
        {
            Guard.NotNull(args);

            var tValue = type.GetTypeInfo().IsValueType;
            if (tValue && args.Length == 0)  //dynamic invocation doesn't see constructors of value types
            {
                return Activator.CreateInstance(type);
            }

           args = Util.GetArgsAndNames( args, out var tArgNames);
           CallSite? tCallSite = null;


            return InvokeHelper.InvokeConstructorCallSite(type, tValue, args, tArgNames, ref tCallSite);
        }


        /// <summary>
        /// FastDynamicInvoke extension method. Runs up to runs up to 20x faster than <see cref="System.Delegate.DynamicInvoke"/> .
        /// </summary>
        /// <param name="del">The del.</param>
        /// <param name="args">The args.</param>
        /// <returns></returns>
        [RequiresUnreferencedCode("For an argument count with no hand-written fast path, invokes del through the DLR (via a 'dynamic' reference to del) rather than Delegate.DynamicInvoke; trimming can remove the member the DLR resolves.")]
        [RequiresDynamicCode("The DLR invocation path requires runtime code generation; not supported when AOT-compiled.")]
		public static object? FastDynamicInvoke(this Delegate del, params object?[] args)
		{
            Guard.NotNull(del);
            Guard.NotNull(args);

            if (del.GetMethodInfo().ReturnType != typeof(void))
            {
                return InvokeHelper.FastDynamicInvokeReturn(del, args!);
            }
            InvokeHelper.FastDynamicInvokeAction(del, args!);
            return null;
        }

        /// <summary>
        /// Given a generic parameter count and whether it returns void or not gives type of Action or Func
        /// </summary>
        /// <param name="paramCount">The param count.</param>
        /// <param name="returnVoid">if set to <c>true</c> [return void].</param>
        /// <returns>Type of Action or Func</returns>
        public static Type GenericDelegateType(int paramCount, bool returnVoid = false)
        {
            var tParamCount = returnVoid ? paramCount : paramCount - 1;
            if (tParamCount > 16)
                throw new ArgumentException(
                    $"{(returnVoid ? "Action" : "Func")} only handle at  most {(returnVoid ? 16 : 17)} parameters", nameof(paramCount));
            if(tParamCount < 0)
                throw new ArgumentException(
                    $"{(returnVoid ? "Action" : "Func")} must have at least {(returnVoid ? 0 : 1)} parameter(s)", nameof(paramCount));


            return returnVoid
                ? InvokeHelper.ActionKinds[tParamCount]
                : InvokeHelper.FuncKinds[tParamCount];
        }

        /// <summary>
        /// Gets the member names of properties. Not all IDynamicMetaObjectProvider have support for this.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="dynamicOnly">if set to <c>true</c> [dynamic only]. Won't add reflected properties</param>
        /// <returns></returns>
        [RequiresUnreferencedCode("For an IDynamicMetaObjectProvider, calls GetMetaObject(...).GetDynamicMemberNames(); for a COM object, resolves member names through a late-bound ComBinder reference. Trimming can remove members either path would otherwise report.")]
        [RequiresDynamicCode("The late-bound ComBinder path goes through the DLR, which requires runtime code generation; not supported when AOT-compiled.")]
        public static IEnumerable<string> GetMemberNames(object target, bool dynamicOnly = false)
        {
            Guard.NotNull(target);

            var tList = new List<string>();
            if (!dynamicOnly)
            {
               tList.AddRange(target.GetType().GetTypeInfo().GetProperties().Select(it => it.Name));
            }

            if (target is IDynamicMetaObjectProvider tTarget)
            {
                tList.AddRange(tTarget.GetMetaObject(Expression.Constant(tTarget)).GetDynamicMemberNames());
            }else
            {

                if (ComObjectType != null && ComObjectType.GetTypeInfo().IsInstanceOfType(target) && ComBinder.IsAvailable)
                {
                    tList.AddRange(ComBinder.GetDynamicDataMemberNames(target));
                }
            }
            return tList;
        } 

        /// <summary>
        /// Dynamically invokes a method determined by the CallSite binder and be given an appropriate delegate type
        /// </summary>
        /// <param name="callSite">The Callsite</param>
        /// <param name="target">The target.</param>
        /// <param name="args">The args.</param>
        /// <returns></returns>
        /// <remarks>
        /// Advanced use only. Use this method for serious custom invocation, otherwise there are other convenience methods such as
        /// <see cref="InvokeMember"></see>, <see cref="InvokeGet"></see>, <see cref="InvokeSet"></see> and <see cref="InvokeMemberAction"></see>
        /// </remarks>
        [RequiresUnreferencedCode("Reads callSite.Target via a 'dynamic' cast (DLR member resolution) before invoking it; trimming can remove the Target member being resolved. Advanced/low-level API - prefer InvokeMember/InvokeGet/etc.")]
        [RequiresDynamicCode("The 'dynamic' cast and subsequent invocation both require the DLR's runtime code generation; not supported when AOT-compiled.")]
        public static dynamic? InvokeCallSite(CallSite callSite, object target, params object?[] args)
        {
         
            
            var tParameters = new List<object?> {callSite, target};
            tParameters.AddRange(args);

            MulticastDelegate tDelegate = ((dynamic)callSite).Target;

            return tDelegate.FastDynamicInvoke(tParameters.ToArray());
        }


    }

}
