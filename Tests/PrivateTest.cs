using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dynamitey.SupportLibrary;
using Microsoft.CSharp.RuntimeBinder;
using NUnit.Framework;

namespace Dynamitey.Tests
{
    [TestFixture]
    public class PrivateTest : Helper
    {
  
        [Test]
        public void TestInvokePrivateMethod()
        {
            var tTest = new TestWithPrivateMethod();
            Assert.That((object)Dynamic.InvokeMember(tTest, "Test"), Is.EqualTo(3));
        }

        [Test]
        public void TestInvokePrivateMethodAcrossAssemblyBoundries()
        {
            var tTest = new PublicType();
            Assert.That((object)Dynamic.InvokeMember(tTest, "PrivateMethod", 3), Is.True);
        }

        [Test]
        public void TestInvokeInternalTypeMethodAcrossAssemblyBoundries()
        {
            var tTest = PublicType.InternalInstance;
            Assert.That((object)Dynamic.InvokeMember(tTest, "InternalMethod", 3), Is.True);
        }

        [Test]
        public void TestInvokeDoNotExposePrivateMethod()
        {
            var tTest = new TestWithPrivateMethod();
            var context = InvokeContext.CreateContext;
            Assert.That(() => Dynamic.InvokeMember(context(tTest,this), "Test"), Throws.InstanceOf<RuntimeBinderException>());
        }

        [Test]
        public void TestCacheableDoNotExposePrivateMethod()
        {
            var tTest = new TestWithPrivateMethod();
            var tCachedInvoke = new CacheableInvocation(InvocationKind.InvokeMember, "Test");
            Assert.That(() => tCachedInvoke.Invoke(tTest), Throws.InstanceOf<RuntimeBinderException>());
        }

        [Test]
        public void TestCacheableExposePrivateMethodViaInstance()
        {
            var tTest = new TestWithPrivateMethod();
            var tCachedInvoke = new CacheableInvocation(InvocationKind.InvokeMember, "Test", context: tTest);
            Assert.That(tCachedInvoke.Invoke(tTest), Is.EqualTo(3));
        }

        [Test]
        public void TestCacheableExposePrivateMethodViaType()
        {
            var tTest = new TestWithPrivateMethod();
            var tCachedInvoke = new CacheableInvocation(InvocationKind.InvokeMember, "Test", context: typeof(TestWithPrivateMethod));
            Assert.That( tCachedInvoke.Invoke(tTest), Is.EqualTo(3));
        }

        // Issue #12: InvokeGet works on an instance field but throws
        // RuntimeBinderException on a static field, even though the same class's
        // instance field works fine. Root cause: InvokeGetCallSite's static-context
        // binder(s) never had a call shape that reaches a field at all (see the
        // fix's comments in InvokeHelper-Regular.cs), so a reflection fallback is
        // used once the DLR binder fails.
        [Test]
        public void TestInvokeGetInstanceField()
        {
            var tTest = new ClassWithPrivateFields();
            Assert.That(Dynamic.InvokeGet(tTest, "field"), Is.EqualTo(16));
        }

        [Test]
        public void TestInvokeGetPrivateStaticField()
        {
            var context = InvokeContext.CreateStatic(typeof(ClassWithPrivateFields));
            Assert.That(Dynamic.InvokeGet(context, "other"), Is.EqualTo(17));
        }

        // Found during investigation: the bug is not limited to private fields.
        // The static-context Get binder always attempts "get_" + name (a property
        // accessor method), so it fails for ANY static field - even a fully public
        // field on a fully public type - because fields have no "get_" method.
        [Test]
        public void TestInvokeGetPublicStaticField()
        {
            var context = InvokeContext.CreateStatic(typeof(PublicClassWithPublicStaticField));
            Assert.That(Dynamic.InvokeGet(context, "Other"), Is.EqualTo(42));
        }

        [Test]
        public void TestCacheableInvokeGetPrivateStaticField()
        {
            var tCachedInvoke = new CacheableInvocation(InvocationKind.Get, "other",
                context: InvokeContext.CreateStatic(typeof(ClassWithPrivateFields)));
            Assert.That(tCachedInvoke.Invoke(typeof(ClassWithPrivateFields)), Is.EqualTo(17));
        }

        // Issue #13: InvokeGet with a static context fails to read a private
        // static PROPERTY when the target type isn't a non-nested public type.
        // Each shape below uses its own type/property so the tests don't depend
        // on each other, or on any other test in the suite, having run first -
        // the reported bug was that behaviour changed depending on execution
        // order, so a test that relied on ordering to pass would be validating
        // the wrong thing.
        [Test]
        public void TestInvokeGetPrivateStaticProperty_PublicNestedClass()
        {
            var context = InvokeContext.CreateStatic(typeof(PublicNestedClassWithPrivateStaticProperty));
            Assert.That(Dynamic.InvokeGet(context, "Hello"), Is.EqualTo("World"));
        }

        // The exact shape named in the issue's title: a private nested class.
        [Test]
        public void TestInvokeGetPrivateStaticProperty_PrivateNestedClass()
        {
            var context = InvokeContext.CreateStatic(typeof(PrivateNestedClassWithPrivateStaticProperty));
            Assert.That(Dynamic.InvokeGet(context, "Hello"), Is.EqualTo("World"));
        }

        [Test]
        public void TestInvokeGetPrivateStaticProperty_InternalTopLevelClass()
        {
            var context = InvokeContext.CreateStatic(typeof(InternalClassWithPrivateStaticProperty));
            Assert.That(Dynamic.InvokeGet(context, "Hello"), Is.EqualTo("World"));
        }

        [Test]
        public void TestInvokeGetPrivateStaticProperty_PublicTopLevelClass()
        {
            var context = InvokeContext.CreateStatic(typeof(PublicClassWithPrivateStaticProperty));
            Assert.That(Dynamic.InvokeGet(context, "Hello"), Is.EqualTo("World"));
        }

        // Public nested class shape for issue #13.
        public class PublicNestedClassWithPrivateStaticProperty
        {
            private static string Hello => "World";
        }

        // Private nested class shape for issue #13.
        private class PrivateNestedClassWithPrivateStaticProperty
        {
            private static string Hello => "World";
        }
    }

    public class TestWithPrivateMethod
    {
        private int Test()
        {
            return 3;
        }
    }

    // For issue #12: reproduces the upstream reporter's exact shape - a private
    // instance field alongside a private static field on an internal
    // (default-access) top-level type.
#pragma warning disable CS0414 // fields are only ever read dynamically, never by name
    class ClassWithPrivateFields
    {
        private int field = 16;
        private static int other = 17;
    }
#pragma warning restore CS0414

    // For issue #12, generalized case found during investigation (see
    // TestInvokeGetPublicStaticField).
    public class PublicClassWithPublicStaticField
    {
        public static int Other = 42;
    }

    // For issue #13, shape from upstream PR #27: internal top-level class.
    class InternalClassWithPrivateStaticProperty
    {
        private static string Hello => "World";
    }

    // For issue #13, shape from upstream PR #27: public top-level class.
    public class PublicClassWithPrivateStaticProperty
    {
        private static string Hello => "World";
    }
}
