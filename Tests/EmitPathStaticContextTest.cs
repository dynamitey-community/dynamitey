// Issue #96. The >14-argument emit path omitted staticContext from CreateCallSite,
// so the binder cache key always hashed as instance. A 15-argument static call
// and an instance call of the same name/arity then shared one CallSite: whichever
// ran first poisoned the other for the rest of the process.
//
// Types here exist only for this fixture. typeof(object) is the accessibility
// context on both sides so the cache keys differ only by the static flag.
// ClearCaches at the start of each test so ordering is explicit, not leftover
// from another test.
using System;
using System.Linq;
using Microsoft.CSharp.RuntimeBinder;
using NUnit.Framework;

namespace Dynamitey.Tests
{
    public class EmitPathStaticContextTest : Helper
    {
        private static readonly object[] FifteenArgs =
            Enumerable.Repeat((object)1, 15).ToArray();

        [SetUp]
        public void ClearBinderCache()
        {
            Dynamic.ClearCaches();
        }

        [Test]
        public void FifteenArgStaticThenInstanceDoNotShareACallSite()
        {
            var tStatic = InvokeContext.CreateStaticWithContext(typeof(Issue96StaticSame), typeof(object));
            var tInstance = new InvokeContext(new Issue96InstanceSame(), typeof(object));

            Assert.That(Dynamic.InvokeMember(tStatic, "Same", FifteenArgs), Is.EqualTo("static"));
            Assert.That(Dynamic.InvokeMember(tInstance, "Same", FifteenArgs), Is.EqualTo("instance"));
        }

        [Test]
        public void FifteenArgInstanceThenStaticDoNotShareACallSite()
        {
            var tStatic = InvokeContext.CreateStaticWithContext(typeof(Issue96StaticSame), typeof(object));
            var tInstance = new InvokeContext(new Issue96InstanceSame(), typeof(object));

            Assert.That(Dynamic.InvokeMember(tInstance, "Same", FifteenArgs), Is.EqualTo("instance"));
            Assert.That(Dynamic.InvokeMember(tStatic, "Same", FifteenArgs), Is.EqualTo("static"));
        }

        [Test]
        public void FifteenArgActionStaticThenInstanceDoNotShareACallSite()
        {
            var tStatic = InvokeContext.CreateStaticWithContext(typeof(Issue96StaticActionSame), typeof(object));
            var tInstance = new Issue96InstanceActionSame();
            var tInstanceCtx = new InvokeContext(tInstance, typeof(object));

            Dynamic.InvokeMemberAction(tStatic, "Same", FifteenArgs);
            Dynamic.InvokeMemberAction(tInstanceCtx, "Same", FifteenArgs);

            Assert.That(Issue96StaticActionSame.Last, Is.EqualTo("static"));
            Assert.That(tInstance.Last, Is.EqualTo("instance"));
        }

        [Test]
        public void FifteenArgActionInstanceThenStaticDoNotShareACallSite()
        {
            var tStatic = InvokeContext.CreateStaticWithContext(typeof(Issue96StaticActionSame), typeof(object));
            var tInstance = new Issue96InstanceActionSame();
            var tInstanceCtx = new InvokeContext(tInstance, typeof(object));

            Dynamic.InvokeMemberAction(tInstanceCtx, "Same", FifteenArgs);
            Dynamic.InvokeMemberAction(tStatic, "Same", FifteenArgs);

            Assert.That(tInstance.Last, Is.EqualTo("instance"));
            Assert.That(Issue96StaticActionSame.Last, Is.EqualTo("static"));
        }

        [Test]
        public void FifteenArgCallSiteIsReusedOnRepeatInvoke()
        {
            var tStatic = InvokeContext.CreateStaticWithContext(typeof(Issue96StaticSame), typeof(object));

            Assert.That(Dynamic.InvokeMember(tStatic, "Same", FifteenArgs), Is.EqualTo("static"));
            Assert.That(Dynamic.InvokeMember(tStatic, "Same", FifteenArgs), Is.EqualTo("static"));
        }
    }

    public class Issue96StaticSame
    {
        public static string Same(
            object a1, object a2, object a3, object a4, object a5,
            object a6, object a7, object a8, object a9, object a10,
            object a11, object a12, object a13, object a14, object a15)
        {
            return "static";
        }
    }

    public class Issue96InstanceSame
    {
        public string Same(
            object a1, object a2, object a3, object a4, object a5,
            object a6, object a7, object a8, object a9, object a10,
            object a11, object a12, object a13, object a14, object a15)
        {
            return "instance";
        }
    }

    public class Issue96StaticActionSame
    {
        public static string Last;

        public static void Same(
            object a1, object a2, object a3, object a4, object a5,
            object a6, object a7, object a8, object a9, object a10,
            object a11, object a12, object a13, object a14, object a15)
        {
            Last = "static";
        }
    }

    public class Issue96InstanceActionSame
    {
        public string Last;

        public void Same(
            object a1, object a2, object a3, object a4, object a5,
            object a6, object a7, object a8, object a9, object a10,
            object a11, object a12, object a13, object a14, object a15)
        {
            Last = "instance";
        }
    }
}
