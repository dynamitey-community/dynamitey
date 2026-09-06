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
using System.Reflection;
using System.Text;
using Dynamitey.Internal.Optimization;
using Microsoft.CSharp;
using Microsoft.CSharp.RuntimeBinder;

namespace Dynamitey.DynamicObjects
{

    /// <summary>
    /// An proxy object
    /// </summary>
    public interface IForwarder
    {
        /// <summary>
        /// Gets the target.
        /// </summary>
        /// <value>The target.</value>
        object? Target { get; }
    }

    /// <summary>
    /// Proxies Calls allows subclasser to override do extra actions before or after base invocation
    /// </summary>
    /// <remarks>
    /// This may not be as efficient as other proxies that can work on just static objects or just dynamic objects...
    /// Consider this when using.
    /// </remarks>

    public abstract class BaseForwarder : BaseObject, IForwarder
    {

        /// <summary>
        /// Marks whether we are adding or removing the delegate
        /// </summary>
        public class AddRemoveMarker
        {
            /// <summary>
            /// Implements the operator +.
            /// </summary>
            /// <param name="left">The left.</param>
            /// <param name="right">The right.</param>
            /// <returns>The result of the operator.</returns>
            public static AddRemoveMarker operator +(AddRemoveMarker left, object right)
            {
                #if NET
                ArgumentNullException.ThrowIfNull(left);
                #else
                if (left is null) throw new ArgumentNullException(nameof(left));
                #endif

                left.Delegate = right;
                left.IsAdding = true;

                return left;
            }

            /// <summary>
            /// Implements the operator -.
            /// </summary>
            /// <param name="left">The left.</param>
            /// <param name="right">The right.</param>
            /// <returns>The result of the operator.</returns>
            public static AddRemoveMarker operator -(AddRemoveMarker left, object right)
            {
                #if NET
                ArgumentNullException.ThrowIfNull(left);
                #else
                if (left is null) throw new ArgumentNullException(nameof(left));
                #endif

                left.Delegate = right;
                left.IsAdding = false;

                return left;
            }

            /// <summary>
            /// Gets or sets the delegate.
            /// </summary>
            /// <value>The delegate.</value>
           
            public object? Delegate { get; protected set; }

            /// <summary>
            /// Gets or sets a value indicating whether this instance is adding.
            /// </summary>
            /// <value><c>true</c> if this instance is adding; otherwise, <c>false</c>.</value>
           
            public bool IsAdding { get; protected set; }

        }
        /// <summary>
        /// Initializes a new instance of the <see cref="BaseForwarder"/> class.
        /// </summary>
        /// <param name="target">The target.</param>
        [RequiresDynamicCode("Constructing any BaseForwarder-derived type instantiates System.Dynamic.DynamicObject, whose default constructor requires the DLR's runtime code generation; not supported when AOT-compiled.")]
        protected BaseForwarder(object? target)
        {
            Target = target;
        }

