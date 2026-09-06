using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Dynamitey.DynamicObjects;
using Microsoft.CSharp.RuntimeBinder;
using Binder = Microsoft.CSharp.RuntimeBinder.Binder;
using Dynamitey.Internal.Compat;

namespace Dynamitey.Internal.Optimization
{

    internal class DummmyNull
    {

    }


    internal static partial class InvokeHelper
    {

        internal const int Unknown =0;
        internal const int KnownGet = 1;
        internal const int KnownSet = 2;
        internal const int KnownMember = 3;
        internal const int KnownDirect = 4;
        internal const int KnownConstructor = 5;



        private static readonly object _clearDynamicLock = new object();
        internal static IDictionary<Type, CallSite<DynamicCreateCallSite>> DynamicInvokeCreateCallSite
        {
            get
            {
                lock (_clearDynamicLock)
                {
                    return _dynamicInvokeCreateCallSite ?? (_dynamicInvokeCreateCallSite =
                               new Dictionary<Type, CallSite<DynamicCreateCallSite>>());
                }

            }
        }

        internal static void ClearFullyDynamicCache()
        {
            lock (_clearDynamicLock)
            {
                _dynamicInvokeCreateCallSite = null;
            }
        }

        private static bool TryDynamicCachedCallSite<T>(BinderHash<T> hash, int knownBinderType, [NotNullWhen(true)] out CallSite<T>? callSite) where T: class
        {
            switch(knownBinderType)
            {
                default:
                    return BinderCache<T>.Cache.TryGetValue(hash, out callSite);

                case KnownGet:
                    return BinderGetCache<T>.Cache.TryGetValue(hash, out callSite);

                case KnownSet:
                    return BinderSetCache<T>.Cache.TryGetValue(hash, out callSite);

                case KnownMember:
                    return BinderMemberCache<T>.Cache.TryGetValue(hash, out callSite);

                case KnownDirect:
                    return BinderDirectCache<T>.Cache.TryGetValue(hash, out callSite);

                case KnownConstructor:
                    return BinderConstructorCache<T>.Cache.TryGetValue(hash, out callSite);
                            
            }

        }




