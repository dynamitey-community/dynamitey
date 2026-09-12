// Builder.cs (the static Builder/Build/Build<T> facade and the Activate/
// Activate<T> constructor-args wrappers) and DynamicObjects/Builder.cs (the
// IBuilder implementation and its ListSetup/ArraySetup/ObjectSetup family)
// are used a lot in the suite through their most common shapes - but several
// overloads (the Func<object[]> factory forms, the generic ListSetup<T>/
// ArraySetup<T>, Activate<T>.Create's MissingMemberException fallback, and a
// couple of the DynamicObject trampolines' error branches) were never
// exercised. These tests fill those in.
using System;
using System.Collections.Generic;
using System.Dynamic;
using Dynamitey.DynamicObjects;
using Dynamitey.SupportLibrary;
using NUnit.Framework;

namespace Dynamitey.Tests
{
    public class BuilderGapTest : Helper
    {
        [Test]
        public void TestBuilderNewNonGeneric()
        {
            IBuilder tBuilder = Builder.New();
            dynamic tObj = tBuilder.Object(Foo: "Bar");

            Assert.That((string)tObj.Foo, Is.EqualTo("Bar"));
        }

        [Test]
        public void TestBuilderNewGeneric()
        {
            IBuilder tBuilder = Builder.New<ExpandoObject>();
            dynamic tObj = tBuilder.Object();

            Assert.That((object)tObj, Is.InstanceOf<ExpandoObject>());
        }

        [Test]
        public void TestActivateWithFactoryFunctionConstructor()
        {
            var tActivate = new Activate(typeof(ParamsConstructorPoco), () => new object[] { "a", "b" });

            var tResult = (ParamsConstructorPoco)tActivate.Create();

            Assert.That(tResult.Args, Is.EqualTo("a,b"));
        }

        [Test]
        public void TestActivateOfTWithFactoryFunctionConstructor()
        {
            var tActivate = new Activate<ParamsConstructorPoco>(() => new object[] { "x", "y" });

            var tResult = (ParamsConstructorPoco)tActivate.Create();

            Assert.That(tResult.Args, Is.EqualTo("x,y"));
        }

        [Test]
        public void TestActivateOfTCreateWithArgsUsesBaseCreate()
        {
            var tActivate = new Activate<ParamsConstructorPoco>("p", "q");

            var tResult = (ParamsConstructorPoco)tActivate.Create();

            Assert.That(tResult.Args, Is.EqualTo("p,q"));
        }

        [Test]
        public void TestActivateOfTCreateFallsBackToDynamicInvokeConstructorWithoutParameterlessCtor()
        {
            // PocoOptConstructor has only an all-optional-parameters constructor, which
            // Activator.CreateInstance<T>() cannot bind (MissingMemberException) - Activate<T>.Create
            // falls back to Dynamic.InvokeConstructor, whose DLR binder can.
            var tActivate = new Activate<PocoOptConstructor>();

            var tResult = (PocoOptConstructor)tActivate.Create();

            Assert.That(tResult.One, Is.EqualTo("-1"));
        }

        [Test]
        public void TestListSetupGenericThenList()
        {
            IBuilder tBuilder = Builder.New();
            tBuilder.ListSetup<List<object>>();

            var tResult = tBuilder.List(1, 2, 3);

            Assert.That(tResult, Is.InstanceOf<List<object>>());
            Assert.That(tResult.Count, Is.EqualTo(3));
        }

        [Test]
        public void TestListSetupWithFactoryFunction()
        {
            IBuilder tBuilder = Builder.New();
            var tCalls = 0;
            tBuilder.ListSetup(() =>
            {
                tCalls++;
                return Array.Empty<object>();
            });

            var tResult = tBuilder.List();

            Assert.That(tResult, Is.InstanceOf<DynamicObjects.List>());
            Assert.That(tCalls, Is.EqualTo(1));
        }

        [Test]
        public void TestArraySetupGeneric()
        {
            IBuilder tBuilder = Builder.New();
            tBuilder.ArraySetup<List<object>>();

            var tResult = tBuilder.Array(4, 5);

            Assert.That(tResult, Is.InstanceOf<List<object>>());
            Assert.That(tResult.Count, Is.EqualTo(2));
        }

        [Test]
        public void TestArraySetupWithFactoryFunction()
        {
            IBuilder tBuilder = Builder.New();
            var tCalls = 0;
            tBuilder.ArraySetup(() =>
            {
                tCalls++;
                return Array.Empty<object>();
            });

            var tResult = tBuilder.Array();

            Assert.That(tResult, Is.InstanceOf<DynamicObjects.List>());
            Assert.That(tCalls, Is.EqualTo(1));
        }

        [Test]
        public void TestObjectSetupWithFactoryFunction()
        {
            IBuilder tBuilder = Builder.New<PropPoco>();
            var tCalls = 0;
            tBuilder.ObjectSetup(() =>
            {
                tCalls++;
                return Array.Empty<object>();
            });

            dynamic tResult = tBuilder.Object();

            Assert.That((object)tResult, Is.InstanceOf<PropPoco>());
            Assert.That(tCalls, Is.EqualTo(1));
        }

        [Test]
        public void TestSetupWithoutArgumentNamesThrows()
        {
            dynamic tNew = Builder.New();

            Assert.Throws<ArgumentException>(() => tNew.Setup(typeof(ExpandoObject)));
        }

        [Test]
        public void TestObjectFactoryWithUnnamedPositionalArgThrows()
        {
            dynamic tNew = Builder.New();

            Assert.Throws<ArgumentException>(() => tNew.Person("unnamed"));
        }

