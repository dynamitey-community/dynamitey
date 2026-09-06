// 
//  Copyright 2011 Ekon Benefits
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

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using Dynamitey.Internal.Optimization;

using System.Reflection;

namespace Dynamitey.Internal
{
    /// <summary>
    /// Internal Implementation of <see cref="Dynamic.Curry(object,System.Nullable{int})"/>
    /// </summary>
    public class Curry : DynamicObject, IPartialApply
        {

            /// <summary>
            /// Pipe argument (left side) into curried function (right side)
            /// </summary>
            /// <param name="argument">The argument.</param>
            /// <param name="function">The function.</param>
            /// <returns></returns>
            [RequiresUnreferencedCode("Invokes function through 'dynamic', resolved via the DLR binder; trimming can remove the member being resolved.")]
            [RequiresDynamicCode("The 'dynamic' invocation binds through the DLR, which requires runtime code generation; not supported when AOT-compiled.")]
            public static dynamic operator |(dynamic argument, Curry function)
            {
                return ((dynamic)function)(argument);
            }

            private readonly object _target;
            private readonly int? _totalArgCount;


            [RequiresDynamicCode("Constructing a Curry instantiates System.Dynamic.DynamicObject, whose default constructor requires the DLR's runtime code generation; not supported when AOT-compiled.")]
            internal Curry(object target, int? totalArgCount=null)
             {
                 _target = target;
                _totalArgCount = totalArgCount;
             }



            /// <summary>
            /// Provides implementation for binary operations. Classes derived from the <see cref="T:System.Dynamic.DynamicObject" /> class can override this method to specify dynamic behavior for operations such as addition and multiplication.
            /// </summary>
            /// <param name="binder">Provides information about the binary operation. The binder.Operation property returns an <see cref="T:System.Linq.Expressions.ExpressionType" /> object. For example, for the sum = first + second statement, where first and second are derived from the DynamicObject class, binder.Operation returns ExpressionType.Add.</param>
            /// <param name="arg">The right operand for the binary operation. For example, for the sum = first + second statement, where first and second are derived from the DynamicObject class, <paramref name="arg" /> is equal to second.</param>
            /// <param name="result">The result of the binary operation.</param>
            /// <returns>
            /// true if the operation is successful; otherwise, false. If this method returns false, the run-time binder of the language determines the behavior. (In most cases, a language-specific run-time exception is thrown.)
            /// </returns>
            [UnconditionalSuppressMessage("Trimming", "IL2026", Justification =
                "Invokes 'this' through 'dynamic' (DLR). This is a DynamicObject.TryBinaryOperation " +
                "override: it can't carry [RequiresUnreferencedCode] itself without mismatching the " +
                "unannotated base member, and the DLR invokes it only after the consumer's own dynamic " +
                "call site already triggered the framework's warning.")]
            [UnconditionalSuppressMessage("AOT", "IL3050", Justification =
                "Same DLR invocation as above; see the IL2026 suppression on this member.")]
            public override bool TryBinaryOperation(BinaryOperationBinder binder, object arg, out object result)
            {
                result = null;
                if (binder.Operation == ExpressionType.LeftShift)
                {
                    result =((dynamic)(this))(arg);
                    return true;
                }
                return false;
            }

            /// <summary>
            /// Provides implementation for type conversion operations. Classes derived from the <see cref="T:System.Dynamic.DynamicObject"/> class can override this method to specify dynamic behavior for operations that convert an object from one type to another.
            /// </summary>
            /// <param name="binder">Provides information about the conversion operation. The binder.Type property provides the type to which the object must be converted. For example, for the statement (String)sampleObject in C# (CType(sampleObject, Type) in Visual Basic), where sampleObject is an instance of the class derived from the <see cref="T:System.Dynamic.DynamicObject"/> class, binder.Type returns the <see cref="T:System.String"/> type. The binder.Explicit property provides information about the kind of conversion that occurs. It returns true for explicit conversion and false for implicit conversion.</param>
            /// <param name="result">The result of the type conversion operation.</param>
            /// <returns>
            /// true if the operation is successful; otherwise, false. If this method returns false, the run-time binder of the language determines the behavior. (In most cases, a language-specific run-time exception is thrown.)
            /// </returns>
            [UnconditionalSuppressMessage("Trimming", "IL2026", Justification =
                "Calls the annotated Dynamic.CoerceToDelegate. This is a DynamicObject.TryConvert " +
                "override: it can't carry [RequiresUnreferencedCode] itself without mismatching the " +
                "unannotated base member, and the DLR invokes it only after the consumer's own dynamic " +
                "conversion already triggered the framework's warning.")]
            [UnconditionalSuppressMessage("AOT", "IL3050", Justification =
                "Same Dynamic.CoerceToDelegate call as above; see the IL2026 suppression on this member.")]
            public override bool TryConvert(ConvertBinder binder, out object result)
            {
                result = Dynamic.CoerceToDelegate(this, binder.Type);

                return result != null;
            }


#if SILVERLIGHT5

