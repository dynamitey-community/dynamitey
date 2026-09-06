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

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Dynamitey
{
    using System;

    /// <summary>
    /// Wraps a <see cref="Task"/> - specifically a <see cref="Task{TResult}"/> whose <c>TResult</c> is not
    /// accessible to the calling assembly - so that <c>await</c>, including a dynamically-bound
    /// <c>await</c> on the <see langword="dynamic"/> result of <see cref="Dynamic.InvokeMember"/>, can
    /// complete without the C# runtime binder ever needing to know <c>TResult</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>await</c> on a <see langword="dynamic"/> expression compiles to dynamic invocations of
    /// <c>GetAwaiter</c>, <c>IsCompleted</c> and <c>GetResult</c>, resolved by the C# runtime binder in the
    /// CALLING assembly's accessibility context. When the awaited <see cref="Task{TResult}"/>'s
    /// <c>TResult</c> is not visible to the caller, the binder cannot produce a value of that type,
    /// <c>GetResult</c> binds to a void-returning form instead, and the compiler-generated conversion of
    /// the await expression's result throws
    /// <see cref="Microsoft.CSharp.RuntimeBinder.RuntimeBinderException"/> ("Cannot implicitly convert
    /// type 'void' to 'object'").
    /// </para>
    /// <para>
    /// This wrapper sidesteps that failure: every member the await pattern needs -
    /// <see cref="GetAwaiter"/>, <see cref="Awaiter.IsCompleted"/>, <see cref="Awaiter.GetResult"/> - is a
    /// public member with a public signature. <see cref="Awaiter.GetResult"/> in particular is declared to
    /// return <see cref="object"/>, never <c>TResult</c>, so the runtime binder can resolve it regardless
    /// of whether the caller can see <c>TResult</c>. Internally, the wrapper observes the underlying task
    /// through its statically-typed, non-generic <see cref="Task"/> base (so no binder or generic
    /// instantiation ever touches <c>TResult</c>) and then reads the result value out via reflection, which
    /// is not subject to accessibility checks. The caller still gets the real result instance back - just
    /// boxed as <see cref="object"/> instead of typed as <c>TResult</c>.
    /// </para>
    /// <para>
    /// <see cref="Dynamic.InvokeMember"/> returns this wrapper only when the invoked member's result is a
    /// <see cref="Task{TResult}"/> whose <c>TResult</c> is not visible outside its declaring assembly (see
    /// <see cref="Type.IsVisible"/>, which - unlike <see cref="Type.IsPublic"/> - correctly treats a public
    /// type nested inside another public type as visible). Every other result - a plain <see cref="Task"/>,
    /// or a <see cref="Task{TResult}"/> whose <c>TResult</c> is visible to callers - passes through
    /// unwrapped and behaves exactly as before.
    /// </para>
    /// </remarks>
    /// <seealso cref="Dynamic.InvokeMember"/>
    /// <seealso cref="Dynamic.AwaitResult"/>
    public sealed class AwaitableResult
    {
        /// <summary>
        /// The underlying task this wrapper awaits. Use this when you need the real <see cref="Task"/>
        /// itself - e.g. to pass it to <see cref="Dynamic.AwaitResult"/>, inspect
        /// <see cref="Task.IsFaulted"/>, or hand it to other task-based code.
        /// </summary>
        public Task Task { get; }

        /// <summary>
        /// Wraps <paramref name="task"/> for accessibility-safe awaiting.
        /// </summary>
        /// <param name="task">The task to wrap.</param>
        /// <exception cref="ArgumentNullException"><paramref name="task"/> is <see langword="null"/>.</exception>
        public AwaitableResult(Task task)
        {
            Task = task ?? throw new ArgumentNullException(nameof(task));
        }

        /// <summary>
        /// Returns the awaiter used by <c>await</c>. Not meant to be called directly - the compiler (or,
        /// for a dynamically-typed <c>await</c>, the C# runtime binder) calls this as part of the await
        /// pattern.
        /// </summary>
        public Awaiter GetAwaiter() => new Awaiter(Task);

        /// <summary>
        /// The awaiter for <see cref="AwaitableResult"/>. Every member is public with a public signature so
        /// the C# runtime binder can resolve them for a dynamically-typed <c>await</c> without ever needing
        /// access to the wrapped task's <c>TResult</c>.
        /// </summary>
        public sealed class Awaiter : ICriticalNotifyCompletion
        {
            private readonly Task _task;

            internal Awaiter(Task task)
            {
                _task = task;
            }

            /// <summary>
            /// <see langword="true"/> once the wrapped task has finished, whether by running to
            /// completion, faulting, or being cancelled.
            /// </summary>
            public bool IsCompleted => _task.IsCompleted;

            /// <summary>
            /// Observes the wrapped task's outcome and returns its result.
            /// </summary>
            /// <returns>
            /// The task's <c>Result</c>, boxed to <see cref="object"/> (<see langword="null"/> if the
            /// wrapped task is a non-generic <see cref="Task"/>, which has no <c>Result</c>).
            /// </returns>
            /// <exception cref="Exception">
            /// The original exception the task faulted with (never wrapped in an
            /// <see cref="AggregateException"/>), or <see cref="OperationCanceledException"/> if the task
            /// was cancelled.
            /// </exception>
            [RequiresUnreferencedCode("Reads the wrapped task's 'Result' property via Type.GetProperty(nameof(Result)) reflection; trimming can remove that property from the task's concrete type.")]
            public object? GetResult()
            {
                // Statically typed against Task, not the task's actual runtime type: the compiler binds
                // GetAwaiter/GetResult at compile time from Task itself, so an inaccessible TResult on the
                // real Task<TResult> instance never enters into it. TaskAwaiter.GetResult() observes the
                // task's outcome exactly like a normal await does - it rethrows the original exception
                // (unwrapped from any AggregateException) on fault, and throws
                // OperationCanceledException/TaskCanceledException on cancellation - but since it is
                // declared to return void, it cannot itself produce the result value.
                _task.GetAwaiter().GetResult();

                // The result, if any, is read via reflection, which is not subject to the caller's
                // accessibility context - PropertyInfo.GetValue can read a public property even when its
                // declared type is not visible to the caller.
                return _task.GetType().GetProperty("Result")?.GetValue(_task);
            }

            /// <summary>
            /// Schedules <paramref name="continuation"/> to run when the wrapped task completes. Delegates
            /// to the wrapped task's own awaiter, so continuation scheduling (including
            /// <see cref="System.Threading.SynchronizationContext"/> capture) behaves exactly like a normal
            /// <c>await</c> on the underlying task would.
            /// </summary>
            /// <param name="continuation">The action to invoke when the task completes.</param>
            public void OnCompleted(Action continuation) => _task.GetAwaiter().OnCompleted(continuation);

            /// <summary>
            /// Like <see cref="OnCompleted"/>, but without flowing
            /// <see cref="System.Threading.ExecutionContext"/> - used by the compiler-generated state
            /// machine when it has already captured what it needs.
            /// </summary>
            /// <param name="continuation">The action to invoke when the task completes.</param>
            public void UnsafeOnCompleted(Action continuation) => _task.GetAwaiter().UnsafeOnCompleted(continuation);
        }
    }
}
