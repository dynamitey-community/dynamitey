using System;
using System.Collections.Generic;
using Dynamitey;
using NUnit.Framework;

namespace Dynamitey.Tests
{
    /// <summary>
    /// Issue #68. Invocation compared Args by content and hashed them by reference, so two
    /// content-equal instances compared equal and hashed differently - which makes the type
    /// unusable as a dictionary key, silently returning a miss rather than throwing.
    ///
    /// The same method carried a second defect found while fixing the first: the argument
    /// comparison read "Equals(other.Args, Args) || SequenceEqual(other.Args!, Args!)", and
    /// `||` short-circuits only when its LEFT side is true. With exactly one side null the
    /// reference comparison returned false, SequenceEqual then ran against a null, and Equals
    /// threw ArgumentNullException - which Equals must never do for a non-null argument.
    /// </summary>
    [TestFixture]
    public class InvocationEqualityTest : Helper
    {
        private static Invocation Make(params object[] args) =>
            new Invocation(InvocationKind.InvokeMember, "M", args);

        [Test]
        public void ContentEqualInvocationsAreEqualAndShareAHashCode()
        {
            var tOne = Make(1, 2);
            var tTwo = Make(1, 2);

            Assert.That(tOne, Is.Not.SameAs(tTwo), "the two instances must be distinct objects.");
            Assert.That(tOne.Equals(tTwo), Is.True);
            Assert.That(tOne.GetHashCode(), Is.EqualTo(tTwo.GetHashCode()),
                "equal objects must return equal hash codes - this is the contract the reference hash broke.");
        }

        [Test]
        public void DifferentArgumentsAreNotEqual()
        {
            Assert.That(Make(1, 2).Equals(Make(1, 3)), Is.False);
            Assert.That(Make(1, 2).Equals(Make(1)), Is.False);
        }

        // Equals must never throw for a non-null argument. Both directions, because the old
        // expression failed asymmetrically depending on which side happened to be null.
        [Test]
        public void NullAndNonNullArgumentsCompareFalseRatherThanThrowing()
        {
            var tNull = Make(null);
            var tArgs = Make(1, 2);

            Assert.That(() => tNull.Equals(tArgs), Throws.Nothing);
            Assert.That(() => tArgs.Equals(tNull), Throws.Nothing);
            Assert.That(tNull.Equals(tArgs), Is.False);
            Assert.That(tArgs.Equals(tNull), Is.False);
        }

        [Test]
        public void TwoNullArgumentListsAreEqual()
        {
            var tOne = Make(null);
            var tTwo = Make(null);

            Assert.That(tOne.Equals(tTwo), Is.True);
            Assert.That(tOne.GetHashCode(), Is.EqualTo(tTwo.GetHashCode()));
        }

        // A null argument list and an empty one are different call shapes, so they must not
        // compare equal - and should not collide on the obvious hash input either.
        [Test]
        public void NullAndEmptyArgumentListsAreDistinct()
        {
            var tNull = Make(null);
            var tEmpty = Make(Array.Empty<object>());

            Assert.That(tNull.Equals(tEmpty), Is.False);
            Assert.That(tNull.GetHashCode(), Is.Not.EqualTo(tEmpty.GetHashCode()));
        }

        // The point of the contract: this is what silently failed before, returning a miss
        // rather than an error.
        [Test]
        public void InvocationWorksAsADictionaryKeyAndSetMember()
        {
            var tStored = Make("a", 1);
            var tLookup = Make("a", 1);

            var tSet = new HashSet<Invocation> { tStored };
            Assert.That(tSet.Contains(tLookup), Is.True);

            var tMap = new Dictionary<Invocation, string> { { tStored, "found" } };
            Assert.That(tMap.TryGetValue(tLookup, out var tValue), Is.True);
            Assert.That(tValue, Is.EqualTo("found"));
        }
    }
}
