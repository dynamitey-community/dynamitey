// InlineLambdas.cs (Return<TR> and ReturnVoid) is a T4-generated arity ladder,
// 0..16 parameters, and every overload is a one-line identity pass-through
// ("return del;"). The suite never called most of the arities. These tests
// call every Arguments/ThisAndArguments overload directly: each asserts the
// returned delegate is reference-equal to the one passed in (the actual
// behaviour of an identity function - if a future edit ever started
// wrapping or cloning the delegate instead, this would catch it) and then
// invokes the result to confirm it still forwards arguments correctly.
using System;
using NUnit.Framework;

namespace Dynamitey.Tests
{
    public class InlineLambdasArityTest : Helper
    {
        [Test]
        public void TestReturnArgumentsArity0()
        {
            Func<int> del = () => 42;
            var result = Return<int>.Arguments(del);
            Assert.That(ReferenceEquals(result, del), Is.True);
            Assert.That(result(), Is.EqualTo(42));
        }

        [Test]
        public void TestReturnThisAndArgumentsArity0()
        {
            ThisFunc<int> del = (@this) => (int)@this + 1;
            var result = Return<int>.ThisAndArguments(del);
            Assert.That(ReferenceEquals(result, del), Is.True);
            Assert.That(result(41), Is.EqualTo(42));
        }

        [Test]
        public void TestReturnVoidArgumentsArity0()
        {
            var tCalled = false;
            Action del = () => tCalled = true;
            var result = ReturnVoid.Arguments(del);
            Assert.That(ReferenceEquals(result, del), Is.True);
            result();
            Assert.That(tCalled, Is.True);
        }

        [Test]
        public void TestReturnVoidThisAndArgumentsArity0()
        {
            object tSeenThis = -1;
            ThisAction del = (@this) => tSeenThis = @this;
            var result = ReturnVoid.ThisAndArguments(del);
            Assert.That(ReferenceEquals(result, del), Is.True);
            result(999);
            Assert.That(tSeenThis, Is.EqualTo(999));
        }

        [Test]
        public void TestReturnArgumentsArity1()
        {
            Func<int,int> del = (a1) => a1;
            var result = Return<int>.Arguments(del);
            Assert.That(ReferenceEquals(result, del), Is.True);
            Assert.That(result(1), Is.EqualTo(1));
        }

        [Test]
        public void TestReturnThisAndArgumentsArity1()
        {
            ThisFunc<int,int> del = (@this,a1) => (int)@this+a1;
            var result = Return<int>.ThisAndArguments(del);
            Assert.That(ReferenceEquals(result, del), Is.True);
            Assert.That(result(100,1), Is.EqualTo(101));
        }

        [Test]
        public void TestReturnVoidArgumentsArity1()
        {
            var tTotal = 0;
            Action<int> del = (a1) => tTotal = a1;
            var result = ReturnVoid.Arguments(del);
            Assert.That(ReferenceEquals(result, del), Is.True);
            result(1);
            Assert.That(tTotal, Is.EqualTo(1));
        }

        [Test]
        public void TestReturnVoidThisAndArgumentsArity1()
        {
            object tSeenThis = -1;
            var tTotal = 0;
            ThisAction<int> del = (@this,a1) => { tSeenThis = @this; tTotal = a1; };
            var result = ReturnVoid.ThisAndArguments(del);
            Assert.That(ReferenceEquals(result, del), Is.True);
            result(999,1);
            Assert.That(tSeenThis, Is.EqualTo(999));
            Assert.That(tTotal, Is.EqualTo(1));
        }

        [Test]
        public void TestReturnArgumentsArity2()
        {
            Func<int,int,int> del = (a1,a2) => a1+a2;
            var result = Return<int>.Arguments(del);
            Assert.That(ReferenceEquals(result, del), Is.True);
            Assert.That(result(1,2), Is.EqualTo(3));
        }

        [Test]
        public void TestReturnThisAndArgumentsArity2()
        {
            ThisFunc<int,int,int> del = (@this,a1,a2) => (int)@this+a1+a2;
            var result = Return<int>.ThisAndArguments(del);
            Assert.That(ReferenceEquals(result, del), Is.True);
            Assert.That(result(100,1,2), Is.EqualTo(103));
        }

        [Test]
        public void TestReturnVoidArgumentsArity2()
        {
            var tTotal = 0;
            Action<int,int> del = (a1,a2) => tTotal = a1+a2;
            var result = ReturnVoid.Arguments(del);
            Assert.That(ReferenceEquals(result, del), Is.True);
            result(1,2);
            Assert.That(tTotal, Is.EqualTo(3));
        }

