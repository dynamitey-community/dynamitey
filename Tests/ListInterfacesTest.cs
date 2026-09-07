// DynamicObjects.List implements IList<object>, IDictionary<string,object>,
// the non-generic ICollection/IList, and INotifyCollectionChanged, on top of
// its usual dynamic-object usage. The rest of the suite only exercises it
// dynamically (as an expando-style array), so the explicit interface
// implementations - the ones a caller reaches only by casting to the
// interface - were never called. These tests go through each interface
// directly.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using NUnit.Framework;

namespace Dynamitey.Tests
{
    public class ListInterfacesTest : Helper
    {
        // Exposes the protected GetRepresentedItem for direct testing - it has no
        // caller anywhere in the library itself.
        private class AccessibleList : DynamicObjects.List
        {
            public AccessibleList(IEnumerable<object> contents = null) : base(contents) { }

            public dynamic PublicGetRepresentedItem() => GetRepresentedItem();
        }

        [Test]
        public void TestGetRepresentedItemReturnsFirstElement()
        {
            var tList = new AccessibleList(new object[] { "first", "second" });

            Assert.That((object)tList.PublicGetRepresentedItem(), Is.EqualTo("first"));
        }

        [Test]
        public void TestGetRepresentedItemOfEmptyListIsNull()
        {
            var tList = new AccessibleList();

            Assert.That((object)tList.PublicGetRepresentedItem(), Is.Null);
        }

        [Test]
        public void TestGenericIListInterfaceMembers()
        {
            IList<object> tList = new DynamicObjects.List(new List<object> { 1, 2, 3 });

            Assert.That(tList.Count, Is.EqualTo(3));
            Assert.That(tList.Contains(2), Is.True);
            Assert.That(tList.Contains(99), Is.False);
            Assert.That(tList.IndexOf(2), Is.EqualTo(1));

            tList.Insert(1, 42);
            Assert.That(tList, Is.EqualTo(new object[] { 1, 42, 2, 3 }));

            var tArray = new object[4];
            tList.CopyTo(tArray, 0);
            Assert.That(tArray, Is.EqualTo(new object[] { 1, 42, 2, 3 }));

            Assert.That(tList.Remove(42), Is.True);
            Assert.That(tList.Remove(12345), Is.False);
            Assert.That(tList, Is.EqualTo(new object[] { 1, 2, 3 }));

            tList.RemoveAt(0);
            Assert.That(tList, Is.EqualTo(new object[] { 2, 3 }));

            tList[0] = 99;
            Assert.That(tList[0], Is.EqualTo(99));

            var tEnumerated = tList.ToList();
            Assert.That(tEnumerated, Is.EqualTo(new object[] { 99, 3 }));
        }

        [Test]
        public void TestNonGenericIListInterfaceMembers()
        {
            IList tList = new DynamicObjects.List(new List<object> { "a", "b" });

            Assert.That(tList.IsFixedSize, Is.False);
            Assert.That(tList[0], Is.EqualTo("a"));

            tList[0] = "z";
            Assert.That(tList[0], Is.EqualTo("z"));

            var tIndex = tList.Add("c");
            Assert.That(tIndex, Is.EqualTo(2));
            Assert.That(tList[2], Is.EqualTo("c"));

            tList.Remove("z");
            Assert.That(tList[0], Is.EqualTo("b"));

            var tEnumerated = new List<object>();
            var tEnumerator = ((IEnumerable)tList).GetEnumerator();
            while (tEnumerator.MoveNext())
            {
                tEnumerated.Add(tEnumerator.Current);
            }
            Assert.That(tEnumerated, Is.EqualTo(new object[] { "b", "c" }));
        }

        [Test]
        public void TestNonGenericICollectionMembers()
        {
            ICollection tList = new DynamicObjects.List(new object[] { 1, 2, 3 });

            Assert.That(tList.IsSynchronized, Is.False);
            Assert.That(tList.SyncRoot, Is.Not.Null);

            var tArray = new object[3];
            tList.CopyTo(tArray, 0);
            Assert.That(tArray, Is.EqualTo(new object[] { 1, 2, 3 }));
        }

        [Test]
        public void TestDictionaryIndexerExplicitInterface()
        {
            var tListObj = new DynamicObjects.List();
            IDictionary<string, object> tDict = tListObj;

            tDict["Foo"] = "Bar";

            Assert.That((string)tDict["Foo"], Is.EqualTo("Bar"));
        }

