// Issue #107. CacheableInvocation.InvokeMember returned the helper's raw
// Task<T> and skipped Dynamic.InvokeMember's WrapIfResultTypeInaccessible.
// Awaiting that object from another assembly threw RuntimeBinderException
// (void-to-object) when T is internal. Types live in SupportLibrary so the
// result type stays inaccessible to this assembly.
using System;
using System.Threading.Tasks;
using Dynamitey.SupportLibrary;
using NUnit.Framework;

namespace Dynamitey.Tests
{
    public class CacheableTaskWrapTest : Helper
    {
        [Test]
        public async Task CachedInvokeMemberAwaitsInaccessibleTaskResult()
        {
            object tTarget = PublicType.AsyncInternalResultInstance;
            var tInvocation = new CacheableInvocation(
                InvocationKind.InvokeMember, "GetInternalResultAsync", argCount: 1, context: tTarget);

            object tResult = await (dynamic)tInvocation.Invoke(tTarget, "ok");

            Assert.That(tResult, Is.Not.Null);
            Assert.That(tResult.GetType().GetProperty("Value")?.GetValue(tResult), Is.EqualTo("ok"));
        }

        [Test]
        public void CachedInvokeMemberWrapsInaccessibleTaskResult()
        {
            object tTarget = PublicType.AsyncInternalResultInstance;
            var tInvocation = new CacheableInvocation(
                InvocationKind.InvokeMember, "GetInternalResultAsync", argCount: 1, context: tTarget);

            object tRaw = tInvocation.Invoke(tTarget, "ok");

            Assert.That(tRaw, Is.InstanceOf<AwaitableResult>());
        }

        [Test]
        public async Task CachedInvokeMemberUnknownAwaitsInaccessibleTaskResult()
        {
            object tTarget = PublicType.AsyncInternalResultInstance;
            var tInvocation = new CacheableInvocation(
                InvocationKind.InvokeMemberUnknown, "GetInternalResultAsync", argCount: 1, context: tTarget);

            object tResult = await (dynamic)tInvocation.Invoke(tTarget, "ok");

            Assert.That(tResult, Is.Not.Null);
            Assert.That(tResult.GetType().GetProperty("Value")?.GetValue(tResult), Is.EqualTo("ok"));
        }

        [Test]
        public async Task CachedInvokeMemberDoesNotWrapPublicTaskResult()
        {
            object tTarget = PublicType.PublicAsyncResultInstance;
            var tInvocation = new CacheableInvocation(
                InvocationKind.InvokeMember, "GetPublicResultAsync", argCount: 1, context: tTarget);

            object tRaw = tInvocation.Invoke(tTarget, "value");
            Assert.That(tRaw, Is.InstanceOf<Task<string>>());
            Assert.That(tRaw, Is.Not.InstanceOf<AwaitableResult>());

            var tAwaited = await (dynamic)tInvocation.Invoke(tTarget, "value2");
            Assert.That((string)tAwaited, Is.EqualTo("value2"));
        }

        [Test]
        public void CachedInvokeMemberAwaitPropagatesFault()
        {
            object tTarget = PublicType.FaultingAsyncResultInstance;
            var tInvocation = new CacheableInvocation(
                InvocationKind.InvokeMember, "GetFaultingResultAsync", context: tTarget);

            Assert.That(
                async () => await (dynamic)tInvocation.Invoke(tTarget),
                Throws.InstanceOf<InvalidTimeZoneException>().With.Message.EqualTo("boom"));
        }

        [Test]
        public void CachedInvokeMemberAwaitPropagatesCancellation()
        {
            object tTarget = PublicType.CancelingAsyncResultInstance;
            var tInvocation = new CacheableInvocation(
                InvocationKind.InvokeMember, "GetCancelingResultAsync", context: tTarget);

            Assert.That(
                async () => await (dynamic)tInvocation.Invoke(tTarget),
                Throws.InstanceOf<OperationCanceledException>());
        }

        [Test]
        public async Task CachedInvokeMemberDoesNotWrapNonGenericTask()
        {
            object tTarget = PublicType.PlainTaskAsyncInstance;
            var tInvocation = new CacheableInvocation(
                InvocationKind.InvokeMember, "GetPlainTaskAsync", context: tTarget);

            object tRaw = tInvocation.Invoke(tTarget);
            Assert.That(tRaw, Is.InstanceOf<Task>());
            Assert.That(tRaw, Is.Not.InstanceOf<AwaitableResult>());

            await (dynamic)tInvocation.Invoke(tTarget);
        }

        [Test]
        public async Task CachedInvokeMemberReusesCallSiteAndStillWraps()
        {
            object tTarget = PublicType.AsyncInternalResultInstance;
            var tInvocation = new CacheableInvocation(
                InvocationKind.InvokeMember, "GetInternalResultAsync", argCount: 1, context: tTarget);

            object tFirst = tInvocation.Invoke(tTarget, "one");
            object tSecond = tInvocation.Invoke(tTarget, "two");

            Assert.That(tFirst, Is.InstanceOf<AwaitableResult>());
            Assert.That(tSecond, Is.InstanceOf<AwaitableResult>());

            object tAwaited = await (dynamic)tSecond;
            Assert.That(tAwaited.GetType().GetProperty("Value")?.GetValue(tAwaited), Is.EqualTo("two"));
        }

        [Test]
        public async Task CurryThroughCacheableInvocationAwaitsInaccessibleTaskResult()
        {
            object tTarget = PublicType.AsyncInternalResultInstance;
            dynamic tCurry = Dynamic.Curry(tTarget).GetInternalResultAsync();

            object tResult = await tCurry("ok");

            Assert.That(tResult, Is.Not.Null);
            Assert.That(tResult.GetType().GetProperty("Value")?.GetValue(tResult), Is.EqualTo("ok"));
        }
    }
}
