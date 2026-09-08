using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Dynamitey.Internal.Compat;
using Dynamitey.Internal;

namespace Dynamitey.DynamicObjects
{


    /// <summary>
    /// A Fake Type
    /// </summary>
    public abstract class FauxType
    {
        /// <summary>
        /// Fauxes the type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        public static implicit operator FauxType(Type type)
        {
            return new RealType(type);
        }

        /// <summary>
        /// Named alternate for the implicit conversion from <see cref="Type"/> above, for callers
        /// in a language that cannot consume operator overloads.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>A <see cref="RealType"/> wrapping <paramref name="type"/>.</returns>
        public static FauxType FromType(Type type)
        {
            return new RealType(type);
        }


        /// <summary>
        /// Gets the members.
        /// </summary>
        /// <param name="binderName">Name of the binder.</param>
        /// <returns></returns>
        [RequiresUnreferencedCode("A FauxType wrapping a real Type (RealType) resolves this by reflecting over the target type's members by name; trimming can remove the member being resolved. Overrides on a data-only FauxType (PropretySpecType, or an AggreType composed only of those) don't need this themselves, but must match the abstract declaration.")]
        public abstract IEnumerable<MemberInfo> GetMember(string binderName);

        /// <summary>
        /// Gets the contained types.
        /// </summary>
        /// <returns></returns>
        public abstract Type[] GetContainedTypes();

        /// <summary>
        /// Gets the names of every member this type reports.
        /// </summary>
        /// <returns>The member names. Implementations may return duplicates only if the
        /// underlying source does; <see cref="AggreType"/> removes them across its children.</returns>
        [RequiresUnreferencedCode("A FauxType wrapping a real Type (RealType) resolves this by reflecting over the target type's members; trimming can remove members it would otherwise report. Overrides on a data-only FauxType (PropretySpecType, or an AggreType composed only of those) don't need this themselves, but must match the abstract declaration.")]
        [SuppressMessage("Design", "CA1024:Use properties where appropriate", Justification =
            "GetMemberNames is declared public API (PublicAPI.Unshipped.txt) and abstract: turning " +
            "it into a property is a breaking signature change for this member and every override " +
            "(RealType, PropretySpecType, AggreType), out of scope for an analyzer-driven cleanup - " +
            "same public-API-freeze reasoning as the CA1819 sites (see Invocation.Args, Invocation.cs).")]
        public abstract IEnumerable<string> GetMemberNames();

        /// <summary>
        /// Determines whether the specified type contains the type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>
        ///   <c>true</c> if the specified type contains type; otherwise, <c>false</c>.
        /// </returns>
        public virtual bool ContainsType(Type type)
        {
            return GetContainedTypes().Contains(type);
        }

    }



    /// <summary>
    /// A <see cref="FauxType"/> described by an explicit name-to-type map rather than by a
    /// real CLR type. Nothing is reflected over: every lookup is a dictionary read, which is
    /// what makes this the cheap option when the shape is already known.
    /// </summary>
    /// <remarks>
    /// The name is misspelled - "Proprety" - and is kept that way deliberately. It is declared
    /// public API in PublicAPI.Unshipped.txt, and correcting it would break the one-line package
    /// swap that <c>README.md</c> promises consumers moving from the original package.
    /// </remarks>
    public class PropretySpecType : FauxType
    {
        /// <summary>
        /// Gets the member name to type map backing this instance.
        /// </summary>
        public IDictionary<string, Type> PropertySpec { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PropretySpecType"/> class.
        /// </summary>
        /// <param name="propertySpec">
        /// The member name to type map. It is stored by reference, not copied, so later changes
        /// to the caller's dictionary are visible through this instance.
        /// </param>
        public PropretySpecType(IDictionary<string, Type> propertySpec)
        {
            PropertySpec = propertySpec;
        }

        /// <summary>
        /// Gets the member matching <paramref name="binderName"/>.
        /// </summary>
        /// <param name="binderName">The member name to look up.</param>
        /// <returns>
        /// A single-element sequence holding the mapped type, or an empty sequence when the name
        /// is not in the map. Absence is reported as an empty result rather than an exception.
        /// </returns>
        [RequiresUnreferencedCode("Matches FauxType.GetMember's abstract declaration; this override is itself a plain dictionary lookup and does no reflection.")]
        public override IEnumerable<MemberInfo> GetMember(string binderName)
        {
            if (PropertySpec.TryGetValue(binderName, out var val))
            {
                return new[] {val.GetTypeInfo()};

            }
            return Enumerable.Empty<MemberInfo>();
        }

        /// <summary>
        /// Gets the names in the map.
        /// </summary>
        /// <returns>The map's keys, in the order the underlying dictionary yields them.</returns>
        [RequiresUnreferencedCode("Matches FauxType.GetMemberNames's abstract declaration; this override is itself a plain dictionary lookup and does no reflection.")]
        public override IEnumerable<string> GetMemberNames()
        {
            return PropertySpec.Keys;
        }

        /// <summary>
        /// Gets the CLR types this instance stands in for.
        /// </summary>
        /// <returns>
        /// Always empty. This type is defined by a name-to-type map rather than by wrapping real
        /// CLR types, so there is nothing to report - unlike <see cref="RealType"/>.
        /// </returns>
        public override Type[] GetContainedTypes()
        {
            return Array.Empty<Type>();
        }
    }


