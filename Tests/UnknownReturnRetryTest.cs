// Issue #102. InvokeMemberUnknown / InvokeUnknown catch every RuntimeBinderException
// from the value-returning CallSite and retry as an action. That conflates a
// bind failure (void member, so the Func site cannot represent the result)
// with an exception thrown after the target has already started running.
// Retrying then duplicates side effects. Existing fallback tests only cover
// genuine void members, which never increment a call counter.
using System;
using Microsoft.CSharp.RuntimeBinder;
using NUnit.Framework;

namespace Dynamitey.Tests
{
    public class UnknownReturnRetryTest : Helper
    {
        public class ThrowsFromBody
        {
            public int Calls;

            public object Run()
            {
                Calls++;
                throw new RuntimeBinderException("from application");
            }

            public void Act()
            {
                Calls++;
            }

            public object RunInnerDynamic()
            {
                Calls++;
                dynamic tInner = 1;
                return tInner.NoSuchMember;
            }
        }

        [Test]
        public void CacheableInvokeMemberUnknownDoesNotRetryWhenBodyThrows()
        {
            var tTarget = new ThrowsFromBody();
            var tInvocation = new CacheableInvocation(InvocationKind.InvokeMemberUnknown, "Run");

            Assert.That(() => tInvocation.Invoke(tTarget), Throws.InstanceOf<RuntimeBinderException>());
            Assert.That(tTarget.Calls, Is.EqualTo(1));
        }

        [Test]
        public void InvocationInvokeMemberUnknownDoesNotRetryWhenBodyThrows()
        {
            var tTarget = new ThrowsFromBody();
            var tInvocation = new Invocation(InvocationKind.InvokeMemberUnknown, "Run");

            Assert.That(() => tInvocation.Invoke(tTarget), Throws.InstanceOf<RuntimeBinderException>());
            Assert.That(tTarget.Calls, Is.EqualTo(1));
        }

        [Test]
        public void CacheableInvokeMemberUnknownDoesNotRetryWhenInnerDynamicThrows()
        {
            var tTarget = new ThrowsFromBody();
            var tInvocation = new CacheableInvocation(InvocationKind.InvokeMemberUnknown, "RunInnerDynamic");

            Assert.That(() => tInvocation.Invoke(tTarget), Throws.InstanceOf<RuntimeBinderException>());
            Assert.That(tTarget.Calls, Is.EqualTo(1));
        }

        [Test]
        public void CacheableInvokeMemberUnknownStillInvokesVoidOnce()
        {
            var tTarget = new ThrowsFromBody();
            var tInvocation = new CacheableInvocation(InvocationKind.InvokeMemberUnknown, "Act");

            var tResult = tInvocation.Invoke(tTarget);

            Assert.That(tResult, Is.Null);
            Assert.That(tTarget.Calls, Is.EqualTo(1));
        }

        [Test]
        public void CacheableInvokeUnknownDoesNotRetryDelegateThatThrows()
        {
            var tCalls = 0;
            Func<object> tFunc = () =>
            {
                tCalls++;
                throw new RuntimeBinderException("from application");
            };
            var tInvocation = new CacheableInvocation(InvocationKind.InvokeUnknown);

            Assert.That(() => tInvocation.Invoke(tFunc), Throws.InstanceOf<RuntimeBinderException>());
            Assert.That(tCalls, Is.EqualTo(1));
        }

        [Test]
        public void CacheableInvokeUnknownStillInvokesVoidDelegateOnce()
        {
            var tCalls = 0;
            Action tAction = () => tCalls++;
            var tInvocation = new CacheableInvocation(InvocationKind.InvokeUnknown);

            var tResult = tInvocation.Invoke(tAction);

            Assert.That(tResult, Is.Null);
            Assert.That(tCalls, Is.EqualTo(1));
        }

        [Test]
        public void ForwarderInvokeMemberDoesNotRetryWhenBodyThrows()
        {
            var tTarget = new ThrowsFromBody();
            dynamic tFwd = new DynamicObjs.TestForwarder(tTarget);

            Assert.That(() => tFwd.Run(), Throws.InstanceOf<RuntimeBinderException>());
            Assert.That(tTarget.Calls, Is.EqualTo(1));
        }

        [Test]
        public void ForwarderInvokeDoesNotRetryDelegateThatThrows()
        {
            var tCalls = 0;
            Func<object> tFunc = () =>
            {
                tCalls++;
                throw new RuntimeBinderException("from application");
            };
            dynamic tFwd = new DynamicObjs.TestForwarder(tFunc);

            Assert.That(() => tFwd(), Throws.InstanceOf<RuntimeBinderException>());
            Assert.That(tCalls, Is.EqualTo(1));
        }

        [Test]
        public void ForwarderInvokeMemberStillInvokesVoidOnce()
        {
            var tTarget = new ThrowsFromBody();
            dynamic tFwd = new DynamicObjs.TestForwarder(tTarget);

            tFwd.Act();

            Assert.That(tTarget.Calls, Is.EqualTo(1));
        }
    }
}
