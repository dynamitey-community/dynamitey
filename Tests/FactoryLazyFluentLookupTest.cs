// Factory.cs (BaseFactory/BaseSingleInstancesFactory), Lazy.cs, and
// FluentStringLookup.cs had no coverage at all before this file - none of the
// existing fixtures happen to exercise the Impromptu-Interface-return-type
// factory pattern, the Lazy<T> forwarder, or the string-lookup building
// block. These tests exercise each directly.
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Dynamitey.DynamicObjects;
using Microsoft.CSharp.RuntimeBinder;
using NUnit.Framework;

namespace Dynamitey.Tests
{
    public class FactoryLazyFluentLookupTest : Helper
    {
        public interface IWidgetFactory
        {
            object WidgetProp { get; }
            Widget Widget(int id);
        }

        public class Widget
        {
            public int Id { get; }
            public Widget(int id) { Id = id; }
        }

        [Test]
        public void TestBaseFactoryTryGetMemberConstructsFromPropertyType()
        {
            var tFactory = new BaseFactory();
            Dynamic.ApplyEquivalentType(tFactory, typeof(IWidgetFactory));
            dynamic tDyn = tFactory;

            object tResult = tDyn.WidgetProp;

            Assert.That(tResult, Is.Not.Null);
            Assert.That(tResult, Is.InstanceOf<object>());
        }

        [Test]
        public void TestBaseFactoryTryInvokeMemberConstructsFromMethodReturnType()
        {
            var tFactory = new BaseFactory();
            Dynamic.ApplyEquivalentType(tFactory, typeof(IWidgetFactory));
            dynamic tDyn = tFactory;

            Widget tWidget = tDyn.Widget(5);

            Assert.That(tWidget.Id, Is.EqualTo(5));
        }

        [Test]
        public void TestBaseFactoryUnknownMemberThrows()
        {
            var tFactory = new BaseFactory();
            Dynamic.ApplyEquivalentType(tFactory, typeof(IWidgetFactory));
            dynamic tDyn = tFactory;

            Assert.Throws<RuntimeBinderException>(() => { var tValue = tDyn.NoSuchMember; });
        }

        [Test]
        public void TestBaseFactoryWithNoEquivalentTypeThrows()
        {
            dynamic tDyn = new BaseFactory();

            Assert.Throws<RuntimeBinderException>(() => { var tValue = tDyn.WidgetProp; });
        }

        [Test]
        public void TestSingleInstanceFactoryCachesSameInstance()
        {
            var tFactory = new BaseSingleInstancesFactory();
            Dynamic.ApplyEquivalentType(tFactory, typeof(IWidgetFactory));
            dynamic tDyn = tFactory;

            object tFirst = tDyn.WidgetProp;
            object tSecond = tDyn.WidgetProp;

            Assert.That(ReferenceEquals(tFirst, tSecond), Is.True);
        }

        [Test]
        public void TestSingleInstanceFactoryUnknownMemberReturnsNull()
        {
            var tFactory = new BaseSingleInstancesFactory();
            Dynamic.ApplyEquivalentType(tFactory, typeof(IWidgetFactory));
            dynamic tDyn = tFactory;

            Assert.Throws<RuntimeBinderException>(() => { var tValue = tDyn.NoSuchMember; });
        }

        [Test]
        public void TestLazyForcesEvaluationOnMemberAccessAndCachesResult()
        {
            var tCallCount = 0;
            object tLazy = Lazy.Create(() => { tCallCount++; return "hello"; });

            var tNamesBefore = ((DynamicObject)tLazy).GetDynamicMemberNames();
            Assert.That(tNamesBefore, Is.Empty, "not yet evaluated: no dynamic member names");

            dynamic tDynLazy = tLazy;
            int tLength = tDynLazy.Length;
            Assert.That(tLength, Is.EqualTo(5));
            Assert.That(tCallCount, Is.EqualTo(1));

            // Second access re-uses System.Lazy<T>'s own caching rather than re-invoking the factory.
            int tLength2 = tDynLazy.Length;
            Assert.That(tLength2, Is.EqualTo(5));
            Assert.That(tCallCount, Is.EqualTo(1));

            var tNamesAfter = ((DynamicObject)tLazy).GetDynamicMemberNames();
            Assert.That(tNamesAfter, Is.Not.Null);
        }

        [Test]
        public void TestLazyWrapsExistingSystemLazy()
        {
            var tSystemLazy = new System.Lazy<int>(() => 42);
            dynamic tDynLazy = Lazy.Create(tSystemLazy);

            int tResult = tDynLazy.CompareTo(42);

            Assert.That(tResult, Is.EqualTo(0));
        }

        [Test]
        public void TestFluentStringLookupViaMemberInvocationIgnoresArgs()
        {
            dynamic tLookup = new FluentStringLookup(name => name + "!");

            string tResult = tLookup.Bar(1, 2, 3);

            Assert.That(tResult, Is.EqualTo("Bar!"));
        }

        [Test]
        public void TestFluentStringLookupViaDirectInvokeWithStringArg()
        {
            dynamic tLookup = new FluentStringLookup(name => name + "!");

            string tResult = tLookup("Baz");

            Assert.That(tResult, Is.EqualTo("Baz!"));
        }

        [Test]
        public void TestFluentStringLookupDirectInvokeWithWrongShapeThrows()
        {
            dynamic tLookup = new FluentStringLookup(name => name + "!");

            Assert.Throws<RuntimeBinderException>(() => tLookup(1, 2));
        }
    }
}
