// Fills in a scattering of Dynamic.cs gaps the rest of the suite never
// happened to exercise: comparison/logical binary operators, the unary and
// binary "unsupported operator" throws, InvokeAction, the InvokeSetIndex
// convenience wrapper and its argument guard, InvokeSetChain's middle-segment
// string-indexer branch, Dynamic.Linq's non-generic-IEnumerable path, the two
// CreateCallSite overloads' null-binder guards, and AwaitResult(null).
using System;
using System.Collections;
using System.Dynamic;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.CSharp.RuntimeBinder;
using NUnit.Framework;

namespace Dynamitey.Tests
{
    public class DynamicMiscTest : Helper
    {
        // Each case body is its own dynamic call site with its own lazily-initialized CallSite
        // field (see the identical comment on Invoke.cs's TestInvokeBasicBinaryOperatorsDynamic) -
        // calling every comparison twice, with the same operand types both times, exercises both
        // the "bind and cache" and "already cached" branches instead of leaving the second one
        // permanently uncovered.
        [Test]
        public void TestInvokeBinaryOperatorComparisons()
        {
            Assert.That(Dynamic.InvokeBinaryOperator(1, ExpressionType.Equal, 1), Is.True);
            Assert.That(Dynamic.InvokeBinaryOperator(2, ExpressionType.Equal, 2), Is.True);
            Assert.That(Dynamic.InvokeBinaryOperator(1, ExpressionType.NotEqual, 2), Is.True);
            Assert.That(Dynamic.InvokeBinaryOperator(1, ExpressionType.NotEqual, 3), Is.True);
            Assert.That(Dynamic.InvokeBinaryOperator(2, ExpressionType.GreaterThan, 1), Is.True);
            Assert.That(Dynamic.InvokeBinaryOperator(3, ExpressionType.GreaterThan, 1), Is.True);
            Assert.That(Dynamic.InvokeBinaryOperator(2, ExpressionType.GreaterThanOrEqual, 2), Is.True);
            Assert.That(Dynamic.InvokeBinaryOperator(3, ExpressionType.GreaterThanOrEqual, 2), Is.True);
            Assert.That(Dynamic.InvokeBinaryOperator(1, ExpressionType.LessThan, 2), Is.True);
            Assert.That(Dynamic.InvokeBinaryOperator(1, ExpressionType.LessThan, 3), Is.True);
            Assert.That(Dynamic.InvokeBinaryOperator(2, ExpressionType.LessThanOrEqual, 2), Is.True);
            Assert.That(Dynamic.InvokeBinaryOperator(1, ExpressionType.LessThanOrEqual, 2), Is.True);
        }

        [Test]
        public void TestInvokeBinaryOperatorLogical()
        {
            Assert.That(Dynamic.InvokeBinaryOperator(true, ExpressionType.OrElse, false), Is.True);
            Assert.That(Dynamic.InvokeBinaryOperator(false, ExpressionType.OrElse, true), Is.True);
            Assert.That(Dynamic.InvokeBinaryOperator(true, ExpressionType.AndAlso, false), Is.False);
            Assert.That(Dynamic.InvokeBinaryOperator(true, ExpressionType.AndAlso, true), Is.True);
        }

        [Test]
        public void TestInvokeBinaryOperatorUnsupportedThrows()
        {
            Assert.Throws<ArgumentException>(() => Dynamic.InvokeBinaryOperator(1, ExpressionType.Constant, 2));
        }

        [Test]
        public void TestInvokeUnaryOperatorUnsupportedThrows()
        {
            Assert.Throws<ArgumentException>(() => Dynamic.InvokeUnaryOperator(ExpressionType.Constant, 1));
        }

        [Test]
        public void TestInvokeAction()
        {
            var tCalled = false;
            Action del = () => tCalled = true;

            Dynamic.InvokeAction(del);

            Assert.That(tCalled, Is.True);
        }

        [Test]
        public void TestInvokeSetValueOnIndexes()
        {
            var tInner = new System.Collections.Generic.Dictionary<int, object>();

            Dynamic.InvokeSetValueOnIndexes(tInner, "value", 5);

            Assert.That(tInner[5], Is.EqualTo("value"));
        }

        [Test]
        public void TestInvokeSetIndexRequiresIndexAndValue()
        {
            dynamic tExpando = new ExpandoObject();

            Assert.Throws<ArgumentException>(() => Dynamic.InvokeSetIndex(tExpando, 5));
        }

        [Test]
        public void TestSetDynamicChainedWithMiddleStringIndexer()
        {
            var tSetValue = "1";
            dynamic tExpando = Build.NewObject(
                Test: Build.NewObject(
                    Sub: Build.NewObject(Test2: Build.NewObject())
                    )
                );

            var tOut = Dynamic.InvokeSetChain(tExpando, "Test['Sub'].Test2.Test3", tSetValue);

            Assert.That(tExpando.Test["Sub"].Test2.Test3, Is.EqualTo(tSetValue));
            Assert.That(tOut, Is.EqualTo(tSetValue));
        }

        [Test]
        public void TestLinqOnNonGenericEnumerable()
        {
            IEnumerable tNonGeneric = new ArrayList { 3, 1, 2 };

            var tActual = Dynamic.Linq(tNonGeneric).Cast<int>().Max();

            Assert.That(tActual, Is.EqualTo(3));
        }

        [Test]
        public void TestCreateCallSiteNonGenericThrowsOnNullBinder()
        {
            Assert.Throws<ArgumentNullException>(() =>
                Dynamic.CreateCallSite(typeof(Func<CallSite, object, object>), null!, "Anything", typeof(DynamicMiscTest)));
        }

        [Test]
        public void TestCreateCallSiteNonGenericBuildsUsableSite()
        {
            var tBinder = Microsoft.CSharp.RuntimeBinder.Binder.GetMember(CSharpBinderFlags.None, "Test",
                typeof(DynamicMiscTest), new[] { CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null) });

            var tSite = Dynamic.CreateCallSite(typeof(Func<CallSite, object, object>), tBinder, "Test", typeof(DynamicMiscTest));

            Assert.That(tSite, Is.Not.Null);
        }

        [Test]
        public void TestCreateCallSiteGenericThrowsOnNullBinder()
        {
            Assert.Throws<ArgumentNullException>(() =>
                Dynamic.CreateCallSite<Func<CallSite, object, object>>(null!, "Anything", typeof(DynamicMiscTest)));
        }

        [Test]
        public void TestCreateCallSiteGenericBuildsUsableSite()
        {
            var tBinder = Microsoft.CSharp.RuntimeBinder.Binder.GetMember(CSharpBinderFlags.None, "Test",
                typeof(DynamicMiscTest), new[] { CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null) });

            var tSite = Dynamic.CreateCallSite<Func<CallSite, object, object>>(tBinder, "Test", typeof(DynamicMiscTest));

            Assert.That(tSite, Is.Not.Null);
        }

        [Test]
        public async Task TestAwaitResultOfNullReturnsNull()
        {
            var tResult = await Dynamic.AwaitResult(null);

            Assert.That(tResult, Is.Null);
        }
    }
}
