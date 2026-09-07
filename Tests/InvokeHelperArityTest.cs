// InvokeHelper.WrapFuncHelper/WrapAction (reached through the public
// Dynamic.CoerceToDelegate) and InvokeHelper.FastDynamicInvokeReturn/
// FastDynamicInvokeAction (reached through the public Delegate.FastDynamicInvoke
// extension) are T4-style arity ladders, one hand-written case per argument
// count 0..16. The rest of the suite only ever exercises a couple of those
// counts. These tests loop the actual arities: for each one, wrap or invoke
// a delegate that sums its arguments (or, for the void-returning shapes,
// records them into a field) and assert on the real result, so a broken case
// (wrong argument forwarded, wrong slot) would show up as a wrong sum.
//
// CoerceToDelegate's WrapFunc/WrapAction call always builds an all-object
// Func</Action<object,...,object> first; only when the REQUESTED delegate
// type also has all-reference-type parameters does CoerceToDelegate return
// that wrapped delegate directly (its "no value-type parameters" fast path).
// Requesting an int-typed Func/Action instead sends it down a further
// Expression.Lambda(...).Compile() adapter path, which is a different piece
// of code entirely - so these tests deliberately request all-object
// delegate types to exercise WrapFuncHelper/WrapAction themselves.
using System;
using NUnit.Framework;

namespace Dynamitey.Tests
{
    public class InvokeHelperArityTest : Helper
    {
        [Test]
        public void TestCoerceToDelegateWrapsFuncArity1()
        {
            Func<object,object> tSource = (a1) => (int)a1;
            var tDelegateType = typeof(Func<object,object>);
            var tWrapped = (Func<object,object>)Dynamic.CoerceToDelegate(tSource, tDelegateType)!;
            Assert.That(tWrapped((object)1), Is.EqualTo(1));
        }

        [Test]
        public void TestCoerceToDelegateWrapsFuncArity2()
        {
            Func<object,object,object> tSource = (a1,a2) => (int)a1+(int)a2;
            var tDelegateType = typeof(Func<object,object,object>);
            var tWrapped = (Func<object,object,object>)Dynamic.CoerceToDelegate(tSource, tDelegateType)!;
            Assert.That(tWrapped((object)1,(object)2), Is.EqualTo(3));
        }

        [Test]
        public void TestCoerceToDelegateWrapsFuncArity3()
        {
            Func<object,object,object,object> tSource = (a1,a2,a3) => (int)a1+(int)a2+(int)a3;
            var tDelegateType = typeof(Func<object,object,object,object>);
            var tWrapped = (Func<object,object,object,object>)Dynamic.CoerceToDelegate(tSource, tDelegateType)!;
            Assert.That(tWrapped((object)1,(object)2,(object)3), Is.EqualTo(6));
        }

        [Test]
        public void TestCoerceToDelegateWrapsFuncArity4()
        {
            Func<object,object,object,object,object> tSource = (a1,a2,a3,a4) => (int)a1+(int)a2+(int)a3+(int)a4;
            var tDelegateType = typeof(Func<object,object,object,object,object>);
            var tWrapped = (Func<object,object,object,object,object>)Dynamic.CoerceToDelegate(tSource, tDelegateType)!;
            Assert.That(tWrapped((object)1,(object)2,(object)3,(object)4), Is.EqualTo(10));
        }

        [Test]
        public void TestCoerceToDelegateWrapsFuncArity5()
        {
            Func<object,object,object,object,object,object> tSource = (a1,a2,a3,a4,a5) => (int)a1+(int)a2+(int)a3+(int)a4+(int)a5;
            var tDelegateType = typeof(Func<object,object,object,object,object,object>);
            var tWrapped = (Func<object,object,object,object,object,object>)Dynamic.CoerceToDelegate(tSource, tDelegateType)!;
            Assert.That(tWrapped((object)1,(object)2,(object)3,(object)4,(object)5), Is.EqualTo(15));
        }

        [Test]
        public void TestCoerceToDelegateWrapsFuncArity6()
        {
            Func<object,object,object,object,object,object,object> tSource = (a1,a2,a3,a4,a5,a6) => (int)a1+(int)a2+(int)a3+(int)a4+(int)a5+(int)a6;
            var tDelegateType = typeof(Func<object,object,object,object,object,object,object>);
            var tWrapped = (Func<object,object,object,object,object,object,object>)Dynamic.CoerceToDelegate(tSource, tDelegateType)!;
            Assert.That(tWrapped((object)1,(object)2,(object)3,(object)4,(object)5,(object)6), Is.EqualTo(21));
        }

        [Test]
        public void TestCoerceToDelegateWrapsFuncArity7()
        {
            Func<object,object,object,object,object,object,object,object> tSource = (a1,a2,a3,a4,a5,a6,a7) => (int)a1+(int)a2+(int)a3+(int)a4+(int)a5+(int)a6+(int)a7;
            var tDelegateType = typeof(Func<object,object,object,object,object,object,object,object>);
            var tWrapped = (Func<object,object,object,object,object,object,object,object>)Dynamic.CoerceToDelegate(tSource, tDelegateType)!;
            Assert.That(tWrapped((object)1,(object)2,(object)3,(object)4,(object)5,(object)6,(object)7), Is.EqualTo(28));
        }