        [Test]
        public void TestReturnVoidThisAndArgumentsArity2()
        {
            object tSeenThis = -1;
            var tTotal = 0;
            ThisAction<int,int> del = (@this,a1,a2) => { tSeenThis = @this; tTotal = a1+a2; };
            var result = ReturnVoid.ThisAndArguments(del);
            Assert.That(ReferenceEquals(result, del), Is.True);
            result(999,1,2);
            Assert.That(tSeenThis, Is.EqualTo(999));
            Assert.That(tTotal, Is.EqualTo(3));
        }

        [Test]
        public void TestReturnArgumentsArity3()
        {
            Func<int,int,int,int> del = (a1,a2,a3) => a1+a2+a3;
            var result = Return<int>.Arguments(del);
            Assert.That(ReferenceEquals(result, del), Is.True);
            Assert.That(result(1,2,3), Is.EqualTo(6));
        }

        [Test]
        public void TestReturnThisAndArgumentsArity3()
        {
            ThisFunc<int,int,int,int> del = (@this,a1,a2,a3) => (int)@this+a1+a2+a3;
            var result = Return<int>.ThisAndArguments(del);
            Assert.That(ReferenceEquals(result, del), Is.True);
            Assert.That(result(100,1,2,3), Is.EqualTo(106));
        }

        [Test]
        public void TestReturnVoidArgumentsArity3()
        {
            var tTotal = 0;
            Action<int,int,int> del = (a1,a2,a3) => tTotal = a1+a2+a3;
            var result = ReturnVoid.Arguments(del);
            Assert.That(ReferenceEquals(result, del), Is.True);
            result(1,2,3);
            Assert.That(tTotal, Is.EqualTo(6));
        }

        [Test]
        public void TestReturnVoidThisAndArgumentsArity3()
        {
            object tSeenThis = -1;
            var tTotal = 0;
            ThisAction<int,int,int> del = (@this,a1,a2,a3) => { tSeenThis = @this; tTotal = a1+a2+a3; };
            var result = ReturnVoid.ThisAndArguments(del);
            Assert.That(ReferenceEquals(result, del), Is.True);
            result(999,1,2,3);
            Assert.That(tSeenThis, Is.EqualTo(999));
            Assert.That(tTotal, Is.EqualTo(6));
        }

        [Test]
        public void TestReturnArgumentsArity4()
        {
            Func<int,int,int,int,int> del = (a1,a2,a3,a4) => a1+a2+a3+a4;
            var result = Return<int>.Arguments(del);
            Assert.That(ReferenceEquals(result, del), Is.True);
            Assert.That(result(1,2,3,4), Is.EqualTo(10));
        }

        [Test]
        public void TestReturnThisAndArgumentsArity4()
        {
            ThisFunc<int,int,int,int,int> del = (@this,a1,a2,a3,a4) => (int)@this+a1+a2+a3+a4;
            var result = Return<int>.ThisAndArguments(del);
            Assert.That(ReferenceEquals(result, del), Is.True);
            Assert.That(result(100,1,2,3,4), Is.EqualTo(110));
        }

        [Test]
        public void TestReturnVoidArgumentsArity4()
        {
            var tTotal = 0;
            Action<int,int,int,int> del = (a1,a2,a3,a4) => tTotal = a1+a2+a3+a4;
            var result = ReturnVoid.Arguments(del);
            Assert.That(ReferenceEquals(result, del), Is.True);
            result(1,2,3,4);
            Assert.That(tTotal, Is.EqualTo(10));
        }

        [Test]
        public void TestReturnVoidThisAndArgumentsArity4()
        {
            object tSeenThis = -1;
            var tTotal = 0;
            ThisAction<int,int,int,int> del = (@this,a1,a2,a3,a4) => { tSeenThis = @this; tTotal = a1+a2+a3+a4; };
            var result = ReturnVoid.ThisAndArguments(del);
            Assert.That(ReferenceEquals(result, del), Is.True);
            result(999,1,2,3,4);
            Assert.That(tSeenThis, Is.EqualTo(999));
            Assert.That(tTotal, Is.EqualTo(10));
        }

        [Test]
        public void TestReturnArgumentsArity5()
        {
            Func<int,int,int,int,int,int> del = (a1,a2,a3,a4,a5) => a1+a2+a3+a4+a5;
            var result = Return<int>.Arguments(del);
            Assert.That(ReferenceEquals(result, del), Is.True);
            Assert.That(result(1,2,3,4,5), Is.EqualTo(15));
        }

