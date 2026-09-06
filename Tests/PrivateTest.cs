using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dynamitey.SupportLibrary;
using Microsoft.CSharp.RuntimeBinder;
using NUnit.Framework;

namespace Dynamitey.Tests
{
    [TestFixture]
    public class PrivateTest : Helper
    {
  
        [Test]
        public void TestInvokePrivateMethod()
        {
            var tTest = new TestWithPrivateMethod();
            Assert.That((object)Dynamic.InvokeMember(tTest, "Test"), Is.EqualTo(3));
        }

        [Test]
        public void TestInvokePrivateMethodAcrossAssemblyBoundries()
        {
            var tTest = new PublicType();
            Assert.That((object)Dynamic.InvokeMember(tTest, "PrivateMethod", 3), Is.True);
        }

        [Test]
        public void TestInvokeInternalTypeMethodAcrossAssemblyBoundries()
        {
            var tTest = PublicType.InternalInstance;
            Assert.That((object)Dynamic.InvokeMember(tTest, "InternalMethod", 3), Is.True);
        }

        // Issue #16 investigation. Reported as: InvokeMember throws
        // RuntimeBinderException calling an async method of an internal class
        // (Azure.Data.Tables' internal TableRestClient) with named InvokeArg
        // arguments. Three variables were tangled in the report - the target
        // being internal, the method being async, and the arguments being named
        // via InvokeArg - plus a fourth found while reducing it: the real
        // signature has optional parameters (nextPartitionKey, nextRowKey)
        // between the ones actually supplied, which named-argument binding must
        // skip in favor of their defaults. InternalType.InternalAsyncMethod in
        // SupportTypes.cs mirrors that shape across the same assembly boundary
        // as TestInvokeInternalTypeMethodAcrossAssemblyBoundries above.
        //
        // This was NOT reproducible, individually or in combination: internal
        // target + positional args, internal + async + named args, internal +
        // sync + named args, public + async + named args, and internal + async
        // + named args that skip optional parameters all succeed - on this
        // repo's current code and against the pristine upstream baseline (tag
        // upstream-baseline). These tests record that behaviour as coverage
        // rather than as fix verification - there was no failing case to fix.
        [Test]
        public async Task TestInvokeInternalTypeAsyncMethodAcrossAssemblyBoundriesPositionalArgs()
        {
            var tTest = PublicType.InternalInstance;

            var tResult = await Dynamic.InvokeMember(tTest, "InternalAsyncMethod", "table", (int?)10, null, null, "opts", CancellationToken.None);

            Assert.That((string)tResult, Is.EqualTo("table-10---opts"));
        }

        [Test]
        public async Task TestInvokeInternalTypeAsyncMethodAcrossAssemblyBoundriesNamedArgsSkippingOptionals()
        {
            var tTest = PublicType.InternalInstance;

            var tResult = await Dynamic.InvokeMember(tTest, new InvokeMemberName("InternalAsyncMethod", false), new object[]
            {
                new InvokeArg("table", "tableName"),
                new InvokeArg("timeout", (int?)10),
                new InvokeArg("queryOptions", "opts"),
                new InvokeArg("cancellationToken", CancellationToken.None)
            });

            Assert.That((string)tResult, Is.EqualTo("tableName-10---opts"));
        }

