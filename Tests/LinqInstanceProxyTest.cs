using System.Collections.Generic;
using Dynamitey.DynamicObjects;
using NUnit.Framework;

namespace Dynamitey.Tests
{
    /// <summary>
    /// LinqInstanceProxy had no direct coverage before this file: every existing test reaches
    /// LINQ through other paths. This exercises the constructor (which builds on
    /// ExtensionToInstanceProxy) and an invoked LINQ extension method (which round-trips
    /// through CreateSelf back into a LinqInstanceProxy).
    /// </summary>
    /// <remarks>
    /// LinqInstanceProxy.GetEnumerator() (both the generic and explicit-interface forms) is
    /// deliberately not exercised here: it casts CallTarget - the InvokeContext wrapper stored
    /// by the base ExtensionToInstanceProxy constructor, not the raw target - straight to
    /// dynamic and calls GetEnumerator() on it. InvokeContext does not implement
    /// IDynamicMetaObjectProvider, so that call resolves against InvokeContext's own members
    /// and throws Microsoft.CSharp.RuntimeBinder.RuntimeBinderException: "'Dynamitey.
    /// InvokeContext' does not contain a definition for 'GetEnumerator'" - for every
    /// LinqInstanceProxy, unconditionally. Confirmed by writing the direct test (foreach over a
    /// LinqInstanceProxy / Dynamic.Linq(...) result) and watching it fail with that message
    /// rather than iterate. Reported rather than fixed or masked, per this task's ground rules.
    /// </remarks>
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
    }
}
