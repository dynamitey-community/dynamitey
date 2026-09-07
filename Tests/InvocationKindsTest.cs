// Invocation.Invoke dispatches on InvocationKind through a big switch. The
// rest of the suite exercises it only indirectly (mostly through
// CacheableInvocation, and only for a couple of Kinds), so most of the
// switch's cases - and Invocation's own Equals/GetHashCode/Create - were
// never directly hit. These tests build an Invocation for every Kind and
// call Invoke, asserting the real effect each one has.
using System;
using System.Dynamic;
using Dynamitey.SupportLibrary;
using Microsoft.CSharp.RuntimeBinder;
using NUnit.Framework;

namespace Dynamitey.Tests
{
    public class InvocationKindsTest : Helper
    {
        [Test]
        public void TestCreateFactory()
        {
            var tInvocation = Invocation.Create(InvocationKind.Get, "Prop1");

            Assert.That(tInvocation.Kind, Is.EqualTo(InvocationKind.Get));
        }

        [Test]
        public void TestInvokeConstructor()
        {
            var tInvocation = new Invocation(InvocationKind.Constructor, null);

            var tResult = (ParamsConstructorPoco)tInvocation.Invoke(typeof(ParamsConstructorPoco), "a", "b")!;

            Assert.That(tResult.Args, Is.EqualTo("a,b"));
        }

        [Test]
        public void TestInvokeConvertImplicit()
        {
            var tInvocation = new Invocation(InvocationKind.Convert, null, typeof(long));

            var tResult = tInvocation.Invoke(5, typeof(long));

            Assert.That(tResult, Is.EqualTo(5L));
        }

        [Test]
        public void TestInvokeConvertExplicit()
        {
            var tInvocation = new Invocation(InvocationKind.Convert, null, typeof(int), true);

            var tResult = tInvocation.Invoke(5L, typeof(int), true);

            Assert.That(tResult, Is.EqualTo(5));
        }

        [Test]
        public void TestInvokeGetAndSet()
        {
            var tTarget = new PropPoco { Prop1 = "initial" };
            var tGetInvocation = new Invocation(InvocationKind.Get, "Prop1");
            var tSetInvocation = new Invocation(InvocationKind.Set, "Prop1");

            Assert.That(tGetInvocation.Invoke(tTarget), Is.EqualTo("initial"));

            tSetInvocation.Invoke(tTarget, "changed");

            Assert.That(tTarget.Prop1, Is.EqualTo("changed"));
        }

        [Test]
        public void TestInvokeGetIndexAndSetIndex()
        {
            dynamic tTarget = new ExpandoObject();
            tTarget.List = new System.Collections.Generic.List<object> { "a", "b", "c" };

            var tGetIndexInvocation = new Invocation(InvocationKind.GetIndex, null);
            var tSetIndexInvocation = new Invocation(InvocationKind.SetIndex, null);

            Assert.That(tGetIndexInvocation.Invoke((object)tTarget.List, 1), Is.EqualTo("b"));

            tSetIndexInvocation.Invoke((object)tTarget.List, 1, "z");

            Assert.That(tTarget.List[1], Is.EqualTo("z"));
        }

        [Test]
        public void TestInvokeMember()
        {
            var tTarget = new ParamsMethodPoco();
            var tInvocation = new Invocation(InvocationKind.InvokeMember, "Join");

            var tResult = tInvocation.Invoke(tTarget, "x", "y");

            Assert.That(tResult, Is.EqualTo("x,y"));
        }

        [Test]
        public void TestInvokeMemberAction()
        {
            var tTarget = new ParamsActionMethodPoco();
            var tInvocation = new Invocation(InvocationKind.InvokeMemberAction, "Join");

            var tResult = tInvocation.Invoke(tTarget, "x", "y");

            Assert.That(tResult, Is.Null);
            Assert.That(tTarget.Joined, Is.EqualTo("x,y"));
        }

        [Test]
        public void TestInvokeMemberUnknownFallsBackToAction()
        {
            var tTarget = new VoidMethodPoco();
            var tInvocation = new Invocation(InvocationKind.InvokeMemberUnknown, "Action");

            var tResult = tInvocation.Invoke(tTarget);

            Assert.That(tResult, Is.Null);
        }

        [Test]
        public void TestInvokeMemberUnknownReturnsValueWhenAvailable()
        {
            var tTarget = new ParamsMethodPoco();
            var tInvocation = new Invocation(InvocationKind.InvokeMemberUnknown, "Join");

            var tResult = tInvocation.Invoke(tTarget, "x", "y");

            Assert.That(tResult, Is.EqualTo("x,y"));
        }