        // Issue #16, the shape that actually fails: unlike the investigation
        // above, the target and method here (PublicType.AsyncInternalResultInstance
        // and its GetInternalResultAsync) are not what's internal - it's the
        // awaited RESULT type, InternalResult, that's internal to SupportLibrary
        // and invisible from this (Tests) assembly. This is exactly
        // Azure.Data.Tables' TableRestClient.QueryEntitiesAsync shape: an
        // internal REST-client type off a public TableClient, whose method
        // returns Task<ResponseWithHeaders<...>> where the generic closure is
        // internal. The investigation above missed this case entirely by
        // making the target internal with a PUBLIC result type instead - the
        // C# runtime binder cares about the accessibility of the value
        // GetResult() must produce, not of the type that owns the invoked
        // method.
        //
        // "await Dynamic.InvokeMember(...)" used to compile to dynamic invocations of
        // GetAwaiter/IsCompleted/GetResult resolved in the CALLER's (this test
        // assembly's) accessibility context. Since Tests cannot see InternalResult,
        // the binder used to be unable to produce a value of that type for
        // GetResult(), falling back to a void-returning form whose compiler-generated
        // conversion to object threw RuntimeBinderException. Dynamic.InvokeMember now
        // detects that the result is a Task<T> with an inaccessible T (via
        // Type.IsVisible) and returns an AwaitableResult wrapper instead of the raw
        // task - every member the dynamic await pattern needs (GetAwaiter,
        // IsCompleted, GetResult) is declared publicly on the wrapper, with
        // GetResult() returning object rather than T, so the binder never needs to
        // know about InternalResult. The direct await now succeeds and returns the
        // same value InternalAsyncResultPoco.GetInternalResultAsync produced.
        [Test]
        public async Task TestInvokeMemberAwaitDirectlySucceedsWhenResultTypeIsInternal()
        {
            var tTarget = PublicType.AsyncInternalResultInstance;

            var tResult = await Dynamic.InvokeMember(tTarget, "GetInternalResultAsync", "value");

            Assert.That((object)tResult, Is.Not.Null);
            Assert.That(((object)tResult).GetType().GetProperty("Value")?.GetValue((object)tResult), Is.EqualTo("value"));
        }

        // Dynamic.InvokeMemberAsync sidesteps the binder entirely: it awaits the
        // returned Task through its static, non-generic Task type (a plain,
        // non-dynamic await, so no accessibility check on InternalResult ever
        // happens) and reads the Result property via reflection, which is not
        // subject to the caller's accessibility context.
        [Test]
        public async Task TestInvokeMemberAsyncSucceedsWhenResultTypeIsInternal()
        {
            var tTarget = PublicType.AsyncInternalResultInstance;

            object tResult = await Dynamic.InvokeMemberAsync(tTarget, "GetInternalResultAsync", "value");

            Assert.That(tResult, Is.Not.Null);
            Assert.That(tResult.GetType().GetProperty("Value")?.GetValue(tResult), Is.EqualTo("value"));
        }

        // Dynamic.AwaitResult is the piece InvokeMemberAsync delegates to; exercise
        // it directly against the un-awaited Task that Dynamic.InvokeMember hands
        // back, confirming it is independently usable and reads the same result.
        [Test]
        public async Task TestAwaitResultReadsInternalResultTypeDirectly()
        {
            var tTarget = PublicType.AsyncInternalResultInstance;

            object tTask = Dynamic.InvokeMember(tTarget, "GetInternalResultAsync", "other");
            object tResult = await Dynamic.AwaitResult(tTask);

            Assert.That(tResult, Is.Not.Null);
            Assert.That(tResult.GetType().GetProperty("Value")?.GetValue(tResult), Is.EqualTo("other"));
        }

        // AwaitResult on a non-generic Task returns null - there is no Result
        // property to read - rather than throwing.
        [Test]
        public async Task TestAwaitResultReturnsNullForNonGenericTask()
        {
            Task tTask = Task.Delay(1);

            object tResult = await Dynamic.AwaitResult(tTask);

            Assert.That(tResult, Is.Null);
        }

        // AwaitResult rejects anything that isn't a Task with a clear exception,
        // rather than an obscure cast failure.
        [Test]
        public void TestAwaitResultThrowsForNonTaskInput()
        {
            Assert.That(async () => await Dynamic.AwaitResult("not a task"),
                        Throws.InstanceOf<InvalidOperationException>());
        }

        // Regression guard for the issue #16 fix's scoping: a Task<T> whose T IS
        // public (string) must come back from Dynamic.InvokeMember exactly as
        // before - the raw Task<string>, not an AwaitableResult - and await it must
        // behave identically to a plain, non-dynamic await. This is the most
        // important new test: every other new test below exercises the
        // inaccessible-T wrapper path and would keep passing even if the fix wrapped
        // every Task<T> indiscriminately (e.g. a broken or inverted Type.IsVisible
        // check); only this test would catch that regression.
        [Test]
        public async Task TestInvokeMemberDoesNotWrapTaskWithPublicResultType()
        {
            var tTarget = PublicType.PublicAsyncResultInstance;

            object tRaw = Dynamic.InvokeMember(tTarget, "GetPublicResultAsync", "value");
            Assert.That(tRaw, Is.InstanceOf<Task<string>>());
            Assert.That(tRaw, Is.Not.InstanceOf<AwaitableResult>());

            var tAwaited = await Dynamic.InvokeMember(tTarget, "GetPublicResultAsync", "value2");
            Assert.That((string)tAwaited, Is.EqualTo("value2"));
        }