    /// <summary>
    /// A Fake Type that represents a real type
    /// </summary>
    public class RealType : FauxType
    {
        /// <summary>
        /// RealType implicitly conversts to an actualy Type
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification =
            "This is a user-defined implicit conversion operator. Adding a null guard here to " +
            "satisfy CA1062 immediately trips SonarAnalyzer's S3877, which flags a conversion " +
            "operator that throws: a conversion is expected to succeed or fail through the type " +
            "system, not surprise the caller with an exception. Rather than open a second backlog " +
            "entry to re-suppress S3877, this stays as the pre-existing NullReferenceException-on-" +
            "null behavior; CA1062 is suppressed instead of guarded.")]
         public static implicit operator Type(RealType type)
         {
             return type.TargetType;
         }

         /// <summary>
         /// Named alternate for the implicit conversion to <see cref="Type"/> above, for callers
         /// in a language that cannot consume operator overloads. TargetType is protected, so this
         /// is the only way an external caller reads the wrapped <see cref="Type"/> back out.
         /// </summary>
         /// <returns>The wrapped <see cref="Type"/>.</returns>
         public Type ToType()
         {
             return TargetType;
         }

         /// <summary>
         /// An actual Type implicitly conversts to a real type
         /// </summary>
         /// <param name="type">The type.</param>
         /// <returns></returns>
         public static implicit operator RealType(Type type)
         {
             return new RealType(type);
         }

         /// <summary>
         /// Named alternate for the implicit conversion from <see cref="Type"/> above, for callers
         /// in a language that cannot consume operator overloads.
         /// </summary>
         /// <param name="type">The type.</param>
         /// <returns>A <see cref="RealType"/> wrapping <paramref name="type"/>.</returns>
         public static new RealType FromType(Type type)
         {
             return new RealType(type);
         }