        [Test]
        public void TestCoerceToDelegateWrapsFuncArity8()
        {
            Func<object,object,object,object,object,object,object,object,object> tSource = (a1,a2,a3,a4,a5,a6,a7,a8) => (int)a1+(int)a2+(int)a3+(int)a4+(int)a5+(int)a6+(int)a7+(int)a8;
            var tDelegateType = typeof(Func<object,object,object,object,object,object,object,object,object>);
            var tWrapped = (Func<object,object,object,object,object,object,object,object,object>)Dynamic.CoerceToDelegate(tSource, tDelegateType)!;
            Assert.That(tWrapped((object)1,(object)2,(object)3,(object)4,(object)5,(object)6,(object)7,(object)8), Is.EqualTo(36));
        }

        [Test]
        public void TestCoerceToDelegateWrapsFuncArity9()
        {
            Func<object,object,object,object,object,object,object,object,object,object> tSource = (a1,a2,a3,a4,a5,a6,a7,a8,a9) => (int)a1+(int)a2+(int)a3+(int)a4+(int)a5+(int)a6+(int)a7+(int)a8+(int)a9;
            var tDelegateType = typeof(Func<object,object,object,object,object,object,object,object,object,object>);
            var tWrapped = (Func<object,object,object,object,object,object,object,object,object,object>)Dynamic.CoerceToDelegate(tSource, tDelegateType)!;
            Assert.That(tWrapped((object)1,(object)2,(object)3,(object)4,(object)5,(object)6,(object)7,(object)8,(object)9), Is.EqualTo(45));
        }

        [Test]
        public void TestCoerceToDelegateWrapsFuncArity10()
        {
            Func<object,object,object,object,object,object,object,object,object,object,object> tSource = (a1,a2,a3,a4,a5,a6,a7,a8,a9,a10) => (int)a1+(int)a2+(int)a3+(int)a4+(int)a5+(int)a6+(int)a7+(int)a8+(int)a9+(int)a10;
            var tDelegateType = typeof(Func<object,object,object,object,object,object,object,object,object,object,object>);
            var tWrapped = (Func<object,object,object,object,object,object,object,object,object,object,object>)Dynamic.CoerceToDelegate(tSource, tDelegateType)!;
            Assert.That(tWrapped((object)1,(object)2,(object)3,(object)4,(object)5,(object)6,(object)7,(object)8,(object)9,(object)10), Is.EqualTo(55));
        }

        [Test]
        public void TestCoerceToDelegateWrapsFuncArity11()
        {
            Func<object,object,object,object,object,object,object,object,object,object,object,object> tSource = (a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11) => (int)a1+(int)a2+(int)a3+(int)a4+(int)a5+(int)a6+(int)a7+(int)a8+(int)a9+(int)a10+(int)a11;
            var tDelegateType = typeof(Func<object,object,object,object,object,object,object,object,object,object,object,object>);
            var tWrapped = (Func<object,object,object,object,object,object,object,object,object,object,object,object>)Dynamic.CoerceToDelegate(tSource, tDelegateType)!;
            Assert.That(tWrapped((object)1,(object)2,(object)3,(object)4,(object)5,(object)6,(object)7,(object)8,(object)9,(object)10,(object)11), Is.EqualTo(66));
        }

        [Test]
        public void TestCoerceToDelegateWrapsFuncArity12()
        {
            Func<object,object,object,object,object,object,object,object,object,object,object,object,object> tSource = (a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12) => (int)a1+(int)a2+(int)a3+(int)a4+(int)a5+(int)a6+(int)a7+(int)a8+(int)a9+(int)a10+(int)a11+(int)a12;
            var tDelegateType = typeof(Func<object,object,object,object,object,object,object,object,object,object,object,object,object>);
            var tWrapped = (Func<object,object,object,object,object,object,object,object,object,object,object,object,object>)Dynamic.CoerceToDelegate(tSource, tDelegateType)!;
            Assert.That(tWrapped((object)1,(object)2,(object)3,(object)4,(object)5,(object)6,(object)7,(object)8,(object)9,(object)10,(object)11,(object)12), Is.EqualTo(78));
        }

        [Test]
        public void TestCoerceToDelegateWrapsFuncArity13()
        {
            Func<object,object,object,object,object,object,object,object,object,object,object,object,object,object> tSource = (a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12,a13) => (int)a1+(int)a2+(int)a3+(int)a4+(int)a5+(int)a6+(int)a7+(int)a8+(int)a9+(int)a10+(int)a11+(int)a12+(int)a13;
            var tDelegateType = typeof(Func<object,object,object,object,object,object,object,object,object,object,object,object,object,object>);
            var tWrapped = (Func<object,object,object,object,object,object,object,object,object,object,object,object,object,object>)Dynamic.CoerceToDelegate(tSource, tDelegateType)!;
            Assert.That(tWrapped((object)1,(object)2,(object)3,(object)4,(object)5,(object)6,(object)7,(object)8,(object)9,(object)10,(object)11,(object)12,(object)13), Is.EqualTo(91));
        }

        [Test]
        public void TestCoerceToDelegateWrapsFuncArity14()
        {
            Func<object,object,object,object,object,object,object,object,object,object,object,object,object,object,object> tSource = (a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12,a13,a14) => (int)a1+(int)a2+(int)a3+(int)a4+(int)a5+(int)a6+(int)a7+(int)a8+(int)a9+(int)a10+(int)a11+(int)a12+(int)a13+(int)a14;
            var tDelegateType = typeof(Func<object,object,object,object,object,object,object,object,object,object,object,object,object,object,object>);
            var tWrapped = (Func<object,object,object,object,object,object,object,object,object,object,object,object,object,object,object>)Dynamic.CoerceToDelegate(tSource, tDelegateType)!;
            Assert.That(tWrapped((object)1,(object)2,(object)3,(object)4,(object)5,(object)6,(object)7,(object)8,(object)9,(object)10,(object)11,(object)12,(object)13,(object)14), Is.EqualTo(105));
        }