        // Issue #16 scoping detail: Type.IsPublic is false for ANY nested type, even
        // one declared public, so checking IsPublic instead of IsVisible would wrap
        // this case too - even though PublicType.NestedPublicResult is genuinely
        // reachable by any caller (both it and its containing PublicType are
        // public). Proves the fix does not over-wrap nested-public results.
        [Test]
        public async Task TestInvokeMemberDoesNotWrapTaskWithNestedPublicResultType()
        {
            var tTarget = new PublicType();

            object tRaw = Dynamic.InvokeMember(tTarget, "GetNestedPublicResultAsync", "value");
            Assert.That(tRaw, Is.InstanceOf<Task<PublicType.NestedPublicResult>>());
            Assert.That(tRaw, Is.Not.InstanceOf<AwaitableResult>());

            var tAwaited = await Dynamic.InvokeMember(tTarget, "GetNestedPublicResultAsync", "value2");
            Assert.That(((PublicType.NestedPublicResult)tAwaited).Value, Is.EqualTo("value2"));
        }

        // The reporter's original line (issue #16): a direct await on
        // Dynamic.InvokeMember's result, where the awaited Task<T>'s T
        // (InternalResult) is internal to another assembly. Before this fix this
        // threw RuntimeBinderException; see
        // TestInvokeMemberAwaitDirectlySucceedsWhenResultTypeIsInternal above for the
        // primary coverage of this shape. This test additionally confirms the
        // AwaitableResult wrapper is what Dynamic.InvokeMember actually handed back,
        // rather than the assertion accidentally passing some other way.
        [Test]
        public void TestInvokeMemberWrapsTaskWithInaccessibleResultType()
        {
            var tTarget = PublicType.AsyncInternalResultInstance;

            object tRaw = Dynamic.InvokeMember(tTarget, "GetInternalResultAsync", "value");

            Assert.That(tRaw, Is.InstanceOf<AwaitableResult>());
        }

        // A faulted Task<InternalResult> - T internal, so the AwaitableResult
        // wrapper path is exercised - must propagate the original exception, not an
        // AggregateException. This is load-bearing against an implementation that
        // reads _task.Result directly (which throws AggregateException) instead of
        // _task.GetAwaiter().GetResult() (which unwraps to the original exception).
        [Test]
        public void TestInvokeMemberAwaitPropagatesOriginalExceptionWhenResultTypeIsInternal()
        {
            var tTarget = PublicType.FaultingAsyncResultInstance;

            Assert.That(
                async () => await Dynamic.InvokeMember(tTarget, "GetFaultingResultAsync"),
                Throws.InstanceOf<InvalidTimeZoneException>()
                      .With.Message.EqualTo("boom"));
        }

        // A cancelled Task<InternalResult> - T internal, so the AwaitableResult
        // wrapper path is exercised - must propagate OperationCanceledException
        // rather than some other failure shape.
        [Test]
        public void TestInvokeMemberAwaitPropagatesCancellationWhenResultTypeIsInternal()
        {
            var tTarget = PublicType.CancelingAsyncResultInstance;

            Assert.That(
                async () => await Dynamic.InvokeMember(tTarget, "GetCancelingResultAsync"),
                Throws.InstanceOf<OperationCanceledException>());
        }

        // A plain, non-generic Task result must never be wrapped - it has no Result
        // property to protect, and Dynamic.InvokeMember already worked correctly for
        // it before this fix.
        [Test]
        public async Task TestInvokeMemberDoesNotWrapNonGenericTask()
        {
            var tTarget = PublicType.PlainTaskAsyncInstance;

            object tRaw = Dynamic.InvokeMember(tTarget, "GetPlainTaskAsync");
            Assert.That(tRaw, Is.InstanceOf<Task>());
            Assert.That(tRaw, Is.Not.InstanceOf<AwaitableResult>());

            await Dynamic.InvokeMember(tTarget, "GetPlainTaskAsync");
        }

