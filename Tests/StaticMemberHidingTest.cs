// Issue #133. FlattenHierarchy GetField/GetProperty can bind a base static
// member when the target type hides it with a different member of the same
// name. Get tries field first, so a derived property hiding a base field
// was invisible. Types here exist only for this fixture.
using System;
using NUnit.Framework;

namespace Dynamitey.Tests
{
    public class StaticMemberHidingTest : Helper
    {
        [SetUp]
        public void ResetStatics()
        {
            Issue133Base.Value = 1;
            Issue133Derived.Value = 2;
            Issue133FieldHides.Value = 3;
            Issue133Inherited.Keep = 4;
            Issue133EventBase.Handlers = null;
        }

        [Test]
        public void DerivedPropertyHidesBaseStaticField()
        {
            var tStatic = InvokeContext.CreateStatic(typeof(Issue133Derived));

            Assert.That(Dynamic.InvokeGet(tStatic, "Value"), Is.EqualTo(2));
        }

        [Test]
        public void DerivedPropertyHideSetDoesNotWriteBaseField()
        {
            var tStatic = InvokeContext.CreateStatic(typeof(Issue133Derived));

            Dynamic.InvokeSet(tStatic, "Value", 9);

            Assert.That(Issue133Derived.Value, Is.EqualTo(9));
            Assert.That(Issue133Base.Value, Is.EqualTo(1));
        }

        [Test]
        public void DerivedFieldHidesBaseStaticProperty()
        {
            var tStatic = InvokeContext.CreateStatic(typeof(Issue133FieldHides));

            Assert.That(Dynamic.InvokeGet(tStatic, "Value"), Is.EqualTo(3));
        }

        [Test]
        public void InheritedStaticStillBinds()
        {
            var tStatic = InvokeContext.CreateStatic(typeof(Issue133Inherited));

            Assert.That(Dynamic.InvokeGet(tStatic, "Keep"), Is.EqualTo(4));
            Dynamic.InvokeSet(tStatic, "Keep", 5);
            Assert.That(Issue133Base.Keep, Is.EqualTo(5));
        }

        [Test]
        public void InheritedStaticEventBindsThroughDerivedType()
        {
            var tStatic = InvokeContext.CreateStatic(typeof(Issue133EventDerived));
            var tHit = false;
            EventHandler tHandler = (s, e) => tHit = true;

            Assert.That(Dynamic.InvokeIsEvent(tStatic, "Changed"), Is.True);
            Dynamic.InvokeAddAssignMember(tStatic, "Changed", tHandler);
            Issue133EventBase.Raise();
            Assert.That(tHit, Is.True);

            tHit = false;
            Dynamic.InvokeSubtractAssignMember(tStatic, "Changed", tHandler);
            Issue133EventBase.Raise();
            Assert.That(tHit, Is.False);
        }

        [Test]
        public void DerivedPropertyHidesBaseStaticEvent()
        {
            var tStatic = InvokeContext.CreateStatic(typeof(Issue133EventHides));

            Assert.That(Dynamic.InvokeIsEvent(tStatic, "Changed"), Is.False);
            Assert.That(Dynamic.InvokeGet(tStatic, "Changed"), Is.EqualTo(0));
        }
    }

    public class Issue133Base
    {
        public static int Value = 1;
        public static int Keep { get; set; } = 4;
    }

    public class Issue133Derived : Issue133Base
    {
        public static new int Value { get; set; } = 2;
    }

    public class Issue133FieldHides : Issue133Base
    {
        public static new int Value = 3;
    }

    public class Issue133Inherited : Issue133Base
    {
    }

    public class Issue133EventBase
    {
        public static EventHandler Handlers;

        public static event EventHandler Changed
        {
            add => Handlers += value;
            remove => Handlers -= value;
        }

        public static void Raise()
        {
            Handlers?.Invoke(null, EventArgs.Empty);
        }
    }

    public class Issue133EventDerived : Issue133EventBase
    {
    }

    public class Issue133EventHides : Issue133EventBase
    {
        public static new int Changed { get; set; }
    }
}
