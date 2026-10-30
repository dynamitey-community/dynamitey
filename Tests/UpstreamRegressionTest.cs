// Regression tests for reports closed upstream (ekonbenefits/dynamitey) before 3.0.3 whose
// fixes are in this fork's history but were never pinned by a test that fails if the bug
// returns. Each test names the upstream number it covers.
using Microsoft.CSharp.RuntimeBinder;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Binder = Microsoft.CSharp.RuntimeBinder.Binder;

namespace Dynamitey.Tests
{
    public class UpstreamRegressionTest : Helper
    {
        private class PrivateAutoProperty
        {
            private int field { get; set; }

            public int Read() => field;
        }

        // Upstream #5: InvokeSet/InvokeGet on a private instance auto-property.
        [Test]
        public void TestInvokeSetThenGetOnPrivateInstanceAutoProperty()
        {
            var tTarget = new PrivateAutoProperty();

            Dynamic.InvokeSet(tTarget, "field", 5);

            Assert.That(tTarget.Read(), Is.EqualTo(5));
            Assert.That(Dynamic.InvokeGet(tTarget, "field"), Is.EqualTo(5));
        }

        // Upstream #8: calling Get.TryInvokeMember directly, as a forwarding DynamicObject
        // would, must return false for a missing member rather than throw.
        [Test]
        public void TestGetTryInvokeMemberReturnsFalseForMissingMember()
        {
            var tGet = new DynamicObjects.Get(new { a = 0 });
            var tBinder = (System.Dynamic.InvokeMemberBinder)Binder.InvokeMember(
                CSharpBinderFlags.None,
                "NoMethodHere",
                null,
                typeof(UpstreamRegressionTest),
                new[] { CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null) });

            var tFound = tGet.TryInvokeMember(tBinder, Array.Empty<object>(), out var tResult);

            Assert.That(tFound, Is.False);
            Assert.That(tResult, Is.Null);
        }

        // Upstream #15: a Get forwarder over a static context dispatches static members.
        [Test]
        public void TestGetOverStaticContextInvokesStaticMethod()
        {
            dynamic tDateTime = new DynamicObjects.Get(InvokeContext.CreateStatic(typeof(DateTime)));

            DateTime tDate = tDateTime.Parse("2009-01-20");

            Assert.That(tDate, Is.EqualTo(new DateTime(2009, 1, 20)));
        }

        private class Issue9Target
        {
            public int Value() => 9;
        }

        // Upstream #9 asked for a way to drop Dynamitey's cached binders so a collectible
        // assembly can unload. ClearCaches is that method, so this checks Dynamitey's own
        // member cache stops holding the target type. It does not GC an unloaded assembly:
        // Microsoft.CSharp's runtime binder caches every type it binds and never releases
        // it, so plain `dynamic` pins a collectible type with no Dynamitey involved.
        [Test]
        public void TestClearCachesDropsCachedBindersForTargetType()
        {
            Assert.That(Dynamic.InvokeMember(new Issue9Target(), "Value"), Is.EqualTo(9));
            Assert.That(CachedContexts(), Does.Contain(typeof(Issue9Target)),
                "The call above should have cached a binder keyed by its target type.");

            Dynamic.ClearCaches();

            Assert.That(CachedContexts(), Does.Not.Contain(typeof(Issue9Target)));
        }

        private static IEnumerable<Type> CachedContexts()
        {
            var tCacheType = typeof(Dynamic).Assembly
                .GetType("Dynamitey.Internal.Optimization.BinderMemberCache`1", throwOnError: true)!
                .MakeGenericType(typeof(Func<CallSite, object, object>));
            var tCache = (IEnumerable)tCacheType
                .GetProperty("Cache", BindingFlags.NonPublic | BindingFlags.Static)!
                .GetValue(null)!;
            foreach (var tEntry in tCache)
            {
                var tKey = tEntry.GetType().GetProperty("Key")!.GetValue(tEntry)!;
                yield return (Type)tKey.GetType().GetProperty("Context")!.GetValue(tKey)!;
            }
        }
    }
}