        [Test]
        public void TestCoerceToDelegateWrapsFuncArity15()
        {
            Func<object,object,object,object,object,object,object,object,object,object,object,object,object,object,object,object> tSource = (a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12,a13,a14,a15) => (int)a1+(int)a2+(int)a3+(int)a4+(int)a5+(int)a6+(int)a7+(int)a8+(int)a9+(int)a10+(int)a11+(int)a12+(int)a13+(int)a14+(int)a15;
            var tDelegateType = typeof(Func<object,object,object,object,object,object,object,object,object,object,object,object,object,object,object,object>);
            var tWrapped = (Func<object,object,object,object,object,object,object,object,object,object,object,object,object,object,object,object>)Dynamic.CoerceToDelegate(tSource, tDelegateType)!;
            Assert.That(tWrapped((object)1,(object)2,(object)3,(object)4,(object)5,(object)6,(object)7,(object)8,(object)9,(object)10,(object)11,(object)12,(object)13,(object)14,(object)15), Is.EqualTo(120));
        }

        [Test]
        public void TestCoerceToDelegateWrapsFuncArity16()
        {
            Func<object,object,object,object,object,object,object,object,object,object,object,object,object,object,object,object,object> tSource = (a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12,a13,a14,a15,a16) => (int)a1+(int)a2+(int)a3+(int)a4+(int)a5+(int)a6+(int)a7+(int)a8+(int)a9+(int)a10+(int)a11+(int)a12+(int)a13+(int)a14+(int)a15+(int)a16;
            var tDelegateType = typeof(Func<object,object,object,object,object,object,object,object,object,object,object,object,object,object,object,object,object>);
            var tWrapped = (Func<object,object,object,object,object,object,object,object,object,object,object,object,object,object,object,object,object>)Dynamic.CoerceToDelegate(tSource, tDelegateType)!;
            Assert.That(tWrapped((object)1,(object)2,(object)3,(object)4,(object)5,(object)6,(object)7,(object)8,(object)9,(object)10,(object)11,(object)12,(object)13,(object)14,(object)15,(object)16), Is.EqualTo(136));
        }

        [Test]
        public void TestCoerceToDelegateWrapsActionArity1()
        {
            var tTotal = 0;
            Action<object> tSource = (a1) => tTotal = (int)a1;
            var tDelegateType = typeof(Action<object>);
            var tWrapped = (Action<object>)Dynamic.CoerceToDelegate(tSource, tDelegateType)!;
            tWrapped((object)1);
            Assert.That(tTotal, Is.EqualTo(1));
        }

        [Test]
        public void TestCoerceToDelegateWrapsActionArity2()
        {
            var tTotal = 0;
            Action<object,object> tSource = (a1,a2) => tTotal = (int)a1+(int)a2;
            var tDelegateType = typeof(Action<object,object>);
            var tWrapped = (Action<object,object>)Dynamic.CoerceToDelegate(tSource, tDelegateType)!;
            tWrapped((object)1,(object)2);
            Assert.That(tTotal, Is.EqualTo(3));
        }

        [Test]
        public void TestCoerceToDelegateWrapsActionArity3()
        {
            var tTotal = 0;
            Action<object,object,object> tSource = (a1,a2,a3) => tTotal = (int)a1+(int)a2+(int)a3;
            var tDelegateType = typeof(Action<object,object,object>);
            var tWrapped = (Action<object,object,object>)Dynamic.CoerceToDelegate(tSource, tDelegateType)!;
            tWrapped((object)1,(object)2,(object)3);
            Assert.That(tTotal, Is.EqualTo(6));
        }

        [Test]
        public void TestCoerceToDelegateWrapsActionArity4()
        {
            var tTotal = 0;
            Action<object,object,object,object> tSource = (a1,a2,a3,a4) => tTotal = (int)a1+(int)a2+(int)a3+(int)a4;
            var tDelegateType = typeof(Action<object,object,object,object>);
            var tWrapped = (Action<object,object,object,object>)Dynamic.CoerceToDelegate(tSource, tDelegateType)!;
            tWrapped((object)1,(object)2,(object)3,(object)4);
            Assert.That(tTotal, Is.EqualTo(10));
        }

        [Test]
        public void TestCoerceToDelegateWrapsActionArity5()
        {
            var tTotal = 0;
            Action<object,object,object,object,object> tSource = (a1,a2,a3,a4,a5) => tTotal = (int)a1+(int)a2+(int)a3+(int)a4+(int)a5;
            var tDelegateType = typeof(Action<object,object,object,object,object>);
            var tWrapped = (Action<object,object,object,object,object>)Dynamic.CoerceToDelegate(tSource, tDelegateType)!;
            tWrapped((object)1,(object)2,(object)3,(object)4,(object)5);
            Assert.That(tTotal, Is.EqualTo(15));
        }

        [Test]
        public void TestCoerceToDelegateWrapsActionArity6()
        {
            var tTotal = 0;
            Action<object,object,object,object,object,object> tSource = (a1,a2,a3,a4,a5,a6) => tTotal = (int)a1+(int)a2+(int)a3+(int)a4+(int)a5+(int)a6;
            var tDelegateType = typeof(Action<object,object,object,object,object,object>);
            var tWrapped = (Action<object,object,object,object,object,object>)Dynamic.CoerceToDelegate(tSource, tDelegateType)!;
            tWrapped((object)1,(object)2,(object)3,(object)4,(object)5,(object)6);
            Assert.That(tTotal, Is.EqualTo(21));
        }

