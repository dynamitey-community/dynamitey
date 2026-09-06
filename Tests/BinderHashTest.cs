using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;

namespace Dynamitey.Tests
{
    // Issue #50, cs/wrong-equals-signature cluster on BinderHash<T>
    // (Internal/Optimization/BinderHash.cs). BinderHash<T> is the key type for Dynamitey's
    // call-site cache (Dictionary<BinderHash<T>, CallSite<T>> in InvokeHelper.cs); a wrong
    // equality contract there can make the cache return a binder for the wrong call shape.
    // Nothing exercised this contract directly before, so this fixture does.
    //
    // BinderHash<T> is internal, and Tests.csproj has no InternalsVisibleTo grant to it (adding
    // one would widen accessibility - see the reasoning already recorded for EmitCallSiteFuncType
    // in Invoke.cs). Instances are built through reflection instead; once held as `object`,
    // Equals(object) and GetHashCode() are public members inherited from object and need no
    // special access to call directly.
    [TestFixture]
    public class BinderHashTest : Helper
    {
        private static readonly Type BinderHashOpenGeneric =
            typeof(Dynamic).Assembly.GetType("Dynamitey.Internal.Optimization.BinderHash`1")!;

        // BinderType only needs to be *some* distinct, stable Type for these tests - Equals
        // compares it with plain reference equality (see BinderHash.cs) - so any two unrelated
        // BCL types stand in for the real DLR CSharpInvokeBinder/CSharpInvokeMemberBinder etc.
        private static readonly Type DummyBinderType = typeof(int);

        private static readonly Type OtherDummyBinderType = typeof(long);

        private static object Create<T>(
            string name,
            Type context,
            string[] argNames,
            Type binderType,
            bool staticContext = false,
            bool isEvent = false,
            bool knownBinder = false) where T : class
        {
            var closedType = BinderHashOpenGeneric.MakeGenericType(typeof(T));
            var createMethod = closedType.GetMethod("Create", BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(string), typeof(Type), typeof(string[]), typeof(Type), typeof(bool), typeof(bool), typeof(bool) },
                null);

            return createMethod.Invoke(null, new object[] { name, context, argNames, binderType, staticContext, isEvent, knownBinder });
        }

        // BinderHash has a second Create overload taking an InvokeMemberName rather than a string.
        // It is the only way to get a non-null GenericArgs onto a hash, because the string
        // constructor hardcodes GenericArgs to null.
        private static object Create<T>(
            InvokeMemberName name,
            Type context,
            string[] argNames,
            Type binderType,
            bool staticContext = false,
            bool isEvent = false,
            bool knownBinder = false) where T : class
        {
            var closedType = BinderHashOpenGeneric.MakeGenericType(typeof(T));
            var createMethod = closedType.GetMethod("Create", BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(InvokeMemberName), typeof(Type), typeof(string[]), typeof(Type), typeof(bool), typeof(bool), typeof(bool) },
                null);

