using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Linq;
using Dynamitey.Internal.Optimization;
using Microsoft.CSharp.RuntimeBinder;
using Dynamitey.Internal.Compat;
using System.Reflection;

namespace Dynamitey.Internal
{
    /// <summary>
    /// Internal class implmenation for <see cref="Dynamic.InvokeSetAll"/>
    /// </summary>
    public class InvokeSetters : DynamicObject
    {
        [RequiresDynamicCode("Constructing an InvokeSetters instantiates System.Dynamic.DynamicObject, whose default constructor requires the DLR's runtime code generation; not supported when AOT-compiled.")]
        internal InvokeSetters()
        {

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
        [UnconditionalSuppressMessage("Trimming", "IL2075", Justification =
            "Reflects over a caller-supplied enumerable's element type (GetInterfaces) and an " +
            "anonymous-typed argument's properties (GetProperties) to build the property/value " +
            "dictionary that Dynamic.InvokeSetChain then applies; both are on this method's own " +
            "runtime-typed arguments, whose members trimming may already have removed.")]
        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification =
            "Reads Tuple<,>.Item1/Item2 via 'dynamic' and calls the annotated Dynamic.InvokeGet/ " +
            "InvokeSetChain. This is a DynamicObject.TryInvoke override: it can't carry " +
            "[RequiresUnreferencedCode] itself without mismatching the unannotated base member, and " +
            "the DLR invokes it only after the consumer's own dynamic call to Dynamic.InvokeSetAll " +
            "already triggered the framework's warning.")]
        [UnconditionalSuppressMessage("AOT", "IL3050", Justification =
            "Same 'dynamic'/Dynamic.InvokeGet/InvokeSetChain calls as above; see the IL2026 suppression on this member.")]
        public override bool TryInvoke(InvokeBinder binder, object?[]? rawArgs, out object? result)
        {
            IEnumerable<KeyValuePair<string, object>>? tDict = null;
            object? target = null;
            result = null;
            // rawArgs is null only per DynamicObject.TryInvoke's own (rarely-null-in-practice)
            // signature; the DLR always supplies a real array, matching every other Try* override
            // in this codebase that forwards args without an explicit null check.
            var args = rawArgs!;

            //Setup Properties as dictionary
            if (binder.CallInfo.ArgumentNames.Any())
            {

                if (binder.CallInfo.ArgumentNames.Count + 1 == binder.CallInfo.ArgumentCount)
                {
                    target = args.First();
                    // The DLR can hand a null named-argument value; the dictionary's value type
                    // stays non-null to match the other branches below, exactly as this untyped
                    // code already did before nullable was enabled.
                    tDict = binder.CallInfo.ArgumentNames
                        .Zip(args.Skip(1), (key, value) => new { key, value })
                        .ToDictionary(k => k.key, v => v.value!);

                }else
                {
                    throw new RuntimeBinderException("InvokeSetAll requires first parameter to be target unamed, and all other parameters to be named.");
                }
            }
            else if (args.Length == 2)
            {
                target = args[0];
                if (args[1] is IEnumerable<KeyValuePair<string, object>>)
                {
                    tDict = (IEnumerable<KeyValuePair<string, object>>)args[1]!;
                }
                else if (args[1] is IEnumerable
                        && args[1]!.GetType().GetTypeInfo().IsGenericType
                    )
                {
                    var tEnumerableArg = (IEnumerable)args[1]!;

                    var tInterface = tEnumerableArg.GetType().GetTypeInfo().GetInterfaces().FirstOrDefault(it=>it.Name =="IEnumerable`1");
                    if(tInterface !=null)
                    {
                        var tParamTypes = tInterface.GetTypeInfo().GetGenericArguments();
                        if(tParamTypes.Length ==1
                            && tParamTypes[0].GetGenericTypeDefinition() == typeof(Tuple<,>))
                        {
                           tDict= tEnumerableArg.Cast<dynamic>().ToDictionary(k => (string) k.Item1, v => (object) v.Item2);
                        }
                    }
                }
                else if (Util.IsAnonymousType(args[1]!))
                {
                    var keyDict = new Dictionary<string, object>();
                    foreach (var tProp in args[1]!.GetType().GetTypeInfo().GetProperties())
                    {
                        keyDict[tProp.Name] = Dynamic.InvokeGet(args[1]!, tProp.Name)!;
                    }
                    tDict = keyDict;
                }
            }
            //Invoke all properties
            if (target != null && tDict != null)
            {
                foreach (var tPair in tDict)
                {
                    Dynamic.InvokeSetChain(target, tPair.Key, tPair.Value);
                }
                result = target;
                return true;
            }
            return false;
        }
    }
}
