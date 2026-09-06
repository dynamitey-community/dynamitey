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
using System.Linq;
using System.Text;

namespace Dynamitey.Internal.Optimization
{
    internal class BinderHash
    {
   

        protected BinderHash(Type delegateType, String name, Type context, string?[]? argNames, Type binderType, bool staticContext, bool isEvent, bool knownBinder)
        {
            KnownBinder = knownBinder;
            BinderType = binderType;
            StaticContext = staticContext;
            DelegateType = delegateType;
            Name = name;
            IsSpecialName = false;
            GenericArgs = null;
            Context = context;
            ArgNames = argNames;
            IsEvent = isEvent;


        }

        protected BinderHash(Type delegateType, InvokeMemberName name, Type context, string?[]? argNames, Type binderType, bool staticContext, bool isEvent, bool knownBinder)
        {
            KnownBinder = knownBinder;
            BinderType = binderType;
            StaticContext = staticContext;
            DelegateType = delegateType;
            Name = name.Name;
            IsSpecialName = name.IsSpecialName;
            GenericArgs = name.GenericArgs;
            Context = context;
            ArgNames = argNames;
            IsEvent = isEvent;


        }


 

        public bool KnownBinder { get; }
        public Type BinderType { get;  }
        public bool StaticContext { get; }
        public bool IsEvent { get;  }
        public Type DelegateType { get; }
        public string Name { get;  }
        public bool IsSpecialName { get; }
        public Type[]? GenericArgs { get;  }
        public Type Context { get; }
        public string?[]? ArgNames { get; }

        /// <summary>
        /// Two arrays match when both are null, or both are non-null with equal contents. A null
        /// array is not the same call shape as an empty one - a site with no named arguments binds
        /// differently from one that has them - so null and non-null must never compare equal.
        /// </summary>
        /// <remarks>
        /// This replaces a "(a == null) == (b == null) && (b == null || b.SequenceEqual(a))" pair
        /// spelled out inline in both Equals implementations. Extracting it is what
        /// cs/complex-condition asks for: that rule flags nesting rather than length - a flat
        /// chain of &amp;&amp; is explicitly acceptable to it - and these parenthesised sub-conditions
        /// were the nesting. It also removes the null-forgiving operators the inline form needed,
        /// because the nullness guard and the comparison now live together instead of being
        /// separated by seven unrelated conjuncts.
        /// </remarks>
        private static bool SequencesEqual<TItem>(TItem[]? left, TItem[]? right)
        {
            if (left == null)
            {
                return right == null;
            }

            return right != null && left.SequenceEqual(right);
        }

        /// <summary>
        /// A known binder is one whose type is already established by the call site that produced
        /// it, so a BinderType difference no longer distinguishes two hashes.
        /// </summary>
        private bool BinderTypeMatches(BinderHash other) => KnownBinder || other.BinderType == BinderType;

        /// <summary>
        /// The comparison shared by this class and <see cref="BinderHash{T}"/>, which previously
        /// carried near-identical copies of it. The copies differed only in ways that were either
        /// immaterial (comparing ArgNames in the opposite direction, which SequenceEqual makes
        /// symmetric) or subsumed by the caller - see the DelegateType note on each caller.
        /// </summary>
        protected bool CoreEquals(BinderHash other) =>
            other.IsEvent == IsEvent
            && other.StaticContext == StaticContext
            && other.Context == Context
            && other.IsSpecialName == IsSpecialName
            && BinderTypeMatches(other)
            && Equals(other.Name, Name)
            && SequencesEqual(GenericArgs, other.GenericArgs)
            && SequencesEqual(ArgNames, other.ArgNames);

        public virtual bool Equals(BinderHash? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            // DelegateType is compared explicitly here because this overload accepts any BinderHash,
            // so nothing else establishes that the two describe the same delegate shape. The
            // generic override below does not need it - see there.
            return CoreEquals(other) && other.DelegateType == DelegateType;
        }


        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (!(obj is BinderHash)) return false;
            return Equals((BinderHash) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var tArgNames = ArgNames;

                int result = (tArgNames == null ? 0 : tArgNames.Length * 397);
                result = (result  ^ StaticContext.GetHashCode());
                //result = (result * 397) ^ DelegateType.GetHashCode();
                //result = (result * 397) ^ Context.GetHashCode();
                result = (result * 397) ^ Name.GetHashCode();
                return result;
            }
        }
    }



    internal class BinderHash<T> : BinderHash where T : class
    {
        public static BinderHash<T> Create(string name, Type context, string?[]? argNames, Type binderType, bool staticContext, bool isEvent, bool knownBinder)
        {
            return new BinderHash<T>(name, context, argNames, binderType, staticContext, isEvent, knownBinder);
        }

        public static BinderHash<T> Create(InvokeMemberName name, Type context, string?[]? argNames, Type binderType, bool staticContext, bool isEvent, bool knownBinder)
        {
            return new BinderHash<T>(name, context, argNames, binderType, staticContext, isEvent, knownBinder);
        }

        protected BinderHash(InvokeMemberName name, Type context, string?[]? argNames, Type binderType, bool staticContext, bool isEvent,bool knownBinder)
            : base(typeof(T), name, context, argNames, binderType, staticContext, isEvent,knownBinder)
        {
        }

        protected BinderHash(string name, Type context, string?[]? argNames, Type binderType, bool staticContext, bool isEvent, bool knownBinder)
            : base(typeof(T), name, context, argNames, binderType, staticContext, isEvent, knownBinder)
        {
        }

        public override bool Equals(BinderHash? other)
        {
            // No DelegateType comparison here, matching what this override has always done: every
            // BinderHash<T> is constructed with typeof(T) as its DelegateType, so "other is
            // BinderHash<T>" already establishes that the two agree on it. Adding the check back
            // would be redundant rather than wrong.
            return other is BinderHash<T> && CoreEquals(other);
        }
    }
}