        /// <summary>
        /// Returns the enumeration of all dynamic member names.
        /// </summary>
        /// <returns>
        /// A sequence that contains dynamic member names.
        /// </returns>
        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification =
            "Calls the annotated Dynamic.GetMemberNames. This is a DynamicObject." +
            "GetDynamicMemberNames override: it can't carry [RequiresUnreferencedCode] itself " +
            "without mismatching the unannotated base member, and the DLR invokes it only after " +
            "the consumer's own dynamic member enumeration already triggered the framework's warning.")]
        [UnconditionalSuppressMessage("AOT", "IL3050", Justification =
            "Same Dynamic.GetMemberNames calls as above; see the IL2026 suppression on this member.")]
        public override IEnumerable<string> GetDynamicMemberNames()
        {
           
                var tDyanmic = Dynamic.GetMemberNames(CallTarget!, dynamicOnly: true);
                if (!tDyanmic.Any())
                {
                    return Dynamic.GetMemberNames(CallTarget!);
                }
            
            return base.GetDynamicMemberNames();
        }


        /// <summary>
        /// Gets or sets the target.
        /// </summary>
        /// <value>The target.</value>
         
       
        protected object? Target {  get;  set; }

        object? IForwarder.Target => Target;

        /// <summary>
        /// Gets the call target.
        /// </summary>
        /// <value>The call target.</value>
        protected virtual object? CallTarget => Target;

        /// <summary>
        /// Provides the implementation for operations that get member values. Classes derived from the <see cref="T:System.Dynamic.DynamicObject"/> class can override this method to specify dynamic behavior for operations such as getting a value for a property.
        /// </summary>
        /// <param name="binder">Provides information about the object that called the dynamic operation. The binder.Name property provides the name of the member on which the dynamic operation is performed. For example, for the Console.WriteLine(sampleObject.SampleProperty) statement, where sampleObject is an instance of the class derived from the <see cref="T:System.Dynamic.DynamicObject"/> class, binder.Name returns "SampleProperty". The binder.IgnoreCase property specifies whether the member name is case-sensitive.</param>
        /// <param name="result">The result of the get operation. For example, if the method is called for a property, you can assign the property value to <paramref name="result"/>.</param>
        /// <returns>
        /// true if the operation is successful; otherwise, false. If this method returns false, the run-time binder of the language determines the behavior. (In most cases, a run-time exception is thrown.)
        /// </returns>
        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification =
            "Calls the annotated Dynamic.InvokeIsEvent/InvokeGet. This is a DynamicObject." +
            "TryGetMember override: it can't carry [RequiresUnreferencedCode] itself without " +
            "mismatching the unannotated base member, and the DLR invokes it only after the " +
            "consumer's own dynamic member access already triggered the framework's warning.")]
        [UnconditionalSuppressMessage("AOT", "IL3050", Justification =
            "Same Dynamic.InvokeIsEvent/InvokeGet calls as above; see the IL2026 suppression on this member.")]
        [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification =
            "Same DLR-only-caller reasoning as the CA1062 suppression on BaseDictionary.TryGetMember; see that member.")]
        public override bool TryGetMember(GetMemberBinder binder, out object? result)
        {
            if (CallTarget == null)
            {
                result = null;
                return false;
            }

            if (Dynamic.InvokeIsEvent(CallTarget, binder.Name))
            {
                result = new AddRemoveMarker();
                return true;
            }

            try
            {
                result = Dynamic.InvokeGet(CallTarget, binder.Name);
            }
            catch (RuntimeBinderException)
            {
                result = null;
                return false;
            }

            return true;

        }
#pragma warning disable 1734
        /// <summary>
        /// Provides the implementation for operations that invoke an object. Classes derived from the <see cref="T:System.Dynamic.DynamicObject" /> class can override this method to specify dynamic behavior for operations such as invoking an object or a delegate.
        /// </summary>
        /// <param name="binder">Provides information about the invoke operation.</param>
        /// <param name="args">The arguments that are passed to the object during the invoke operation. For example, for the sampleObject(100) operation, where sampleObject is derived from the <see cref="T:System.Dynamic.DynamicObject" /> class, <paramref name="args[0]" /> is equal to 100.</param>
        /// <param name="result">The result of the object invocation.</param>
        /// <returns>
        /// true if the operation is successful; otherwise, false. If this method returns false, the run-time binder of the language determines the behavior. (In most cases, a language-specific run-time exception is thrown.
        /// </returns>
