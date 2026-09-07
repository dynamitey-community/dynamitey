using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Linq;
using System.Reflection;

using System.Text;
using Dynamitey.Internal.Optimization;


namespace Dynamitey.DynamicObjects
{
    /// <summary>
    /// Late bind types from libraries not not at compile type
    /// </summary>
    public class LateType:BaseForwarder
    {


        /// <summary>
        /// Exception When The Late Type can not be found to bind.
        /// </summary>
        [SuppressMessage("Usage", "RCS1194:Implement exception constructors",
            Justification = "The standard set includes a parameterless constructor and a (string message) one. "
                + "Neither fits: this exception exists to name the type that could not be found, and its single "
                + "string parameter is that type name, not a message - it is formatted into one. A parameterless "
                + "overload could only produce a message with no type in it, which is worse than not offering it.")]
        [SuppressMessage("Design", "CA1032:Implement standard exception constructors", Justification =
            "Same reasoning as the RCS1194 suppression immediately above - CA1032 wants exactly the " +
            "parameterless and (string message) constructors that suppression already explains are the " +
            "wrong shape for this exception.")]
        [SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification =
            "See AwaitableResult.Awaiter; identical reasoning. MissingTypeException's identity only makes " +
            "sense next to LateType, the class whose lookups throw it.")]
        public class MissingTypeException:Exception
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="MissingTypeException" /> class.
            /// </summary>
            /// <param name="typename">The typename.</param>
             public MissingTypeException(string typename)
                 : base(String.Format(System.Globalization.CultureInfo.InvariantCulture, "Could Not Find Type. {0}", typename))
             {

             }

