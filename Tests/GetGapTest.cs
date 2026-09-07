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

        [Test]
        public void TestGetMissingMemberInvocationReturnsFalseAndThrows()
        {
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
    }
}
