using System.Threading.Tasks;
using NUnit.Framework;

namespace Dynamitey.Tests
{
    /// <summary>
    /// Every existing AwaitableResult scenario (Tests/PrivateTest.cs) wraps a Task&lt;T&gt;, so
    /// Awaiter.GetResult's reflection lookup always finds a "Result" property, and every wrapped
    /// task is already complete by the time it's awaited, so neither continuation-scheduling
    /// path (OnCompleted/UnsafeOnCompleted) ever runs. This covers both gaps directly against
    /// the public AwaitableResult/Awaiter API.
    /// </summary>
    [TestFixture]
    public class AwaitableResultGapTest : Helper
    {
        [Test]
        public async Task GetResult_ForPlainNonGenericTask_ReturnsNull()
        {
            // Task.CompletedTask is deliberately avoided here: its concrete runtime type is
            // itself a completed Task<VoidTaskResult> (a BCL implementation detail), which does
            // have a "Result" property and would defeat the point of this test. A real Task
            // returned by Task.Run over an Action has none.
            var tPlainTask = Task.Run(() => { });
            var tWrapper = new AwaitableResult(tPlainTask);

            var tResult = await tWrapper;

            Assert.That(tResult, Is.Null);
        }

        [Test]
        public async Task Await_OnATaskThatIsNotYetComplete_SchedulesAContinuation()
        {
            var tCompletionSource = new TaskCompletionSource<int>();
            var tWrapper = new AwaitableResult(tCompletionSource.Task);

            var tAwaitingTask = Task.Run(async () => (int)(await tWrapper)!);
            tCompletionSource.SetResult(42);

            var tResult = await tAwaitingTask;

            Assert.That(tResult, Is.EqualTo(42));
        }
    }
}