        [Test]
        public void TestReturnThisAndArgumentsArity5()
        {
            ThisFunc<int,int,int,int,int,int> del = (@this,a1,a2,a3,a4,a5) => (int)@this+a1+a2+a3+a4+a5;
            var result = Return<int>.ThisAndArguments(del);
            Assert.That(ReferenceEquals(result, del), Is.True);
            Assert.That(result(100,1,2,3,4,5), Is.EqualTo(115));
        }

        [Test]
        public void TestReturnVoidArgumentsArity5()
        {
            var tTotal = 0;
            Action<int,int,int,int,int> del = (a1,a2,a3,a4,a5) => tTotal = a1+a2+a3+a4+a5;
            var result = ReturnVoid.Arguments(del);
            Assert.That(ReferenceEquals(result, del), Is.True);
            result(1,2,3,4,5);
            Assert.That(tTotal, Is.EqualTo(15));
        }

        [Test]
        public void TestReturnVoidThisAndArgumentsArity5()
        {
            object tSeenThis = -1;
            var tTotal = 0;
            ThisAction<int,int,int,int,int> del = (@this,a1,a2,a3,a4,a5) => { tSeenThis = @this; tTotal = a1+a2+a3+a4+a5; };
            var result = ReturnVoid.ThisAndArguments(del);
            Assert.That(ReferenceEquals(result, del), Is.True);
            result(999,1,2,3,4,5);
            Assert.That(tSeenThis, Is.EqualTo(999));
            Assert.That(tTotal, Is.EqualTo(15));
        }

        [Test]
        public void TestReturnArgumentsArity6()
        {
            Func<int,int,int,int,int,int,int> del = (a1,a2,a3,a4,a5,a6) => a1+a2+a3+a4+a5+a6;
            var result = Return<int>.Arguments(del);
            Assert.That(ReferenceEquals(result, del), Is.True);
            Assert.That(result(1,2,3,4,5,6), Is.EqualTo(21));
        }

        [Test]
        public void TestReturnThisAndArgumentsArity6()
        {
            ThisFunc<int,int,int,int,int,int,int> del = (@this,a1,a2,a3,a4,a5,a6) => (int)@this+a1+a2+a3+a4+a5+a6;
            var result = Return<int>.ThisAndArguments(del);
            Assert.That(ReferenceEquals(result, del), Is.True);
            Assert.That(result(100,1,2,3,4,5,6), Is.EqualTo(121));
        }

        [Test]
        public void TestReturnVoidArgumentsArity6()
        {
            var tTotal = 0;
            Action<int,int,int,int,int,int> del = (a1,a2,a3,a4,a5,a6) => tTotal = a1+a2+a3+a4+a5+a6;
            var result = ReturnVoid.Arguments(del);
            Assert.That(ReferenceEquals(result, del), Is.True);
            result(1,2,3,4,5,6);
            Assert.That(tTotal, Is.EqualTo(21));
        }

        [Test]
        public void TestReturnVoidThisAndArgumentsArity6()
        {
            object tSeenThis = -1;
            var tTotal = 0;
            ThisAction<int,int,int,int,int,int> del = (@this,a1,a2,a3,a4,a5,a6) => { tSeenThis = @this; tTotal = a1+a2+a3+a4+a5+a6; };
            var result = ReturnVoid.ThisAndArguments(del);
            Assert.That(ReferenceEquals(result, del), Is.True);
            result(999,1,2,3,4,5,6);
            Assert.That(tSeenThis, Is.EqualTo(999));
            Assert.That(tTotal, Is.EqualTo(21));
        }

        [Test]
        public void TestReturnArgumentsArity7()
        {
            Func<int,int,int,int,int,int,int,int> del = (a1,a2,a3,a4,a5,a6,a7) => a1+a2+a3+a4+a5+a6+a7;
            var result = Return<int>.Arguments(del);
            Assert.That(ReferenceEquals(result, del), Is.True);
            Assert.That(result(1,2,3,4,5,6,7), Is.EqualTo(28));
        }

        [Test]
        public void TestReturnThisAndArgumentsArity7()
        {
            ThisFunc<int,int,int,int,int,int,int,int> del = (@this,a1,a2,a3,a4,a5,a6,a7) => (int)@this+a1+a2+a3+a4+a5+a6+a7;
            var result = Return<int>.ThisAndArguments(del);
            Assert.That(ReferenceEquals(result, del), Is.True);
            Assert.That(result(100,1,2,3,4,5,6,7), Is.EqualTo(128));
        }

