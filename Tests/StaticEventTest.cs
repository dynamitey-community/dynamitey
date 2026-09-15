// Issue #97. InvokeIsEvent computed staticContext from GetTargetContext and then
// dropped it. Binder.IsEvent has no IsStaticType argument list, so a static
// event on a Type target reported false and add/remove took the Get/+=/Set
// path. Custom add/remove static events threw. Types here exist only for this
// fixture.
using System;
using Dynamitey.SupportLibrary;
using NUnit.Framework;

namespace Dynamitey.Tests
{
    public class StaticEventTest : Helper
    {
        [SetUp]
        public void ResetHandlers()
        {
            Issue97PublicStaticCustomEvent.Handlers = null;
        }

        [Test]
        public void InvokeIsEventOnPublicStaticCustomEventReturnsTrue()
        {
            var tStatic = InvokeContext.CreateStatic(typeof(Issue97PublicStaticCustomEvent));

            Assert.That(Dynamic.InvokeIsEvent(tStatic, "Changed"), Is.True);
        }

        [Test]
        public void InvokeIsEventOnInstanceEventStillReturnsTrue()
        {
            Assert.That(Dynamic.InvokeIsEvent(new PocoEvent(), "Event"), Is.True);
        }

        [Test]
        public void CacheableInvocationIsEventAgreesForStaticEvent()
        {
            var tStatic = InvokeContext.CreateStatic(typeof(Issue97PublicStaticCustomEvent));
            var tInvocation = new CacheableInvocation(InvocationKind.IsEvent, "Changed", context: tStatic);

            Assert.That(tInvocation.Invoke(tStatic), Is.True);
        }

        [Test]
        public void AddAssignAndSubtractAssignACustomStaticEvent()
        {
            var tStatic = InvokeContext.CreateStatic(typeof(Issue97PublicStaticCustomEvent));
            var tHit = false;
            EventHandler<EventArgs> tHandler = (s, e) => tHit = true;

            Dynamic.InvokeAddAssignMember(tStatic, "Changed", tHandler);
            Issue97PublicStaticCustomEvent.Raise();
            Assert.That(tHit, Is.True);

            tHit = false;
            Dynamic.InvokeSubtractAssignMember(tStatic, "Changed", tHandler);
            Issue97PublicStaticCustomEvent.Raise();
            Assert.That(tHit, Is.False);
        }

        [Test]
        public void CacheableAddAssignAndSubtractAssignACustomStaticEvent()
        {
            var tStatic = InvokeContext.CreateStatic(typeof(Issue97PublicStaticCustomEvent));
            var tHit = false;
            EventHandler<EventArgs> tHandler = (s, e) => tHit = true;

            new CacheableInvocation(InvocationKind.AddAssign, "Changed", context: tStatic)
                .Invoke(tStatic, tHandler);
            Issue97PublicStaticCustomEvent.Raise();
            Assert.That(tHit, Is.True);

            tHit = false;
            new CacheableInvocation(InvocationKind.SubtractAssign, "Changed", context: tStatic)
                .Invoke(tStatic, tHandler);
            Issue97PublicStaticCustomEvent.Raise();
            Assert.That(tHit, Is.False);
        }

        [Test]
        public void RestrictedContextCannotSeeAPrivateStaticEvent()
        {
            var tRestricted = InvokeContext.CreateStaticWithContext(
                typeof(Issue97PrivateStaticEvent), typeof(object));

            Assert.That(Dynamic.InvokeIsEvent(tRestricted, "Hidden"), Is.False);
        }

        [Test]
        public void OwningStaticContextCanSeeAPrivateStaticEvent()
        {
            var tOwned = InvokeContext.CreateStatic(typeof(Issue97PrivateStaticEvent));

            Assert.That(Dynamic.InvokeIsEvent(tOwned, "Hidden"), Is.True);
        }
    }

    public class Issue97PublicStaticCustomEvent
    {
        public static EventHandler<EventArgs> Handlers;

        public static event EventHandler<EventArgs> Changed
        {
            add => Handlers += value;
            remove => Handlers -= value;
        }

        public static void Raise()
        {
            Handlers?.Invoke(null, EventArgs.Empty);
        }
    }

    public class Issue97PrivateStaticEvent
    {
        private static event EventHandler<EventArgs> Hidden
        {
            add { }
            remove { }
        }
    }
}