        [Test]
        public void TestCoerceToDelegateWrapsActionArity7()
        {
            var tTotal = 0;
            Action<object,object,object,object,object,object,object> tSource = (a1,a2,a3,a4,a5,a6,a7) => tTotal = (int)a1+(int)a2+(int)a3+(int)a4+(int)a5+(int)a6+(int)a7;
            var tDelegateType = typeof(Action<object,object,object,object,object,object,object>);
            var tWrapped = (Action<object,object,object,object,object,object,object>)Dynamic.CoerceToDelegate(tSource, tDelegateType)!;
            tWrapped((object)1,(object)2,(object)3,(object)4,(object)5,(object)6,(object)7);
            Assert.That(tTotal, Is.EqualTo(28));
        }

        [Test]
        public void TestCoerceToDelegateWrapsActionArity8()
        {
            var tTotal = 0;
            Action<object,object,object,object,object,object,object,object> tSource = (a1,a2,a3,a4,a5,a6,a7,a8) => tTotal = (int)a1+(int)a2+(int)a3+(int)a4+(int)a5+(int)a6+(int)a7+(int)a8;
            var tDelegateType = typeof(Action<object,object,object,object,object,object,object,object>);
            var tWrapped = (Action<object,object,object,object,object,object,object,object>)Dynamic.CoerceToDelegate(tSource, tDelegateType)!;
            tWrapped((object)1,(object)2,(object)3,(object)4,(object)5,(object)6,(object)7,(object)8);
            Assert.That(tTotal, Is.EqualTo(36));
        }

        [Test]
        public void TestCoerceToDelegateWrapsActionArity9()
        {
            var tTotal = 0;
            Action<object,object,object,object,object,object,object,object,object> tSource = (a1,a2,a3,a4,a5,a6,a7,a8,a9) => tTotal = (int)a1+(int)a2+(int)a3+(int)a4+(int)a5+(int)a6+(int)a7+(int)a8+(int)a9;
            var tDelegateType = typeof(Action<object,object,object,object,object,object,object,object,object>);
            var tWrapped = (Action<object,object,object,object,object,object,object,object,object>)Dynamic.CoerceToDelegate(tSource, tDelegateType)!;
            tWrapped((object)1,(object)2,(object)3,(object)4,(object)5,(object)6,(object)7,(object)8,(object)9);
            Assert.That(tTotal, Is.EqualTo(45));
        }

        [Test]
        public void TestCoerceToDelegateWrapsActionArity10()
        {
            var tTotal = 0;
            Action<object,object,object,object,object,object,object,object,object,object> tSource = (a1,a2,a3,a4,a5,a6,a7,a8,a9,a10) => tTotal = (int)a1+(int)a2+(int)a3+(int)a4+(int)a5+(int)a6+(int)a7+(int)a8+(int)a9+(int)a10;
            var tDelegateType = typeof(Action<object,object,object,object,object,object,object,object,object,object>);
            var tWrapped = (Action<object,object,object,object,object,object,object,object,object,object>)Dynamic.CoerceToDelegate(tSource, tDelegateType)!;
            tWrapped((object)1,(object)2,(object)3,(object)4,(object)5,(object)6,(object)7,(object)8,(object)9,(object)10);
            Assert.That(tTotal, Is.EqualTo(55));
        }

        [Test]
        public void TestCoerceToDelegateWrapsActionArity11()
        {
            var tTotal = 0;
            Action<object,object,object,object,object,object,object,object,object,object,object> tSource = (a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11) => tTotal = (int)a1+(int)a2+(int)a3+(int)a4+(int)a5+(int)a6+(int)a7+(int)a8+(int)a9+(int)a10+(int)a11;
            var tDelegateType = typeof(Action<object,object,object,object,object,object,object,object,object,object,object>);
            var tWrapped = (Action<object,object,object,object,object,object,object,object,object,object,object>)Dynamic.CoerceToDelegate(tSource, tDelegateType)!;
            tWrapped((object)1,(object)2,(object)3,(object)4,(object)5,(object)6,(object)7,(object)8,(object)9,(object)10,(object)11);
            Assert.That(tTotal, Is.EqualTo(66));
        }

        [Test]
        public void TestCoerceToDelegateWrapsActionArity12()
        {
            var tTotal = 0;
            Action<object,object,object,object,object,object,object,object,object,object,object,object> tSource = (a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12) => tTotal = (int)a1+(int)a2+(int)a3+(int)a4+(int)a5+(int)a6+(int)a7+(int)a8+(int)a9+(int)a10+(int)a11+(int)a12;
            var tDelegateType = typeof(Action<object,object,object,object,object,object,object,object,object,object,object,object>);
            var tWrapped = (Action<object,object,object,object,object,object,object,object,object,object,object,object>)Dynamic.CoerceToDelegate(tSource, tDelegateType)!;
            tWrapped((object)1,(object)2,(object)3,(object)4,(object)5,(object)6,(object)7,(object)8,(object)9,(object)10,(object)11,(object)12);
            Assert.That(tTotal, Is.EqualTo(78));
        }