        [Test]
        public void TestInvokeDirect()
        {
            Func<int, int> tAddOne = x => x + 1;
            var tInvocation = new Invocation(InvocationKind.Invoke, null);

            var tResult = tInvocation.Invoke(tAddOne, 5);

            Assert.That(tResult, Is.EqualTo(6));
        }

        [Test]
        public void TestInvokeActionDirect()
        {
            var tCalled = false;
            Action<int> tAction = x => tCalled = x == 7;
            var tInvocation = new Invocation(InvocationKind.InvokeAction, null);

            var tResult = tInvocation.Invoke(tAction, 7);

            Assert.That(tResult, Is.Null);
            Assert.That(tCalled, Is.True);
        }

        [Test]
        public void TestInvokeUnknownFallsBackToAction()
        {
            var tCalled = false;
            Action<int> tAction = x => tCalled = x == 3;
            var tInvocation = new Invocation(InvocationKind.InvokeUnknown, null);

            var tResult = tInvocation.Invoke(tAction, 3);

            Assert.That(tResult, Is.Null);
            Assert.That(tCalled, Is.True);
        }

        [Test]
        public void TestInvokeUnknownReturnsValueWhenAvailable()
        {
            Func<int, int> tAddOne = x => x + 1;
            var tInvocation = new Invocation(InvocationKind.InvokeUnknown, null);

            var tResult = tInvocation.Invoke(tAddOne, 5);

            Assert.That(tResult, Is.EqualTo(6));
        }

        [Test]
        public void TestAddAssignSubtractAssignAndIsEvent()
        {
            var tTarget = new PocoEvent();
            var tHit = false;
            EventHandler<EventArgs> tHandler = (s, e) => tHit = true;

            var tIsEventInvocation = new Invocation(InvocationKind.IsEvent, "Event");
            Assert.That(tIsEventInvocation.Invoke(tTarget), Is.True);

            var tAddInvocation = new Invocation(InvocationKind.AddAssign, "Event");
            tAddInvocation.Invoke(tTarget, tHandler);
            tTarget.OnEvent(tTarget, EventArgs.Empty);
            Assert.That(tHit, Is.True);

            tHit = false;
            var tSubtractInvocation = new Invocation(InvocationKind.SubtractAssign, "Event");
            tSubtractInvocation.Invoke(tTarget, tHandler);
            tTarget.OnEvent(tTarget, EventArgs.Empty);
            Assert.That(tHit, Is.False);
        }

        [Test]
        public void TestUnknownKindThrows()
        {
            var tInvocation = new Invocation(InvocationKind.NotSet, null);

            Assert.Throws<InvalidOperationException>(() => tInvocation.Invoke(new object()));
        }

        [Test]
        public void TestInvokeWithStoredArgs()
        {
            var tTarget = new ParamsMethodPoco();
            var tInvocation = new Invocation(InvocationKind.InvokeMember, "Join", "x", "y");

            var tResult = tInvocation.InvokeWithStoredArgs(tTarget);

            Assert.That(tResult, Is.EqualTo("x,y"));
        }

        [Test]
        public void TestEqualsAndHashCode()
        {
            var tA = new Invocation(InvocationKind.Get, "Prop1", 1, 2);
            var tB = new Invocation(InvocationKind.Get, "Prop1", 1, 2);
            var tDifferentKind = new Invocation(InvocationKind.Set, "Prop1", 1, 2);
            var tDifferentArgs = new Invocation(InvocationKind.Get, "Prop1", 1, 3);

            Assert.That(tA.Equals(tA), Is.True);
            Assert.That(tA.Equals(tB), Is.True);
            Assert.That(tA.Equals(tDifferentKind), Is.False);
            Assert.That(tA.Equals(tDifferentArgs), Is.False);
            Assert.That(tA.Equals((Invocation)null), Is.False);

            Assert.That(tA.Equals((object)tB), Is.True);
            Assert.That(tA.Equals((object)null), Is.False);
            Assert.That(tA.Equals(new object()), Is.False);
        }

        // Issue #68. GetHashCode used to fold in Args.GetHashCode() - the array's own
        // reference-identity hash - while Equals compared Args by SequenceEqual. Two
        // Invocations built from distinct but equal-content argument arrays were therefore
        // equal with different hash codes, which breaks the Equals/GetHashCode contract and
        // makes the type miss its own key in a Dictionary or HashSet. It now hashes contents.
        [Test]
        public void TestGetHashCodeHonorsEqualsContractForDistinctArgsArrayInstances()
        {
            var tA = new Invocation(InvocationKind.Get, "Prop1", 1, 2);
            var tB = new Invocation(InvocationKind.Get, "Prop1", 1, 2);

            Assert.That(tA.Equals(tB), Is.True, "precondition: these compare as equal");
            Assert.That(tA.GetHashCode(), Is.EqualTo(tB.GetHashCode()),
                "equal objects must return equal hash codes");
        }
    }
}
