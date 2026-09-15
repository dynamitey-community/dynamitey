// Issue #108. Static reflection allowed a non-public member only when
// context == targetType. That rejects a derived-class context, which in C#
// has protected access to the base static member. Private must stay rejected
// from derived and unrelated contexts. Types here exist only for this fixture.
using System;
using Dynamitey.SupportLibrary;
using Microsoft.CSharp.RuntimeBinder;
using NUnit.Framework;

namespace Dynamitey.Tests
{
    public class StaticProtectedAccessTest : Helper
    {
        [SetUp]
        public void ResetStatics()
        {
            Issue108Base.Reset();
            Issue108Outer.Reset();
            Issue108InternalHolder.Value = 4;
            Issue108ProtectedEvent.Handlers = null;
            Issue108PiLocal.Reset();
            Issue108PpLocal.Reset();
            Issue108CrossAssemblyBase.Reset();
            Issue108NestHost.Nested.Reset();
        }

        [Test]
        public void DerivedContextCanGetProtectedStaticProperty()
        {
            var tContext = InvokeContext.CreateStaticWithContext(
                typeof(Issue108Base), typeof(Issue108Derived));

            Assert.That(Dynamic.InvokeGet(tContext, "Value"), Is.EqualTo(7));
        }

        [Test]
        public void DerivedContextCanSetProtectedStaticProperty()
        {
            var tContext = InvokeContext.CreateStaticWithContext(
                typeof(Issue108Base), typeof(Issue108Derived));

            Dynamic.InvokeSet(tContext, "Value", 9);

            Assert.That(Dynamic.InvokeGet(tContext, "Value"), Is.EqualTo(9));
        }

        [Test]
        public void DerivedContextCanGetAndSetProtectedStaticField()
        {
            var tContext = InvokeContext.CreateStaticWithContext(
                typeof(Issue108Base), typeof(Issue108Derived));

            Assert.That(Dynamic.InvokeGet(tContext, "Field"), Is.EqualTo(7));
            Dynamic.InvokeSet(tContext, "Field", 11);
            Assert.That(Dynamic.InvokeGet(tContext, "Field"), Is.EqualTo(11));
        }

        [Test]
        public void DerivedContextCannotGetPrivateStaticProperty()
        {
            var tContext = InvokeContext.CreateStaticWithContext(
                typeof(Issue108Base), typeof(Issue108Derived));

            Assert.That(() => Dynamic.InvokeGet(tContext, "Secret"),
                Throws.InstanceOf<RuntimeBinderException>());
        }

        [Test]
        public void UnrelatedContextCannotGetProtectedStaticProperty()
        {
            var tContext = InvokeContext.CreateStaticWithContext(
                typeof(Issue108Base), typeof(Issue108Unrelated));

            Assert.That(() => Dynamic.InvokeGet(tContext, "Value"),
                Throws.InstanceOf<RuntimeBinderException>());
        }

        [Test]
        public void NestedTypeCanGetEnclosingPrivateStatic()
        {
            var tContext = InvokeContext.CreateStaticWithContext(
                typeof(Issue108Outer), typeof(Issue108Outer.Inner));

            Assert.That(Dynamic.InvokeGet(tContext, "Secret"), Is.EqualTo(3));
        }

        [Test]
        public void SameAssemblyContextCanGetInternalStatic()
        {
            var tContext = InvokeContext.CreateStaticWithContext(
                typeof(Issue108InternalHolder), typeof(Issue108Unrelated));

            Assert.That(Dynamic.InvokeGet(tContext, "Value"), Is.EqualTo(4));
        }

        [Test]
        public void EnclosingTypeCannotGetNestedPrivateStatic()
        {
            var tContext = InvokeContext.CreateStaticWithContext(
                typeof(Issue108NestHost.Nested), typeof(Issue108NestHost));

            Assert.That(() => Dynamic.InvokeGet(tContext, "Hidden"),
                Throws.InstanceOf<RuntimeBinderException>());
        }

        [Test]
        public void SameAssemblyUnrelatedContextCanGetProtectedInternal()
        {
            var tContext = InvokeContext.CreateStaticWithContext(
                typeof(Issue108PiLocal), typeof(Issue108Unrelated));

            Assert.That(Dynamic.InvokeGet(tContext, "Value"), Is.EqualTo(8));
        }

