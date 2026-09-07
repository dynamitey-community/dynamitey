using System;
using System.Collections.Generic;
using System.Reflection;
using Dynamitey.DynamicObjects;
using NUnit.Framework;

namespace Dynamitey.Tests
{
    // A minimal EventInfo whose EventHandlerType is null - something the real CLR reflection API
    // never produces, but exactly the shape BaseObject.TryTypeForName's EventInfo case needs to
    // hit its documented, otherwise-unreachable null-handler-type edge (issue #42 gap 5).
    internal sealed class NullHandlerTypeEventInfo : EventInfo
    {
        public override EventAttributes Attributes => EventAttributes.None;
        public override Type DeclaringType => typeof(object);
        public override string Name => "FakeEvent";
        public override Type ReflectedType => typeof(object);
        public override Type EventHandlerType => null;

        public override MethodInfo GetAddMethod(bool nonPublic) => null;
        public override MethodInfo GetRemoveMethod(bool nonPublic) => null;
        public override MethodInfo GetRaiseMethod(bool nonPublic) => null;

        public override object[] GetCustomAttributes(bool inherit) => Array.Empty<object>();
        public override object[] GetCustomAttributes(Type attributeType, bool inherit) => Array.Empty<object>();
        public override bool IsDefined(Type attributeType, bool inherit) => false;
    }

    // A FauxType that reports a single member: the pathological event above.
    internal sealed class NullHandlerTypeEventFauxType : FauxType
    {
        public override IEnumerable<MemberInfo> GetMember(string binderName)
        {
            if (binderName == "FakeEvent")
                return new MemberInfo[] { new NullHandlerTypeEventInfo() };
            return Array.Empty<MemberInfo>();
        }

        public override IEnumerable<string> GetMemberNames()
        {
            return new[] { "FakeEvent" };
        }

        public override Type[] GetContainedTypes() => Array.Empty<Type>();
    }

    [TestFixture]
    public class TryTypeForNameEventGapTest : Helper
    {
        // Issue #42 gap 5: TryTypeForName's [NotNullWhen(true)] out parameter could be null when
        // the resolved member is an event whose EventHandlerType is itself null - unreachable via
        // real CLR reflection, so this uses a custom EventInfo to exercise the exact hole and
        // prove the tightened check closes it: the out parameter is now treated the same as any
        // other type mismatch (falls back to object) instead of leaking null through a
        // NotNullWhen(true) result.
        [Test]
        public void EventWithNullHandlerType_DoesNotReturnNullDespiteNotNullWhenTrue()
        {
            var tDict = new DynamicObjects.Dictionary();
            ((IEquivalentType)tDict).EquivalentType = new NullHandlerTypeEventFauxType();

            var tFound = tDict.TryTypeForName("FakeEvent", out var tType);

            Assert.That(tFound, Is.True);
            Assert.That(tType, Is.Not.Null);
            Assert.That(tType, Is.EqualTo(typeof(object)));
        }
    }
}