        // Dynamic.AwaitResult must accept the AwaitableResult wrapper directly -
        // e.g. a caller who captured Dynamic.InvokeMember's result without awaiting
        // it, then wants to await it via AwaitResult instead of a dynamic await.
        [Test]
        public async Task TestAwaitResultAcceptsAwaitableResultWrapper()
        {
            var tTarget = PublicType.AsyncInternalResultInstance;

            object tWrapped = Dynamic.InvokeMember(tTarget, "GetInternalResultAsync", "wrapped");
            Assert.That(tWrapped, Is.InstanceOf<AwaitableResult>());

            object tResult = await Dynamic.AwaitResult(tWrapped);

            Assert.That(tResult, Is.Not.Null);
            Assert.That(tResult.GetType().GetProperty("Value")?.GetValue(tResult), Is.EqualTo("wrapped"));
        }

        [Test]
        public void TestInvokeDoNotExposePrivateMethod()
        {
            var tTest = new TestWithPrivateMethod();
            var context = InvokeContext.CreateContext;
            Assert.That(() => Dynamic.InvokeMember(context(tTest,this), "Test"), Throws.InstanceOf<RuntimeBinderException>());
        }

        [Test]
        public void TestCacheableDoNotExposePrivateMethod()
        {
            var tTest = new TestWithPrivateMethod();
            var tCachedInvoke = new CacheableInvocation(InvocationKind.InvokeMember, "Test");
            Assert.That(() => tCachedInvoke.Invoke(tTest), Throws.InstanceOf<RuntimeBinderException>());
        }

        [Test]
        public void TestCacheableExposePrivateMethodViaInstance()
        {
            var tTest = new TestWithPrivateMethod();
            var tCachedInvoke = new CacheableInvocation(InvocationKind.InvokeMember, "Test", context: tTest);
            Assert.That(tCachedInvoke.Invoke(tTest), Is.EqualTo(3));
        }

        [Test]
        public void TestCacheableExposePrivateMethodViaType()
        {
            var tTest = new TestWithPrivateMethod();
            var tCachedInvoke = new CacheableInvocation(InvocationKind.InvokeMember, "Test", context: typeof(TestWithPrivateMethod));
            Assert.That( tCachedInvoke.Invoke(tTest), Is.EqualTo(3));
        }

        // Issue #12: InvokeGet works on an instance field but throws
        // RuntimeBinderException on a static field, even though the same class's
        // instance field works fine. Root cause: InvokeGetCallSite's static-context
        // binder(s) never had a call shape that reaches a field at all (see the
        // fix's comments in InvokeHelper-Regular.cs), so a reflection fallback is
        // used once the DLR binder fails.
        [Test]
        public void TestInvokeGetInstanceField()
        {
            var tTest = new ClassWithPrivateFields();
            Assert.That(Dynamic.InvokeGet(tTest, "field"), Is.EqualTo(16));
        }

        [Test]
        public void TestInvokeGetPrivateStaticField()
        {
            var context = InvokeContext.CreateStatic(typeof(ClassWithPrivateFields));
            Assert.That(Dynamic.InvokeGet(context, "other"), Is.EqualTo(17));
        }

        // Found during investigation: the bug is not limited to private fields.
        // The static-context Get binder always attempts "get_" + name (a property
        // accessor method), so it fails for ANY static field - even a fully public
        // field on a fully public type - because fields have no "get_" method.
        [Test]
        public void TestInvokeGetPublicStaticField()
        {
            var context = InvokeContext.CreateStatic(typeof(PublicClassWithPublicStaticField));
            Assert.That(Dynamic.InvokeGet(context, "Other"), Is.EqualTo(42));
        }

        [Test]
        public void TestCacheableInvokeGetPrivateStaticField()
        {
            var tCachedInvoke = new CacheableInvocation(InvocationKind.Get, "other",
                context: InvokeContext.CreateStatic(typeof(ClassWithPrivateFields)));
            Assert.That(tCachedInvoke.Invoke(typeof(ClassWithPrivateFields)), Is.EqualTo(17));
        }