        [Test]
        public void TestCoerceToDelegateWrapsActionArity13()
        {
            var tTotal = 0;
            Action<object,object,object,object,object,object,object,object,object,object,object,object,object> tSource = (a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12,a13) => tTotal = (int)a1+(int)a2+(int)a3+(int)a4+(int)a5+(int)a6+(int)a7+(int)a8+(int)a9+(int)a10+(int)a11+(int)a12+(int)a13;
            var tDelegateType = typeof(Action<object,object,object,object,object,object,object,object,object,object,object,object,object>);
            var tWrapped = (Action<object,object,object,object,object,object,object,object,object,object,object,object,object>)Dynamic.CoerceToDelegate(tSource, tDelegateType)!;
            tWrapped((object)1,(object)2,(object)3,(object)4,(object)5,(object)6,(object)7,(object)8,(object)9,(object)10,(object)11,(object)12,(object)13);
            Assert.That(tTotal, Is.EqualTo(91));
        }

        [Test]
        public void TestCoerceToDelegateWrapsActionArity14()
        {
            var tTotal = 0;
            Action<object,object,object,object,object,object,object,object,object,object,object,object,object,object> tSource = (a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12,a13,a14) => tTotal = (int)a1+(int)a2+(int)a3+(int)a4+(int)a5+(int)a6+(int)a7+(int)a8+(int)a9+(int)a10+(int)a11+(int)a12+(int)a13+(int)a14;
            var tDelegateType = typeof(Action<object,object,object,object,object,object,object,object,object,object,object,object,object,object>);
            var tWrapped = (Action<object,object,object,object,object,object,object,object,object,object,object,object,object,object>)Dynamic.CoerceToDelegate(tSource, tDelegateType)!;
            tWrapped((object)1,(object)2,(object)3,(object)4,(object)5,(object)6,(object)7,(object)8,(object)9,(object)10,(object)11,(object)12,(object)13,(object)14);
            Assert.That(tTotal, Is.EqualTo(105));
        }

        [Test]
        public void TestCoerceToDelegateWrapsActionArity15()
        {
            var tTotal = 0;
            Action<object,object,object,object,object,object,object,object,object,object,object,object,object,object,object> tSource = (a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12,a13,a14,a15) => tTotal = (int)a1+(int)a2+(int)a3+(int)a4+(int)a5+(int)a6+(int)a7+(int)a8+(int)a9+(int)a10+(int)a11+(int)a12+(int)a13+(int)a14+(int)a15;
            var tDelegateType = typeof(Action<object,object,object,object,object,object,object,object,object,object,object,object,object,object,object>);
            var tWrapped = (Action<object,object,object,object,object,object,object,object,object,object,object,object,object,object,object>)Dynamic.CoerceToDelegate(tSource, tDelegateType)!;
            tWrapped((object)1,(object)2,(object)3,(object)4,(object)5,(object)6,(object)7,(object)8,(object)9,(object)10,(object)11,(object)12,(object)13,(object)14,(object)15);
            Assert.That(tTotal, Is.EqualTo(120));
        }

        [Test]
        public void TestCoerceToDelegateWrapsActionArity16()
        {
            var tTotal = 0;
            Action<object,object,object,object,object,object,object,object,object,object,object,object,object,object,object,object> tSource = (a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12,a13,a14,a15,a16) => tTotal = (int)a1+(int)a2+(int)a3+(int)a4+(int)a5+(int)a6+(int)a7+(int)a8+(int)a9+(int)a10+(int)a11+(int)a12+(int)a13+(int)a14+(int)a15+(int)a16;
            var tDelegateType = typeof(Action<object,object,object,object,object,object,object,object,object,object,object,object,object,object,object,object>);
            var tWrapped = (Action<object,object,object,object,object,object,object,object,object,object,object,object,object,object,object,object>)Dynamic.CoerceToDelegate(tSource, tDelegateType)!;
            tWrapped((object)1,(object)2,(object)3,(object)4,(object)5,(object)6,(object)7,(object)8,(object)9,(object)10,(object)11,(object)12,(object)13,(object)14,(object)15,(object)16);
            Assert.That(tTotal, Is.EqualTo(136));
        }

        [Test]
        public void TestFastDynamicInvokeReturnArity1()
        {
            Func<object,int> tDel = (a1) => (int)a1;
            var tResult = ((Delegate)tDel).FastDynamicInvoke((object)1);
            Assert.That(tResult, Is.EqualTo(1));
        }

        [Test]
        public void TestFastDynamicInvokeReturnArity2()
        {
            Func<object,object,int> tDel = (a1,a2) => (int)a1+(int)a2;
            var tResult = ((Delegate)tDel).FastDynamicInvoke((object)1,(object)2);
            Assert.That(tResult, Is.EqualTo(3));
        }

        [Test]
        public void TestFastDynamicInvokeReturnArity3()
        {
            Func<object,object,object,int> tDel = (a1,a2,a3) => (int)a1+(int)a2+(int)a3;
            var tResult = ((Delegate)tDel).FastDynamicInvoke((object)1,(object)2,(object)3);
            Assert.That(tResult, Is.EqualTo(6));
        }

        [Test]
        public void TestFastDynamicInvokeReturnArity4()
        {
            Func<object,object,object,object,int> tDel = (a1,a2,a3,a4) => (int)a1+(int)a2+(int)a3+(int)a4;
            var tResult = ((Delegate)tDel).FastDynamicInvoke((object)1,(object)2,(object)3,(object)4);
            Assert.That(tResult, Is.EqualTo(10));
        }

        [Test]
        public void TestFastDynamicInvokeReturnArity5()
        {
            Func<object,object,object,object,object,int> tDel = (a1,a2,a3,a4,a5) => (int)a1+(int)a2+(int)a3+(int)a4+(int)a5;
            var tResult = ((Delegate)tDel).FastDynamicInvoke((object)1,(object)2,(object)3,(object)4,(object)5);
            Assert.That(tResult, Is.EqualTo(15));
        }

