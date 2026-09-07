using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using NUnit.Framework;

namespace Dynamitey.Tests
{
    /// <summary>
    /// Issue #69. DynamicObjects.List implements IList&lt;object&gt; over its elements AND
    /// IDictionary&lt;string, object&gt; over its dynamic properties. A single public member
    /// cannot mean the right thing to both, so the members satisfying both interfaces at once -
    /// Count and Clear - answered for the elements even when asked through the dictionary.
    ///
    /// Count was a wrong number. Clear was worse: asking to clear the properties destroyed the
    /// elements and left the properties intact.
    /// </summary>
    [TestFixture]
    public class ListDualInterfaceTest : Helper
    {
        private static DynamicObjects.List Make() => new DynamicObjects.List(
            new List<object> { 1, 2, 3 },
            new Dictionary<string, object> { { "P", 1 }, { "Q", 2 } });

        [Test]
        public void EachInterfaceReportsItsOwnCount()
        {
            var tList = Make();

            Assert.That(((ICollection<object>)tList).Count, Is.EqualTo(3), "the element count");
            Assert.That(((ICollection<KeyValuePair<string, object>>)tList).Count, Is.EqualTo(2), "the property count");
            Assert.That(tList.Count, Is.EqualTo(3), "the public Count stays the element count");
        }

        // LINQ special-cases ICollection<T>: Count() and ToList() read Count directly rather
        // than enumerating. That is how the wrong number reached a consumer, and it made
        // counting and iterating disagree.
        [Test]
        public void CountingPropertiesAgreesWithEnumeratingThem()
        {
            IEnumerable<KeyValuePair<string, object>> tProperties = Make();

            Assert.That(tProperties.Count(), Is.EqualTo(2));
            Assert.That(tProperties.ToList(), Has.Count.EqualTo(2));
            Assert.That(tProperties.Select(it => it.Key), Is.EquivalentTo(new[] { "P", "Q" }));
        }

        [Test]
        public void ClearingThroughTheDictionaryLeavesTheElementsAlone()
        {
            var tList = Make();

            ((ICollection<KeyValuePair<string, object>>)tList).Clear();

            Assert.That(((IDictionary<string, object>)tList).Keys, Is.Empty, "the properties are cleared");
            Assert.That(((ICollection<object>)tList).Count, Is.EqualTo(3), "the elements are untouched");
        }

        [Test]
        public void ClearingThroughTheListLeavesThePropertiesAlone()
        {
            var tList = Make();

            tList.Clear();

            Assert.That(((ICollection<object>)tList).Count, Is.EqualTo(0), "the elements are cleared");
            Assert.That(((IDictionary<string, object>)tList).Keys, Has.Count.EqualTo(2), "the properties are untouched");
        }

        [Test]
        public void ClearingPropertiesRaisesPropertyChangedForEachRemovedKey()
        {
            var tList = Make();
            var tFired = new List<string>();
            ((INotifyPropertyChanged)tList).PropertyChanged += (s, e) => tFired.Add(e.PropertyName!);

            ((ICollection<KeyValuePair<string, object>>)tList).Clear();

            Assert.That(tFired, Does.Contain("P").And.Contain("Q"));
        }

        // The same live-view trap in the sibling type: Dictionary.Clear snapshotted Keys, which
        // is a view over the dictionary it then emptied, so no notification fired at all.
        [Test]
        public void DictionaryClearRaisesPropertyChangedForEachRemovedKey()
        {
            var tDictionary = new DynamicObjects.Dictionary(
                new Dictionary<string, object> { { "A", 1 }, { "B", 2 } });
            var tFired = new List<string>();
            ((INotifyPropertyChanged)tDictionary).PropertyChanged += (s, e) => tFired.Add(e.PropertyName!);

            tDictionary.Clear();

            Assert.That(tDictionary.Count, Is.EqualTo(0));
            Assert.That(tFired, Does.Contain("A").And.Contain("B"));
        }
    }
}
