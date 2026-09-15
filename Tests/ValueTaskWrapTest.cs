// Issue #100. WrapIfResultTypeInaccessible covered Task<T> only. A
// ValueTask<InternalResult> from another assembly failed dynamic await
// with ValueType.GetAwaiter. Public ValueTask<T> from #15 must stay unwrapped.
using System.Threading.Tasks;
using Dynamitey.SupportLibrary;
using NUnit.Framework;

namespace Dynamitey.Tests
{
    public class ValueTaskWrapTest : Helper
    {
        [Test]
        public async Task InvokeMemberAwaitsInaccessibleValueTaskResult()
        {
            object tTarget = PublicType.AsyncInternalResultInstance;

            object tResult = await Dynamic.InvokeMember(tTarget, "GetInternalValueTaskAsync", "ok");

            Assert.That(tResult, Is.Not.Null);
            Assert.That(tResult.GetType().GetProperty("Value")?.GetValue(tResult), Is.EqualTo("ok"));
        }

        [Test]
        public void InvokeMemberWrapsInaccessibleValueTaskResult()
        {
            object tTarget = PublicType.AsyncInternalResultInstance;

            object tRaw = Dynamic.InvokeMember(tTarget, "GetInternalValueTaskAsync", "ok");

            Assert.That(tRaw, Is.InstanceOf<AwaitableResult>());
        }

        [Test]
        public async Task InvokeMemberAwaitsCompletedInaccessibleValueTaskResult()
        {
            object tTarget = PublicType.AsyncInternalResultInstance;

            object tResult = await Dynamic.InvokeMember(tTarget, "GetCompletedInternalValueTaskAsync", "done");

            Assert.That(tResult.GetType().GetProperty("Value")?.GetValue(tResult), Is.EqualTo("done"));
        }

        [Test]
        public async Task InvokeMemberDoesNotWrapPublicValueTaskResult()
        {
            object tTarget = PublicType.PublicAsyncResultInstance;

            object tRaw = Dynamic.InvokeMember(tTarget, "GetPublicValueTaskAsync", "value");
            Assert.That(tRaw, Is.Not.InstanceOf<AwaitableResult>());

            var tAwaited = await Dynamic.InvokeMember(tTarget, "GetPublicValueTaskAsync", "value2");
            Assert.That((string)tAwaited, Is.EqualTo("value2"));
        }

        [Test]
        public async Task CacheableInvocationAwaitsInaccessibleValueTaskResult()
        {
            object tTarget = PublicType.AsyncInternalResultInstance;
            var tInvocation = new CacheableInvocation(
                InvocationKind.InvokeMember, "GetInternalValueTaskAsync", argCount: 1, context: tTarget);

            object tRaw = tInvocation.Invoke(tTarget, "ok");
            Assert.That(tRaw, Is.InstanceOf<AwaitableResult>());

            object tResult = await (dynamic)tInvocation.Invoke(tTarget, "ok2");
            Assert.That(tResult.GetType().GetProperty("Value")?.GetValue(tResult), Is.EqualTo("ok2"));
        }
    }
}