        /// <summary>
        /// Gets the custom Type.
        /// </summary>
        /// <returns></returns>
        public Type GetCustomType()
        {
            return this.GetDynamicCustomType();
        }
#endif

            /// <summary>
            /// Provides the implementation for operations that invoke a member. Classes derived from the <see cref="T:System.Dynamic.DynamicObject"/> class can override this method to specify dynamic behavior for operations such as calling a method.
            /// </summary>
            /// <param name="binder">Provides information about the dynamic operation. The binder.Name property provides the name of the member on which the dynamic operation is performed. For example, for the statement sampleObject.SampleMethod(100), where sampleObject is an instance of the class derived from the <see cref="T:System.Dynamic.DynamicObject"/> class, binder.Name returns "SampleMethod". The binder.IgnoreCase property specifies whether the member name is case-sensitive.</param>
            /// <param name="args">The arguments that are passed to the object member during the invoke operation. For example, for the statement sampleObject.SampleMethod(100), where sampleObject is derived from the <see cref="T:System.Dynamic.DynamicObject"/> class, <paramref name="args"/>[0] is equal to 100.</param>
            /// <param name="result">The result of the member invocation.</param>
            /// <returns>
            /// true if the operation is successful; otherwise, false. If this method returns false, the run-time binder of the language determines the behavior. (In most cases, a language-specific run-time exception is thrown.)
            /// </returns>
           [UnconditionalSuppressMessage("Trimming", "IL2026", Justification =
               "Constructs the annotated PartialApply. This is a DynamicObject.TryInvokeMember " +
               "override: it can't carry [RequiresUnreferencedCode] itself without mismatching the " +
               "unannotated base member, and the DLR invokes it only after the consumer's own dynamic " +
               "call site already triggered the framework's warning.")]
           [UnconditionalSuppressMessage("AOT", "IL3050", Justification =
               "Same PartialApply construction as above; see the IL2026 suppression on this member.")]
           public override bool  TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
           {
               result = new PartialApply(_target, Util.NameArgsIfNecessary(binder.CallInfo, args), binder.Name, _totalArgCount);
               return true;
           }
           /// <summary>
           /// Provides the implementation for operations that invoke an object. Classes derived from the <see cref="T:System.Dynamic.DynamicObject"/> class can override this method to specify dynamic behavior for operations such as invoking an object or a delegate.
           /// </summary>
           /// <param name="binder">Provides information about the invoke operation.</param>
           /// <param name="args">The arguments that are passed to the object during the invoke operation. For example, for the sampleObject(100) operation, where sampleObject is derived from the <see cref="T:System.Dynamic.DynamicObject"/> class, <paramref name="args"/>[0] is equal to 100.</param>
           /// <param name="result">The result of the object invocation.</param>
           /// <returns>
           /// true if the operation is successful; otherwise, false. If this method returns false, the run-time binder of the language determines the behavior. (In most cases, a language-specific run-time exception is thrown.
           /// </returns>
            [UnconditionalSuppressMessage("Trimming", "IL2026", Justification =
                "Constructs the annotated PartialApply and invokes the result through 'dynamic'. This " +
                "is a DynamicObject.TryInvoke override: it can't carry [RequiresUnreferencedCode] " +
                "itself without mismatching the unannotated base member, and the DLR invokes it only " +
                "after the consumer's own dynamic call site already triggered the framework's warning.")]
            [UnconditionalSuppressMessage("AOT", "IL3050", Justification =
                "Same PartialApply construction/invocation as above; see the IL2026 suppression on this member.")]
            public override bool  TryInvoke(InvokeBinder binder, object[] args, out object result)
            {
                var tCurrying = _target as PartialApply;

                
               var  curryResult = tCurrying != null
                             //If already currying append
                             ? new PartialApply(tCurrying.Target,
                                            tCurrying.Args.Concat(Util.NameArgsIfNecessary(binder.CallInfo, args)).
                                                ToArray(), tCurrying.MemberName, tCurrying.TotalArgCount, tCurrying.InvocationKind)
                             : new PartialApply(_target, Util.NameArgsIfNecessary(binder.CallInfo, args), String.Empty, _totalArgCount);


               result = curryResult;
               if (args.Length == curryResult.TotalArgCount)
                   result= ((dynamic) curryResult)();
               return true;
           }
        }


     

}