         /// <summary>
         /// The target type
         /// </summary>
        [SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification =
            "Protected extension-point field - see BaseDictionary._dictionary (DynamicObjects/BaseDictionary.cs) for the full reasoning.")]
        protected readonly Type TargetType;

        /// <summary>
        /// Initializes a new instance of the <see cref="RealType" /> class.
        /// </summary>
        /// <param name="type">The type.</param>
        public RealType(Type type)
        {
            TargetType = type;
        }

        /// <summary>
        /// Gets the members.
        /// </summary>
        /// <param name="binderName">Name of the binder.</param>
        /// <returns></returns>
        [RequiresUnreferencedCode("Reflects over TargetType's members by name; trimming can remove the member being resolved.")]
        public override IEnumerable<MemberInfo> GetMember(string binderName)
        {
            return TargetType.GetTypeInfo().GetMember(binderName);
        }

        /// <summary>
        /// Gets the names of the wrapped type's members, reflected from the type itself.
        /// </summary>
        /// <returns>The member names reported by the underlying CLR type.</returns>
        [RequiresUnreferencedCode("Reflects over TargetType's members; trimming can remove members it would otherwise report.")]
        public override IEnumerable<string> GetMemberNames()
        {
      
            var members = TargetType.GetTypeInfo()
                .GetMembers(BindingFlags.Public | BindingFlags.FlattenHierarchy | BindingFlags.Instance)
                .Where(it=> !((it as MethodInfo)?.IsHideBySig ?? false))
                .Select(it => it.Name)
                .Distinct();
            return members.ToList();
        }

        /// <summary>
        /// Gets the contained types.
        /// </summary>
        /// <returns></returns>
        public override Type[] GetContainedTypes()
        {
            return new[] { TargetType };
        }

   
    }


    /// <summary>
    /// A Fake Tupe that is an aggregate of other types
    /// </summary>
    public class AggreType : FauxType
    {

        /// <summary>
        /// Makes the type appendable.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        public static AggreType MakeTypeAppendable(IEquivalentType type)
        {
            Guard.NotNull(type);

            if (type.EquivalentType == null)
            {
                type.EquivalentType = new AggreType();
            }
            if (!(type.EquivalentType is AggreType))
            {
                type.EquivalentType = new AggreType(type.EquivalentType);
            }
            return (AggreType) type.EquivalentType;
        }


       
        private readonly List<FauxType> Types = new List<FauxType>();

        /// <summary>
        /// Initializes a new instance of the <see cref="AggreType" /> class.
        /// </summary>
        /// <param name="types">The types.</param>
        public AggreType(params FauxType[] types)
        {
            Types.AddRange(types);
        }

        /// <summary>
        /// Gets the interface types.
        /// </summary>
        /// <returns></returns>
        public Type[] GetInterfaceTypes()
        {
            return Types.SelectMany(it => it.GetContainedTypes()).Where(it => it.GetTypeInfo().IsInterface).ToArray();
        }

        /// <summary>
        /// Gets the union of every child type's member names.
        /// </summary>
        /// <returns>
        /// The distinct member names across all aggregated types. Duplicates are removed here, so
        /// a name declared by two children is reported once.
        /// </returns>
        [RequiresUnreferencedCode("Matches FauxType.GetMemberNames's abstract declaration; delegates to each child FauxType, one of which may be a RealType that reflects over its target's members.")]
        public override IEnumerable<string> GetMemberNames()
        {
            return Types.SelectMany(it => it.GetMemberNames()).Distinct();
        }

        /// <summary>
        /// Adds the type.
        /// </summary>
        /// <param name="type">The type.</param>
        public void AddType(Type type)
        {
            if (!ContainsType(type))
            {
                Types.Add(type);
            }
        }

        /// <summary>
        /// Adds the type.
        /// </summary>
        /// <param name="type">The type.</param>
        public void AddType(FauxType type)
        {
            if (type is RealType)
            {
                foreach (var realType in type.GetContainedTypes())
                {
                    AddType(realType);
                }
            }else if (type is AggreType)
            {
                foreach (var fauxType in ((AggreType)type).Types)
                {
                    AddType(fauxType);
                }
            }
            else
            {
                Types.Add(type);
            }

        }

        /// <summary>
        /// Gets the members.
        /// </summary>
        /// <param name="binderName">Name of the binder.</param>
        /// <returns></returns>
        [RequiresUnreferencedCode("Matches FauxType.GetMember's abstract declaration; delegates to each child FauxType, one of which may be a RealType that reflects over its target's members by name.")]
        public override IEnumerable<MemberInfo> GetMember(string binderName)
        {
            var list = new List<MemberInfo>();
            foreach (FauxType t in Types)
            {
                list.AddRange(t.GetMember(binderName));
            }
            return list;
        }

        /// <summary>
        /// Gets the contained types.
        /// </summary>
        /// <returns></returns>
        public override Type[] GetContainedTypes()
        {
            return Types.SelectMany(it => it.GetContainedTypes()).ToArray();
        }
    }
}
