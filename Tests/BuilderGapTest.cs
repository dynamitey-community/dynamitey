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
            tBuilder.ListSetup(() => Array.Empty<object>());

            var tResult = tBuilder.List();

            Assert.That(tResult, Is.InstanceOf<DynamicObjects.List>());
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
            tBuilder.ArraySetup(() => Array.Empty<object>());

            var tResult = tBuilder.Array();

            Assert.That(tResult, Is.InstanceOf<DynamicObjects.List>());
        }

        [Test]
        public void TestObjectSetupWithFactoryFunction()
        {
            IBuilder tBuilder = Builder.New<PropPoco>();
            tBuilder.ObjectSetup(() => Array.Empty<object>());

            dynamic tResult = tBuilder.Object();

            Assert.That((object)tResult, Is.InstanceOf<PropPoco>());
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
    }
}
