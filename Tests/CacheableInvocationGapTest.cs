// CacheableInvocation.Invoke dispatches on InvocationKind, much like its base
// Invocation, but the rest of the suite exercises only a couple of Kinds
// through it directly. These tests cover the constructor's per-Kind argCount
// validation, the arg-count-mismatch branches in Invoke (including the
// Convert-specific checks), the InvokeContext-target unwrap, the
// InvokeMemberUnknown/InvokeUnknown fallbacks, and Equals/GetHashCode.
using System;
using System.Dynamic;
using Dynamitey.SupportLibrary;
using NUnit.Framework;

namespace Dynamitey.Tests
{
    public class CacheableInvocationGapTest : Helper
    {
        [Test]
        public void TestCreateConvertFactory()
        {
            var tInvocation = CacheableInvocation.CreateConvert(typeof(long));

            var tResult = tInvocation.Invoke(5, typeof(long), false);

            Assert.That(tResult, Is.EqualTo(5L));
        }

        [Test]
        public void TestCreateConvertRequiresConvertType()
        {
            Assert.Throws<ArgumentNullException>(() => CacheableInvocation.CreateConvert(null!));
        }

        [Test]
        public void TestCreateCallFactory()
        {
            var tInvocation = CacheableInvocation.CreateCall(InvocationKind.InvokeMember, "Join",
                new System.Dynamic.CallInfo(2));

            var tResult = tInvocation.Invoke(new ParamsMethodPoco(), "a", "b");

            Assert.That(tResult, Is.EqualTo("a,b"));
        }

        [Test]
        public void TestGetIndexRequiresAtLeastOneArg()
        {
            Assert.Throws<ArgumentException>(() => new CacheableInvocation(InvocationKind.GetIndex, argCount: 0));
        }

        [Test]
        public void TestSetIndexRequiresAtLeastTwoArgs()
        {
            Assert.Throws<ArgumentException>(() => new CacheableInvocation(InvocationKind.SetIndex, argCount: 1));
        }

        [Test]
        public void TestInvokeUnwrapsInvokeContextTarget()
        {
            var tTarget = new PropPoco { Prop1 = "hello" };
            var tContext = InvokeContext.CreateContext(tTarget, null);
            var tInvocation = new CacheableInvocation(InvocationKind.Get, "Prop1");

            var tResult = tInvocation.Invoke(tContext);

            Assert.That(tResult, Is.EqualTo("hello"));
        }

        [Test]
        public void TestInvokeWithNullArgsDefaultsToSingleNullArg()
        {
            var tTarget = new PropPoco();
            var tInvocation = new CacheableInvocation(InvocationKind.Set, "Prop1", argCount: 1);

            tInvocation.Invoke(tTarget, null);

            Assert.That(tTarget.Prop1, Is.Null);
        }

        [Test]
        public void TestInvokeWrongArgCountThrows()
        {
            var tInvocation = new CacheableInvocation(InvocationKind.Get, "Prop1");

            Assert.Throws<ArgumentException>(() => tInvocation.Invoke(new PropPoco(), "unexpected"));
        }

        [Test]
        public void TestInvokeConvertRejectsChangedConvertType()
        {
            var tInvocation = CacheableInvocation.CreateConvert(typeof(long));

            Assert.Throws<ArgumentException>(() => tInvocation.Invoke(5, typeof(int)));
        }

        [Test]
        public void TestInvokeConvertRejectsChangedExplicitFlag()
        {
            var tInvocation = CacheableInvocation.CreateConvert(typeof(long), convertExplicit: false);

            Assert.Throws<ArgumentException>(() => tInvocation.Invoke(5, typeof(long), true));
        }

        [Test]
        public void TestInvokeConvertWithTooManyArgsThrows()
        {
            var tInvocation = CacheableInvocation.CreateConvert(typeof(long));

            Assert.Throws<ArgumentException>(() => tInvocation.Invoke(5, typeof(long), false, "extra"));
        }

        [Test]
        public void TestInvokeMemberUnknownFallsBackToAction()
        {
            var tTarget = new VoidMethodPoco();
            var tInvocation = new CacheableInvocation(InvocationKind.InvokeMemberUnknown, "Action");

            var tResult = tInvocation.Invoke(tTarget);

            Assert.That(tResult, Is.Null);
        }

        [Test]
        public void TestInvokeUnknownFallsBackToAction()
        {
            var tCalled = false;
            Action<int> tAction = x => tCalled = x == 3;
            var tInvocation = new CacheableInvocation(InvocationKind.InvokeUnknown, argCount: 1);

            var tResult = tInvocation.Invoke(tAction, 3);

            Assert.That(tResult, Is.Null);
            Assert.That(tCalled, Is.True);
        }

        [Test]
        public void TestUnknownKindThrows()
        {
            var tInvocation = new CacheableInvocation(InvocationKind.NotSet);

            Assert.Throws<InvalidOperationException>(() => tInvocation.Invoke(new object()));
        }

        [Test]
        public void TestAddAssignSubtractAssignAndIsEvent()
        {
            var tTarget = new PocoEvent();
            var tHit = false;
            EventHandler<EventArgs> tHandler = (s, e) => tHit = true;

            var tIsEventInvocation = new CacheableInvocation(InvocationKind.IsEvent, "Event");
            Assert.That(tIsEventInvocation.Invoke(tTarget), Is.True);

            var tAddInvocation = new CacheableInvocation(InvocationKind.AddAssign, "Event");
            tAddInvocation.Invoke(tTarget, tHandler);
            tTarget.OnEvent(tTarget, EventArgs.Empty);
            Assert.That(tHit, Is.True);

            tHit = false;
            var tSubtractInvocation = new CacheableInvocation(InvocationKind.SubtractAssign, "Event");
            tSubtractInvocation.Invoke(tTarget, tHandler);
            tTarget.OnEvent(tTarget, EventArgs.Empty);
            Assert.That(tHit, Is.False);
        }

        [Test]
        public void TestEqualsAndHashCode()
        {
            var tA = new CacheableInvocation(InvocationKind.Get, "Prop1");
            var tB = new CacheableInvocation(InvocationKind.Get, "Prop1");
            var tDifferentKind = new CacheableInvocation(InvocationKind.Set, "Prop1", argCount: 1);

            Assert.That(tA.Equals(tA), Is.True);
            Assert.That(tA.Equals(tB), Is.True);
            Assert.That(tA.Equals(tDifferentKind), Is.False);
            Assert.That(tA.Equals((CacheableInvocation)null), Is.False);

            Assert.That(tA.Equals((object)tB), Is.True);
            Assert.That(tA.Equals((object)null), Is.False);
            Assert.That(tA.Equals(new object()), Is.False);

            Assert.That(tA.GetHashCode(), Is.EqualTo(tB.GetHashCode()));
        }
    }
}
