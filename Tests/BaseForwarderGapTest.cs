// BaseForwarder.cs (DynamicObjects.BaseForwarder, exercised here through the
// existing bare TestForwarder subclass in DynamicObjects.cs) has several
// branches the rest of the suite never triggers: the RuntimeBinderException
// catches on each Try* override (reached only when the underlying DLR bind
// genuinely fails), the null-CallTarget short-circuits, TryInvoke's
// Invoke-then-InvokeAction fallback, the event add/remove branches in
// TrySetMember, and Equals/GetHashCode.
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Dynamitey.SupportLibrary;
using Microsoft.CSharp.RuntimeBinder;
using NUnit.Framework;

namespace Dynamitey.Tests
{
    public class BaseForwarderGapTest : Helper
    {
        [Test]
        public void TestForwarderGetMissingMemberThrows()
        {
            dynamic tFwd = new DynamicObjs.TestForwarder(new object());
            Assert.Throws<RuntimeBinderException>(() => { var tValue = tFwd.NoSuchMember; });
        }

        [Test]
        public void TestForwarderInvokeDelegateTarget()
        {
            Func<int, int> tAddOne = x => x + 1;
            dynamic tFwd = new DynamicObjs.TestForwarder(tAddOne);

            var tResult = tFwd(5);

            Assert.That(tResult, Is.EqualTo(6));
        }

        [Test]
        public void TestForwarderInvokeNullTargetThrows()
        {
            dynamic tFwd = new DynamicObjs.TestForwarder(null!);
            Assert.Throws<RuntimeBinderException>(() => tFwd());
        }

        [Test]
        public void TestForwarderInvokeActionTargetFallsBackToInvokeAction()
        {
            var tCalled = false;
            Action<int> tAction = x => tCalled = x == 7;
            dynamic tFwd = new DynamicObjs.TestForwarder(tAction);

            tFwd(7);

            Assert.That(tCalled, Is.True);
        }

        [Test]
        public void TestForwarderInvokeBothFailThrows()
        {
            Func<int, int> tAddOne = x => x + 1;
            dynamic tFwd = new DynamicObjs.TestForwarder(tAddOne);

            Assert.Throws<RuntimeBinderException>(() => tFwd(1, 2, 3));
        }

        [Test]
        public void TestForwarderInvokeMemberNullTargetThrows()
        {
            dynamic tFwd = new DynamicObjs.TestForwarder(null!);
            Assert.Throws<RuntimeBinderException>(() => tFwd.SomeMethod(1));
        }

        [Test]
        public void TestForwarderInvokeVoidMemberFallsBackToInvokeMemberAction()
        {
            var tTarget = new ParamsActionMethodPoco();
            dynamic tFwd = new DynamicObjs.TestForwarder(tTarget);

            tFwd.Join("a", "b");

            Assert.That(tTarget.Joined, Is.EqualTo("a,b"));
        }

        [Test]
        public void TestForwarderInvokeMemberBothFailThrows()
        {
            dynamic tFwd = new DynamicObjs.TestForwarder(new object());
            Assert.Throws<RuntimeBinderException>(() => tFwd.NoSuchMethod(1));
        }

        [Test]
        public void TestForwarderSetMember()
        {
            dynamic tInner = new ExpandoObject();
            dynamic tFwd = new DynamicObjs.TestForwarder(tInner);

            tFwd.Foo = "Bar";

            Assert.That((string)tInner.Foo, Is.EqualTo("Bar"));
        }

        [Test]
        public void TestForwarderSetMemberEventAddSubtract()
        {
            var tEvent = new TestEvent();
            dynamic tFwd = new DynamicObjs.TestForwarder(tEvent);
            var tHit = false;
            EventHandler<EventArgs> tHandler = (s, e) => tHit = true;

            tFwd.Event += tHandler;
            tEvent.OnEvent(tEvent, EventArgs.Empty);
            Assert.That(tHit, Is.True);

            tHit = false;
            tFwd.Event -= tHandler;
            tEvent.OnEvent(tEvent, EventArgs.Empty);
            Assert.That(tHit, Is.False);
        }

        [Test]
        public void TestForwarderSetMemberFailureThrows()
        {
            dynamic tFwd = new DynamicObjs.TestForwarder(new object());
            Assert.Throws<RuntimeBinderException>(() => tFwd.NoSuchProp = 5);
        }

        [Test]
        public void TestForwarderSetMemberNullTargetReturnsFalse()
        {
            dynamic tFwd = new DynamicObjs.TestForwarder(null!);
            Assert.Throws<RuntimeBinderException>(() => tFwd.Foo = 5);
        }

        [Test]
        public void TestForwarderGetIndex()
        {
            var tList = new List<int> { 10, 20, 30 };
            dynamic tFwd = new DynamicObjs.TestForwarder(tList);

            Assert.That(tFwd[1], Is.EqualTo(20));
        }

        [Test]
        public void TestForwarderGetIndexNullTargetThrows()
        {
            dynamic tFwd = new DynamicObjs.TestForwarder(null!);
            Assert.Throws<RuntimeBinderException>(() => { var tValue = tFwd[0]; });
        }

        [Test]
        public void TestForwarderGetIndexFailureThrows()
        {
            dynamic tFwd = new DynamicObjs.TestForwarder(new object());
            Assert.Throws<RuntimeBinderException>(() => { var tValue = tFwd[0]; });
        }