#pragma warning restore 1734
        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification =
            "Calls the annotated Dynamic.Invoke/InvokeAction. This is a DynamicObject.TryInvoke " +
            "override: it can't carry [RequiresUnreferencedCode] itself without mismatching the " +
            "unannotated base member, and the DLR invokes it only after the consumer's own dynamic " +
            "call site already triggered the framework's warning.")]
        [UnconditionalSuppressMessage("AOT", "IL3050", Justification =
            "Same Dynamic.Invoke/InvokeAction calls as above; see the IL2026 suppression on this member.")]
        [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification =
            "Same DLR-only-caller reasoning as the CA1062 suppression on BaseDictionary.TryGetMember; see that member.")]
        public override bool TryInvoke(InvokeBinder binder, object?[]? args, out object? result)
        {
            if (CallTarget == null)
            {
                result = null;
                return false;
            }

            var tArgs = Util.NameArgsIfNecessary(binder.CallInfo, args!);

            try
            {
                result = Dynamic.Invoke(CallTarget, tArgs);

            }
            catch (RuntimeBinderException)
            {
                result = null;
                try
                {
                    Dynamic.InvokeAction(CallTarget, tArgs);
                }
                catch (RuntimeBinderException)
                {

                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Tries the invoke member.
        /// </summary>
        /// <param name="binder">The binder.</param>
        /// <param name="args">The args.</param>
        /// <param name="result">The result.</param>
        /// <returns></returns>
        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification =
            "Calls the annotated Dynamic.InvokeGet (to read the binder's own generic-arguments " +
            "property by name) and Dynamic.InvokeMember/InvokeMemberAction. This is a " +
            "DynamicObject.TryInvokeMember override: it can't carry [RequiresUnreferencedCode] " +
            "itself without mismatching the unannotated base member, and the DLR invokes it only " +
            "after the consumer's own dynamic call site already triggered the framework's warning.")]
        [UnconditionalSuppressMessage("AOT", "IL3050", Justification =
            "Same Dynamic.InvokeGet/InvokeMember/InvokeMemberAction calls as above; see the IL2026 suppression on this member.")]
        [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification =
            "Same DLR-only-caller reasoning as the CA1062 suppression on BaseDictionary.TryGetMember; see that member.")]
        public override bool TryInvokeMember(InvokeMemberBinder binder, object?[]? args, out object? result)
        {
            if (CallTarget == null)
            {
                result = null;
                return false;
            }

            object?[] tArgs = Util.NameArgsIfNecessary(binder.CallInfo, args!);


            Type[]? types = null;

            try
            { 
                //.net core
                // Try and pull generic arguments from binder
                IList<Type>? typeList = Dynamic.InvokeGet(binder,
                    "TypeArguments");
                if (typeList != null)
                {
                    types = typeList.ToArray();
                }

            }
            catch (RuntimeBinderException)
            {
                types = null;
            }

            if (types == null)
            {
                try
                { 
                    //.net 4.0
                    // Try and pull generic arguments from binder
                    IList<Type>? typeList = Dynamic.InvokeGet(binder,
                        "Microsoft.CSharp.RuntimeBinder.ICSharpInvokeOrInvokeMemberBinder.TypeArguments");
                    if (typeList != null)
                    {

                        types = typeList.ToArray();

                    }

                }
                catch (RuntimeBinderException)
                {
                    types = null;
                }
            }

            var name = InvokeMemberName.Create;
            var fullName = name(binder.Name, types);
            try
            {
                result = Dynamic.InvokeMember(CallTarget, fullName, tArgs);
               
            }
            catch (RuntimeBinderException)
            {
                result = null;
                try
                {
                    Dynamic.InvokeMemberAction(CallTarget, fullName, tArgs);
                }
                catch (RuntimeBinderException)
                {

                    return false;
                }
            }
            return true;
        }



        /// <summary>
        /// Tries the set member.
        /// </summary>
        /// <param name="binder">The binder.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification =
            "Calls the annotated Dynamic.InvokeIsEvent/InvokeAddAssignMember/InvokeSubtractAssignMember/" +
            "InvokeSet. This is a DynamicObject.TrySetMember override: it can't carry " +
            "[RequiresUnreferencedCode] itself without mismatching the unannotated base member, and " +
            "the DLR invokes it only after the consumer's own dynamic member assignment already " +
            "triggered the framework's warning.")]
        [UnconditionalSuppressMessage("AOT", "IL3050", Justification =
            "Same Dynamic.* calls as above; see the IL2026 suppression on this member.")]
        [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification =
            "Same DLR-only-caller reasoning as the CA1062 suppression on BaseDictionary.TryGetMember; see that member.")]
        public override bool TrySetMember(SetMemberBinder binder, object? value)
        {
            if (CallTarget == null)
            {
                return false;
            }

            if (Dynamic.InvokeIsEvent(CallTarget, binder.Name) && value is AddRemoveMarker arm)
            {
      

                if (arm.IsAdding)
                {
                    Dynamic.InvokeAddAssignMember(CallTarget, binder.Name, arm.Delegate);
                }
                else
                {
                    Dynamic.InvokeSubtractAssignMember(CallTarget, binder.Name, arm.Delegate);
                }

                return true;
            }

            try
            {
                Dynamic.InvokeSet(CallTarget, binder.Name, value);

                return true;
            }
            catch (RuntimeBinderException)
            {
                return false;
            }
        }

        /// <summary>
        /// Tries the index of the get.
        /// </summary>
        /// <param name="binder">The binder.</param>
        /// <param name="indexes">The indexes.</param>
        /// <param name="result">The result.</param>
        /// <returns></returns>
        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification =
            "Calls the annotated Dynamic.InvokeGetIndex. This is a DynamicObject.TryGetIndex " +
            "override: it can't carry [RequiresUnreferencedCode] itself without mismatching the " +
            "unannotated base member, and the DLR invokes it only after the consumer's own dynamic " +
            "indexer access already triggered the framework's warning.")]
        [UnconditionalSuppressMessage("AOT", "IL3050", Justification =
            "Same Dynamic.InvokeGetIndex call as above; see the IL2026 suppression on this member.")]
        [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification =
            "Same DLR-only-caller reasoning as the CA1062 suppression on BaseDictionary.TryGetMember; see that member.")]
        public override bool TryGetIndex(GetIndexBinder binder, object?[]? indexes, out object result)
        {
            if (CallTarget == null)
            {
                result = null!;
                return false;
            }

            object?[] tArgs = Util.NameArgsIfNecessary(binder.CallInfo, indexes!);

            try
            {
                // DynamicObject.TryGetIndex declares result non-nullable even though an indexer
                // can genuinely return null; matching upstream rather than the real value space.
                result = Dynamic.InvokeGetIndex(CallTarget, tArgs)!;
                return true;
            }
            catch (RuntimeBinderException)
            {
                result = null!;
                return false;
            }
        }

        /// <summary>
        /// Tries the index of the set.
        /// </summary>
        /// <param name="binder">The binder.</param>
        /// <param name="indexes">The indexes.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification =
            "Calls the annotated Dynamic.InvokeSetIndex. This is a DynamicObject.TrySetIndex " +
            "override: it can't carry [RequiresUnreferencedCode] itself without mismatching the " +
            "unannotated base member, and the DLR invokes it only after the consumer's own dynamic " +
            "indexer assignment already triggered the framework's warning.")]
        [UnconditionalSuppressMessage("AOT", "IL3050", Justification =
            "Same Dynamic.InvokeSetIndex call as above; see the IL2026 suppression on this member.")]
        [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification =
            "Same DLR-only-caller reasoning as the CA1062 suppression on BaseDictionary.TryGetMember; see that member.")]
        public override bool TrySetIndex(SetIndexBinder binder, object?[]? indexes, object? value)
        {
            if (CallTarget == null)
            {
                return false;
            }

            var tCombinedArgs = indexes!.Concat(new[] { value }).ToArray();
            object?[] tArgs = Util.NameArgsIfNecessary(binder.CallInfo, tCombinedArgs);
            try
            {


                Dynamic.InvokeSetIndex(CallTarget, tArgs);
                return true;
            }
            catch (RuntimeBinderException)
            {
                return false;
            }
        }


        /// <summary>
        /// Equals the specified other.
        /// </summary>
        /// <param name="other">The other.</param>
        /// <returns></returns>
        public bool Equals(BaseForwarder? other)
        {
            if (ReferenceEquals(null, other)) return ReferenceEquals(null, CallTarget);
            if (ReferenceEquals(this, other)) return true;
            return Equals(other.CallTarget, CallTarget);
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object" /> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object" /> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return ReferenceEquals(null, CallTarget);
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof (BaseForwarder)) return false;
            return Equals((BaseForwarder) obj);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            return (CallTarget != null ? CallTarget.GetHashCode() : 0);
        }

    
    }
}