        [Test]
        public void TestReturnVoidArgumentsArity7()
        {
            var tTotal = 0;
            Action<int,int,int,int,int,int,int> del = (a1,a2,a3,a4,a5,a6,a7) => tTotal = a1+a2+a3+a4+a5+a6+a7;
            var result = ReturnVoid.Arguments(del);
            Assert.That(ReferenceEquals(result, del), Is.True);
            result(1,2,3,4,5,6,7);
            Assert.That(tTotal, Is.EqualTo(28));
        }

        [Test]
        public void TestReturnVoidThisAndArgumentsArity7()
        {
            object tSeenThis = -1;
            var tTotal = 0;
            ThisAction<int,int,int,int,int,int,int> del = (@this,a1,a2,a3,a4,a5,a6,a7) => { tSeenThis = @this; tTotal = a1+a2+a3+a4+a5+a6+a7; };
            var result = ReturnVoid.ThisAndArguments(del);
            Assert.That(ReferenceEquals(result, del), Is.True);
            result(999,1,2,3,4,5,6,7);
            Assert.That(tSeenThis, Is.EqualTo(999));
            Assert.That(tTotal, Is.EqualTo(28));
        }

        [Test]
        public void TestReturnArgumentsArity8()
        {
            Func<int,int,int,int,int,int,int,int,int> del = (a1,a2,a3,a4,a5,a6,a7,a8) => a1+a2+a3+a4+a5+a6+a7+a8;
            var result = Return<int>.Arguments(del);
            Assert.That(ReferenceEquals(result, del), Is.True);
            Assert.That(result(1,2,3,4,5,6,7,8), Is.EqualTo(36));
        }

        [Test]
        public void TestReturnThisAndArgumentsArity8()
        {
            ThisFunc<int,int,int,int,int,int,int,int,int> del = (@this,a1,a2,a3,a4,a5,a6,a7,a8) => (int)@this+a1+a2+a3+a4+a5+a6+a7+a8;
            var result = Return<int>.ThisAndArguments(del);
            Assert.That(ReferenceEquals(result, del), Is.True);
            Assert.That(result(100,1,2,3,4,5,6,7,8), Is.EqualTo(136));
        }

        [Test]
        public void TestReturnVoidArgumentsArity8()
        {
            var tTotal = 0;
            Action<int,int,int,int,int,int,int,int> del = (a1,a2,a3,a4,a5,a6,a7,a8) => tTotal = a1+a2+a3+a4+a5+a6+a7+a8;
            var result = ReturnVoid.Arguments(del);
            Assert.That(ReferenceEquals(result, del), Is.True);
            result(1,2,3,4,5,6,7,8);
            Assert.That(tTotal, Is.EqualTo(36));
        }

        [Test]
        public void TestReturnVoidThisAndArgumentsArity8()
        {
            object tSeenThis = -1;
            var tTotal = 0;
            ThisAction<int,int,int,int,int,int,int,int> del = (@this,a1,a2,a3,a4,a5,a6,a7,a8) => { tSeenThis = @this; tTotal = a1+a2+a3+a4+a5+a6+a7+a8; };
            var result = ReturnVoid.ThisAndArguments(del);
            Assert.That(ReferenceEquals(result, del), Is.True);
            result(999,1,2,3,4,5,6,7,8);
            Assert.That(tSeenThis, Is.EqualTo(999));
            Assert.That(tTotal, Is.EqualTo(36));
        }

        [Test]
        public void TestReturnArgumentsArity9()
        {
            Func<int,int,int,int,int,int,int,int,int,int> del = (a1,a2,a3,a4,a5,a6,a7,a8,a9) => a1+a2+a3+a4+a5+a6+a7+a8+a9;
            var result = Return<int>.Arguments(del);
            Assert.That(ReferenceEquals(result, del), Is.True);
            Assert.That(result(1,2,3,4,5,6,7,8,9), Is.EqualTo(45));
        }

        [Test]
        public void TestReturnThisAndArgumentsArity9()
        {
            ThisFunc<int,int,int,int,int,int,int,int,int,int> del = (@this,a1,a2,a3,a4,a5,a6,a7,a8,a9) => (int)@this+a1+a2+a3+a4+a5+a6+a7+a8+a9;
            var result = Return<int>.ThisAndArguments(del);
            Assert.That(ReferenceEquals(result, del), Is.True);
            Assert.That(result(100,1,2,3,4,5,6,7,8,9), Is.EqualTo(145));
        }