        // Used only as a Dictionary<TKey,...> key: identifies a CallSite delegate
        // signature (its parameter types plus return type) so that a type emitted
        // for one signature can be found and reused for every later call with the
        // same signature, instead of emitting - and leaking - a fresh Type per call.
        private readonly struct CallSiteDelegateSignature : IEquatable<CallSiteDelegateSignature>
        {
            private readonly Type[] _argTypes;
            private readonly Type _returnType;

            public CallSiteDelegateSignature(Type[] argTypes, Type returnType)
            {
                _argTypes = argTypes;
                _returnType = returnType;
            }

            public bool Equals(CallSiteDelegateSignature other)
            {
                if (_returnType != other._returnType || _argTypes.Length != other._argTypes.Length)
                    return false;

                for (var i = 0; i < _argTypes.Length; i++)
                {
                    if (_argTypes[i] != other._argTypes[i])
                        return false;
                }

                return true;
            }

            public override bool Equals(object? obj) => obj is CallSiteDelegateSignature other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    var tHash = _returnType.GetHashCode();
                    foreach (var tArgType in _argTypes)
                    {
                        tHash = tHash * 31 + tArgType.GetHashCode();
                    }
                    return tHash;
                }
            }
        }

        // Guards both _emittedModule's lazy initialization and all reads/writes of
        // _emittedDelegateTypes below. EmitCallSiteFuncType is only ever reached
        // through the >14-argument fallback branch of InvokeHelper.cs's generated
        // switches, which is already far off the hot path, so a single lock across
        // the whole method (cache lookup included) is simpler than lock-free
        // tricks and costs nothing that matters here.
        private static readonly object _emitLock = new object();
        private static readonly Dictionary<CallSiteDelegateSignature, Type> _emittedDelegateTypes =
            new Dictionary<CallSiteDelegateSignature, Type>();
        private static ModuleBuilder? _emittedModule;
        private static int _emittedTypeCount;

        /// <summary>
        /// Builds (or returns a cached, previously-built) delegate type shaped
        /// <c>TReturn Invoke(CallSite, object, object, ..., object)</c> - one
        /// <see cref="CallSite"/> parameter followed by <paramref name="argTypes"/>
        /// - for use as the generic argument of <see cref="CallSite{T}"/> when
        /// invoking with more arguments than InvokeHelper.tt has a generated
        /// <c>Func&lt;...&gt;</c>/<c>Action&lt;...&gt;</c> case for (more than 14).
        /// </summary>
        /// <remarks>
        /// Up to and including Dynamitey 3.0.3, this delegate type was obtained by
        /// reaching into ImpromptuInterface by name (see the removed <c>BuildProxy</c>
        /// <see cref="LateType"/> field) - which threw <see cref="TypeLoadException"/>
        /// when ImpromptuInterface was not installed, even though Dynamitey never
        /// declared a dependency on it. That was circular by construction:
        /// ImpromptuInterface depends on Dynamitey, so Dynamitey cannot reference it
        /// back. Emitting the delegate type here removes both the missing-dependency
        /// failure and the circularity (fixes #27).
        /// </remarks>
        [RequiresUnreferencedCode("Defines the emitted delegate type's constructor and Invoke method via Reflection.Emit, which the trimmer cannot see into.")]
        [RequiresDynamicCode("Emits a delegate type via System.Reflection.Emit for call sites wider than the 14-argument fast path (issue #27); Reflection.Emit requires runtime code generation and throws PlatformNotSupportedException when AOT-compiled or otherwise unavailable.")]
        internal static Type EmitCallSiteFuncType(IEnumerable<Type> argTypes, Type returnType)
        {
            var tArgTypes = argTypes as Type[] ?? argTypes.ToArray();
            var tSignature = new CallSiteDelegateSignature(tArgTypes, returnType);

            lock (_emitLock)
            {
                if (_emittedDelegateTypes.TryGetValue(tSignature, out var tCachedType))
                {
                    return tCachedType;
                }

                var tNewType = EmitCallSiteDelegateType(tArgTypes, returnType);
                _emittedDelegateTypes[tSignature] = tNewType;
                return tNewType;
            }
        }

        // Only ever called from inside the _emitLock in EmitCallSiteFuncType.
        [RequiresUnreferencedCode("Defines the emitted delegate type's constructor and Invoke method via Reflection.Emit, which the trimmer cannot see into.")]
        [RequiresDynamicCode("Builds the emitted delegate type via AssemblyBuilder/TypeBuilder; Reflection.Emit requires runtime code generation and is not supported when AOT-compiled.")]
        private static Type EmitCallSiteDelegateType(Type[] argTypes, Type returnType)
        {
            try
            {
                if (_emittedModule == null)
                {
                    _emittedModule = AssemblyBuilder
                        .DefineDynamicAssembly(new AssemblyName("Dynamitey.CallSiteDelegates"), AssemblyBuilderAccess.Run)
                        .DefineDynamicModule("Dynamitey.CallSiteDelegates");
                }
            }
            catch (PlatformNotSupportedException ex)
            {
                // The documented failure mode for Reflection.Emit being unavailable
                // (e.g. NativeAOT, iOS/tvOS, Blazor WebAssembly). Wrap it so the
                // caller gets an actionable message instead of either a bare
                // PlatformNotSupportedException with no context, or - as before this
                // fix - a misleading complaint about a missing ImpromptuInterface
                // installation that no longer has anything to do with the failure.
                throw new PlatformNotSupportedException(
                    "Dynamitey cannot invoke members with more than 14 arguments on this runtime: " +
                    "building the call site for them requires System.Reflection.Emit, which this runtime " +
                    "does not support (this is common on AOT-compiled, trimmed, or mobile/WebAssembly " +
                    "targets). Keep argument counts at 14 or fewer, or run on a runtime with " +
                    "Reflection.Emit support.", ex);
            }

            // Dynamic.InvokeCallSite invokes the delegate with [callSite, target,
            // ...args] - one more argument than argTypes lists, because argTypes
            // is only the user-supplied args (see InvokeHelper.tt's default
            // branches: tArgTypes has args.Length entries, never target). The slot
            // for target sits between CallSite and the user args, and is always
            // object - target's static type at every call site is object/TTarget
            // erased to object by the time it reaches here.
            var tParameterTypes = new Type[argTypes.Length + 2];
            tParameterTypes[0] = typeof(CallSite);
            tParameterTypes[1] = typeof(object);
            Array.Copy(argTypes, 0, tParameterTypes, 2, argTypes.Length);

            var tTypeBuilder = _emittedModule.DefineType(
                $"CallSiteFunc{++_emittedTypeCount}",
                TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.AnsiClass |
                TypeAttributes.AutoClass,
                typeof(MulticastDelegate));

            var tConstructor = tTypeBuilder.DefineConstructor(
                MethodAttributes.RTSpecialName | MethodAttributes.HideBySig | MethodAttributes.Public,
                CallingConventions.Standard, new[] { typeof(object), typeof(IntPtr) });
            tConstructor.SetImplementationFlags(MethodImplAttributes.Runtime | MethodImplAttributes.Managed);

            var tInvoke = tTypeBuilder.DefineMethod(
                "Invoke",
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual,
                returnType, tParameterTypes);
            tInvoke.SetImplementationFlags(MethodImplAttributes.Runtime | MethodImplAttributes.Managed);

            // The netstandard2.0-only System.Reflection.Emit package annotates CreateTypeInfo()
            // nullable (net10.0's in-box implementation doesn't); it never actually returns null
            // for a TypeBuilder that was just fully defined, as this one was.
            return tTypeBuilder.CreateTypeInfo()!.AsType();
        }

        internal static HashSet<object> _allCaches = new HashSet<object>();
        private static readonly object _binderCacheLock = new object();
        private static readonly object _callSiteCacheLock = new object();
        internal static IDictionary<Type, CallSite<DynamicCreateCallSite>>? _dynamicInvokeCreateCallSite;


        internal static void ClearAllCaches()
        {
            lock (_binderCacheLock)
            {
                foreach (Action instance in _allCaches)
                {
                    instance();
                }
            }

            lock (_callSiteCacheLock)
            {
                ClearFullyDynamicCache();
            }
        }


        private static void SetDynamicCachedCallSite<T>(BinderHash<T> hash, int knownBinderType, CallSite<T> callSite) where T: class 
        {
            switch (knownBinderType)
            {
                default:
                    _allCaches.Add(BinderCache<T>.ClearCache);
                    BinderCache<T>.Cache[hash] = callSite;
                    break;
                case KnownGet:
                    _allCaches.Add(BinderGetCache<T>.ClearCache);
                    BinderGetCache<T>.Cache[hash] = callSite;
                    break;
                case KnownSet:
                    _allCaches.Add(BinderSetCache<T>.ClearCache);
                    BinderSetCache<T>.Cache[hash] = callSite;
                    break;
                case KnownMember:
                    _allCaches.Add(BinderMemberCache<T>.ClearCache);
                    BinderMemberCache<T>.Cache[hash] = callSite;
                    break;
                case KnownDirect:
                    _allCaches.Add(BinderDirectCache<T>.ClearCache);
                    BinderDirectCache<T>.Cache[hash] = callSite;
                    break;
                case KnownConstructor:
                    _allCaches.Add(BinderConstructorCache<T>.ClearCache);
                    BinderConstructorCache<T>.Cache[hash] = callSite;
                    break;
            }
        }

      

        /// <summary>
        /// LazyBinderType
        /// </summary>
        internal delegate CallSiteBinder LazyBinder();



        public static bool IsActionOrFunc(object target)
        {
            if (target == null)
                return false;
            var tType = target as Type ?? target.GetType();

            if (tType.GetTypeInfo().IsGenericType)
            {
                tType = tType.GetGenericTypeDefinition();
            }

            return FuncArgs.ContainsKey(tType) || ActionArgs.ContainsKey(tType);
         }

   

        [RequiresUnreferencedCode("Invokes tFunc via FastDynamicInvoke, which for a known arg count calls it through a 'dynamic' reference (the DLR); trimming can remove the member the DLR resolves.")]
        [RequiresDynamicCode("FastDynamicInvoke's DLR path requires the DLR's runtime code generation; not supported when AOT-compiled.")]
        internal static object? InvokeMethodDelegate(this object target, Delegate tFunc, object?[] args)
        {
            object? result;

            try
            {
                result = tFunc.FastDynamicInvoke(
                    tFunc.IsSpecialThisDelegate()
                        ? new[] { target }.Concat(args).ToArray()
                        : args
                    );
            }
            catch (TargetInvocationException ex)
            {
                if (ex.InnerException != null)
                    throw ex.InnerException;
                throw;
            }
            return result;
        }



        internal static IEnumerable<CSharpArgumentInfo> GetBindingArgumentList(object?[] args, string?[]? argNames, bool staticContext)
        {

            var tTargetFlag = CSharpArgumentInfoFlags.None;
            if (staticContext)
            {
                tTargetFlag |= CSharpArgumentInfoFlags.IsStaticType | CSharpArgumentInfoFlags.UseCompileTimeType;
            }



            var tList = new BareBonesList<CSharpArgumentInfo>(args.Length + 1)
                        {
                            CSharpArgumentInfo.Create(tTargetFlag, null)
                        };

            //Optimization: linq statement creates a slight overhead in this case
            // ReSharper disable LoopCanBeConvertedToQuery
            // ReSharper disable ForCanBeConvertedToForeach
            for (int i = 0; i < args.Length; i++)
            {
                var tFlag = CSharpArgumentInfoFlags.None;
                string? tName = null;
                if (argNames != null && argNames.Length > i)
                    tName = argNames[i];

                if (!String.IsNullOrEmpty(tName))
                {
                    tFlag |= CSharpArgumentInfoFlags.NamedArgument;

                }
                tList.Add(CSharpArgumentInfo.Create(
                    tFlag, tName));
            }
            // ReSharper restore ForCanBeConvertedToForeach
            // ReSharper restore LoopCanBeConvertedToQuery

            return tList;
        }




  

        [RequiresUnreferencedCode("Resolves and invokes the generic CreateCallSite<T> for delegateType via the DLR binder (Binder.InvokeMember); trimming can remove that generic method instantiation.")]
        [RequiresDynamicCode("Binds through Microsoft.CSharp.RuntimeBinder, which requires the DLR's runtime code generation; not supported when AOT-compiled.")]
        internal static CallSite CreateCallSite(
            Type delegateType,
            Type specificBinderType,
             int knownType,
            LazyBinder binder,
            InvokeMemberName name,
            Type context,
            string?[]? argNames = null,
            bool staticContext = false,
            bool isEvent = false
           
            )
        {
            CallSite<DynamicCreateCallSite>? tSite;

            bool foundInCache;

            lock (_callSiteCacheLock)
            {
                foundInCache = DynamicInvokeCreateCallSite.TryGetValue(delegateType, out tSite);
            }

            if (!foundInCache)
            {
                tSite = CallSite<DynamicCreateCallSite>.Create(
                    Binder.InvokeMember(
                        CSharpBinderFlags.None,
                        "CreateCallSite",
                        new[] { delegateType },
                        typeof(InvokeHelper),
                        new[]
                            {
                                CSharpArgumentInfo.Create(
                                    CSharpArgumentInfoFlags.IsStaticType | CSharpArgumentInfoFlags.UseCompileTimeType,
                                    null), // InvokeHelper
                                CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null), //binderType
                                CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null), //knownType
                                CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null), //binder
                                CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null), //name
                                CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null), //context
                                CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null), //argnames
                                CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null), //staticcontext
                                CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null), //isevent
                            }
                        ));

                lock (_callSiteCacheLock)
                {
                    // another thread might have been faster; add to dictionary only if we are the first
                    if (!DynamicInvokeCreateCallSite.ContainsKey(delegateType))
                    {
                        DynamicInvokeCreateCallSite[delegateType] = tSite;
                    }
                }
            }
            // tSite is null only per Dictionary<K,V>.TryGetValue's [MaybeNullWhen(false)] contract
            // (i.e. when foundInCache is false); the `if (!foundInCache)` block above always
            // (re)assigns it before this point is reached.
            return (CallSite)tSite!.Target(tSite, typeof(InvokeHelper), specificBinderType, knownType, binder, name, context, argNames, staticContext, isEvent);
        }

        internal delegate object DynamicCreateCallSite(
           CallSite site,
           Type targetType,
           Type specificBinderType,
           int knownType,
           LazyBinder binder,
           InvokeMemberName name,
           Type context,
           string?[]? argNames,
           bool staticContext,
           bool isEvent
       );



        [RequiresUnreferencedCode("Calls binder() and CallSite<T>.Create; the binder itself resolves a member by name via Microsoft.CSharp.RuntimeBinder, and trimming can remove that member.")]
        [RequiresDynamicCode("CallSite<T>.Create requires the DLR's runtime code generation; not supported when AOT-compiled.")]
        internal static CallSite<T> CreateCallSite<T>(
        Type specificBinderType,
        int knownType,
        LazyBinder binder,
        InvokeMemberName name,
        Type context,
        string?[]? argNames = null,
        bool staticContext = false,
        bool isEvent = false
        )
        where T : class
        {
            var tHash = BinderHash<T>.Create(name, context, argNames, specificBinderType, staticContext, isEvent, knownType != Unknown);
            lock (_binderCacheLock)
            {
                if (!TryDynamicCachedCallSite(tHash, knownType, out var tOut))
                {
                    tOut = CallSite<T>.Create(binder());
                    SetDynamicCachedCallSite(tHash, knownType, tOut);
                }
                return tOut;
            }
        }

        [RequiresUnreferencedCode("Calls binder() and CallSite<T>.Create; the binder itself resolves a member by name via Microsoft.CSharp.RuntimeBinder, and trimming can remove that member.")]
        [RequiresDynamicCode("CallSite<T>.Create requires the DLR's runtime code generation; not supported when AOT-compiled.")]
        internal static CallSite<T> CreateCallSite<T>(
   Type specificBinderType,
   int knownType,
   LazyBinder binder,
   string name,
   Type context,
   string?[]? argNames = null,
   bool staticContext = false,
   bool isEvent = false
   )
   where T : class
        {
            var tHash = BinderHash<T>.Create(name, context, argNames, specificBinderType, staticContext, isEvent, knownType != Unknown);
            lock (_binderCacheLock)
            {
                if (!TryDynamicCachedCallSite(tHash, knownType, out var tOut))
                {
                    tOut = CallSite<T>.Create(binder());
                    SetDynamicCachedCallSite(tHash, knownType, tOut);
                }
                return tOut;
            }
        }


        internal delegate object DynamicInvokeMemberConstructorValueType(
            CallSite funcSite,
            Type funcTarget,
            ref CallSite? callsite,
            Type? binderType,
            int knownType,
            LazyBinder? binder,
            InvokeMemberName name,
            bool staticContext,
            Type context,
            string?[]? argNames,
            Type target,
            object[] args);

        internal static readonly IDictionary<Type, CallSite<DynamicInvokeMemberConstructorValueType>> _dynamicInvokeMemberSite = new Dictionary<Type, CallSite<DynamicInvokeMemberConstructorValueType>>();

        [RequiresUnreferencedCode("Resolves and invokes the generic InvokeMemberTargetType<Type,TReturn> via the DLR binder (Binder.InvokeMember); trimming can remove that generic method instantiation.")]
        [RequiresDynamicCode("Binds through Microsoft.CSharp.RuntimeBinder, which requires the DLR's runtime code generation; not supported when AOT-compiled.")]
        internal static dynamic DynamicInvokeStaticMember(Type tReturn, ref CallSite? callsite, Type? binderType, int knownType, LazyBinder? binder,
                                       InvokeMemberName name,
                                     bool staticContext,
                                     Type context,
                                     string?[]? argNames,
                                     Type target, params object[] args)
        {
            if (!_dynamicInvokeMemberSite.TryGetValue(tReturn, out var tSite))
            {
                tSite = CallSite<DynamicInvokeMemberConstructorValueType>.Create(
                        Binder.InvokeMember(
                            CSharpBinderFlags.None,
                            "InvokeMemberTargetType",
                            new[] { typeof(Type), tReturn },
                            typeof(InvokeHelper),
                            new[]
                                {
                                    CSharpArgumentInfo.Create(
                                        CSharpArgumentInfoFlags.IsStaticType |
                                        CSharpArgumentInfoFlags.UseCompileTimeType, null),
                                     CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType | CSharpArgumentInfoFlags.IsRef, null),
                                    CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null),
                                    CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null),
                                    CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null),
                                    CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null),
                                    CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null),
                                    CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null),
                                    CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null),
                                    CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null),
                                    CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null),
                                }
                            )
                    );
                _dynamicInvokeMemberSite[tReturn] = tSite;
            }

            return tSite.Target(tSite, typeof(InvokeHelper), ref callsite, binderType, knownType, binder, name, staticContext, context, argNames, target, args);
        }


        [RequiresUnreferencedCode("Calls InvokeMemberTargetType<object,TReturn>, which binds through the DLR and can lose a trimmed member.")]
        [RequiresDynamicCode("InvokeMemberTargetType binds through the DLR, which requires runtime code generation; not supported when AOT-compiled.")]
        internal static TReturn InvokeMember<TReturn>(ref CallSite? callsite, Type? binderType,int knownType, LazyBinder? binder,
                                       InvokeMemberName name,
                                     bool staticContext,
                                     Type context,
                                     string?[]? argNames,
                                     object target, params object[] args)
        {
            return InvokeMemberTargetType<object, TReturn>(ref callsite, binderType, knownType, binder, name, staticContext, context, argNames, target, args);
        }

        // Issue #31: a static-context Get or Set used to reach the property
        // through its accessor *method* - Binder.InvokeMember("get_"/"set_" +
        // name, ...) for a public top-level type, or Binder.GetMember(...,
        // IsStaticType) otherwise. Microsoft.CSharp.RuntimeBinder keeps its
        // own symbol cache for a type, shared process-wide and independent of
        // Dynamitey's own CallSite caching (BinderHash/CacheableInvocation key
        // correctly by type, name and delegate type - clearing those caches
        // does not touch this). The FIRST successful InvokeMember bind of
        // either accessor of a property registers that property in the
        // binder's symbol table; binding the SIBLING accessor of the SAME
        // property afterward then fails with "cannot explicitly call
        // operator or accessor", because the binder now recognizes the call
        // as an explicit accessor invocation and (correctly, per C#
        // semantics) refuses it. Whichever accessor's bind runs second always
        // throws - proven by instrumenting the Get-side fallback below: it
        // fires, and is silently swallowed, on a get that follows a set on
        // the same property (see the commit message). Get looks unaffected
        // only because the #12/#13 reflection fallback happens to catch and
        // paper over its RuntimeBinderException; Set has no such fallback,
        // so the failure is directional in what's observable, not in what
        // actually happens inside the binder.
        //
        // Both Get and Set now go straight to reflection for a static
        // context instead of attempting the DLR bind at all. That removes
        // the collision entirely (no InvokeMember call means no symbol-table
        // entry to poison) rather than adding another failure path to catch,
        // and per Benchmarks/StaticMemberBenchmarks.cs it costs nothing extra
        // in the steady state: cached, Get is ~1.16x reflection's cost and
        // Set ~1.63x - about what the DLR trick already cost - because the
        // static path never had a real DLR fast path to lose; it was always
        // the slow, uncommon case relative to instance access.
        private const BindingFlags StaticMemberFlags =
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;

        [RequiresUnreferencedCode("Resolves 'name' via reflection against the static members of the target type; trimming can remove the member being resolved.")]
        private static object? GetStaticMemberByReflection(Type targetType, string name, Type context)
        {
            // "context" is Dynamitey's accessibility control (see
            // TestInvokeDoNotExposePrivateMethod in PrivateTest.cs) - a
            // caller who deliberately supplies a context unrelated to the
            // target type is asserting that it should NOT see the target's
            // private members. A PUBLIC member is visible from any context.
            // A NON-PUBLIC member is only returned when "context" is the
            // target type itself - the ordinary InvokeContext.CreateStatic(type)
            // case, where GetTargetContext defaulted context to target.
            var tContextOwnsPrivateAccess = context == targetType;

            var tField = targetType.GetField(name, StaticMemberFlags);
            if (tField != null && (tField.IsPublic || tContextOwnsPrivateAccess))
            {
                return tField.GetValue(null);
            }

            var tProperty = targetType.GetProperty(name, StaticMemberFlags);
            if (tProperty?.GetMethod != null && (tProperty.GetMethod.IsPublic || tContextOwnsPrivateAccess))
            {
                return tProperty.GetValue(null);
            }

            throw new RuntimeBinderException($"'{targetType}' does not contain an accessible definition for '{name}'");
        }

        [RequiresUnreferencedCode("Resolves 'name' via reflection against the static members of the target type; trimming can remove the member being resolved.")]
        private static void SetStaticMemberByReflection(Type targetType, string name, Type context, object? value)
        {
            // Same accessibility gate as GetStaticMemberByReflection above.
            var tContextOwnsPrivateAccess = context == targetType;

            var tField = targetType.GetField(name, StaticMemberFlags);
            if (tField != null && (tField.IsPublic || tContextOwnsPrivateAccess))
            {
                tField.SetValue(null, value);
                return;
            }

            var tProperty = targetType.GetProperty(name, StaticMemberFlags);
            if (tProperty?.SetMethod != null && (tProperty.SetMethod.IsPublic || tContextOwnsPrivateAccess))
            {
                tProperty.SetValue(null, value);
                return;
            }

            throw new RuntimeBinderException($"'{targetType}' does not contain an accessible, settable definition for '{name}'");
        }

        [RequiresUnreferencedCode("Resolves 'name' via Binder.GetMember for an instance context, or via reflection for a static context; trimming can remove the member being resolved.")]
        [RequiresDynamicCode("The instance-context path binds through Microsoft.CSharp.RuntimeBinder, which requires the DLR's runtime code generation; not supported when AOT-compiled.")]
        internal static object? InvokeGetCallSite(object target, string name, Type context, bool staticContext, ref CallSite? callsite)
        {
            if (staticContext && target is Type tTargetType)
            {
                return GetStaticMemberByReflection(tTargetType, name, context);
            }

            if (callsite == null)
            {
                var tTargetFlag = CSharpArgumentInfoFlags.None;
                LazyBinder tBinder = ()=> Binder.GetMember(CSharpBinderFlags.None, name,
                                                  context,
                                                  new List<CSharpArgumentInfo>
                                                      {
                                                          CSharpArgumentInfo.Create(
                                                              tTargetFlag, null)
                                                      });

                callsite = CreateCallSite<Func<CallSite, object, object>>(typeof(GetMemberBinder), KnownGet, tBinder, name, context,
                                staticContext: staticContext);
            }
            var tCallSite = (CallSite<Func<CallSite, object, object>>) callsite;

            return tCallSite.Target(tCallSite, target);
        }

        [RequiresUnreferencedCode("Resolves 'name' via Binder.SetMember for an instance context, or via reflection for a static context; trimming can remove the member being resolved.")]
        [RequiresDynamicCode("The instance-context path binds through Microsoft.CSharp.RuntimeBinder, which requires the DLR's runtime code generation; not supported when AOT-compiled.")]
        internal static object? InvokeSetCallSite(object target, string name, object? value, Type context, bool staticContext, ref CallSite? callSite)
        {
            if (staticContext && target is Type tTargetType)
            {
                SetStaticMemberByReflection(tTargetType, name, context, value);
                return value;
            }

            if (callSite == null)
            {
                LazyBinder tBinder = ()=> Binder.SetMember(CSharpBinderFlags.None, name,
                                           context,
                                           new List<CSharpArgumentInfo>
                                               {
                                                   CSharpArgumentInfo.Create(
                                                       CSharpArgumentInfoFlags.None, null),
                                                   CSharpArgumentInfo.Create(

                                                       CSharpArgumentInfoFlags.None

                                                       , null)

                                               });

                callSite = CreateCallSite<Func<CallSite, object, object, object>>(typeof(SetMemberBinder),KnownSet, tBinder, name, context, staticContext: false);
            }

            var tCallSiteResult = (CallSite<Func<CallSite, object, object, object>>) callSite;
            // The generic call-site delegate's slots are always plain 'object' (the DLR itself is
            // nullable-oblivious); value's own nullability is preserved at runtime regardless.
            var tResult = tCallSiteResult.Target(tCallSiteResult, target, value!);
            return tResult;
        }

        [RequiresUnreferencedCode("Resolves name.Name via Binder.InvokeMember; trimming can remove the member being resolved.")]
        [RequiresDynamicCode("Binds through Microsoft.CSharp.RuntimeBinder, which requires the DLR's runtime code generation; not supported when AOT-compiled.")]
        internal static object? InvokeMemberCallSite(object target,  InvokeMemberName name, object?[] args, string?[]? tArgNames, Type tContext, bool tStaticContext, ref CallSite? callSite)
        {
            LazyBinder? tBinder = null;
            Type? tBinderType = null;
            if (callSite == null)
            {
              
                tBinder = () =>
                {
                                var tList = GetBindingArgumentList(args, tArgNames, tStaticContext);
                                var tFlag = CSharpBinderFlags.None;
                                if (name.IsSpecialName)
                                {
                                    tFlag |= CSharpBinderFlags.InvokeSpecialName;
                                }
                                 return Binder.InvokeMember(tFlag, name.Name, name.GenericArgs,
                                                             tContext, tList);
                              };
                tBinderType = typeof (InvokeMemberBinder);
            }


            // InvokeMember's generated fast-path slots are always plain 'object' (see
            // InvokeHelper.tt); args' element nullability is preserved at runtime regardless
            // of this array-identity cast.
            return InvokeMember<object>(ref callSite, tBinderType, KnownMember, tBinder, name, tStaticContext, tContext, tArgNames, target, args!);
        }

        [RequiresUnreferencedCode("Resolves target's invoke/call operator via Binder.Invoke; trimming can remove the member being resolved.")]
        [RequiresDynamicCode("Binds through Microsoft.CSharp.RuntimeBinder, which requires the DLR's runtime code generation; not supported when AOT-compiled.")]
        internal static object? InvokeDirectCallSite(object target, object?[] args, string?[]? tArgNames, Type tContext, bool tStaticContext, ref CallSite? callSite)
        {
            LazyBinder? tBinder = null;
            Type? tBinderType = null;

            if (callSite == null)
            {

                tBinder = () =>
                {
                    var tList = GetBindingArgumentList(args, tArgNames, tStaticContext);
                    var tFlag = CSharpBinderFlags.None;
                    return Binder.Invoke(tFlag,tContext, tList);
                };
                tBinderType = typeof(InvokeBinder);
            }


            return InvokeMember<object>(ref callSite, tBinderType, KnownDirect,tBinder, String.Empty, tStaticContext, tContext, tArgNames, target, args!);
        }

        [RequiresUnreferencedCode("Resolves target's indexer via Binder.GetIndex; trimming can remove the indexer being resolved.")]
        [RequiresDynamicCode("Binds through Microsoft.CSharp.RuntimeBinder, which requires the DLR's runtime code generation; not supported when AOT-compiled.")]
        internal static object? InvokeGetIndexCallSite(object target, object?[] indexes, string?[]? argNames, Type context, bool tStaticContext,ref CallSite? callSite)
        {
            LazyBinder? tBinder=null;
            Type? tBinderType = null;
            if (callSite == null)
            {

                tBinder = () =>
                              {
                                  var tList = GetBindingArgumentList(indexes, argNames,
                                                                               tStaticContext);
                                  return Binder.GetIndex(CSharpBinderFlags.None, context, tList);
                              };
                tBinderType = typeof (GetIndexBinder);

            }

            return InvokeMember<object>(ref callSite,tBinderType, Unknown, tBinder, Invocation.IndexBinderName, tStaticContext, context, argNames, target, indexes!);
        }

        [RequiresUnreferencedCode("Resolves target's indexer setter via Binder.SetIndex; trimming can remove the indexer being resolved.")]
        [RequiresDynamicCode("Binds through Microsoft.CSharp.RuntimeBinder, which requires the DLR's runtime code generation; not supported when AOT-compiled.")]
        internal static object? InvokeSetIndexCallSite(object target, object?[] indexesThenValue, string?[]? tArgNames, Type tContext, bool tStaticContext, ref CallSite? tCallSite)
        {
            LazyBinder? tBinder =null;
            Type? tBinderType = null;
            if (tCallSite == null)
            {

                tBinder = () =>
                              {
                                  var tList = GetBindingArgumentList(indexesThenValue, tArgNames,
                                                                               tStaticContext);
                                  return Binder.SetIndex(CSharpBinderFlags.None, tContext, tList);
                              };

                tBinderType = typeof (SetIndexBinder);
            }

            return InvokeMember<object>(ref tCallSite, tBinderType, Unknown, tBinder, Invocation.IndexBinderName, tStaticContext, tContext, tArgNames, target, indexesThenValue!);
        }

        [RequiresUnreferencedCode("Resolves name.Name via Binder.InvokeMember; trimming can remove the member being resolved.")]
        [RequiresDynamicCode("Binds through Microsoft.CSharp.RuntimeBinder, which requires the DLR's runtime code generation; not supported when AOT-compiled.")]
        internal static void InvokeMemberActionCallSite(object target,InvokeMemberName name, object?[] args, string?[]? tArgNames, Type tContext, bool tStaticContext,ref CallSite? callSite)
        {
            LazyBinder? tBinder =null;
            Type? tBinderType = null;
            if (callSite == null)
            {

                tBinder = () =>
                              {
                                  IEnumerable<CSharpArgumentInfo> tList;
                                  tList = GetBindingArgumentList(args, tArgNames, tStaticContext);

                                  var tFlag = CSharpBinderFlags.ResultDiscarded;
                                  if (name.IsSpecialName)
                                  {
                                      tFlag |= CSharpBinderFlags.InvokeSpecialName;
                                  }

                                  return Binder.InvokeMember(tFlag, name.Name, name.GenericArgs,
                                                             tContext, tList);
                              };
                tBinderType = typeof (InvokeMemberBinder);
            }


            InvokeMemberAction(ref callSite,tBinderType, KnownMember, tBinder, name, tStaticContext, tContext, tArgNames, target, args!);
        }


        [RequiresUnreferencedCode("Resolves target's invoke/call operator via Binder.Invoke; trimming can remove the member being resolved.")]
        [RequiresDynamicCode("Binds through Microsoft.CSharp.RuntimeBinder, which requires the DLR's runtime code generation; not supported when AOT-compiled.")]
        internal static void InvokeDirectActionCallSite(object target, object?[] args, string?[]? tArgNames, Type tContext, bool tStaticContext, ref CallSite? callSite)
        {
            LazyBinder? tBinder = null;
            Type? tBinderType = null;

            if (callSite == null)
            {

                tBinder = () =>
                {
                    IEnumerable<CSharpArgumentInfo> tList;
                    tList = GetBindingArgumentList(args, tArgNames, tStaticContext);

                    var tFlag = CSharpBinderFlags.ResultDiscarded;
                   

                    return Binder.Invoke(tFlag,tContext, tList);
                };
                tBinderType = typeof(InvokeBinder);
            }


            InvokeMemberAction(ref callSite, tBinderType, KnownDirect, tBinder, String.Empty, tStaticContext, tContext, tArgNames, target, args!);
        }

        internal class IsEventBinderDummy{
            
        }
        [RequiresUnreferencedCode("Resolves 'name' via Binder.IsEvent; trimming can remove the member being resolved.")]
        [RequiresDynamicCode("Binds through Microsoft.CSharp.RuntimeBinder, which requires the DLR's runtime code generation; not supported when AOT-compiled.")]
        internal static bool InvokeIsEventCallSite(object target, string name, Type tContext, ref CallSite? callSite)
        {
            if (callSite == null)
            {
                LazyBinder tBinder = ()=> Binder.IsEvent(CSharpBinderFlags.None, name, tContext);
                var tBinderType = typeof (IsEventBinderDummy);
                callSite = CreateCallSite<Func<CallSite, object, bool>>(tBinderType, Unknown, tBinder, name, tContext, isEvent: true);
            }
            var tCallSite = (CallSite<Func<CallSite, object, bool>>)callSite;

            return tCallSite.Target(tCallSite, target);
        }

        [RequiresUnreferencedCode("Calls InvokeIsEventCallSite/InvokeMemberActionCallSite/InvokeGetCallSite/InvokeSetCallSite and a 'dynamic +=' operator, each of which resolves a member via the DLR binder; trimming can remove the member being resolved.")]
        [RequiresDynamicCode("Every path binds through the DLR, which requires runtime code generation; not supported when AOT-compiled.")]
        internal static void InvokeAddAssignCallSite(object target, string name, object?[] args, string?[]? argNames, Type context, bool staticContext, //lgtm [cs/too-many-ref-parameters]
            ref CallSite? callSiteIsEvent, ref CallSite? callSiteAdd, ref CallSite? callSiteGet, ref CallSite? callSiteSet) //This is an optimization readability isn't the concern. 
        {

            if (InvokeIsEventCallSite(target, name, context, ref callSiteIsEvent))
            {
                InvokeMemberActionCallSite(target, InvokeMemberName.CreateSpecialName("add_" + name), args, argNames, context, staticContext, ref callSiteAdd);
            }
            else
            {
                dynamic tGet = InvokeGetCallSite(target,name, context, staticContext, ref callSiteGet)!;
                tGet += (dynamic)(args[0]!);
                InvokeSetCallSite(target, name,  (object)tGet, context, staticContext, ref callSiteSet);
            }
        }

        [RequiresUnreferencedCode("Calls InvokeIsEventCallSite/InvokeMemberActionCallSite/InvokeGetCallSite/InvokeSetCallSite and a 'dynamic -=' operator, each of which resolves a member via the DLR binder; trimming can remove the member being resolved.")]
        [RequiresDynamicCode("Every path binds through the DLR, which requires runtime code generation; not supported when AOT-compiled.")]
        internal static void InvokeSubtractAssignCallSite(object target, string name, object?[] args, string?[]? argNames, Type context, bool staticContext, // lgtm [cs/too-many-ref-parameters]
            ref CallSite? callSiteIsEvent, ref CallSite? callSiteRemove, ref CallSite? callSiteGet, ref CallSite? callSiteSet) //This is an optimization readability isn't the concern. 
        {
            if (InvokeIsEventCallSite(target, name, context, ref callSiteIsEvent))
            {
                InvokeMemberActionCallSite(target, InvokeMemberName.CreateSpecialName("remove_" + name), args, argNames, context, staticContext, ref callSiteRemove);
            }
            else
            {
                dynamic tGet = InvokeGetCallSite(target, name, context, staticContext, ref callSiteGet)!;
                tGet -= (dynamic)(args[0]!);
                InvokeHelper.InvokeSetCallSite(target, name, tGet, context, staticContext, ref callSiteSet);
            }
        }

        public delegate void DynamicAction(params object[] args);
        public delegate TReturn DynamicFunc<out TReturn>(params object[] args);

        [RequiresUnreferencedCode("Resolves the conversion to type via Binder.Convert; trimming can remove the conversion being resolved.")]
        [RequiresDynamicCode("MakeGenericType and the Binder.Convert binding both require the DLR's runtime code generation; not supported when AOT-compiled.")]
        internal static object? InvokeConvertCallSite(object target, bool explict, Type type, Type context, ref CallSite? callSite)
        {
            if (callSite == null) 
            {
                LazyBinder tBinder = () =>
                                         {
                                             var tFlags = explict ? CSharpBinderFlags.ConvertExplicit : CSharpBinderFlags.None;

                                             return Binder.Convert(tFlags, type, context);
                                         };
                Type tBinderType = typeof (ConvertBinder);

                var tFunc = typeof(Func<,,>).MakeGenericType(typeof(CallSite), typeof(object), type);


                callSite = CreateCallSite(tFunc, tBinderType,Unknown, tBinder,
                                          explict
                                              ? Invocation.ExplicitConvertBinderName
                                              : Invocation.ImplicitConvertBinderName, context);
            }
            dynamic tDynCallSite = callSite;
            return tDynCallSite.Target(callSite, target);

        }

        internal class InvokeConstructorDummy{};

        internal static InvokeMemberName ConstructorName = new InvokeMemberName(Invocation.ConstructorBinderName);

        [RequiresUnreferencedCode("Resolves type's constructor via Binder.InvokeConstructor; trimming can remove the constructor being resolved.")]
        [RequiresDynamicCode("Binds through Microsoft.CSharp.RuntimeBinder, which requires the DLR's runtime code generation; not supported when AOT-compiled.")]
        internal static object? InvokeConstructorCallSite(Type type, bool isValueType, object?[] args, string?[]? argNames, ref CallSite? callSite)
        {
            LazyBinder? tBinder = null;
            Type? tBinderType  = typeof (InvokeConstructorDummy);
            if (callSite == null || isValueType)
            {
                if (isValueType && args.Length == 0)  //dynamic invocation doesn't see no argument constructors of value types
                {
                    return Activator.CreateInstance(type);
                }


                tBinder = () =>
                              {
                                  var tList = GetBindingArgumentList(args, argNames, true);
                                  return Binder.InvokeConstructor(CSharpBinderFlags.None, type, tList);
                              };
            }


            if (isValueType)
            {
                CallSite? tDummy = null;
                return DynamicInvokeStaticMember(type, ref tDummy, tBinderType, KnownConstructor, tBinder, ConstructorName, true, type,
                                                              argNames, type, args!);
            }

            return InvokeMemberTargetType<Type, object>(ref callSite, tBinderType, KnownConstructor, tBinder, ConstructorName, true, type, argNames,
                                                                     type, args!);
        }

        internal static readonly IDictionary<Type, CallSite<DynamicInvokeWrapFunc>> _dynamicInvokeWrapFunc = new Dictionary<Type, CallSite<DynamicInvokeWrapFunc>>();

        internal delegate object DynamicInvokeWrapFunc(
         CallSite funcSite,
         Type funcTarget,
         object invokable,
         int length
         );

        [RequiresUnreferencedCode("Resolves and invokes the generic WrapFuncHelper<TReturn> via the DLR binder (Binder.InvokeMember); trimming can remove that generic method instantiation.")]
        [RequiresDynamicCode("Binds through Microsoft.CSharp.RuntimeBinder, which requires the DLR's runtime code generation; not supported when AOT-compiled.")]
        internal static Delegate WrapFunc(Type returnType, object invokable, int length)
        {
            if (!_dynamicInvokeWrapFunc.TryGetValue(returnType, out var tSite))
            {

                var tMethod =  "WrapFuncHelper";
 
                tSite = CallSite<DynamicInvokeWrapFunc>.Create(
                    Binder.InvokeMember(
                        CSharpBinderFlags.None,
                        tMethod,
                        new[] {returnType},
                        typeof (InvokeHelper),
                        new[]
                            {
                                CSharpArgumentInfo.Create(
                                    CSharpArgumentInfoFlags.IsStaticType | CSharpArgumentInfoFlags.UseCompileTimeType,
                                    null),
                                CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null),
                                CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null),
                            }
                        )); 
                _dynamicInvokeWrapFunc[returnType] = tSite;
            }
            return (Delegate) tSite.Target(tSite, typeof(InvokeHelper), invokable, length);
        }
    }
}
