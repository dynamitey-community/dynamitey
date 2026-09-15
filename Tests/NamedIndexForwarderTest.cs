// Issue #106. BaseForwarder and Recorder append the setter value, then
// NameArgsIfNecessary zips SetIndexBinder.CallInfo (indexes only) across
// the combined array. Named indexes truncate the value.
// Types here exist only for this fixture.
using Dynamitey.DynamicObjects;
using NUnit.Framework;

namespace Dynamitey.Tests
{
    public class NamedIndexForwarderTest : Helper
    {
        [Test]
        public void DirectNamedSetIndexIsTheControl()
        {
            var tTarget = new Issue106Indexed();

            Dynamic.InvokeSetIndex(tTarget, InvokeArg.Create("column", 2), InvokeArg.Create("row", 1), "ok");

            Assert.That(tTarget.Last, Is.EqualTo("1,2:ok"));
        }

        [Test]
        public void ForwarderNamedSetIndexPreservesValueAndOrder()
        {
            var tTarget = new Issue106Indexed();
            dynamic tProxy = new Get(tTarget);

            tProxy[column: 2, row: 1] = "ok";

            Assert.That(tTarget.Last, Is.EqualTo("1,2:ok"));
        }

        [Test]
        public void ForwarderPositionalSetIndexStillWorks()
        {
            var tTarget = new Issue106Indexed();
            dynamic tProxy = new Get(tTarget);

            tProxy[1, 2] = "ok";

            Assert.That(tTarget.Last, Is.EqualTo("1,2:ok"));
        }

        [Test]
        public void RecorderNamedSetIndexWritesThrough()
        {
            var tTarget = new Issue106Indexed();
            dynamic tRecorder = new Recorder(tTarget);

            tRecorder[column: 2, row: 1] = "ok";

            Assert.That(tTarget.Last, Is.EqualTo("1,2:ok"));
        }

        [Test]
        public void RecorderReplaysNamedSetIndexOutOfOrder()
        {
            var tSource = new Issue106Indexed();
            dynamic tRecorder = new Recorder(tSource);
            tRecorder[column: 2, row: 1] = "ok";

            var tReplay = new Issue106Indexed();
            ((Recorder)tRecorder).ReplayOn(tReplay);

            Assert.That(tReplay.Last, Is.EqualTo("1,2:ok"));
        }

        [Test]
        public void RecorderReplaysPositionalSetIndex()
        {
            var tSource = new Issue106Indexed();
            dynamic tRecorder = new Recorder(tSource);
            tRecorder[1, 2] = "ok";

            var tReplay = new Issue106Indexed();
            ((Recorder)tRecorder).ReplayOn(tReplay);

            Assert.That(tReplay.Last, Is.EqualTo("1,2:ok"));
        }
    }

    public class Issue106Indexed
    {
        public string Last;

        public string this[int row, int column]
        {
            get => Last;
            set => Last = row + "," + column + ":" + value;
        }
    }
}