        // Issue #13: InvokeGet with a static context fails to read a private
        // static PROPERTY when the target type isn't a non-nested public type.
        // Each shape below uses its own type/property so the tests don't depend
        // on each other, or on any other test in the suite, having run first -
        // the reported bug was that behaviour changed depending on execution
        // order, so a test that relied on ordering to pass would be validating
        // the wrong thing.
        [Test]
        public void TestInvokeGetPrivateStaticProperty_PublicNestedClass()
        {
            var context = InvokeContext.CreateStatic(typeof(PublicNestedClassWithPrivateStaticProperty));
            Assert.That(Dynamic.InvokeGet(context, "Hello"), Is.EqualTo("World"));
        }

        // The exact shape named in the issue's title: a private nested class.
        [Test]
        public void TestInvokeGetPrivateStaticProperty_PrivateNestedClass()
        {
            var context = InvokeContext.CreateStatic(typeof(PrivateNestedClassWithPrivateStaticProperty));
            Assert.That(Dynamic.InvokeGet(context, "Hello"), Is.EqualTo("World"));
        }

        [Test]
        public void TestInvokeGetPrivateStaticProperty_InternalTopLevelClass()
        {
            var context = InvokeContext.CreateStatic(typeof(InternalClassWithPrivateStaticProperty));
            Assert.That(Dynamic.InvokeGet(context, "Hello"), Is.EqualTo("World"));
        }

        [Test]
        public void TestInvokeGetPrivateStaticProperty_PublicTopLevelClass()
        {
            var context = InvokeContext.CreateStatic(typeof(PublicClassWithPrivateStaticProperty));
            Assert.That(Dynamic.InvokeGet(context, "Hello"), Is.EqualTo("World"));
        }

        // Public nested class shape for issue #13.
        public class PublicNestedClassWithPrivateStaticProperty
        {
            private static string Hello => "World";
        }

        // Private nested class shape for issue #13.
        private class PrivateNestedClassWithPrivateStaticProperty
        {
            private static string Hello => "World";
        }

        // Regression coverage: "context" is Dynamitey's accessibility control
        // (see TestInvokeDoNotExposePrivateMethod above). The #12/#13 reflection
        // fallback must honor it rather than unconditionally exposing private
        // static members regardless of which context the caller supplied.
        [Test]
        public void TestInvokeGetPrivateStaticFieldRestrictedContextThrows()
        {
            var context = InvokeContext.CreateStaticWithContext(typeof(ClassWithPrivateStaticFieldForContextTest), this);
            Assert.That(() => Dynamic.InvokeGet(context, "Secret"), Throws.InstanceOf<RuntimeBinderException>());
        }

        [Test]
        public void TestInvokeGetPrivateStaticPropertyRestrictedContextThrows()
        {
            var context = InvokeContext.CreateStaticWithContext(typeof(ClassWithPrivateStaticPropertyForContextTest), this);
            Assert.That(() => Dynamic.InvokeGet(context, "Secret"), Throws.InstanceOf<RuntimeBinderException>());
        }

        // A PUBLIC static field is visible from any context, so a restrictive
        // context must not block it - the gate should not over-correct.
        [Test]
        public void TestInvokeGetPublicStaticFieldRestrictedContextStillSucceeds()
        {
            var context = InvokeContext.CreateStaticWithContext(typeof(ClassWithPublicStaticFieldForContextTest), this);
            Assert.That(Dynamic.InvokeGet(context, "Visible"), Is.EqualTo(123));
        }

        // Issue #31: get and set of the SAME static property used to collide
        // inside Microsoft.CSharp.RuntimeBinder's process-wide symbol cache -
        // see the long comment on InvokeGetCallSite/InvokeSetCallSite in
        // InvokeHelper-Regular.cs for the mechanism. Reproducing the exact bug
        // shape needs a public TOP-LEVEL type (that's the one path that used
        // to route through the accessor-method DLR trick at all); each test
        // below gets its own type, never touched by any other test, because
        // the whole point of the bug was that outcome depended on what else
        // had run first against the type in this process.