        [Test]
        public void TestReturnVoidArgumentsArity9()
        {
            var tTotal = 0;
            Action<int,int,int,int,int,int,int,int,int> del = (a1,a2,a3,a4,a5,a6,a7,a8,a9) => tTotal = a1+a2+a3+a4+a5+a6+a7+a8+a9;
            var result = ReturnVoid.Arguments(del);
            Assert.That(ReferenceEquals(result, del), Is.True);
            result(1,2,3,4,5,6,7,8,9);
            Assert.That(tTotal, Is.EqualTo(45));
        }

        [Test]
        public void TestReturnVoidThisAndArgumentsArity9()
        {
            object tSeenThis = -1;
            var tTotal = 0;
            ThisAction<int,int,int,int,int,int,int,int,int> del = (@this,a1,a2,a3,a4,a5,a6,a7,a8,a9) => { tSeenThis = @this; tTotal = a1+a2+a3+a4+a5+a6+a7+a8+a9; };
            var result = ReturnVoid.ThisAndArguments(del);
            Assert.That(ReferenceEquals(result, del), Is.True);
            result(999,1,2,3,4,5,6,7,8,9);
            Assert.That(tSeenThis, Is.EqualTo(999));
            Assert.That(tTotal, Is.EqualTo(45));
        }

        [Test]
        public void TestReturnArgumentsArity10()
        {
            Func<int,int,int,int,int,int,int,int,int,int,int> del = (a1,a2,a3,a4,a5,a6,a7,a8,a9,a10) => a1+a2+a3+a4+a5+a6+a7+a8+a9+a10;
            var result = Return<int>.Arguments(del);
            Assert.That(ReferenceEquals(result, del), Is.True);
            Assert.That(result(1,2,3,4,5,6,7,8,9,10), Is.EqualTo(55));
        }

        [Test]
        public void TestReturnThisAndArgumentsArity10()
        {
            ThisFunc<int,int,int,int,int,int,int,int,int,int,int> del = (@this,a1,a2,a3,a4,a5,a6,a7,a8,a9,a10) => (int)@this+a1+a2+a3+a4+a5+a6+a7+a8+a9+a10;
            var result = Return<int>.ThisAndArguments(del);
            Assert.That(ReferenceEquals(result, del), Is.True);
            Assert.That(result(100,1,2,3,4,5,6,7,8,9,10), Is.EqualTo(155));
        }

        [Test]
        public void TestReturnVoidArgumentsArity10()
        {
            var tTotal = 0;
            Action<int,int,int,int,int,int,int,int,int,int> del = (a1,a2,a3,a4,a5,a6,a7,a8,a9,a10) => tTotal = a1+a2+a3+a4+a5+a6+a7+a8+a9+a10;
            var result = ReturnVoid.Arguments(del);
            Assert.That(ReferenceEquals(result, del), Is.True);
            result(1,2,3,4,5,6,7,8,9,10);
            Assert.That(tTotal, Is.EqualTo(55));
        }

        [Test]
        public void TestReturnVoidThisAndArgumentsArity10()
        {
            object tSeenThis = -1;
            var tTotal = 0;
            ThisAction<int,int,int,int,int,int,int,int,int,int> del = (@this,a1,a2,a3,a4,a5,a6,a7,a8,a9,a10) => { tSeenThis = @this; tTotal = a1+a2+a3+a4+a5+a6+a7+a8+a9+a10; };
            var result = ReturnVoid.ThisAndArguments(del);
            Assert.That(ReferenceEquals(result, del), Is.True);
            result(999,1,2,3,4,5,6,7,8,9,10);
            Assert.That(tSeenThis, Is.EqualTo(999));
            Assert.That(tTotal, Is.EqualTo(55));
        }

        [Test]
        public void TestReturnArgumentsArity11()
        {
            Func<int,int,int,int,int,int,int,int,int,int,int,int> del = (a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11) => a1+a2+a3+a4+a5+a6+a7+a8+a9+a10+a11;
            var result = Return<int>.Arguments(del);
            Assert.That(ReferenceEquals(result, del), Is.True);
            Assert.That(result(1,2,3,4,5,6,7,8,9,10,11), Is.EqualTo(66));
        }

        [Test]
        public void TestReturnThisAndArgumentsArity11()
        {
            ThisFunc<int,int,int,int,int,int,int,int,int,int,int,int> del = (@this,a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11) => (int)@this+a1+a2+a3+a4+a5+a6+a7+a8+a9+a10+a11;
            var result = Return<int>.ThisAndArguments(del);
            Assert.That(ReferenceEquals(result, del), Is.True);
            Assert.That(result(100,1,2,3,4,5,6,7,8,9,10,11), Is.EqualTo(166));
        }

