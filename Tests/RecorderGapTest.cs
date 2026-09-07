using Dynamitey.DynamicObjects;
using Microsoft.CSharp.RuntimeBinder;
using NUnit.Framework;

namespace Dynamitey.Tests
{
    /// <summary>
    /// Covers the Recorder overrides that TestRecorder/TestRecorderReplaysIndexAssignment
    /// (Tests/DynamicObjects.cs) do not exercise: TryGetMember, TryInvokeMember and
    /// TryGetIndex all forward to the wrapped target (Dummy by default, which answers every
    /// call) and additionally append the corresponding Invocation to Recording.
    /// </summary>
    [TestFixture]
    public class RecorderGapTest : Helper
    {
        [Test]
        public void GetMember_RecordsAGetInvocation()
        {
            dynamic recorder = new Recorder();

            _ = recorder.SomeProperty;

            var recording = (Recorder)recorder;
            Assert.That(recording.Recording, Has.Count.EqualTo(1));
            Assert.That(recording.Recording[0].Kind, Is.EqualTo(InvocationKind.Get));
            Assert.That(recording.Recording[0].Name.Name, Is.EqualTo("SomeProperty"));
        }

        [Test]
        public void InvokeMember_RecordsAnInvokeMemberUnknownInvocation()
        {
            dynamic recorder = new Recorder();

            recorder.SomeMethod(1, 2);

            var recording = (Recorder)recorder;
            Assert.That(recording.Recording, Has.Count.EqualTo(1));
            Assert.That(recording.Recording[0].Kind, Is.EqualTo(InvocationKind.InvokeMemberUnknown));
            Assert.That(recording.Recording[0].Name.Name, Is.EqualTo("SomeMethod"));
        }

        [Test]
        public void GetIndex_RecordsAGetIndexInvocation()
        {
            dynamic recorder = new Recorder();

            _ = recorder[0];

            var recording = (Recorder)recorder;
            Assert.That(recording.Recording, Has.Count.EqualTo(1));
            Assert.That(recording.Recording[0].Kind, Is.EqualTo(InvocationKind.GetIndex));
        }

        [Test]
        public void ReplayOn_ReplaysGetInvocation()
        {
            // Get is a read: replaying it against an ExpandoObject that already carries the
            // property should not throw, and Replay should hand back the same target instance.
            dynamic recorder = new Recorder();
            _ = recorder.Test;

            dynamic target = new System.Dynamic.ExpandoObject();
            target.Test = "value";

            var replayed = ((Recorder)recorder).ReplayOn((object)target);

            Assert.That(replayed, Is.SameAs((object)target));
        }

        // Every test above uses the parameterless Recorder(), which wraps a Dummy target that
        // answers every call - so base.Try* always succeeds and Recorder's own "record nothing,
        // return false" branches never run. Wrapping a plain object() (which has none of the
        // members these calls name) makes base.Try* fail instead, and also exercises the
        // Recorder(object target) constructor overload itself.
        [Test]
        public void GetMember_WhenBaseFails_RecordsNothingAndReturnsFalse()
        {
            dynamic recorder = new Recorder(new object());

            Assert.Throws<RuntimeBinderException>(() =>
            {
                _ = recorder.NoSuchProperty;
            });
            Assert.That(((Recorder)recorder).Recording, Is.Empty);
        }

        [Test]
        public void SetMember_WhenBaseFails_RecordsNothingAndReturnsFalse()
        {
            dynamic recorder = new Recorder(new object());

            Assert.Throws<RuntimeBinderException>(() => recorder.NoSuchProperty = 1);
            Assert.That(((Recorder)recorder).Recording, Is.Empty);
        }

        [Test]
        public void InvokeMember_WhenBaseFails_RecordsNothingAndReturnsFalse()
        {
            dynamic recorder = new Recorder(new object());

            Assert.Throws<RuntimeBinderException>(() => recorder.NoSuchMethod());
            Assert.That(((Recorder)recorder).Recording, Is.Empty);
        }

        [Test]
        public void GetIndex_WhenBaseFails_RecordsNothingAndReturnsFalse()
        {
            dynamic recorder = new Recorder(new object());

            Assert.Throws<RuntimeBinderException>(() =>
            {
                _ = recorder[0];
            });
            Assert.That(((Recorder)recorder).Recording, Is.Empty);
        }

        [Test]
        public void SetIndex_WhenBaseFails_RecordsNothingAndReturnsFalse()
        {
            dynamic recorder = new Recorder(new object());

            Assert.Throws<RuntimeBinderException>(() => recorder[0] = "value");
            Assert.That(((Recorder)recorder).Recording, Is.Empty);
        }
    }
}
