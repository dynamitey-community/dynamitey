using System;
using Dynamitey.DynamicObjects;
using Dynamitey.Internal.Optimization;
using NUnit.Framework;

namespace Dynamitey.Tests
{
    // Util.IsAnonymousType and the "member not found, but the applied equivalent type says what
    // it would have been" branches of Util.MassageResultBasedOnInterface had no direct
    // coverage: every existing scenario either finds the member (BaseDictionary's usual case)
    // or - for the event pre-check that also produces an AddRemoveMarker - goes through
    // BaseForwarder's own dedicated Dynamic.InvokeIsEvent check instead of this shared helper.
    public interface IHasFuncProp
    {
        Func<int> Foo { get; }
    }

    public interface IHasIntProp
    {
        int Foo { get; }
    }

    [TestFixture]
    public class UtilGapTest : Helper
    {
        [Test]
        public void IsAnonymousType_Null_ReturnsFalse()
        {
            Assert.That(Util.IsAnonymousType(null), Is.False);
        }

        [Test]
        public void IsAnonymousType_AnonymousObject_ReturnsTrue()
        {
            var tAnon = new { A = 1 };

            Assert.That(Util.IsAnonymousType(tAnon), Is.True);
        }

        [Test]
        public void IsAnonymousType_RegularObject_ReturnsFalse()
        {
            Assert.That(Util.IsAnonymousType(new object()), Is.False);
        }

        // Missing member + an equivalent-type property whose declared type is a delegate:
        // MassageResultBasedOnInterface hands back an AddRemoveMarker instead of null, on the
        // theory that a delegate-typed member is an event even when the DLR itself never
        // classified it as one.
        [Test]
        public void MissingMemberWithDelegateEquivalentType_ReturnsAddRemoveMarker()
        {
            dynamic tDict = new DynamicObjects.Dictionary();
            Dynamic.ApplyEquivalentType(tDict, typeof(IHasFuncProp));

            var tResult = tDict.Foo;

            Assert.That((object)tResult, Is.InstanceOf<BaseForwarder.AddRemoveMarker>());
        }

        // Same missing-member path, but the equivalent type's property is a value type: the
        // helper constructs a default instance instead of leaving the result null.
        [Test]
        public void MissingMemberWithValueTypeEquivalentType_ReturnsDefaultInstance()
        {
            dynamic tDict = new DynamicObjects.Dictionary();
            Dynamic.ApplyEquivalentType(tDict, typeof(IHasIntProp));

            var tResult = tDict.Foo;

            Assert.That((int)tResult, Is.EqualTo(0));
        }
    }
}