        [Test]
        public void TestStaticPropertyGetThenSet_PublicTopLevelClass()
        {
            var context = InvokeContext.CreateStatic(typeof(Issue31GetThenSetType));
            Assert.That(Dynamic.InvokeGet(context, "Prop"), Is.EqualTo("initial"));
            Dynamic.InvokeSet(context, "Prop", "changed");
            Assert.That(Dynamic.InvokeGet(context, "Prop"), Is.EqualTo("changed"));
        }

        [Test]
        public void TestStaticPropertySetThenGet_PublicTopLevelClass()
        {
            var context = InvokeContext.CreateStatic(typeof(Issue31SetThenGetType));
            Dynamic.InvokeSet(context, "Prop", "changed");
            Assert.That(Dynamic.InvokeGet(context, "Prop"), Is.EqualTo("changed"));
        }

        // Proves the fix isn't a one-shot: a single lucky pair passing isn't
        // enough evidence, since #31's failure appeared only on whichever
        // accessor bound second.
        [Test]
        public void TestStaticPropertyAlternatingGetSetRepeatedly_PublicTopLevelClass()
        {
            var context = InvokeContext.CreateStatic(typeof(Issue31AlternatingType));
            Assert.That(Dynamic.InvokeGet(context, "Prop"), Is.EqualTo("initial"));
            for (var i = 0; i < 4; i++)
            {
                var tExpected = "value" + i;
                Dynamic.InvokeSet(context, "Prop", tExpected);
                Assert.That(Dynamic.InvokeGet(context, "Prop"), Is.EqualTo(tExpected));
            }
        }

        // Regression coverage for issue #12: this fix routes static Get AND
        // Set through reflection unconditionally, so a static FIELD (not just
        // a property) must keep working, in both directions, on a public
        // top-level type.
        [Test]
        public void TestStaticFieldAlternatingGetSetRepeatedly_PublicTopLevelClass()
        {
            var context = InvokeContext.CreateStatic(typeof(Issue31FieldType));
            Assert.That(Dynamic.InvokeGet(context, "Field"), Is.EqualTo(1));
            Dynamic.InvokeSet(context, "Field", 2);
            Assert.That(Dynamic.InvokeGet(context, "Field"), Is.EqualTo(2));
            Dynamic.InvokeSet(context, "Field", 3);
            Assert.That(Dynamic.InvokeGet(context, "Field"), Is.EqualTo(3));
        }

        // Non-public and nested types never took the accessor-method DLR path
        // that #31 poisons (see the #12/#13 fallback) - they must keep
        // working unchanged by this fix, get/set alternating just like the
        // public top-level case above.
        [Test]
        public void TestStaticPropertyAlternatingGetSetRepeatedly_PrivateNestedClass()
        {
            var context = InvokeContext.CreateStatic(typeof(Issue31PrivateNestedType));
            Assert.That(Dynamic.InvokeGet(context, "Prop"), Is.EqualTo("initial"));
            Dynamic.InvokeSet(context, "Prop", "changed");
            Assert.That(Dynamic.InvokeGet(context, "Prop"), Is.EqualTo("changed"));
            Dynamic.InvokeSet(context, "Prop", "changed-again");
            Assert.That(Dynamic.InvokeGet(context, "Prop"), Is.EqualTo("changed-again"));
        }

        [Test]
        public void TestStaticPropertyAlternatingGetSetRepeatedly_InternalTopLevelClass()
        {
            var context = InvokeContext.CreateStatic(typeof(Issue31InternalTopLevelType));
            Assert.That(Dynamic.InvokeGet(context, "Prop"), Is.EqualTo("initial"));
            Dynamic.InvokeSet(context, "Prop", "changed");
            Assert.That(Dynamic.InvokeGet(context, "Prop"), Is.EqualTo("changed"));
            Dynamic.InvokeSet(context, "Prop", "changed-again");
            Assert.That(Dynamic.InvokeGet(context, "Prop"), Is.EqualTo("changed-again"));
        }

        // Set now reaches static members through the same reflection path as
        // Get, so it needs the same accessibility gate (see
        // TestInvokeGetPrivateStaticFieldRestrictedContextThrows and its
        // siblings above): a caller-supplied context unrelated to the target
        // type must not be able to set a private static member, but a PUBLIC
        // one must remain settable regardless.
        [Test]
        public void TestInvokeSetPrivateStaticFieldRestrictedContextThrows()
        {
            var context = InvokeContext.CreateStaticWithContext(typeof(Issue31PrivateFieldForSetContextTest), this);
            Assert.That(() => Dynamic.InvokeSet(context, "Secret", 5), Throws.InstanceOf<RuntimeBinderException>());
        }

