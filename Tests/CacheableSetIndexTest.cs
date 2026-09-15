// Issue #98. CacheableInvocation.SetIndex called Dynamic.InvokeSetIndex and
// dropped constructor arg names, CallSite, and baked context. Invoke still
// unwraps an InvokeContext to its target, but context is constructor-only.
using System;
using Microsoft.CSharp.RuntimeBinder;
using NUnit.Framework;

namespace Dynamitey.Tests
{
    public class CacheableSetIndexTest : Helper
    {
        [Test]
        public void NamedIndexerArgumentsFromConstructionAreApplied()
        {
            var tTarget = new Issue98Indexed();
            var tDirect = new Issue98Indexed();
            Dynamic.InvokeSetIndex(tDirect, InvokeArg.Create("column", 2), InvokeArg.Create("row", 1), "ok");

            var tInvocation = new CacheableInvocation(InvocationKind.SetIndex,
                argCount: 3, argNames: new string[] { "column", "row", null });
            tInvocation.Invoke(tTarget, 2, 1, "ok");

            Assert.That(tTarget.Row, Is.EqualTo(tDirect.Row));
            Assert.That(tTarget.Column, Is.EqualTo(tDirect.Column));
            Assert.That(tTarget.Value, Is.EqualTo(tDirect.Value));
            Assert.That(tTarget.Row, Is.EqualTo(1));
            Assert.That(tTarget.Column, Is.EqualTo(2));
        }

        [Test]
        public void RepeatedSetIndexReusesTheCallSite()
        {
            var tTarget = new Issue98Indexed();
            var tInvocation = new CacheableInvocation(InvocationKind.SetIndex, argCount: 3);
            var tField = typeof(CacheableInvocation).GetField("_callSite",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            tInvocation.Invoke(tTarget, 0, 1, "first");
            var tFirst = tField!.GetValue(tInvocation);
            tInvocation.Invoke(tTarget, 1, 0, "second");
            var tSecond = tField.GetValue(tInvocation);

            Assert.That(tTarget.Value, Is.EqualTo("second"));
            Assert.That(tFirst, Is.Not.Null);
            Assert.That(tSecond, Is.SameAs(tFirst));
        }

        [Test]
        public void ConstructorContextAllowsAPrivateIndexer()
        {
            var tTarget = new Issue98PrivateIndexed();
            var tInvocation = new CacheableInvocation(InvocationKind.SetIndex, argCount: 2,
                context: typeof(Issue98PrivateIndexed));

            tInvocation.Invoke(tTarget, 0, "ok");

            Assert.That(tTarget.Stored, Is.EqualTo("ok"));
        }

        [Test]
        public void DefaultContextDoesNotSeeAPrivateIndexer()
        {
            var tTarget = new Issue98PrivateIndexed();
            var tInvocation = new CacheableInvocation(InvocationKind.SetIndex, argCount: 2);

            Assert.That(() => tInvocation.Invoke(tTarget, 0, "nope"),
                Throws.InstanceOf<RuntimeBinderException>());
        }

        [Test]
        public void InvokeTimeInvokeContextDoesNotReplaceConstructorContext()
        {
            var tTarget = new Issue98PrivateIndexed();
            var tInvocation = new CacheableInvocation(InvocationKind.SetIndex, argCount: 2);
            var tWrapped = new InvokeContext(tTarget, typeof(Issue98PrivateIndexed));

            Assert.That(() => tInvocation.Invoke(tWrapped, 0, "nope"),
                Throws.InstanceOf<RuntimeBinderException>());
        }
    }

    public class Issue98Indexed
    {
        public int Row { get; private set; }
        public int Column { get; private set; }
        public string Value { get; private set; }

        public string this[int row, int column]
        {
            get => Value;
            set
            {
                Row = row;
                Column = column;
                Value = value;
            }
        }
    }

    public class Issue98PrivateIndexed
    {
        public string Stored { get; private set; }

        private string this[int index]
        {
            get => Stored;
            set => Stored = value;
        }
    }
}