        [Test]
        public void TestFastDynamicInvokeReturnArity6()
        {
            Func<object,object,object,object,object,object,int> tDel = (a1,a2,a3,a4,a5,a6) => (int)a1+(int)a2+(int)a3+(int)a4+(int)a5+(int)a6;
            var tResult = ((Delegate)tDel).FastDynamicInvoke((object)1,(object)2,(object)3,(object)4,(object)5,(object)6);
            Assert.That(tResult, Is.EqualTo(21));
        }

        [Test]
        public void TestFastDynamicInvokeReturnArity7()
        {
            Func<object,object,object,object,object,object,object,int> tDel = (a1,a2,a3,a4,a5,a6,a7) => (int)a1+(int)a2+(int)a3+(int)a4+(int)a5+(int)a6+(int)a7;
            var tResult = ((Delegate)tDel).FastDynamicInvoke((object)1,(object)2,(object)3,(object)4,(object)5,(object)6,(object)7);
            Assert.That(tResult, Is.EqualTo(28));
        }

        [Test]
        public void TestFastDynamicInvokeReturnArity8()
        {
            Func<object,object,object,object,object,object,object,object,int> tDel = (a1,a2,a3,a4,a5,a6,a7,a8) => (int)a1+(int)a2+(int)a3+(int)a4+(int)a5+(int)a6+(int)a7+(int)a8;
            var tResult = ((Delegate)tDel).FastDynamicInvoke((object)1,(object)2,(object)3,(object)4,(object)5,(object)6,(object)7,(object)8);
            Assert.That(tResult, Is.EqualTo(36));
        }

        [Test]
        public void TestFastDynamicInvokeReturnArity9()
        {
            Func<object,object,object,object,object,object,object,object,object,int> tDel = (a1,a2,a3,a4,a5,a6,a7,a8,a9) => (int)a1+(int)a2+(int)a3+(int)a4+(int)a5+(int)a6+(int)a7+(int)a8+(int)a9;
            var tResult = ((Delegate)tDel).FastDynamicInvoke((object)1,(object)2,(object)3,(object)4,(object)5,(object)6,(object)7,(object)8,(object)9);
            Assert.That(tResult, Is.EqualTo(45));
        }

        [Test]
        public void TestFastDynamicInvokeReturnArity10()
        {
            Func<object,object,object,object,object,object,object,object,object,object,int> tDel = (a1,a2,a3,a4,a5,a6,a7,a8,a9,a10) => (int)a1+(int)a2+(int)a3+(int)a4+(int)a5+(int)a6+(int)a7+(int)a8+(int)a9+(int)a10;
            var tResult = ((Delegate)tDel).FastDynamicInvoke((object)1,(object)2,(object)3,(object)4,(object)5,(object)6,(object)7,(object)8,(object)9,(object)10);
            Assert.That(tResult, Is.EqualTo(55));
        }

        [Test]
        public void TestFastDynamicInvokeReturnArity11()
        {
            Func<object,object,object,object,object,object,object,object,object,object,object,int> tDel = (a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11) => (int)a1+(int)a2+(int)a3+(int)a4+(int)a5+(int)a6+(int)a7+(int)a8+(int)a9+(int)a10+(int)a11;
            var tResult = ((Delegate)tDel).FastDynamicInvoke((object)1,(object)2,(object)3,(object)4,(object)5,(object)6,(object)7,(object)8,(object)9,(object)10,(object)11);
            Assert.That(tResult, Is.EqualTo(66));
        }

        [Test]
        public void TestFastDynamicInvokeReturnArity12()
        {
            Func<object,object,object,object,object,object,object,object,object,object,object,object,int> tDel = (a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12) => (int)a1+(int)a2+(int)a3+(int)a4+(int)a5+(int)a6+(int)a7+(int)a8+(int)a9+(int)a10+(int)a11+(int)a12;
            var tResult = ((Delegate)tDel).FastDynamicInvoke((object)1,(object)2,(object)3,(object)4,(object)5,(object)6,(object)7,(object)8,(object)9,(object)10,(object)11,(object)12);
            Assert.That(tResult, Is.EqualTo(78));
        }

        [Test]
        public void TestFastDynamicInvokeReturnArity13()
        {
            Func<object,object,object,object,object,object,object,object,object,object,object,object,object,int> tDel = (a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12,a13) => (int)a1+(int)a2+(int)a3+(int)a4+(int)a5+(int)a6+(int)a7+(int)a8+(int)a9+(int)a10+(int)a11+(int)a12+(int)a13;
            var tResult = ((Delegate)tDel).FastDynamicInvoke((object)1,(object)2,(object)3,(object)4,(object)5,(object)6,(object)7,(object)8,(object)9,(object)10,(object)11,(object)12,(object)13);
            Assert.That(tResult, Is.EqualTo(91));
        }

        [Test]
        public void TestFastDynamicInvokeReturnArity14()
        {
            Func<object,object,object,object,object,object,object,object,object,object,object,object,object,object,int> tDel = (a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12,a13,a14) => (int)a1+(int)a2+(int)a3+(int)a4+(int)a5+(int)a6+(int)a7+(int)a8+(int)a9+(int)a10+(int)a11+(int)a12+(int)a13+(int)a14;
            var tResult = ((Delegate)tDel).FastDynamicInvoke((object)1,(object)2,(object)3,(object)4,(object)5,(object)6,(object)7,(object)8,(object)9,(object)10,(object)11,(object)12,(object)13,(object)14);
            Assert.That(tResult, Is.EqualTo(105));
        }