            return createMethod.Invoke(null, new object[] { name, context, argNames, binderType, staticContext, isEvent, knownBinder });
        }

        [Test]
        public void EqualBinderHashesAreEqualAndShareHashCode()
        {
            var tArgNames = new[] { "a", "b" };
            var tOne = Create<Func<object>>("Foo", typeof(string), tArgNames, DummyBinderType, staticContext: true, isEvent: false, knownBinder: false);
            var tTwo = Create<Func<object>>("Foo", typeof(string), (string[])tArgNames.Clone(), DummyBinderType, staticContext: true, isEvent: false, knownBinder: false);

            Assert.That(tOne, Is.Not.SameAs(tTwo), "the two instances must be distinct objects, not the same reference.");
            Assert.That(tOne.Equals(tTwo), Is.True);
            Assert.That(tTwo.Equals(tOne), Is.True);
            Assert.That(tOne.GetHashCode(), Is.EqualTo(tTwo.GetHashCode()));
        }

        [TestCase("Name")]
        [TestCase("ArgNames")]
        [TestCase("Context")]
        [TestCase("IsEvent")]
        [TestCase("StaticContext")]
        [TestCase("BinderType")]
        public void DifferingFieldMakesBinderHashesUnequal(string tVaryingField)
        {
            var tBase = Create<Func<object>>("Foo", typeof(string), new[] { "a" }, DummyBinderType, staticContext: true, isEvent: false, knownBinder: false);

            object tVaried = tVaryingField switch
            {
                "Name" => Create<Func<object>>("Bar", typeof(string), new[] { "a" }, DummyBinderType, staticContext: true, isEvent: false, knownBinder: false),
                "ArgNames" => Create<Func<object>>("Foo", typeof(string), new[] { "b" }, DummyBinderType, staticContext: true, isEvent: false, knownBinder: false),
                "Context" => Create<Func<object>>("Foo", typeof(int), new[] { "a" }, DummyBinderType, staticContext: true, isEvent: false, knownBinder: false),
                "IsEvent" => Create<Func<object>>("Foo", typeof(string), new[] { "a" }, DummyBinderType, staticContext: true, isEvent: true, knownBinder: false),
                "StaticContext" => Create<Func<object>>("Foo", typeof(string), new[] { "a" }, DummyBinderType, staticContext: false, isEvent: false, knownBinder: false),
                "BinderType" => Create<Func<object>>("Foo", typeof(string), new[] { "a" }, OtherDummyBinderType, staticContext: true, isEvent: false, knownBinder: false),
                _ => throw new ArgumentOutOfRangeException(nameof(tVaryingField))
            };

            Assert.That(tBase.Equals(tVaried), Is.False);
            Assert.That(tVaried.Equals(tBase), Is.False);
        }

        [Test]
        public void KnownBinderIgnoresBinderTypeDifference()
        {
            // Equals: "(KnownBinder || other.BinderType == BinderType)" - once the binder is
            // known, a BinderType mismatch should no longer matter.
            var tOne = Create<Func<object>>("Foo", typeof(string), new[] { "a" }, DummyBinderType, knownBinder: true);
            var tTwo = Create<Func<object>>("Foo", typeof(string), new[] { "a" }, OtherDummyBinderType, knownBinder: true);

            Assert.That(tOne.Equals(tTwo), Is.True);
            Assert.That(tTwo.Equals(tOne), Is.True);
        }

        // A null ArgNames is not the same call shape as an empty or populated one: a call site with
        // no named arguments binds differently from one that has them. Every other case in this
        // fixture passes a non-null array, which left the nullness comparison in Equals untested -
        // and that is the conjunct guarding the SequenceEqual calls below it, so getting it wrong
        // either returns a cached binder for the wrong call shape or throws on a null array.
        [Test]
        public void NullAndNonNullArgNamesAreNeverEqual()
        {
            var tNull = Create<Func<object>>("Foo", typeof(string), null!, DummyBinderType);
            var tNamed = Create<Func<object>>("Foo", typeof(string), new[] { "a" }, DummyBinderType);

            // Both directions: an asymmetric Equals would corrupt the binder cache depending only
            // on which instance happened to be the lookup key.
            Assert.That(tNull.Equals(tNamed), Is.False);
            Assert.That(tNamed.Equals(tNull), Is.False);
        }

        // GenericArgs had no coverage at all until these. Every other case in this fixture builds
        // its hash from a string name, and that constructor sets GenericArgs to null
        // unconditionally - so the whole GenericArgs comparison was unreachable from the tests, in
        // any form. Reaching it needs the InvokeMemberName constructor, which is where a call
        // site's generic arguments actually come from. A binder cached for Foo<string> must never
        // be handed back for Foo<int>.
        private static object CreateGeneric<T>(string name, params Type[] genericArgs) where T : class =>
            Create<T>(new InvokeMemberName(name, genericArgs), typeof(string), new[] { "a" }, DummyBinderType);

        [Test]
        public void DifferentGenericArgsAreNeverEqual()
        {
            var tOfString = CreateGeneric<Func<object>>("Foo", typeof(string));
            var tOfInt = CreateGeneric<Func<object>>("Foo", typeof(int));

            Assert.That(tOfString.Equals(tOfInt), Is.False);
            Assert.That(tOfInt.Equals(tOfString), Is.False);
        }

        [Test]
        public void EqualGenericArgsAreEqual()
        {
            var tOne = CreateGeneric<Func<object>>("Foo", typeof(string), typeof(int));
            var tTwo = CreateGeneric<Func<object>>("Foo", typeof(string), typeof(int));

            Assert.That(tOne, Is.Not.SameAs(tTwo), "the two instances must be distinct objects, not the same reference.");
            Assert.That(tOne.Equals(tTwo), Is.True);
            Assert.That(tTwo.Equals(tOne), Is.True);
        }

        [Test]
        public void NullAndNonNullGenericArgsAreNeverEqual()
        {
            var tNone = CreateGeneric<Func<object>>("Foo", null);
            var tGeneric = CreateGeneric<Func<object>>("Foo", typeof(string));

            Assert.That(tNone.Equals(tGeneric), Is.False);
            Assert.That(tGeneric.Equals(tNone), Is.False);
        }

        // An arity difference must be caught by the contents comparison, not by a length shortcut
        // that a refactor could drop: Foo<string> and Foo<string, int> are different call shapes.
        [Test]
        public void DifferentGenericArityIsNeverEqual()
        {
            var tOne = CreateGeneric<Func<object>>("Foo", typeof(string));
            var tTwo = CreateGeneric<Func<object>>("Foo", typeof(string), typeof(int));

            Assert.That(tOne.Equals(tTwo), Is.False);
            Assert.That(tTwo.Equals(tOne), Is.False);
        }

        [Test]
        public void TwoNullArgNamesAreEqual()
        {
            var tOne = Create<Func<object>>("Foo", typeof(string), null!, DummyBinderType);
            var tTwo = Create<Func<object>>("Foo", typeof(string), null!, DummyBinderType);

            Assert.That(tOne, Is.Not.SameAs(tTwo), "the two instances must be distinct objects, not the same reference.");
            Assert.That(tOne.Equals(tTwo), Is.True);
            Assert.That(tTwo.Equals(tOne), Is.True);
            Assert.That(tOne.GetHashCode(), Is.EqualTo(tTwo.GetHashCode()));
        }

        [Test]
        public void DifferentGenericDelegateTypeIsNeverEqual()
        {
            // BinderHash<T1> and BinderHash<T2> are unrelated closed generic types; the derived
            // Equals(BinderHash) narrows with "other is BinderHash<T>", which must reject a
            // same-shaped hash built for a different T even though every other field matches.
            // This is what lets InvokeHelper<T>'s per-T Dictionary<BinderHash<T>, CallSite<T>>
            // omit T (DelegateType) from GetHashCode without risking cross-T collisions leaking
            // through Equals.
            var tForFuncOfObject = Create<Func<object>>("Foo", typeof(string), new[] { "a" }, DummyBinderType);
            var tForAction = Create<Action>("Foo", typeof(string), new[] { "a" }, DummyBinderType);

            Assert.That(tForFuncOfObject.Equals(tForAction), Is.False);
            Assert.That(tForAction.Equals(tForFuncOfObject), Is.False);
            // Object.Equals(object), inherited (not overridden) by BinderHash<T> - the exact
            // dispatch path cs/wrong-equals-signature flags - must agree with the above.
            Assert.That(((object)tForFuncOfObject).Equals((object)tForAction), Is.False);
        }

        [Test]
        public void DictionaryKeyedByBinderHashLooksUpByValueEquality()
        {
            // Reproduces InvokeHelper.cs's actual cache shape - Dictionary<BinderHash<T>,
            // CallSite<T>> - via reflection (BinderHash<T> can't be named directly without
            // InternalsVisibleTo). BinderHash<T> implements no IEquatable<T>, so
            // EqualityComparer<T>.Default resolves to the ObjectEqualityComparer path: this
            // dictionary lookup exercises exactly the Equals(object)/GetHashCode() pair that
            // cs/wrong-equals-signature is concerned with, end to end.
            var tClosedBinderHashType = BinderHashOpenGeneric.MakeGenericType(typeof(Func<object>));
            var tDictType = typeof(Dictionary<,>).MakeGenericType(tClosedBinderHashType, typeof(int));
            var tDict = Activator.CreateInstance(tDictType)!;
            var tAdd = tDictType.GetMethod("Add")!;
            var tContainsKey = tDictType.GetMethod("ContainsKey")!;

            var tCached = Create<Func<object>>("Foo", typeof(string), new[] { "a" }, DummyBinderType, staticContext: true);
            tAdd.Invoke(tDict, new object[] { tCached, 42 });

            var tSameShapeDifferentInstance = Create<Func<object>>("Foo", typeof(string), new[] { "a" }, DummyBinderType, staticContext: true);
            var tDifferentName = Create<Func<object>>("Bar", typeof(string), new[] { "a" }, DummyBinderType, staticContext: true);

            Assert.That((bool)tContainsKey.Invoke(tDict, new object[] { tSameShapeDifferentInstance })!, Is.True,
                "a same-shaped, distinct BinderHash<T> instance must hit the cache entry.");
            Assert.That((bool)tContainsKey.Invoke(tDict, new object[] { tDifferentName })!, Is.False,
                "a different call shape must miss the cache entry.");
        }
    }
}
