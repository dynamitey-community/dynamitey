using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System;
using System.Reflection;



namespace Dynamitey.DynamicObjects
{
    /// <summary>
    /// Class for TDD, used for mocking any dynamic object
    /// </summary>
   
    public class Mimic : DynamicObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Mimic"/> class.
        /// </summary>
        [RequiresDynamicCode("Constructing a Mimic instantiates System.Dynamic.DynamicObject, whose default constructor requires the DLR's runtime code generation; not supported when AOT-compiled.")]
        public Mimic()
        {
        }

        /// <summary>
        /// Override on DynamicObject
        /// </summary>
        /// <param name="binder"></param>
        /// <param name="arg"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        [UnconditionalSuppressMessage("AOT", "IL3050", Justification =
            "Constructs another Mimic. This is a DynamicObject.TryBinaryOperation override: it " +
            "can't carry [RequiresDynamicCode] itself without mismatching the unannotated base " +
            "member, and the DLR invokes it only after the consumer's own dynamic call site " +
            "already triggered the framework's warning.")]
        public override bool TryBinaryOperation(BinaryOperationBinder binder, object arg, out object? result)
        {
            result = new Mimic();
            return true;
        }

        /// <summary>
        /// Override on DynamicObject
        /// </summary>
        /// <param name="binder"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification =
            "Calls the annotated Dynamic.InvokeConstructor. This is a DynamicObject.TryConvert " +
            "override: it can't carry [RequiresUnreferencedCode] itself without mismatching the " +
            "unannotated base member, and the DLR invokes it only after the consumer's own dynamic " +
            "conversion already triggered the framework's warning.")]
        [UnconditionalSuppressMessage("AOT", "IL3050", Justification =
            "Same Dynamic.InvokeConstructor call as above; see the IL2026 suppression on this member.")]
        [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification =
            "Same DLR-only-caller reasoning as the CA1062 suppression on BaseDictionary.TryGetMember; see that member.")]
        public override bool TryConvert(ConvertBinder binder, out object? result)
        {

            result = Dynamic.InvokeConstructor(binder.ReturnType);
            return true;
        }

        /// <summary>
        /// Override on DynamicObject
        /// </summary>
        /// <param name="binder"></param>
        /// <param name="args"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        [UnconditionalSuppressMessage("AOT", "IL3050", Justification =
            "Constructs another Mimic. This is a DynamicObject override: it can't carry " +
            "[RequiresDynamicCode] itself without mismatching the unannotated base member, " +
            "and the DLR invokes it only after the consumer's own dynamic call site already " +
            "triggered the framework's warning.")]
        public override bool TryCreateInstance(CreateInstanceBinder binder, object?[]? args, out object result)
        {
            result = new Mimic();
            return true;
        }

        /// <summary>
        /// Override on DynamicObject
        /// </summary>
        /// <param name="binder"></param>
        /// <param name="indexes"></param>
        /// <returns></returns>
        public override bool TryDeleteIndex(DeleteIndexBinder binder, object?[]? indexes)
        {
            return true;
        }

        /// <summary>
        /// Override on DynamicObject
        /// </summary>
        /// <param name="binder"></param>
        /// <returns></returns>
        public override bool TryDeleteMember(DeleteMemberBinder binder)
        {
            return true;
        }

        /// <summary>
        /// Override on DynamicObject
        /// </summary>
        /// <param name="binder"></param>
        /// <param name="indexes"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        [UnconditionalSuppressMessage("AOT", "IL3050", Justification =
            "Constructs another Mimic. This is a DynamicObject override: it can't carry " +
            "[RequiresDynamicCode] itself without mismatching the unannotated base member, " +
            "and the DLR invokes it only after the consumer's own dynamic call site already " +
            "triggered the framework's warning.")]
        public override bool TryGetIndex(GetIndexBinder binder, object?[]? indexes, out object result)
        {
            result = new Mimic();
            return true;
        }

        /// <summary>
        /// Override on DynamicObject
        /// </summary>
        /// <param name="binder"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        [UnconditionalSuppressMessage("AOT", "IL3050", Justification =
            "Constructs another Mimic. This is a DynamicObject override: it can't carry " +
            "[RequiresDynamicCode] itself without mismatching the unannotated base member, " +
            "and the DLR invokes it only after the consumer's own dynamic call site already " +
            "triggered the framework's warning.")]
        public override bool TryGetMember(GetMemberBinder binder, out object? result)
        {
            result = new Mimic();
            return true;
        }

        /// <summary>
        /// Override on DynamicObject
        /// </summary>
        /// <param name="binder"></param>
        /// <param name="args"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        [UnconditionalSuppressMessage("AOT", "IL3050", Justification =
            "Constructs another Mimic. This is a DynamicObject override: it can't carry " +
            "[RequiresDynamicCode] itself without mismatching the unannotated base member, " +
            "and the DLR invokes it only after the consumer's own dynamic call site already " +
            "triggered the framework's warning.")]
        public override bool TryInvoke(InvokeBinder binder, object?[]? args, out object? result)
        {
            result = new Mimic();
            return true;
        }

        /// <summary>
        /// Override on DynamicObject
        /// </summary>
        /// <param name="binder"></param>
        /// <param name="args"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        [UnconditionalSuppressMessage("AOT", "IL3050", Justification =
            "Constructs another Mimic. This is a DynamicObject override: it can't carry " +
            "[RequiresDynamicCode] itself without mismatching the unannotated base member, " +
            "and the DLR invokes it only after the consumer's own dynamic call site already " +
            "triggered the framework's warning.")]
        public override bool TryInvokeMember(InvokeMemberBinder binder, object?[]? args, out object? result)
        {
            result = new Mimic();
            return true;
        }

        /// <summary>
        /// Override on DynamicObject
        /// </summary>
        /// <param name="binder"></param>
        /// <param name="indexes"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public override bool TrySetIndex(SetIndexBinder binder, object?[]? indexes, object? value)
        {
            return true;
        }

        /// <summary>
        /// Override on DynamicObject
        /// </summary>
        /// <param name="binder"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public override bool TrySetMember(SetMemberBinder binder, object? value)
        {
            return true;
        }

        /// <summary>
        /// Override on DynamicObject
        /// </summary>
        /// <param name="binder"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        [UnconditionalSuppressMessage("AOT", "IL3050", Justification =
            "Constructs another Mimic. This is a DynamicObject override: it can't carry " +
            "[RequiresDynamicCode] itself without mismatching the unannotated base member, " +
            "and the DLR invokes it only after the consumer's own dynamic call site already " +
            "triggered the framework's warning.")]
        public override bool TryUnaryOperation(UnaryOperationBinder binder, out object? result)
        {
            result = new Mimic();
            return true;
        }

    }
}