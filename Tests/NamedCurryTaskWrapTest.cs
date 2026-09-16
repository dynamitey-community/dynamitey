// Issue #135. PartialApply uses Invocation (not CacheableInvocation) when
// names are present. Invocation.InvokeMemberUnknown skipped
// WrapIfResultTypeInaccessible, so named curry of an inaccessible Task<T>
// failed dynamic await. Types live in SupportLibrary.
using System.Threading.Tasks;
using Dynamitey.SupportLibrary;
using NUnit.Framework;

namespace Dynamitey.Tests
{
    public class NamedCurryTaskWrapTest : Helper
    {
        [Test]
        public async Task NamedCurryAwaitsInaccessibleTaskResult()
        {
            object tTarget = PublicType.AsyncInternalResultInstance;
            dynamic tComplete = Dynamic.Curry(tTarget).GetInternalResultAsync(value: "ok");

            object tResult = await tComplete();

            Assert.That(tResult.GetType().GetProperty("Value")?.GetValue(tResult), Is.EqualTo("ok"));
        }

        [Test]
        public void NamedCurryWrapsInaccessibleTaskResult()
        {
            object tTarget = PublicType.AsyncInternalResultInstance;
            dynamic tComplete = Dynamic.Curry(tTarget).GetInternalResultAsync(value: "ok");

            object tRaw = tComplete();

            Assert.That(tRaw, Is.InstanceOf<AwaitableResult>());
        }

        [Test]
        public async Task NamedCurryAwaitsInaccessibleValueTaskResult()
        {
            object tTarget = PublicType.AsyncInternalResultInstance;
            dynamic tComplete = Dynamic.Curry(tTarget).GetInternalValueTaskAsync(value: "ok");

            object tResult = await tComplete();

            Assert.That(tResult.GetType().GetProperty("Value")?.GetValue(tResult), Is.EqualTo("ok"));
        }

        [Test]
        public async Task NamedCurryDoesNotWrapPublicTaskResult()
        {
            object tTarget = PublicType.PublicAsyncResultInstance;
            dynamic tComplete = Dynamic.Curry(tTarget).GetPublicResultAsync(value: "pub");

            object tRaw = tComplete();
            Assert.That(tRaw, Is.InstanceOf<Task<string>>());
            Assert.That(tRaw, Is.Not.InstanceOf<AwaitableResult>());

            var tAwaited = await (dynamic)tComplete();
            Assert.That((string)tAwaited, Is.EqualTo("pub"));
        }

        [Test]
        public async Task NamedInvocationUnknownAwaitsInaccessibleTaskResult()
        {
            object tTarget = PublicType.AsyncInternalResultInstance;
            var tInvocation = new Invocation(InvocationKind.InvokeMemberUnknown, "GetInternalResultAsync");

            object tResult = await (dynamic)tInvocation.Invoke(tTarget, InvokeArg.Create("value", "ok"));

            Assert.That(tResult.GetType().GetProperty("Value")?.GetValue(tResult), Is.EqualTo("ok"));
        }
    }
}