        [Test]
        public void TestReturnVoidArgumentsArity11()
        {
            var tTotal = 0;
            Action<int,int,int,int,int,int,int,int,int,int,int> del = (a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11) => tTotal = a1+a2+a3+a4+a5+a6+a7+a8+a9+a10+a11;
            var result = ReturnVoid.Arguments(del);
            Assert.That(ReferenceEquals(result, del), Is.True);
            result(1,2,3,4,5,6,7,8,9,10,11);
            Assert.That(tTotal, Is.EqualTo(66));
        }

        [Test]
        public void TestReturnVoidThisAndArgumentsArity11()
        {
            object tSeenThis = -1;
            var tTotal = 0;
            ThisAction<int,int,int,int,int,int,int,int,int,int,int> del = (@this,a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11) => { tSeenThis = @this; tTotal = a1+a2+a3+a4+a5+a6+a7+a8+a9+a10+a11; };
            var result = ReturnVoid.ThisAndArguments(del);
            Assert.That(ReferenceEquals(result, del), Is.True);
            result(999,1,2,3,4,5,6,7,8,9,10,11);
            Assert.That(tSeenThis, Is.EqualTo(999));
            Assert.That(tTotal, Is.EqualTo(66));
        }

        [Test]
        public void TestReturnArgumentsArity12()
        {
            Func<int,int,int,int,int,int,int,int,int,int,int,int,int> del = (a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12) => a1+a2+a3+a4+a5+a6+a7+a8+a9+a10+a11+a12;
            var result = Return<int>.Arguments(del);
            Assert.That(ReferenceEquals(result, del), Is.True);
            Assert.That(result(1,2,3,4,5,6,7,8,9,10,11,12), Is.EqualTo(78));
        }

        [Test]
        public void TestReturnThisAndArgumentsArity12()
        {
            ThisFunc<int,int,int,int,int,int,int,int,int,int,int,int,int> del = (@this,a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12) => (int)@this+a1+a2+a3+a4+a5+a6+a7+a8+a9+a10+a11+a12;
            var result = Return<int>.ThisAndArguments(del);
            Assert.That(ReferenceEquals(result, del), Is.True);
            Assert.That(result(100,1,2,3,4,5,6,7,8,9,10,11,12), Is.EqualTo(178));
        }

        [Test]
        public void TestReturnVoidArgumentsArity12()
        {
            var tTotal = 0;
            Action<int,int,int,int,int,int,int,int,int,int,int,int> del = (a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12) => tTotal = a1+a2+a3+a4+a5+a6+a7+a8+a9+a10+a11+a12;
            var result = ReturnVoid.Arguments(del);
            Assert.That(ReferenceEquals(result, del), Is.True);
            result(1,2,3,4,5,6,7,8,9,10,11,12);
            Assert.That(tTotal, Is.EqualTo(78));
        }

        [Test]
        public void TestReturnVoidThisAndArgumentsArity12()
        {
            object tSeenThis = -1;
            var tTotal = 0;
            ThisAction<int,int,int,int,int,int,int,int,int,int,int,int> del = (@this,a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12) => { tSeenThis = @this; tTotal = a1+a2+a3+a4+a5+a6+a7+a8+a9+a10+a11+a12; };
            var result = ReturnVoid.ThisAndArguments(del);
            Assert.That(ReferenceEquals(result, del), Is.True);
            result(999,1,2,3,4,5,6,7,8,9,10,11,12);
            Assert.That(tSeenThis, Is.EqualTo(999));
            Assert.That(tTotal, Is.EqualTo(78));
        }

        [Test]
        public void TestReturnArgumentsArity13()
        {
            Func<int,int,int,int,int,int,int,int,int,int,int,int,int,int> del = (a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12,a13) => a1+a2+a3+a4+a5+a6+a7+a8+a9+a10+a11+a12+a13;
            var result = Return<int>.Arguments(del);
            Assert.That(ReferenceEquals(result, del), Is.True);
            Assert.That(result(1,2,3,4,5,6,7,8,9,10,11,12,13), Is.EqualTo(91));
        }

        [Test]
        public void TestReturnThisAndArgumentsArity13()
        {
            ThisFunc<int,int,int,int,int,int,int,int,int,int,int,int,int,int> del = (@this,a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12,a13) => (int)@this+a1+a2+a3+a4+a5+a6+a7+a8+a9+a10+a11+a12+a13;
            var result = Return<int>.ThisAndArguments(del);
            Assert.That(ReferenceEquals(result, del), Is.True);
            Assert.That(result(100,1,2,3,4,5,6,7,8,9,10,11,12,13), Is.EqualTo(191));
        }

