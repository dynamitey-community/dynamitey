using System;
using System.Collections.Generic;
using System.Dynamic;
using Microsoft.CSharp.RuntimeBinder;
using NUnit.Framework;

namespace Dynamitey.Tests
{
    /// <summary>
    /// Dynamic.InvokeSetAll (backed by Dynamitey.Internal.InvokeSetters) is exercised elsewhere
    /// (Tests/Invoke.cs) via its dictionary, anonymous-type and named-argument shapes. This
    /// covers the remaining shapes: an IEnumerable of Tuple&lt;string,object&gt;, the mismatched
    /// named/unnamed argument-count guard, and an unrecognized second argument.
    /// </summary>
    [TestFixture]
    public class InvokeSettersGapTest : Helper
    {
        [Test]
        public void InvokeSetAll_WithTupleEnumerable_SetsEachPropertyByName()
        {
            dynamic tExpando = new ExpandoObject();
            var tPairs = new List<Tuple<string, object>> { Tuple.Create("One", (object)1), Tuple.Create("Two", (object)2) };

            Dynamic.InvokeSetAll(tExpando, tPairs);

            Assert.That((int)tExpando.One, Is.EqualTo(1));
            Assert.That((int)tExpando.Two, Is.EqualTo(2));
        }

        [Test]
        public void InvokeSetAll_MismatchedNamedAndUnnamedArgumentCounts_Throws()
        {
            dynamic tExpando = new ExpandoObject();

            // Two unnamed arguments (tExpando, 5) plus one named (Foo: 10): ArgumentNames.Count
            // (1) + 1 != ArgumentCount (3), which InvokeSetters treats as a caller error rather
            // than guessing which unnamed argument is the target.
            Assert.Throws<RuntimeBinderException>(() => Dynamic.InvokeSetAll(tExpando, 5, Foo: 10));
        }

        [Test]
        public void InvokeSetAll_UnrecognizedSecondArgument_Throws()
        {
            dynamic tExpando = new ExpandoObject();

            Assert.Throws<RuntimeBinderException>(() => Dynamic.InvokeSetAll(tExpando, 5));
        }
    }
}