        // Exercises the private InvokeHelper's "single positional arg" detection: a lone
        // anonymous-type argument is treated as a set of named properties rather than requiring
        // ArgumentNames to line up with ArgumentCount.
        [Test]
        public void TestObjectFactoryWithSingleAnonymousArgSetsItsProperties()
        {
            dynamic tResult = Build.NewObject(new { Prop1 = "AnonSet", Prop2 = 7L });

            Assert.That((string)tResult.Prop1, Is.EqualTo("AnonSet"));
            Assert.That((long)tResult.Prop2, Is.EqualTo(7L));
        }

        // Same InvokeHelper branch, the other half of the "or": a lone
        // IEnumerable<KeyValuePair<string,object>> argument is likewise treated as named
        // properties instead of a single unnamed positional argument.
        [Test]
        public void TestObjectFactoryWithSingleKeyValuePairEnumerableArgSetsItsProperties()
        {
            var tPairs = new List<KeyValuePair<string, object>> { new("Prop1", "FromKv") };

            dynamic tResult = Build.NewObject(tPairs);

            Assert.That((string)tResult.Prop1, Is.EqualTo("FromKv"));
        }

        // Exercises Builder<T>.TrySetMember's Type branch, then that the stored Activate is
        // used by TryInvokeMember to construct the named factory.
        [Test]
        public void TestSetMemberWithTypeThenInvokeCreatesThatType()
        {
            dynamic tNew = Builder.New();
            tNew.Foo = typeof(ExpandoObject);

            dynamic tResult = tNew.Foo();

            Assert.That((object)tResult, Is.InstanceOf<ExpandoObject>());
        }

        // Exercises Builder<T>.TrySetMember's Activate branch directly (as opposed to the Type
        // branch, which wraps the value in a new Activate itself).
        [Test]
        public void TestSetMemberWithActivateThenInvokeCreatesThatType()
        {
            dynamic tNew = Builder.New();
            tNew.Foo = new Activate(typeof(ExpandoObject));

            dynamic tResult = tNew.Foo();

            Assert.That((object)tResult, Is.InstanceOf<ExpandoObject>());
        }

        // Exercises Builder<T>.TrySetMember's null branch: explicitly setting a member to null
        // clears any factory previously registered for that name rather than throwing.
        [Test]
        public void TestSetMemberWithNullClearsFactory()
        {
            dynamic tNew = Builder.New();
            tNew.Foo = typeof(ExpandoObject);
            tNew.Foo = null;

            dynamic tResult = tNew.Foo();

            Assert.That((object)tResult, Is.InstanceOf<ChainableDictionary>());
        }

        // Exercises Builder<T>.TrySetMember's false branch: a value that is neither null, a
        // Type, nor an Activate is rejected, and the DLR turns that into an exception at the
        // call site.
        [Test]
        public void TestSetMemberWithUnsupportedValueThrows()
        {
            dynamic tNew = Builder.New();

            Assert.Throws<Microsoft.CSharp.RuntimeBinder.RuntimeBinderException>(() => tNew.Foo = 5);
        }

        // Exercises SetupTrampoline.TryInvoke's ternary both ways: a Type argument gets wrapped
        // in a new Activate, while an Activate argument is used as-is.
        [Test]
        public void TestSetupWithTypeAndActivateArgumentsBothRegisterFactories()
        {
            dynamic tNew = Builder.New();

            tNew.Setup(FromType: typeof(ExpandoObject), FromActivate: new Activate(typeof(List<object>)));

            Assert.That((object)tNew.FromType(), Is.InstanceOf<ExpandoObject>());
            Assert.That((object)tNew.FromActivate(), Is.InstanceOf<List<object>>());
        }

        // Build<T>.NewList had no coverage: every existing NewList use goes through the
        // non-generic Build facade.
        [Test]
        public void TestBuildOfTNewList()
        {
            List<object> tResult = Build<List<object>>.NewList(1, 2, 3);

            Assert.That(tResult, Is.EqualTo(new object[] { 1, 2, 3 }));
        }

        // Exercises Builder<T>.ListSetup's "reuse the Object factory's type" branch: when
        // ListSetup is called with plain constructor args (no explicit Activate) and no List
        // factory was registered yet, it falls back to whatever ObjectSetup already registered
        // under "Object" rather than defaulting to List<object>.
        [Test]
        public void TestListSetupWithoutActivateReusesObjectFactoryType()
        {
            IBuilder tBuilder = Builder.New(); // TObjectPrototype defaults to ChainableDictionary
            tBuilder.ObjectSetup();
            var tSeed = new Dictionary<string, object> { { "Foo", "Bar" } };

            tBuilder.ListSetup(tSeed);
            var tResult = tBuilder.List();

            Assert.That(tResult, Is.InstanceOf<ChainableDictionary>());
            Assert.That((string)tResult.Foo, Is.EqualTo("Bar"));
        }

        // ArraySetup's plain params-array overload (as opposed to the generic <TList> or
        // Func<object[]> overloads, both covered above) had no direct coverage.
        [Test]
        public void TestArraySetupWithPlainConstructorArgs()
        {
            IBuilder tBuilder = Builder.New();
            tBuilder.ArraySetup();

            var tResult = tBuilder.Array(1, 2, 3);

            Assert.That(tResult, Is.InstanceOf<DynamicObjects.List>());
            Assert.That(tResult.Count, Is.EqualTo(3));
        }

        // BaseForwarder's explicit IForwarder.Target implementation is never read through the
        // interface anywhere else in the suite - every other test reaches Target only
        // indirectly (through dynamic dispatch on CallTarget).
        [Test]
        public void TestForwarderTargetIsReadableThroughTheInterface()
        {
            var tTarget = new object();
            DynamicObjects.IForwarder tForwarder = new DynamicObjects.Get(tTarget);

            Assert.That(tForwarder.Target, Is.SameAs(tTarget));
        }
    }
}
