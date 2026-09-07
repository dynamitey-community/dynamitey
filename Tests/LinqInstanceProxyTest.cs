using System.Collections;
using System.Collections.Generic;
using Dynamitey.DynamicObjects;
using NUnit.Framework;

namespace Dynamitey.Tests
{
    /// <summary>
    /// LinqInstanceProxy had no direct coverage before this file: every existing test reaches
    /// LINQ through other paths. This exercises the constructor (which builds on
    /// ExtensionToInstanceProxy), an invoked LINQ extension method (which round-trips through
    /// CreateSelf back into a LinqInstanceProxy), and GetEnumerator() (both the generic and
    /// explicit-interface forms), which unwraps the InvokeContext stored by the base
    /// ExtensionToInstanceProxy constructor before enumerating the real target - see issue #82.
    /// </summary>
    [TestFixture]
    public class LinqInstanceProxyTest : Helper
    {
        [Test]
        public void Count_InvokesTheUnderlyingEnumerableExtensionMethod()
        {
            dynamic linq = new LinqInstanceProxy(new List<int> { 1, 2, 3 });

            var count = linq.Count();

            Assert.That((int)count, Is.EqualTo(3));
        }

        [Test]
        public void Where_ReturnsALinqInstanceProxyThatCanBeIteratedAndCounted()
        {
            dynamic linq = new LinqInstanceProxy(new List<int> { 1, 2, 3, 4 });

            dynamic filtered = linq.Where((System.Func<int, bool>)(x => x % 2 == 0));

            Assert.That(filtered, Is.InstanceOf<LinqInstanceProxy>());
            Assert.That((int)filtered.Count(), Is.EqualTo(2));
        }

        [Test]
        public void Foreach_OverDynamicLinqResult_IteratesTheUnderlyingValueTypeSequenceInOrder()
        {
            dynamic tLinq = Dynamic.Linq(new List<int> { 1, 2, 3 });

            var tResult = new List<int>();
            foreach (var it in (IEnumerable)tLinq)
            {
                tResult.Add((int)it);
            }

            Assert.That(tResult, Is.EqualTo(new List<int> { 1, 2, 3 }));
        }

        [Test]
        public void Foreach_OverDynamicLinqResult_IteratesTheUnderlyingReferenceTypeSequenceInOrder()
        {
            dynamic tLinq = Dynamic.Linq(new List<string> { "a", "b", "c" });

            var tResult = new List<string>();
            foreach (var it in (IEnumerable)tLinq)
            {
                tResult.Add((string)it);
            }

            Assert.That(tResult, Is.EqualTo(new List<string> { "a", "b", "c" }));
        }

        [Test]
        public void GenericGetEnumerator_IteratesTheUnderlyingSequenceInOrder()
        {
            IEnumerable<object> tLinq = new LinqInstanceProxy(new List<int> { 1, 2, 3 });

            var tResult = new List<int>();
            using (var tEnumerator = tLinq.GetEnumerator())
            {
                while (tEnumerator.MoveNext())
                {
                    tResult.Add((int)tEnumerator.Current);
                }
            }

            Assert.That(tResult, Is.EqualTo(new List<int> { 1, 2, 3 }));
        }

        [Test]
        public void ExplicitNonGenericGetEnumerator_IteratesTheUnderlyingSequenceInOrder()
        {
            IEnumerable tLinq = new LinqInstanceProxy(new List<int> { 1, 2, 3 });

            var tResult = new List<int>();
            foreach (var it in tLinq)
            {
                tResult.Add((int)it);
            }

            Assert.That(tResult, Is.EqualTo(new List<int> { 1, 2, 3 }));
        }
    }
}