        [Test]
        public void TestKeyValuePairEnumeratorExplicitInterface()
        {
            var tListObj = new DynamicObjects.List();
            IDictionary<string, object> tDict = tListObj;
            tDict["Foo"] = 1;
            tDict["Bar"] = 2;

            Assert.That(tDict["Foo"], Is.EqualTo(1), "sanity: indexer round-trips");

            // Manual foreach rather than LINQ's ToList()/Count(): those special-case
            // ICollection<T> and read Count or call CopyTo instead of enumerating, so they
            // would not exercise the thing under test here. Since #69 that path reports the
            // right number - see TestDictionaryCountThroughCollectionInterfaceMatchesEnumeration
            // below - but it still bypasses GetEnumerator(), and isolating that explicit
            // implementation is the whole point of this test.
            IEnumerable<KeyValuePair<string, object>> tKvEnumerable = tListObj;
            var tPairs = new List<KeyValuePair<string, object>>();
            using (var tEnumerator = tKvEnumerable.GetEnumerator())
            {
                while (tEnumerator.MoveNext())
                {
                    tPairs.Add(tEnumerator.Current);
                }
            }

            Assert.That(tPairs.Count, Is.EqualTo(2), $"got: [{string.Join(", ", tPairs.Select(p => p.Key + "=" + p.Value))}]");
        }

        // Issue #69. List declares one public Count - the element count - and that same
        // property used to satisfy ICollection<KeyValuePair<string,object>>.Count, which List
        // acquires through IDictionary<string,object>. Reading the dictionary count therefore
        // reported the element count, and because LINQ special-cases ICollection<T> and reads
        // Count rather than enumerating, counting and iterating the same sequence disagreed: a
        // List with no elements and two properties enumerated two pairs while Count() said 0.
        // The dictionary side is now implemented explicitly, so the two agree.
        [Test]
        public void TestDictionaryCountThroughCollectionInterfaceMatchesEnumeration()
        {
            var tListObj = new DynamicObjects.List(); // 0 list items
            IDictionary<string, object> tDict = tListObj;
            tDict["Foo"] = 1;
            tDict["Bar"] = 2; // 2 dictionary properties

            IEnumerable<KeyValuePair<string, object>> tKvEnumerable = tListObj;
            var tForeachCount = 0;
            using (var tEnumerator = tKvEnumerable.GetEnumerator())
            {
                while (tEnumerator.MoveNext()) tForeachCount++;
            }

            Assert.That(tForeachCount, Is.EqualTo(2), "manual enumeration sees both properties");
            Assert.That(tKvEnumerable.Count(), Is.EqualTo(2),
                "LINQ's ICollection<T>.Count fast path now reads the property count, agreeing with enumeration");
        }

        [Test]
        public void TestCollectionChangedNotifications()
        {
            var tList = new DynamicObjects.List(new List<object> { 1, 2, 3 });
            var tEvents = new List<NotifyCollectionChangedEventArgs>();
            tList.CollectionChanged += (s, e) => tEvents.Add(e);

            tList.Add(4);
            tList[0] = 99;
            tList.RemoveAt(0);
            tList.Clear();

            Assert.That(tEvents.Select(e => e.Action), Is.EqualTo(new[]
            {
                NotifyCollectionChangedAction.Add,
                NotifyCollectionChangedAction.Replace,
                NotifyCollectionChangedAction.Remove,
                NotifyCollectionChangedAction.Reset
            }));
        }

        [Test]
        public void TestEqualsAndHashCode()
        {
            var tBackingList = new List<object> { 1, 2 };
            var tBackingDict = new Dictionary<string, object>();
            var tListA = new DynamicObjects.List(tBackingList, tBackingDict);
            var tListB = new DynamicObjects.List(tBackingList, tBackingDict);
            var tListC = new DynamicObjects.List(new List<object> { 1, 2 });

            Assert.That(tListA.Equals(tListA), Is.True);
            Assert.That(tListA.Equals(tListB), Is.True, "same backing store");
            Assert.That(tListA.Equals(tListC), Is.False, "different backing store, even with equal contents");
            Assert.That(tListA.Equals((DynamicObjects.List)null), Is.False);

            Assert.That(tListA.Equals((object)tListB), Is.True);
            Assert.That(tListA.Equals((object)null), Is.False);
            Assert.That(tListA.Equals(new object()), Is.False);

            Assert.That(tListA.GetHashCode(), Is.EqualTo(tListB.GetHashCode()));
        }
    }
}
