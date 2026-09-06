using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Dynamitey.Internal.Optimization;


namespace Dynamitey.DynamicObjects
{
    /// <summary>
    /// Dummy that just returns null or default for everything.
    /// </summary>
   
    public class Dummy:BaseObject
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="Dummy"/> class.
        /// </summary>
        [RequiresDynamicCode("Constructing any BaseObject-derived type instantiates System.Dynamic.DynamicObject, whose default constructor requires the DLR's runtime code generation; not supported when AOT-compiled.")]
        public Dummy()
        {

        }




        /// <summary>
        /// Provides the implementation for operations that get member values. Classes derived from the <see cref="T:System.Dynamic.DynamicObject"/> class can override this method to specify dynamic behavior for operations such as getting a value for a property.
        /// </summary>
        /// <param name="binder">Provides information about the object that called the dynamic operation. The binder.Name property provides the name of the member on which the dynamic operation is performed. For example, for the Console.WriteLine(sampleObject.SampleProperty) statement, where sampleObject is an instance of the class derived from the <see cref="T:System.Dynamic.DynamicObject"/> class, binder.Name returns "SampleProperty". The binder.IgnoreCase property specifies whether the member name is case-sensitive.</param>
        /// <param name="result">The result of the get operation. For example, if the method is called for a property, you can assign the property value to <paramref name="result"/>.</param>
        /// <returns>
        /// true if the operation is successful; otherwise, false. If this method returns false, the run-time binder of the language determines the behavior. (In most cases, a run-time exception is thrown.)
        /// </returns>
        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification =
            "Calls the annotated MassageResultBasedOnInterface. This is a DynamicObject.TryGetMember " +
            "override: it can't carry [RequiresUnreferencedCode] itself without mismatching the " +
            "unannotated base member, and the DLR invokes it only after the consumer's own dynamic " +
            "member access already triggered the framework's warning.")]
        [UnconditionalSuppressMessage("AOT", "IL3050", Justification =
            "Same MassageResultBasedOnInterface call as above; see the IL2026 suppression on this member.")]
        public override bool TryGetMember(System.Dynamic.GetMemberBinder binder, out object? result)
        {
            result = null;
            return this.MassageResultBasedOnInterface(binder.Name, true, ref result);
          
        }

        /// <summary>
        /// Provides the implementation for operations that set member values. Classes derived from the <see cref="T:System.Dynamic.DynamicObject"/> class can override this method to specify dynamic behavior for operations such as setting a value for a property.
        /// </summary>
        /// <param name="binder">Provides information about the object that called the dynamic operation. The binder.Name property provides the name of the member to which the value is being assigned. For example, for the statement sampleObject.SampleProperty = "Test", where sampleObject is an instance of the class derived from the <see cref="T:System.Dynamic.DynamicObject"/> class, binder.Name returns "SampleProperty". The binder.IgnoreCase property specifies whether the member name is case-sensitive.</param>
        /// <param name="value">The value to set to the member. For example, for sampleObject.SampleProperty = "Test", where sampleObject is an instance of the class derived from the <see cref="T:System.Dynamic.DynamicObject"/> class, the <paramref name="value"/> is "Test".</param>
        /// <returns>
        /// true if the operation is successful; otherwise, false. If this method returns false, the run-time binder of the language determines the behavior. (In most cases, a language-specific run-time exception is thrown.)
        /// </returns>
        public override bool TrySetMember(System.Dynamic.SetMemberBinder binder, object? value)
        {
            return true;
        }


        /// <summary>
        /// Provides the implementation for operations that invoke a member. Classes derived from the <see cref="T:System.Dynamic.DynamicObject"/> class can override this method to specify dynamic behavior for operations such as calling a method.
        /// </summary>
        /// <param name="binder">Provides information about the dynamic operation. The binder.Name property provides the name of the member on which the dynamic operation is performed. For example, for the statement sampleObject.SampleMethod(100), where sampleObject is an instance of the class derived from the <see cref="T:System.Dynamic.DynamicObject"/> class, binder.Name returns "SampleMethod". The binder.IgnoreCase property specifies whether the member name is case-sensitive.</param>
        /// <param name="args">The arguments that are passed to the object member during the invoke operation. For example, for the statement sampleObject.SampleMethod(100), where sampleObject is derived from the <see cref="T:System.Dynamic.DynamicObject"/> class, <paramref name="args"/> is equal to 100.</param>
        /// <param name="result">The result of the member invocation.</param>
        /// <returns>
        /// true if the operation is successful; otherwise, false. If this method returns false, the run-time binder of the language determines the behavior. (In most cases, a language-specific run-time exception is thrown.)
        /// </returns>
        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification =
            "Calls the annotated MassageResultBasedOnInterface. This is a DynamicObject." +
            "TryInvokeMember override: it can't carry [RequiresUnreferencedCode] itself without " +
            "mismatching the unannotated base member, and the DLR invokes it only after the " +
            "consumer's own dynamic call site already triggered the framework's warning.")]
        [UnconditionalSuppressMessage("AOT", "IL3050", Justification =
            "Same MassageResultBasedOnInterface call as above; see the IL2026 suppression on this member.")]
        public override bool TryInvokeMember(System.Dynamic.InvokeMemberBinder binder, object?[]? args, out object? result)
        {

            result = null;
            return this.MassageResultBasedOnInterface(binder.Name, true, ref result);
        }

      

        /// <summary>
        /// Tries the index of the get.
        /// </summary>
        /// <param name="binder">The binder.</param>
        /// <param name="indexes">The indexes.</param>
        /// <param name="result">The result.</param>
        /// <returns></returns>
        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification =
            "Calls the annotated MassageResultBasedOnInterface. This is a DynamicObject.TryGetIndex " +
            "override: it can't carry [RequiresUnreferencedCode] itself without mismatching the " +
            "unannotated base member, and the DLR invokes it only after the consumer's own dynamic " +
            "indexer access already triggered the framework's warning.")]
        [UnconditionalSuppressMessage("AOT", "IL3050", Justification =
            "Same MassageResultBasedOnInterface call as above; see the IL2026 suppression on this member.")]
        public override bool TryGetIndex(System.Dynamic.GetIndexBinder binder, object?[]? indexes, out object result)
        {
            // See Get.TryGetIndex for why this bridges through a nullable local: TryGetIndex's
            // own result is non-nullable (matching DynamicObject's base signature) but
            // MassageResultBasedOnInterface takes `ref object?`.
            object? tResult = null;
            var success = this.MassageResultBasedOnInterface(Invocation.IndexBinderName, true, ref tResult);
            result = tResult!;
            return success;
        }

        /// <summary>
        /// Tries the index of the set.
        /// </summary>
        /// <param name="binder">The binder.</param>
        /// <param name="indexes">The indexes.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public override bool TrySetIndex(System.Dynamic.SetIndexBinder binder, object?[]? indexes, object? value)
        {
            return true;
        }
    }
}
