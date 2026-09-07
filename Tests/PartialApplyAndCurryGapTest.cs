using System;
using Microsoft.CSharp.RuntimeBinder;
using NUnit.Framework;

namespace Dynamitey.Tests
{
    /// <summary>
    /// PartialApply.TryBinaryOperation and Dynamitey.Internal.Curry.TryBinaryOperation both
    /// special-case only the left-shift operator ("pipe a value in") and return false for every
    /// other binary operator - Tests/Curry.cs's CurryLeftPipeTest only ever exercises the
    /// left-shift branch on both types. This exercises the other branch, plus Curry's `|`
    /// operator overload, which no test in the repository otherwise touches.
    /// </summary>
    [TestFixture]
    public class PartialApplyAndCurryGapTest : Helper
    {
        [Test]
        public void PartialApply_UnsupportedBinaryOperator_ReturnsFalseAndThrows()
        {
            // A two-argument delegate curried with only one argument (via the supported
            // left-shift pipe) stays a genuine, not-yet-invoked PartialApply - piping in the
            // second argument would complete it, but a different operator should just fail.
            Func<int, int, int> tAdd = (x, y) => x + y;
            dynamic tPartial = Dynamic.Curry(tAdd) << 1;

            Assert.Throws<RuntimeBinderException>(() =>
            {
                _ = tPartial + 1;
            });
        }

        [Test]
        public void InternalCurry_UnsupportedBinaryOperator_ReturnsFalseAndThrows()
        {
            Func<int, int> tIncrement = x => x + 1;
            dynamic tCurried = Dynamic.Curry(tIncrement);

            Assert.Throws<RuntimeBinderException>(() =>
            {
                _ = tCurried + 1;
            });
        }

        [Test]
        public void InternalCurry_PipeOperator_InvokesTheCurriedFunction()
        {
            Func<int, int> tIncrement = x => x + 1;
            var tCurried = (Dynamitey.Internal.Curry)Dynamic.Curry(tIncrement);

            var tResult = 4 | tCurried;

            Assert.That((int)tResult, Is.EqualTo(5));
        }
    }
}