        [Test]
        public void TestReturnVoidArgumentsArity13()
        {
            var tTotal = 0;
            Action<int,int,int,int,int,int,int,int,int,int,int,int,int> del = (a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12,a13) => tTotal = a1+a2+a3+a4+a5+a6+a7+a8+a9+a10+a11+a12+a13;
            var result = ReturnVoid.Arguments(del);
            Assert.That(ReferenceEquals(result, del), Is.True);
            result(1,2,3,4,5,6,7,8,9,10,11,12,13);
            Assert.That(tTotal, Is.EqualTo(91));
        }

        [Test]
        public void TestReturnVoidThisAndArgumentsArity13()
        {
            object tSeenThis = -1;
            var tTotal = 0;
            ThisAction<int,int,int,int,int,int,int,int,int,int,int,int,int> del = (@this,a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12,a13) => { tSeenThis = @this; tTotal = a1+a2+a3+a4+a5+a6+a7+a8+a9+a10+a11+a12+a13; };
            var result = ReturnVoid.ThisAndArguments(del);
            Assert.That(ReferenceEquals(result, del), Is.True);
            result(999,1,2,3,4,5,6,7,8,9,10,11,12,13);
            Assert.That(tSeenThis, Is.EqualTo(999));
            Assert.That(tTotal, Is.EqualTo(91));
        }

        [Test]
        public void TestReturnArgumentsArity14()
        {
            Func<int,int,int,int,int,int,int,int,int,int,int,int,int,int,int> del = (a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12,a13,a14) => a1+a2+a3+a4+a5+a6+a7+a8+a9+a10+a11+a12+a13+a14;
            var result = Return<int>.Arguments(del);
            Assert.That(ReferenceEquals(result, del), Is.True);
            Assert.That(result(1,2,3,4,5,6,7,8,9,10,11,12,13,14), Is.EqualTo(105));
        }

        [Test]
        public void TestReturnThisAndArgumentsArity14()
        {
            ThisFunc<int,int,int,int,int,int,int,int,int,int,int,int,int,int,int> del = (@this,a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12,a13,a14) => (int)@this+a1+a2+a3+a4+a5+a6+a7+a8+a9+a10+a11+a12+a13+a14;
            var result = Return<int>.ThisAndArguments(del);
            Assert.That(ReferenceEquals(result, del), Is.True);
            Assert.That(result(100,1,2,3,4,5,6,7,8,9,10,11,12,13,14), Is.EqualTo(205));
        }

        [Test]
        public void TestReturnVoidArgumentsArity14()
        {
            var tTotal = 0;
            Action<int,int,int,int,int,int,int,int,int,int,int,int,int,int> del = (a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12,a13,a14) => tTotal = a1+a2+a3+a4+a5+a6+a7+a8+a9+a10+a11+a12+a13+a14;
            var result = ReturnVoid.Arguments(del);
            Assert.That(ReferenceEquals(result, del), Is.True);
            result(1,2,3,4,5,6,7,8,9,10,11,12,13,14);
            Assert.That(tTotal, Is.EqualTo(105));
        }

        [Test]
        public void TestReturnVoidThisAndArgumentsArity14()
        {
            object tSeenThis = -1;
            var tTotal = 0;
            ThisAction<int,int,int,int,int,int,int,int,int,int,int,int,int,int> del = (@this,a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12,a13,a14) => { tSeenThis = @this; tTotal = a1+a2+a3+a4+a5+a6+a7+a8+a9+a10+a11+a12+a13+a14; };
            var result = ReturnVoid.ThisAndArguments(del);
            Assert.That(ReferenceEquals(result, del), Is.True);
            result(999,1,2,3,4,5,6,7,8,9,10,11,12,13,14);
            Assert.That(tSeenThis, Is.EqualTo(999));
            Assert.That(tTotal, Is.EqualTo(105));
        }

        [Test]
        public void TestReturnArgumentsArity15()
        {
            Func<int,int,int,int,int,int,int,int,int,int,int,int,int,int,int,int> del = (a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12,a13,a14,a15) => a1+a2+a3+a4+a5+a6+a7+a8+a9+a10+a11+a12+a13+a14+a15;
            var result = Return<int>.Arguments(del);
            Assert.That(ReferenceEquals(result, del), Is.True);
            Assert.That(result(1,2,3,4,5,6,7,8,9,10,11,12,13,14,15), Is.EqualTo(120));
        }