        [Test]
        public void TestInvokeSetPrivateStaticPropertyRestrictedContextThrows()
        {
            var context = InvokeContext.CreateStaticWithContext(typeof(Issue31PrivatePropertyForSetContextTest), this);
            Assert.That(() => Dynamic.InvokeSet(context, "Secret", "hacked"), Throws.InstanceOf<RuntimeBinderException>());
        }

        [Test]
        public void TestInvokeSetPublicStaticFieldRestrictedContextStillSucceeds()
        {
            var context = InvokeContext.CreateStaticWithContext(typeof(Issue31PublicFieldForSetContextTest), this);
            Dynamic.InvokeSet(context, "Visible", 456);
            Assert.That(Issue31PublicFieldForSetContextTest.Visible, Is.EqualTo(456));
        }

        // Nested private class shape for the non-public/nested coverage above.
        // The property itself is private too, so this also exercises the
        // context-owns-private-access gate (InvokeContext.CreateStatic
        // defaults context to the target type itself, so access is granted).
        private class Issue31PrivateNestedType
        {
            private static string Prop { get; set; } = "initial";
        }
    }

    public class TestWithPrivateMethod
    {
        private int Test()
        {
            return 3;
        }
    }

    // For issue #12: reproduces the upstream reporter's exact shape - a private
    // instance field alongside a private static field on an internal
    // (default-access) top-level type.
#pragma warning disable CS0414 // fields are only ever read dynamically, never by name
    class ClassWithPrivateFields
    {
        private int field = 16;
        private static int other = 17;
    }
#pragma warning restore CS0414

    // For issue #12, generalized case found during investigation (see
    // TestInvokeGetPublicStaticField).
    public class PublicClassWithPublicStaticField
    {
        public static int Other = 42;
    }

    // For issue #13, shape from upstream PR #27: internal top-level class.
    class InternalClassWithPrivateStaticProperty
    {
        private static string Hello => "World";
    }

    // For issue #13, shape from upstream PR #27: public top-level class.
    public class PublicClassWithPrivateStaticProperty
    {
        private static string Hello => "World";
    }

    // For the accessibility-gate regression tests: a context unrelated to
    // these types must not be able to read their private static members via
    // the reflection fallback, even though reflection itself has no such
    // restriction.
#pragma warning disable CS0414 // field is only ever read dynamically, never by name
    class ClassWithPrivateStaticFieldForContextTest
    {
        private static int Secret = 99;
    }
#pragma warning restore CS0414

    class ClassWithPrivateStaticPropertyForContextTest
    {
        private static string Secret => "Hidden";
    }

    public class ClassWithPublicStaticFieldForContextTest
    {
        public static int Visible = 123;
    }

    // Issue #31 fixtures. Each type below backs exactly one test above - see
    // that test's comment for why they can't be shared.

    public class Issue31GetThenSetType
    {
        public static string Prop { get; set; } = "initial";
    }

    public class Issue31SetThenGetType
    {
        public static string Prop { get; set; } = "initial";
    }

    public class Issue31AlternatingType
    {
        public static string Prop { get; set; } = "initial";
    }

    public class Issue31FieldType
    {
        public static int Field = 1;
    }

    // For issue #31's non-public/nested coverage: internal top-level class,
    // matching the #13 shape (see InternalClassWithPrivateStaticProperty
    // above), but with a settable property so both directions can be
    // exercised.
    class Issue31InternalTopLevelType
    {
        private static string Prop { get; set; } = "initial";
    }

#pragma warning disable CS0414 // field is only ever read dynamically, never by name
    class Issue31PrivateFieldForSetContextTest
    {
        private static int Secret = 99;
    }
#pragma warning restore CS0414

    class Issue31PrivatePropertyForSetContextTest
    {
        private static string Secret { get; set; } = "Hidden";
    }

    public class Issue31PublicFieldForSetContextTest
    {
        public static int Visible = 123;
    }
}
