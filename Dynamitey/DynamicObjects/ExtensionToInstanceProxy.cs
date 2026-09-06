using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Linq;
using System.Reflection;

using System.Text;
using Microsoft.CSharp.RuntimeBinder;
using Dynamitey.Internal.Compat;
using Dynamitey.Internal.Optimization;

namespace Dynamitey.DynamicObjects
{


    /// <summary>
    /// Proxy that can turn extension methods into instance methods 
    /// </summary>
    public class ExtensionToInstanceProxy: BaseForwarder
    {
       
        private readonly Type _extendedType;
       
        private readonly Type[] _staticTypes;
       
        private readonly Type[]? _instanceHints;

        /// <summary>
        /// Gets the instance hints.
        /// </summary>
        /// <value>
        /// The instance hints.
        /// </value>
        public IEnumerable<Type>? InstanceHints => _instanceHints;


        /// <summary>
        /// Initializes a new instance of the <see cref="ExtensionToInstanceProxy" /> class.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="extendedType">Type of the extended.</param>
        /// <param name="staticTypes">The static types.</param>
        /// <param name="instanceHints">The instance hints.</param>
        /// <exception cref="System.ArgumentException">Don't Nest ExtensionToInstance Objects</exception>
        [RequiresUnreferencedCode("Calls IsExtendedType, which reflects over target's interfaces (GetInterfaces) to check it against extendedType; trimming can remove an interface this depends on. target is also statically 'dynamic', which forces DLR binding on the IsExtendedType calls below even though that method takes a plain object.")]
        [RequiresDynamicCode("Constructing any BaseForwarder-derived type instantiates System.Dynamic.DynamicObject, whose default constructor requires the DLR's runtime code generation; not supported when AOT-compiled.")]
        public ExtensionToInstanceProxy(dynamic target,  Type extendedType, Type[] staticTypes, Type[]? instanceHints = null):base((object)target)
        {
            _staticTypes = staticTypes;
            _extendedType = extendedType;
            _instanceHints = instanceHints;

            if(target is ExtensionToInstanceProxy)
                throw new ArgumentException("Don't Nest ExtensionToInstance Objects");

            if (IsExtendedType(target) || IsExtendedType(Util.GetTargetContext(target,out Type _, out bool _)))
            {
                return;
            }

            throw new ArgumentException($"Non a valid {_extendedType} to be wrapped.");
            
        }

        private object UnwrappedTarget(){
            return Util.GetTargetContext(CallTarget!, out Type _, out bool _);
        }