        [Test]
        public void SameAssemblyDerivedContextCanGetPrivateProtected()
        {
            var tContext = InvokeContext.CreateStaticWithContext(
                typeof(Issue108PpLocal), typeof(Issue108PpDerived));

            Assert.That(Dynamic.InvokeGet(tContext, "Value"), Is.EqualTo(5));
        }

        [Test]
        public void CrossAssemblyDerivedContextCanGetProtectedInternal()
        {
            var tContext = InvokeContext.CreateStaticWithContext(
                typeof(Issue108CrossAssemblyBase), typeof(Issue108CrossDerived));

            Assert.That(Dynamic.InvokeGet(tContext, "ProtectedInternal"), Is.EqualTo(6));
        }

        [Test]
        public void CrossAssemblyDerivedContextCannotGetPrivateProtected()
        {
            var tContext = InvokeContext.CreateStaticWithContext(
                typeof(Issue108CrossAssemblyBase), typeof(Issue108CrossDerived));

            Assert.That(() => Dynamic.InvokeGet(tContext, "PrivateProtected"),
                Throws.InstanceOf<RuntimeBinderException>());
        }

        [Test]
        public void CrossAssemblyUnrelatedContextCannotGetProtectedInternal()
        {
            var tContext = InvokeContext.CreateStaticWithContext(
                typeof(Issue108CrossAssemblyBase), typeof(Issue108Unrelated));

            Assert.That(() => Dynamic.InvokeGet(tContext, "ProtectedInternal"),
                Throws.InstanceOf<RuntimeBinderException>());
        }

        [Test]
        public void DerivedContextCanAddAndRemoveProtectedStaticEvent()
        {
            var tContext = InvokeContext.CreateStaticWithContext(
                typeof(Issue108ProtectedEvent), typeof(Issue108DerivedEvent));
            var tHit = false;
            EventHandler<EventArgs> tHandler = (s, e) => tHit = true;

            Assert.That(Dynamic.InvokeIsEvent(tContext, "Changed"), Is.True);
            Dynamic.InvokeAddAssignMember(tContext, "Changed", tHandler);
            Issue108ProtectedEvent.Raise();
            Assert.That(tHit, Is.True);

            tHit = false;
            Dynamic.InvokeSubtractAssignMember(tContext, "Changed", tHandler);
            Issue108ProtectedEvent.Raise();
            Assert.That(tHit, Is.False);
        }
    }

    public class Issue108Base
    {
        protected static int Value { get; set; } = 7;
        protected static int Field = 7;
        private static int Secret { get; set; } = 1;

        public static void Reset()
        {
            Value = 7;
            Field = 7;
            Secret = 1;
        }
    }

    public class Issue108Derived : Issue108Base
    {
    }

    public class Issue108Unrelated
    {
    }

    public class Issue108Outer
    {
        private static int Secret { get; set; } = 3;

        public static void Reset()
        {
            Secret = 3;
        }

        public class Inner
        {
        }
    }

    internal class Issue108InternalHolder
    {
        internal static int Value { get; set; } = 4;
    }

    public class Issue108ProtectedEvent
    {
        public static EventHandler<EventArgs> Handlers;

        protected static event EventHandler<EventArgs> Changed
        {
            add => Handlers += value;
            remove => Handlers -= value;
        }

        public static void Raise()
        {
            Handlers?.Invoke(null, EventArgs.Empty);
        }
    }

    public class Issue108DerivedEvent : Issue108ProtectedEvent
    {
    }

    public class Issue108NestHost
    {
        public class Nested
        {
            private static int Hidden { get; set; } = 9;

            public static void Reset()
            {
                Hidden = 9;
            }
        }
    }

    public class Issue108PiLocal
    {
        protected internal static int Value { get; set; } = 8;

        public static void Reset()
        {
            Value = 8;
        }
    }

    public class Issue108PpLocal
    {
        private protected static int Value { get; set; } = 5;

        public static void Reset()
        {
            Value = 5;
        }
    }

    public class Issue108PpDerived : Issue108PpLocal
    {
    }

    public class Issue108CrossDerived : Issue108CrossAssemblyBase
    {
    }
}
