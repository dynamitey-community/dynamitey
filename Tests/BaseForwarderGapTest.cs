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
        public void TestForwarderSetMemberNullTargetThrowsRuntimeBinderException()
        {
            // Named for what is asserted, not for what the override returns. The DynamicObject
            // Try* method returns false here; the DLR is what converts that false into a
            // RuntimeBinderException at the call site, and the exception is the only part a
            // consumer can observe.
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
        public void TestForwarderSetIndexNullTargetThrowsRuntimeBinderException()
        {
            // Named for what is asserted, not for what the override returns. The DynamicObject
            // Try* method returns false here; the DLR is what converts that false into a
            // RuntimeBinderException at the call site, and the exception is the only part a
            // consumer can observe.
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

        // Issue #67. The branches were inverted: when the target DID report dynamic members
        // this returned base.GetDynamicMemberNames() - DynamicObject's own, always empty -
        // discarding the list it had just computed. A forwarder over an ExpandoObject
        // therefore reported nothing, so a debugger's dynamic view, an
        // IDynamicMetaObjectProvider consumer, or anything serializing a wrapped expando saw
        // no members at all.
        [Test]
        public void TestForwarderGetDynamicMemberNamesReportsTheTargetsDynamicMembers()
        {
            dynamic tInner = new ExpandoObject();
            tInner.Foo = "Bar";
            tInner.Baz = 42;
            var tFwd = new DynamicObjs.TestForwarder((object)tInner);

            var tNames = tFwd.GetDynamicMemberNames().ToList();

            Assert.That(tNames, Is.EquivalentTo(new[] { "Foo", "Baz" }));
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

        // Issue #67. Equals(object?) used to guard on `obj.GetType() != typeof(BaseForwarder)`
        // - a literal comparison against the ABSTRACT base type. No instance's runtime type is
        // ever equal to it, so the guard was unconditionally true and the typed overload
        // beneath was unreachable: two forwarders over the same target compared unequal.
        [Test]
        public void TestForwarderEqualsObjectOverloadReachesTheTypedOverload()
        {
            var tTarget = new object();
            var tFwd1 = new DynamicObjs.TestForwarder(tTarget);
            var tFwd2 = new DynamicObjs.TestForwarder(tTarget);
            var tFwdOther = new DynamicObjs.TestForwarder(new object());
            var tFwdNull = new DynamicObjs.TestForwarder(null!);

            Assert.That(tFwd1.Equals((object)tFwd1), Is.True, "reference-equal shortcut still works");
            Assert.That(tFwd1.Equals((object)null), Is.False, "CallTarget is non-null, obj is null");
            Assert.That(tFwdNull.Equals((object)null), Is.True, "both CallTarget and obj are null");

            Assert.That(tFwd1.Equals((object)tFwd2), Is.True, "same target, so equal - this is what the bug blocked");
            Assert.That(tFwd1.Equals((object)tFwdOther), Is.False, "different targets are still unequal");
            Assert.That(tFwd1.Equals((object)"not a forwarder"), Is.False, "an unrelated type is still unequal");
        }

        // Equality here is target-based and deliberately ignores the wrapper's own type: the
        // typed Equals compares only CallTarget, and GetHashCode hashes only CallTarget. A
        // GetType()-based guard would have made these two unequal while still handing them the
        // same hash code - legal, but it would put Equals(object) at odds with both the typed
        // overload and the hash.
        [Test]
        public void TestForwarderEqualsAcrossDifferentSubclassesWrappingTheSameTarget()
        {
            var tTarget = new object();
            object tOne = new DynamicObjs.TestForwarder(tTarget);
            object tTwo = new DynamicObjects.Get(tTarget);

            Assert.That(tOne.GetHashCode(), Is.EqualTo(tTwo.GetHashCode()),
                "premise: both hash their CallTarget, so the hashes already agreed");
            Assert.That(tOne.Equals(tTwo), Is.True);
            Assert.That(tTwo.Equals(tOne), Is.True, "and symmetrically");
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
