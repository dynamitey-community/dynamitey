// DynamicObjects.Get.TryInvokeMember's own fallback (past base.TryInvokeMember
// failing) was never exercised by the rest of the suite: every existing Get-based
// test either invokes a real method (base.TryInvokeMember succeeds directly) or
// only ever does plain member gets. These target the fallback itself: a member
// that resolves as a property holding a delegate, called with invoke syntax.
using System;
using Dynamitey.SupportLibrary;
using Microsoft.CSharp.RuntimeBinder;
using NUnit.Framework;

namespace Dynamitey.Tests
{
    public class GetGapTest : Helper
    {
        private class DelegatePropertyPoco
        {
            public Func<int, int, int> Adder { get; set; } = (x, y) => x + y;
        }

        private class NullPropertyPoco
        {
            public object Foo { get; set; }
        }

        [Test]
        public void TestGetMissingMemberInvocationThrowsRuntimeBinderException()
        {
            // Named for what is asserted, not for what the override returns. The DynamicObject
            // Try* method returns false here; the DLR is what converts that false into a
            // RuntimeBinderException at the call site, and the exception is the only part a
            // consumer can observe.
            dynamic tGet = new DynamicObjects.Get(new object());

            Assert.Throws<RuntimeBinderException>(() => tGet.NoSuchMethod(1));
        }

        // Exercises Get.TryInvokeMember's own fallback: base.TryInvokeMember fails (no
        // real "Adder(int,int)" method), so Get falls through to InvokeGet, finds the
        // delegate-valued "Adder" property, and invokes it directly.
        [Test]
        public void TestGetDelegateValuedPropertyInvokedAsMethod()
        {
            dynamic tGet = new DynamicObjects.Get(new DelegatePropertyPoco());

            var tResult = tGet.Adder(2, 3);

            Assert.That((int)tResult, Is.EqualTo(5));
        }

        // Exercises Get.TryGetMember's own false branch: base.TryGetMember fails (no such
        // property on the plain object target), so Get returns false without calling
        // MassageResultBasedOnInterface at all - the DLR turns that false into the exception.
        [Test]
        public void TestGetMissingPropertyThrowsRuntimeBinderException()
        {
            dynamic tGet = new DynamicObjects.Get(new object());

            Assert.Throws<RuntimeBinderException>(() =>
            {
                _ = tGet.NoSuchProperty;
            });
        }

        // Exercises Get.TryGetIndex's own false branch: base.TryGetIndex fails (the plain
        // object target has no indexer), so Get returns false and the DLR raises the
        // exception at the call site.
        [Test]
        public void TestGetIndexMissingIndexerThrowsRuntimeBinderException()
        {
            dynamic tGet = new DynamicObjects.Get(new object());

            Assert.Throws<RuntimeBinderException>(() =>
            {
                _ = tGet[0];
            });
        }

        // Exercises the part of Get.TryInvokeMember's own fallback that
        // TestGetDelegateValuedPropertyInvokedAsMethod does not reach: base.TryInvokeMember
        // fails to invoke "Foo(1, 2)" because the real property's value is null (not a
        // delegate), so Dynamic.InvokeMember can't treat it as callable. Get's own fallback
        // then finds the same property via InvokeGet - which succeeds, returning null - so it
        // returns false itself rather than throwing from inside its own try/catch.
        [Test]
        public void TestGetPropertyThatIsNullFallsBackAndReturnsFalse()
        {
            dynamic tGet = new DynamicObjects.Get(new NullPropertyPoco());

            Assert.Throws<RuntimeBinderException>(() => tGet.Foo(1, 2));
        }
    }
}