             /// <summary>
             /// Initializes a new instance of the <see cref="MissingTypeException" /> class.
             /// </summary>
             /// <param name="message">The message.</param>
             /// <param name="innerException">The inner exception.</param>
            public MissingTypeException(string message, Exception innerException) : base(message, innerException)
            {
                
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LateType"/> class.
        /// </summary>
        /// <param name="type">The type.</param>
        [RequiresDynamicCode("Constructing any BaseForwarder-derived type instantiates System.Dynamic.DynamicObject, whose default constructor requires the DLR's runtime code generation; not supported when AOT-compiled.")]
        public LateType(Type type)
            : base(type)
        {

        }

        private readonly string? TypeName;


        [RequiresUnreferencedCode("Resolves typeName via Assembly.GetType/Type.GetType, both name-based type lookups the trimmer cannot see; a type this depends on can be removed. Returns null instead of throwing when the type can't be found.")]
        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification =
            "Same type-probe reasoning as Dynamic.ProbeComObjectType (Dynamic.cs): throwOnError:false " +
            "only suppresses the \"not found\" case, and Assembly.GetType/Type.GetType can still throw " +
            "ArgumentException, FileNotFoundException, FileLoadException, or BadImageFormatException " +
            "for other resolution failures. This method's own documented contract is to return null " +
            "for any of them, not just the one - narrowing the catch would break that contract.")]
        public static Type? FindType(string typeName, Assembly? assembly = null)
        {
            try
            {
                if (assembly != null)
                {
                    return assembly.GetType(typeName, false);
                }
                return Type.GetType(typeName, false);
            }
            catch
            {
                return null;
            }
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="LateType"/> class.
        /// </summary>
        /// <param name="typeName">Qualified Name of the type.</param>
        [RequiresUnreferencedCode("Calls FindType, which resolves typeName via Assembly.GetType/Type.GetType, a name-based type lookup the trimmer cannot see.")]
        [RequiresDynamicCode("Constructing any BaseForwarder-derived type instantiates System.Dynamic.DynamicObject, whose default constructor requires the DLR's runtime code generation; not supported when AOT-compiled.")]
        public LateType(string typeName)
            : base(FindType(typeName))
        {
            TypeName = typeName;
          
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LateType" /> class.
        /// </summary>
        /// <param name="assembly">The assembly.</param>
        /// <param name="typeName">Name of the type.</param>
        [RequiresUnreferencedCode("Calls FindType, which resolves typeName via Assembly.GetType/Type.GetType, a name-based type lookup the trimmer cannot see.")]
        [RequiresDynamicCode("Constructing any BaseForwarder-derived type instantiates System.Dynamic.DynamicObject, whose default constructor requires the DLR's runtime code generation; not supported when AOT-compiled.")]
        public LateType(Assembly assembly, string typeName)
            : base(FindType(typeName, assembly))
        {
            TypeName = typeName;

        }

        /// <summary>
        /// Returns a late bound constructor
        /// </summary>
        /// <value>The late bound constructor</value>
        public dynamic @new
        {
            [RequiresDynamicCode("Constructing the returned ConstructorForward instantiates System.Dynamic.DynamicObject, whose default constructor requires the DLR's runtime code generation; not supported when AOT-compiled.")]
            get => new ConstructorForward((Type)Target!);
        }

        /// <summary>
        /// Forward argument to constructor including named arguments
        /// </summary>
        [SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification =
            "See AwaitableResult.Awaiter; identical reasoning. ConstructorForward must be public because it " +
            "overrides DynamicObject.TryInvoke, a public member.")]
        public class ConstructorForward:DynamicObject
        {
            private readonly Type _type;
            [RequiresDynamicCode("Constructing any DynamicObject-derived type instantiates System.Dynamic.DynamicObject, whose default constructor requires the DLR's runtime code generation; not supported when AOT-compiled.")]
            internal ConstructorForward(Type type)
            {
                _type = type;
            }
            /// <summary>
            /// Tries the invoke.
            /// </summary>
            /// <param name="binder">The binder.</param>
            /// <param name="args">The args.</param>
            /// <param name="result">The result.</param>
            /// <returns></returns>
            [UnconditionalSuppressMessage("Trimming", "IL2026", Justification =
                "Calls the annotated Dynamic.InvokeConstructor. This is a DynamicObject.TryInvoke " +
                "override: it can't carry [RequiresUnreferencedCode] itself without mismatching the " +
                "unannotated base member, and the DLR invokes it only after the consumer's own " +
                "dynamic call site already triggered the framework's warning.")]
            [UnconditionalSuppressMessage("AOT", "IL3050", Justification =
                "Same Dynamic.InvokeConstructor call as above; see the IL2026 suppression on this member.")]
            [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification =
                "Same DLR-only-caller reasoning as the CA1062 suppression on BaseDictionary.TryGetMember; see that member.")]
            public override bool TryInvoke(InvokeBinder binder, object?[]? args, out object? result)
            {
                result = Dynamic.InvokeConstructor(_type, Util.NameArgsIfNecessary(binder.CallInfo, args!));
                return true;
            }

        }

        /// <summary>
        /// Gets a value indicating whether this Type is available at runtime.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is available; otherwise, <c>false</c>.
        /// </value>
        public bool IsAvailable => Target != null;


        /// <summary>
        /// Gets the call target.
        /// </summary>
        /// <value>
        /// The call target.
        /// </value>
        /// <exception cref="Dynamitey.DynamicObjects.LateType.MissingTypeException"></exception>
        [SuppressMessage("Design", "CA1065:Do not raise exceptions in unexpected locations", Justification =
            "Overlaps SonarAnalyzer S2372 (\"property getter that throws\"), already in the Sonar half " +
            "of the backlog for this exact getter and deliberately deferred there rather than fixed here " +
            "- the two rules flag the same defect, so resolving one by converting CallTarget into a " +
            "method would just close S2372 for free while reopening it as a CA1065 fix that isn't this " +
            "batch's to make. CallTarget overrides BaseForwarder's abstract member, so changing its shape " +
            "from a property to a method is a design change affecting every forwarder subclass, not a " +
            "narrow fix; the throw itself is intentional (see the summary above): a LateType constructed " +
            "from a name whose type never resolved has nothing to forward to.")]
        protected override object CallTarget
        {
            get
            {
                if(Target ==null)
                    // TypeName is set whenever Target can be null: the (Type type) constructor
                    // wraps an already-resolved Type and never leaves Target null.
                    throw new MissingTypeException(TypeName!);

                return InvokeContext.CreateStatic((Type)Target);
            }
        }
    


    }
}