        [Test]
        public void TestReturnThisAndArgumentsArity15()
        {
            ThisFunc<int,int,int,int,int,int,int,int,int,int,int,int,int,int,int,int> del = (@this,a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12,a13,a14,a15) => (int)@this+a1+a2+a3+a4+a5+a6+a7+a8+a9+a10+a11+a12+a13+a14+a15;
            var result = Return<int>.ThisAndArguments(del);
            Assert.That(ReferenceEquals(result, del), Is.True);
            Assert.That(result(100,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15), Is.EqualTo(220));
        }

        [Test]
        public void TestReturnVoidArgumentsArity15()
        {
            var tTotal = 0;
            Action<int,int,int,int,int,int,int,int,int,int,int,int,int,int,int> del = (a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12,a13,a14,a15) => tTotal = a1+a2+a3+a4+a5+a6+a7+a8+a9+a10+a11+a12+a13+a14+a15;
            var result = ReturnVoid.Arguments(del);
            Assert.That(ReferenceEquals(result, del), Is.True);
            result(1,2,3,4,5,6,7,8,9,10,11,12,13,14,15);
            Assert.That(tTotal, Is.EqualTo(120));
        }

        [Test]
        public void TestReturnVoidThisAndArgumentsArity15()
        {
            object tSeenThis = -1;
            var tTotal = 0;
            ThisAction<int,int,int,int,int,int,int,int,int,int,int,int,int,int,int> del = (@this,a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12,a13,a14,a15) => { tSeenThis = @this; tTotal = a1+a2+a3+a4+a5+a6+a7+a8+a9+a10+a11+a12+a13+a14+a15; };
            var result = ReturnVoid.ThisAndArguments(del);
            Assert.That(ReferenceEquals(result, del), Is.True);
            result(999,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15);
            Assert.That(tSeenThis, Is.EqualTo(999));
            Assert.That(tTotal, Is.EqualTo(120));
        }

        [Test]
        public void TestReturnArgumentsArity16()
        {
            Func<int,int,int,int,int,int,int,int,int,int,int,int,int,int,int,int,int> del = (a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12,a13,a14,a15,a16) => a1+a2+a3+a4+a5+a6+a7+a8+a9+a10+a11+a12+a13+a14+a15+a16;
            var result = Return<int>.Arguments(del);
            Assert.That(ReferenceEquals(result, del), Is.True);
            Assert.That(result(1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16), Is.EqualTo(136));
        }

        [Test]
        public void TestReturnThisAndArgumentsArity16()
        {
            ThisFunc<int,int,int,int,int,int,int,int,int,int,int,int,int,int,int,int,int> del = (@this,a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12,a13,a14,a15,a16) => (int)@this+a1+a2+a3+a4+a5+a6+a7+a8+a9+a10+a11+a12+a13+a14+a15+a16;
            var result = Return<int>.ThisAndArguments(del);
            Assert.That(ReferenceEquals(result, del), Is.True);
            Assert.That(result(100,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16), Is.EqualTo(236));
        }

        [Test]
        public void TestReturnVoidArgumentsArity16()
        {
            var tTotal = 0;
            Action<int,int,int,int,int,int,int,int,int,int,int,int,int,int,int,int> del = (a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12,a13,a14,a15,a16) => tTotal = a1+a2+a3+a4+a5+a6+a7+a8+a9+a10+a11+a12+a13+a14+a15+a16;
            var result = ReturnVoid.Arguments(del);
            Assert.That(ReferenceEquals(result, del), Is.True);
            result(1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16);
            Assert.That(tTotal, Is.EqualTo(136));
        }

        [Test]
        public void TestReturnVoidThisAndArgumentsArity16()
        {
            object tSeenThis = -1;
            var tTotal = 0;
            ThisAction<int,int,int,int,int,int,int,int,int,int,int,int,int,int,int,int> del = (@this,a1,a2,a3,a4,a5,a6,a7,a8,a9,a10,a11,a12,a13,a14,a15,a16) => { tSeenThis = @this; tTotal = a1+a2+a3+a4+a5+a6+a7+a8+a9+a10+a11+a12+a13+a14+a15+a16; };
            var result = ReturnVoid.ThisAndArguments(del);
            Assert.That(ReferenceEquals(result, del), Is.True);
            result(999,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16);
            Assert.That(tSeenThis, Is.EqualTo(999));
            Assert.That(tTotal, Is.EqualTo(136));
        }

    }
}