        [Test]
        public void TestForwarderSetIndex()
        {
            var tList = new List<int> { 10, 20, 30 };
            dynamic tFwd = new DynamicObjs.TestForwarder(tList);

            tFwd[1] = 99;

            Assert.That(tList[1], Is.EqualTo(99));
        }

        [Test]
        public void TestForwarderSetIndexNullTargetReturnsFalse()
        {
            dynamic tFwd = new DynamicObjs.TestForwarder(null!);
            Assert.Throws<RuntimeBinderException>(() => tFwd[0] = 1);
        }

        [Test]
        public void TestForwarderSetIndexFailureThrows()
        {
            dynamic tFwd = new DynamicObjs.TestForwarder(new object());
            Assert.Throws<RuntimeBinderException>(() => tFwd[0] = 1);
        }

        [Test]
        public void TestForwarderGetDynamicMemberNamesFallsBackToReflection()
        {
            var tFwd = new DynamicObjs.TestForwarder(new { SomeProp = 1 });

            var tNames = tFwd.GetDynamicMemberNames().ToList();

            Assert.That(tNames, Does.Contain("SomeProp"));
        }

        // BUG (found while writing this coverage): when the wrapped target itself reports
        // dynamic-only member names (Dynamic.GetMemberNames(..., dynamicOnly: true).Any()
        // is true), BaseForwarder.GetDynamicMemberNames returns `base.GetDynamicMemberNames()`
        // - DynamicObject's own default implementation, which is always empty - instead of
        // the `tDyanmic` list it just computed. So a forwarder wrapping an ExpandoObject (or
        // any other genuinely dynamic target) always reports zero dynamic member names; only
        // the "target has no dynamic members, fall back to reflection" branch actually
        // returns anything. This test pins the current (buggy, always-empty) behaviour rather
        // than papering over it - see the coverage task's report for the write-up.
        [Test]
        public void TestForwarderGetDynamicMemberNamesFromDynamicTargetIsAlwaysEmpty()
        {
            dynamic tInner = new ExpandoObject();
            tInner.Foo = "Bar";
            var tFwd = new DynamicObjs.TestForwarder((object)tInner);

            var tNames = tFwd.GetDynamicMemberNames().ToList();

            Assert.That(tNames, Is.Empty);
        }

        [Test]
        public void TestForwarderEqualsTypedOverload()
        {
            var tTarget = new object();
            var tOtherTarget = new object();
            var tFwd1 = new DynamicObjs.TestForwarder(tTarget);
            var tFwd2 = new DynamicObjs.TestForwarder(tTarget);
            var tFwd3 = new DynamicObjs.TestForwarder(tOtherTarget);
            var tFwdNull = new DynamicObjs.TestForwarder(null!);

            Assert.That(tFwd1.Equals(tFwd1), Is.True);
            Assert.That(tFwd1.Equals(tFwd2), Is.True);
            Assert.That(tFwd1.Equals(tFwd3), Is.False);
            Assert.That(tFwd1.Equals((Dynamitey.DynamicObjects.BaseForwarder)null), Is.False);
            Assert.That(tFwdNull.Equals((Dynamitey.DynamicObjects.BaseForwarder)null), Is.True);
        }

        // BUG (found while writing this coverage): BaseForwarder.Equals(object?) checks
        // `obj.GetType() != typeof(BaseForwarder)` - a literal comparison against the
        // *abstract* base type - rather than `obj.GetType() != GetType()`. BaseForwarder
        // can never be instantiated directly, so that check is false for every real
        // subclass instance and the method always falls through to `return false` instead
        // of ever reaching `Equals((BaseForwarder)obj)`. The strongly-typed
        // Equals(BaseForwarder?) overload (tested above) is correct; only the
        // object.Equals(object?) override is broken. This test pins the current
        // (buggy) behaviour rather than papering over it - see the coverage task's
        // report for the write-up.
        [Test]
        public void TestForwarderEqualsObjectOverloadNeverMatchesConcreteSubclassInstances()
        {
            var tTarget = new object();
            var tFwd1 = new DynamicObjs.TestForwarder(tTarget);
            var tFwd2 = new DynamicObjs.TestForwarder(tTarget);
            var tFwdNull = new DynamicObjs.TestForwarder(null!);

            Assert.That(tFwd1.Equals((object)tFwd1), Is.True, "reference-equal shortcut still works");
            Assert.That(tFwd1.Equals((object)null), Is.False, "CallTarget is non-null, obj is null");
            Assert.That(tFwdNull.Equals((object)null), Is.True, "both CallTarget and obj are null");

            // Would be True under a correct GetType()-based comparison; is False because of
            // the bug described above.
            Assert.That(tFwd1.Equals((object)tFwd2), Is.False);
        }

        [Test]
        public void TestForwarderGetHashCode()
        {
            var tTarget = new object();
            var tFwd = new DynamicObjs.TestForwarder(tTarget);
            var tFwdNull = new DynamicObjs.TestForwarder(null!);

            Assert.That(tFwd.GetHashCode(), Is.EqualTo(tTarget.GetHashCode()));
            Assert.That(tFwdNull.GetHashCode(), Is.EqualTo(0));
        }
    }
}