        /// <summary>
        /// Provides the implementation for operations that get member values. Classes derived from the <see cref="T:System.Dynamic.DynamicObject" /> class can override this method to specify dynamic behavior for operations such as getting a value for a property.
        /// </summary>
        /// <param name="binder">Provides information about the object that called the dynamic operation. The binder.Name property provides the name of the member on which the dynamic operation is performed. For example, for the Console.WriteLine(sampleObject.SampleProperty) statement, where sampleObject is an instance of the class derived from the <see cref="T:System.Dynamic.DynamicObject" /> class, binder.Name returns "SampleProperty". The binder.IgnoreCase property specifies whether the member name is case-sensitive.</param>
        /// <param name="result">The result of the get operation. For example, if the method is called for a property, you can assign the property value to <paramref name="result" />.</param>
        /// <returns>
        /// true if the operation is successful; otherwise, false. If this method returns false, the run-time binder of the language determines the behavior. (In most cases, a run-time exception is thrown.)
        /// </returns>
        [UnconditionalSuppressMessage("Trimming", "IL2075", Justification =
            "Reflects over the (unwrapped) target's interfaces to build an Invoker for the " +
            "extended-method binder.Name. This is a DynamicObject.TryGetMember override: it " +
            "can't carry [RequiresUnreferencedCode] itself without mismatching the unannotated " +
            "base member, and the DLR invokes it only after the consumer's own dynamic member " +
            "access already triggered the framework's warning.")]
        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification =
            "Constructs the annotated Invoker. Same DynamicObject.TryGetMember override reasoning as the IL2075 suppression above.")]
        [UnconditionalSuppressMessage("AOT", "IL3050", Justification =
            "Same Invoker construction as above; see the IL2075/IL2026 suppressions on this member.")]
        public override bool TryGetMember(GetMemberBinder binder, out object? result)
        {

            if (!base.TryGetMember(binder, out result))
            {

                var tInterface = UnwrappedTarget().GetType().GetTypeInfo().GetInterfaces().Single(it => it.Name == _extendedType.Name);
                var typeInfo = tInterface.GetTypeInfo();
                result = new Invoker(binder.Name,
                                     typeInfo.IsGenericType ? typeInfo.GetGenericArguments() : new Type[] {},null, this);
            }
            return true;
        }

        /// <summary>
        /// Basic Invoker syntax for dynamic generics
        /// </summary>
        public class Invoker:BaseObject
        {
            /// <summary>
            /// The name
            /// </summary>
            protected string Name;
            /// <summary>
            /// The parent
            /// </summary>
            protected ExtensionToInstanceProxy Parent;
            /// <summary>
            /// The overload types
            /// </summary>
            protected IDictionary<int, Type[]> OverloadTypes;
            /// <summary>
            /// The generic params
            /// </summary>
            protected Type[] GenericParams;
            /// <summary>
            /// The generic method parameters
            /// </summary>
            protected Type[]? GenericMethodParameters;

            [RequiresUnreferencedCode("Reflects over parent.InstanceHints' methods by name (GetMethods/MakeGenericType) to find overloads matching Name; trimming can remove a method this depends on.")]
            [RequiresDynamicCode("Constructing any BaseObject-derived type instantiates System.Dynamic.DynamicObject, whose default constructor requires the DLR's runtime code generation; not supported when AOT-compiled.")]
            internal Invoker(string name, Type[] genericParameters, Type[]? genericMethodParameters, ExtensionToInstanceProxy parent, Type[]? overloadTypes = null)
            {
                Name = name;
                Parent = parent;
                GenericParams = genericParameters;
                GenericMethodParameters = genericMethodParameters;
                OverloadTypes = new Dictionary<int,Type[]>();

                if (overloadTypes == null)
                {

                    // Pre-existing gap (not introduced here, not fixed): parent.InstanceHints is null
                    // whenever the ExtensionToInstanceProxy was constructed without instanceHints, and
                    // this foreach would NRE in that case. The `!` preserves that exact behavior.
                    foreach (var tGenInterface in parent.InstanceHints!)
                    {
                        var tNewType = tGenInterface;

                        if (tNewType.GetTypeInfo().IsGenericType)
                        {
                            tNewType = tNewType.MakeGenericType(GenericParams);
                        }

                        var members = tNewType.GetTypeInfo().GetMethods(BindingFlags.Instance |
                                                                                   BindingFlags.Public).Where(
                                                                                       it => it.Name == Name).ToList();
                        foreach (var tMethodInfo in members)
                        {
                            var tParams = tMethodInfo.GetParameters().Select(it => it.ParameterType).ToArray();

                            if (OverloadTypes.ContainsKey(tParams.Length))
                            {
                                OverloadTypes[tParams.Length] = new Type[] {};
                            }
                            else
                            {
                                OverloadTypes[tParams.Length] = tParams.Select(ReplaceGenericTypes).ToArray();
                            }
                        }

                        foreach (var tOverloadType in OverloadTypes.ToList())
                        {
                            if (tOverloadType.Value.Length == 0)
                            {
                                OverloadTypes.Remove(tOverloadType);
                            }
                        }

                    }
                }
                else
                    {
                        OverloadTypes[overloadTypes.Length] = overloadTypes;
                    }
            }

            [RequiresUnreferencedCode("Calls Type.MakeGenericType, which the trimmer cannot statically analyze; a required generic instantiation can be removed.")]
            [RequiresDynamicCode("Type.MakeGenericType can require runtime code generation to construct the closed generic type; not supported when AOT-compiled.")]
            private Type ReplaceGenericTypes(Type type)
            {
                var typeInfo = type.GetTypeInfo();
                if (typeInfo.IsGenericType && typeInfo.ContainsGenericParameters)
                {
                    var tArgs = typeInfo.GetGenericArguments();

                    tArgs = tArgs.Select(ReplaceGenericTypes).ToArray();

                    return type.GetGenericTypeDefinition().MakeGenericType(tArgs);
                }

                if (typeInfo.ContainsGenericParameters)
                {
                    return typeof (object);
                }
               
                return type;
            }

            /// <summary>
            /// Tries the get member.
            /// </summary>
            /// <param name="binder">The binder.</param>
            /// <param name="result">The result.</param>
            /// <returns></returns>
            [UnconditionalSuppressMessage("Trimming", "IL2026", Justification =
                "Constructs the annotated OverloadInvoker. This is a DynamicObject.TryGetMember " +
                "override: it can't carry [RequiresUnreferencedCode] itself without mismatching " +
                "the unannotated base member, and the DLR invokes it only after the consumer's " +
                "own dynamic member access already triggered the framework's warning.")]
            [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Same OverloadInvoker construction as above; see the IL2026 suppression on this member.")]
            public override bool TryGetMember(GetMemberBinder binder, out object? result)
            {
                if (binder.Name == "Overloads")
                {
                    result = new OverloadInvoker(Name, GenericParams,GenericMethodParameters, Parent);
                    return true;
                }
                return base.TryGetMember(binder, out result);
            }



            /// <summary>
            /// Tries the invoke.
            /// </summary>
            /// <param name="binder">The binder.</param>
            /// <param name="args">The args.</param>
            /// <param name="result">The result.</param>
            /// <returns></returns>
            [UnconditionalSuppressMessage("Trimming", "IL2026", Justification =
                "Calls the annotated Dynamic.InvokeConvert and Parent.InvokeStaticMethod. This is a DynamicObject override: it can't carry " +
                "[RequiresUnreferencedCode] itself without mismatching the unannotated base " +
                "member, and the DLR invokes it only after the consumer's own dynamic call " +
                "site already triggered the framework's warning.")]
            [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Same calls as above; see the IL2026 suppression on this member.")]
            public override bool TryInvoke(InvokeBinder binder, object?[]? args, out object? result)
            {
                object?[] tArgs = args!;
                if (OverloadTypes.ContainsKey(args!.Length))
                {
                    tArgs = OverloadTypes[args.Length].Zip(args, Tuple.Create)
                        .Select(it => it.Item2 != null ? Dynamic.InvokeConvert(it.Item2, it.Item1, @explicit: true) : null).ToArray();

                }

                var name = InvokeMemberName.Create(Name, GenericMethodParameters);

                result = Parent.InvokeStaticMethod(name, tArgs);
                return true;
            }

            /// <summary>
            /// Tries the index of the get.
            /// </summary>
            /// <param name="binder">The binder.</param>
            /// <param name="indexes">The indexes.</param>
            /// <param name="result">The result.</param>
            /// <returns></returns>
            [UnconditionalSuppressMessage("Trimming", "IL2026", Justification =
                "Calls the annotated Dynamic.InvokeConvert and constructs the annotated Invoker. This is a DynamicObject override: it can't carry " +
                "[RequiresUnreferencedCode] itself without mismatching the unannotated base " +
                "member, and the DLR invokes it only after the consumer's own dynamic call " +
                "site already triggered the framework's warning.")]
            [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Same calls as above; see the IL2026 suppression on this member.")]
            public override bool TryGetIndex(GetIndexBinder binder, object?[]? indexes, out object result)
            {
                result = new Invoker(Name, GenericParams, indexes!.Select(it => Dynamic.InvokeConvert(it!, typeof(Type), @explicit: true)).Cast<Type>().ToArray(), Parent);
                return true;
            }
        }

        /// <summary>
        /// Overload Invoker
        /// </summary>
        public class OverloadInvoker:Invoker
        {
            [RequiresUnreferencedCode("Calls the annotated Invoker constructor, which reflects over parent.InstanceHints' methods by name.")]
            [RequiresDynamicCode("Constructing any BaseObject-derived type instantiates System.Dynamic.DynamicObject, whose default constructor requires the DLR's runtime code generation; not supported when AOT-compiled.")]
            internal OverloadInvoker(string name, Type[] genericParameters, Type[]? genericMethodParameters, ExtensionToInstanceProxy parent)
                : base(name, genericParameters,genericMethodParameters, parent)
            {
            }


            /// <summary>
            /// Tries the index of the get.
            /// </summary>
            /// <param name="binder">The binder.</param>
            /// <param name="indexes">The indexes.</param>
            /// <param name="result">The result.</param>
            /// <returns></returns>
            [UnconditionalSuppressMessage("Trimming", "IL2026", Justification =
                "Calls the annotated Dynamic.InvokeConvert and constructs the annotated Invoker. This is a DynamicObject override: it can't carry " +
                "[RequiresUnreferencedCode] itself without mismatching the unannotated base " +
                "member, and the DLR invokes it only after the consumer's own dynamic call " +
                "site already triggered the framework's warning.")]
            [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Same calls as above; see the IL2026 suppression on this member.")]
            public override bool TryGetIndex(GetIndexBinder binder, object?[]? indexes, out object result)
            {
                result = new Invoker(Name, GenericParams, GenericMethodParameters, Parent, indexes!.Select(it => Dynamic.InvokeConvert(it!, typeof(Type), @explicit: true)).Cast<Type>().ToArray());
                return true;
            }
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
            "property by name) and the annotated InvokeStaticMethod. This is a " +
            "DynamicObject.TryInvokeMember override: it can't carry [RequiresUnreferencedCode] " +
            "itself without mismatching the unannotated base member, and the DLR invokes it " +
            "only after the consumer's own dynamic call site already triggered the framework's warning.")]
        [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Same calls as above; see the IL2026 suppression on this member.")]
        public override bool TryInvokeMember(System.Dynamic.InvokeMemberBinder binder, object?[]? args, out object? result)
        {
            if (!base.TryInvokeMember(binder, args, out result))
            {

                Type[]? types = null;
                try
                {
                    IList<Type>? typeList =Dynamic.InvokeGet(binder,
                                           "Microsoft.CSharp.RuntimeBinder.ICSharpInvokeOrInvokeMemberBinder.TypeArguments");
                    if(typeList != null)
                    {

                        types = typeList.ToArray();

                    }

                }catch(RuntimeBinderException)
                {
                    try
                    {
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
                }

                var name=InvokeMemberName.Create;
                result = InvokeStaticMethod(name(binder.Name, types), args!);
            }
            return true;
        }

        /// <summary>
        /// Invokes the static method.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="args">The args.</param>
        /// <returns></returns>
        [RequiresUnreferencedCode("Reflects over target's interfaces (GetInterfaces) for generic-argument resolution and calls the annotated Dynamic.InvokeMember/CreateSelf; trimming can remove a member either depends on.")]
        [RequiresDynamicCode("Dynamic.InvokeMember binds through the DLR, which requires runtime code generation; not supported when AOT-compiled.")]
        protected object? InvokeStaticMethod(String_OR_InvokeMemberName name, object?[] args)
        {
            var staticType = InvokeContext.CreateStatic;
            var nameArgs = InvokeMemberName.Create;

            var tList = new List<object?> { UnwrappedTarget() };
            tList.AddRange(args);

            object? result =null;
            var sucess = false;
            var exceptionList = new List<Exception>();

            var tGenericPossibles = new List<Type[]?>();
            if (name.GenericArgs != null && name.GenericArgs.Length > 0)
            {
                var tInterface = UnwrappedTarget().GetType().GetTypeInfo().GetInterfaces().Single(it => it.Name == _extendedType.Name);
                var tTypeGenerics = (tInterface.GetTypeInfo().IsGenericType ? tInterface.GetTypeInfo().GetGenericArguments()
                                            : new Type[] { }).Concat(name.GenericArgs).ToArray();

                tGenericPossibles.Add(tTypeGenerics);
                tGenericPossibles.Add(name.GenericArgs);
            }
            else
            {
                tGenericPossibles.Add(null);
            }
                      


            foreach (var sType in _staticTypes)
            {
                foreach (var tGenericPossible in tGenericPossibles)
                {
                    try
                    {
                        result = Dynamic.InvokeMember(staticType(sType), nameArgs(name.Name, tGenericPossible), tList.ToArray());
                        sucess = true;
                        break;
                    }
                    catch (RuntimeBinderException ex)
                    {
                        exceptionList.Add(ex);
                    }
                }
                if(sucess){
                    break;
                }
            }

            if (!sucess)
            {
                throw exceptionList.First();
            }


            if (TryTypeForName(name.Name, out var tOutType))
            {
                var outTypeInfo = tOutType.GetTypeInfo();
                if (outTypeInfo.IsInterface)
                {
                    var tIsGeneric = outTypeInfo.IsGenericType;
                    if (outTypeInfo.IsGenericType)
                    {
                        tOutType = tOutType.GetGenericTypeDefinition();
                    }

                    if (InstanceHints!.Select(it => tIsGeneric && it.GetTypeInfo().IsGenericType ? it.GetGenericTypeDefinition() : it)
                            .Any(it=> it.Name == tOutType.Name))
                    { 
                        result = CreateSelf(result, _extendedType, _staticTypes, _instanceHints);
                    }
                }
            }
            else
            {
                if (IsExtendedType(result))
                {
                    result = CreateSelf(result, _extendedType, _staticTypes, _instanceHints);
                }
            }

            return result;
        }

        /// <summary>
        /// Creates the self.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="extendedType">Type of the extended.</param>
        /// <param name="staticTypes">The static types.</param>
        /// <param name="instanceHints">The instance hints.</param>
        /// <returns></returns>
        [RequiresUnreferencedCode("Constructs the annotated ExtensionToInstanceProxy.")]
        [RequiresDynamicCode("Constructs the annotated ExtensionToInstanceProxy, which requires the DLR's runtime code generation.")]
        protected virtual ExtensionToInstanceProxy CreateSelf(object? target, Type extendedType, Type[] staticTypes, Type[]? instanceHints)
        {
            // Same pre-existing null-target gap as IsExtendedType above.
            return  new ExtensionToInstanceProxy(target!,extendedType,staticTypes, instanceHints);
        }

        // target is null only via the pre-existing gap noted on IsExtendedType's own call sites
        // above (a null InvokeMember/InvokeStaticMethod result); GetType() below would NRE exactly
        // as it always has.
        [RequiresUnreferencedCode("Reflects over target's interfaces (GetInterfaces) to compare against _extendedType; trimming can remove an interface this depends on.")]
        private bool IsExtendedType(object? target)
        {

            if (target is ExtensionToInstanceProxy)
            {
                return false;
            }

            bool genericDef = _extendedType.GetTypeInfo().IsGenericTypeDefinition;

            return target!.GetType().GetTypeInfo().GetInterfaces().Any(
                it => ((genericDef && it.GetTypeInfo().IsGenericType) ? it.GetGenericTypeDefinition() : it).Name == _extendedType.Name);

        }

        
    }
}
