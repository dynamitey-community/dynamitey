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
        public class MissingTypeException:Exception
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="MissingTypeException" /> class.
            /// </summary>
            /// <param name="typename">The typename.</param>
             public MissingTypeException(string typename)
                 : base(String.Format("Could Not Find Type. {0}", typename))
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
                // Deliberately broad (cs/catch-of-all-exceptions): throwOnError:false only
                // suppresses the "not found" case. Assembly.GetType/Type.GetType can still throw
                // ArgumentException, FileNotFoundException, FileLoadException, or
                // BadImageFormatException for other resolution failures; this method's contract
                // (see the summary above) is to return null for any of them, not just the one.
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
