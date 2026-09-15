// Issue #105. PartialApply.TryInvoke chose CacheableInvocation / FastDynamicInvoke
// from the current binder's names only. InvokeArg values stored in earlier
// stages were passed as positional objects. Types here exist only for this fixture.
using System;
using Dynamitey.SupportLibrary;
using NUnit.Framework;

namespace Dynamitey.Tests
{
    public class CurryStoredNamesTest : Helper
    {
        [Test]
        public void AllNamedMethodArgsThenEmptyInvoke()
        {
            dynamic tComplete = Dynamic.Curry(new Issue105Target()).Join(b: "B", a: "A");

            Assert.That(tComplete(), Is.EqualTo("AB"));
        }

        [Test]
        public void NamedArgsAccumulatedThenNamedFinalStage()
        {
            dynamic tPartial = Dynamic.Curry(new Issue105Target()).Join(b: "B");

            Assert.That(tPartial(a: "A"), Is.EqualTo("AB"));
        }

        [Test]
        public void AllNamedDelegateArgsThenEmptyInvoke()
        {
            Func<string, string, string> tJoin = (a, b) => a + b;

            Assert.That(Dynamic.Curry(tJoin)(arg2: "B", arg1: "A"), Is.EqualTo("AB"));
        }

        [Test]
        public void NamedPocoMethodThenEmptyInvoke()
        {
            dynamic tComplete = Dynamic.Curry(new PocoAdder()).Add(y: 3, x: 4);

            Assert.That(tComplete(), Is.EqualTo(7));
        }

        [Test]
        public void PositionalCurryStillUsesFastPath()
        {
            Func<int, int, int> tAdd = (x, y) => x + y;
            var tCurried = Dynamic.Curry(tAdd)(4);

            Assert.That(tCurried(6), Is.EqualTo(10));
            Assert.That(tCurried(30), Is.EqualTo(34));
        }
    }

    public class Issue105Target
    {
        public string Join(string a, string b) => a + b;
    }
}