        [Test]
        public void TestFastDynamicInvokeReturnArity15()
        {
            Func<object,object,object,object,object,object,object,object,object,object,object,object,object,object,object,int> tDel = (a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12,a13,a14,a15) => (int)a1+(int)a2+(int)a3+(int)a4+(int)a5+(int)a6+(int)a7+(int)a8+(int)a9+(int)a10+(int)a11+(int)a12+(int)a13+(int)a14+(int)a15;
            var tResult = ((Delegate)tDel).FastDynamicInvoke((object)1,(object)2,(object)3,(object)4,(object)5,(object)6,(object)7,(object)8,(object)9,(object)10,(object)11,(object)12,(object)13,(object)14,(object)15);
            Assert.That(tResult, Is.EqualTo(120));
        }

        [Test]
        public void TestFastDynamicInvokeReturnArity16()
        {
            Func<object,object,object,object,object,object,object,object,object,object,object,object,object,object,object,object,int> tDel = (a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12,a13,a14,a15,a16) => (int)a1+(int)a2+(int)a3+(int)a4+(int)a5+(int)a6+(int)a7+(int)a8+(int)a9+(int)a10+(int)a11+(int)a12+(int)a13+(int)a14+(int)a15+(int)a16;
            var tResult = ((Delegate)tDel).FastDynamicInvoke((object)1,(object)2,(object)3,(object)4,(object)5,(object)6,(object)7,(object)8,(object)9,(object)10,(object)11,(object)12,(object)13,(object)14,(object)15,(object)16);
            Assert.That(tResult, Is.EqualTo(136));
        }

        [Test]
        public void TestFastDynamicInvokeActionArity1()
        {
            var tTotal = 0;
            Action<object> tDel = (a1) => tTotal = (int)a1;
            ((Delegate)tDel).FastDynamicInvoke((object)1);
            Assert.That(tTotal, Is.EqualTo(1));
        }

        [Test]
        public void TestFastDynamicInvokeActionArity2()
        {
            var tTotal = 0;
            Action<object,object> tDel = (a1,a2) => tTotal = (int)a1+(int)a2;
            ((Delegate)tDel).FastDynamicInvoke((object)1,(object)2);
            Assert.That(tTotal, Is.EqualTo(3));
        }

        [Test]
        public void TestFastDynamicInvokeActionArity3()
        {
            var tTotal = 0;
            Action<object,object,object> tDel = (a1,a2,a3) => tTotal = (int)a1+(int)a2+(int)a3;
            ((Delegate)tDel).FastDynamicInvoke((object)1,(object)2,(object)3);
            Assert.That(tTotal, Is.EqualTo(6));
        }

        [Test]
        public void TestFastDynamicInvokeActionArity4()
        {
            var tTotal = 0;
            Action<object,object,object,object> tDel = (a1,a2,a3,a4) => tTotal = (int)a1+(int)a2+(int)a3+(int)a4;
            ((Delegate)tDel).FastDynamicInvoke((object)1,(object)2,(object)3,(object)4);
            Assert.That(tTotal, Is.EqualTo(10));
        }

        [Test]
        public void TestFastDynamicInvokeActionArity5()
        {
            var tTotal = 0;
            Action<object,object,object,object,object> tDel = (a1,a2,a3,a4,a5) => tTotal = (int)a1+(int)a2+(int)a3+(int)a4+(int)a5;
            ((Delegate)tDel).FastDynamicInvoke((object)1,(object)2,(object)3,(object)4,(object)5);
            Assert.That(tTotal, Is.EqualTo(15));
        }

        [Test]
        public void TestFastDynamicInvokeActionArity6()
        {
            var tTotal = 0;
            Action<object,object,object,object,object,object> tDel = (a1,a2,a3,a4,a5,a6) => tTotal = (int)a1+(int)a2+(int)a3+(int)a4+(int)a5+(int)a6;
            ((Delegate)tDel).FastDynamicInvoke((object)1,(object)2,(object)3,(object)4,(object)5,(object)6);
            Assert.That(tTotal, Is.EqualTo(21));
        }

        [Test]
        public void TestFastDynamicInvokeActionArity7()
        {
            var tTotal = 0;
            Action<object,object,object,object,object,object,object> tDel = (a1,a2,a3,a4,a5,a6,a7) => tTotal = (int)a1+(int)a2+(int)a3+(int)a4+(int)a5+(int)a6+(int)a7;
            ((Delegate)tDel).FastDynamicInvoke((object)1,(object)2,(object)3,(object)4,(object)5,(object)6,(object)7);
            Assert.That(tTotal, Is.EqualTo(28));
        }

        [Test]
        public void TestFastDynamicInvokeActionArity8()
        {
            var tTotal = 0;
            Action<object,object,object,object,object,object,object,object> tDel = (a1,a2,a3,a4,a5,a6,a7,a8) => tTotal = (int)a1+(int)a2+(int)a3+(int)a4+(int)a5+(int)a6+(int)a7+(int)a8;
            ((Delegate)tDel).FastDynamicInvoke((object)1,(object)2,(object)3,(object)4,(object)5,(object)6,(object)7,(object)8);
            Assert.That(tTotal, Is.EqualTo(36));
        }

        [Test]
        public void TestFastDynamicInvokeActionArity9()
        {
            var tTotal = 0;
            Action<object,object,object,object,object,object,object,object,object> tDel = (a1,a2,a3,a4,a5,a6,a7,a8,a9) => tTotal = (int)a1+(int)a2+(int)a3+(int)a4+(int)a5+(int)a6+(int)a7+(int)a8+(int)a9;
            ((Delegate)tDel).FastDynamicInvoke((object)1,(object)2,(object)3,(object)4,(object)5,(object)6,(object)7,(object)8,(object)9);
            Assert.That(tTotal, Is.EqualTo(45));
        }

        [Test]
        public void TestFastDynamicInvokeActionArity10()
        {
            var tTotal = 0;
            Action<object,object,object,object,object,object,object,object,object,object> tDel = (a1,a2,a3,a4,a5,a6,a7,a8,a9,a10) => tTotal = (int)a1+(int)a2+(int)a3+(int)a4+(int)a5+(int)a6+(int)a7+(int)a8+(int)a9+(int)a10;
            ((Delegate)tDel).FastDynamicInvoke((object)1,(object)2,(object)3,(object)4,(object)5,(object)6,(object)7,(object)8,(object)9,(object)10);
            Assert.That(tTotal, Is.EqualTo(55));
        }

        [Test]
        public void TestFastDynamicInvokeActionArity11()
        {
            var tTotal = 0;
            Action<object,object,object,object,object,object,object,object,object,object,object> tDel = (a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11) => tTotal = (int)a1+(int)a2+(int)a3+(int)a4+(int)a5+(int)a6+(int)a7+(int)a8+(int)a9+(int)a10+(int)a11;
            ((Delegate)tDel).FastDynamicInvoke((object)1,(object)2,(object)3,(object)4,(object)5,(object)6,(object)7,(object)8,(object)9,(object)10,(object)11);
            Assert.That(tTotal, Is.EqualTo(66));
        }

        [Test]
        public void TestFastDynamicInvokeActionArity12()
        {
            var tTotal = 0;
            Action<object,object,object,object,object,object,object,object,object,object,object,object> tDel = (a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12) => tTotal = (int)a1+(int)a2+(int)a3+(int)a4+(int)a5+(int)a6+(int)a7+(int)a8+(int)a9+(int)a10+(int)a11+(int)a12;
            ((Delegate)tDel).FastDynamicInvoke((object)1,(object)2,(object)3,(object)4,(object)5,(object)6,(object)7,(object)8,(object)9,(object)10,(object)11,(object)12);
            Assert.That(tTotal, Is.EqualTo(78));
        }

        [Test]
        public void TestFastDynamicInvokeActionArity13()
        {
            var tTotal = 0;
            Action<object,object,object,object,object,object,object,object,object,object,object,object,object> tDel = (a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12,a13) => tTotal = (int)a1+(int)a2+(int)a3+(int)a4+(int)a5+(int)a6+(int)a7+(int)a8+(int)a9+(int)a10+(int)a11+(int)a12+(int)a13;
            ((Delegate)tDel).FastDynamicInvoke((object)1,(object)2,(object)3,(object)4,(object)5,(object)6,(object)7,(object)8,(object)9,(object)10,(object)11,(object)12,(object)13);
            Assert.That(tTotal, Is.EqualTo(91));
        }

        [Test]
        public void TestFastDynamicInvokeActionArity14()
        {
            var tTotal = 0;
            Action<object,object,object,object,object,object,object,object,object,object,object,object,object,object> tDel = (a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12,a13,a14) => tTotal = (int)a1+(int)a2+(int)a3+(int)a4+(int)a5+(int)a6+(int)a7+(int)a8+(int)a9+(int)a10+(int)a11+(int)a12+(int)a13+(int)a14;
            ((Delegate)tDel).FastDynamicInvoke((object)1,(object)2,(object)3,(object)4,(object)5,(object)6,(object)7,(object)8,(object)9,(object)10,(object)11,(object)12,(object)13,(object)14);
            Assert.That(tTotal, Is.EqualTo(105));
        }

        [Test]
        public void TestFastDynamicInvokeActionArity15()
        {
            var tTotal = 0;
            Action<object,object,object,object,object,object,object,object,object,object,object,object,object,object,object> tDel = (a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12,a13,a14,a15) => tTotal = (int)a1+(int)a2+(int)a3+(int)a4+(int)a5+(int)a6+(int)a7+(int)a8+(int)a9+(int)a10+(int)a11+(int)a12+(int)a13+(int)a14+(int)a15;
            ((Delegate)tDel).FastDynamicInvoke((object)1,(object)2,(object)3,(object)4,(object)5,(object)6,(object)7,(object)8,(object)9,(object)10,(object)11,(object)12,(object)13,(object)14,(object)15);
            Assert.That(tTotal, Is.EqualTo(120));
        }

        [Test]
        public void TestFastDynamicInvokeActionArity16()
        {
            var tTotal = 0;
            Action<object,object,object,object,object,object,object,object,object,object,object,object,object,object,object,object> tDel = (a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12,a13,a14,a15,a16) => tTotal = (int)a1+(int)a2+(int)a3+(int)a4+(int)a5+(int)a6+(int)a7+(int)a8+(int)a9+(int)a10+(int)a11+(int)a12+(int)a13+(int)a14+(int)a15+(int)a16;
            ((Delegate)tDel).FastDynamicInvoke((object)1,(object)2,(object)3,(object)4,(object)5,(object)6,(object)7,(object)8,(object)9,(object)10,(object)11,(object)12,(object)13,(object)14,(object)15,(object)16);
            Assert.That(tTotal, Is.EqualTo(136));
        }

    }
}
