// Issue #103. Static InvokeSet goes through FieldInfo/PropertyInfo.SetValue
// and skips the C# runtime binder's implicit conversions. int-to-decimal
// then throws, and null-to-int writes default(int) instead of rejecting.
// Instance InvokeSet still uses Binder.SetMember, so the two paths disagree.
// Types here exist only for this fixture. Reset statics in SetUp so tests
// do not share leftover values.
using System;
using Microsoft.CSharp.RuntimeBinder;
using NUnit.Framework;

namespace Dynamitey.Tests
{
    public class StaticSetConversionTest : Helper
    {
        [SetUp]
        public void ResetStatics()
        {
            Issue103Target.StaticAmount = 0m;
            Issue103Target.StaticNumber = 42;
            Issue103Target.StaticName = "start";
            Issue103Target.StaticMaybe = 42;
            Issue103FieldTarget.StaticAmount = 0m;
            Issue103FieldTarget.StaticNumber = 42;
            Issue103ImplicitTarget.StaticNumber = 0;
        }

        [Test]
        public void InstanceSetConvertsIntToDecimal()
        {
            var tInstance = new Issue103Target();

            Dynamic.InvokeSet(tInstance, "Amount", 1);

            Assert.That(tInstance.Amount, Is.EqualTo(1m));
        }

        [Test]
        public void StaticSetConvertsIntToDecimalOnProperty()
        {
            var tStatic = InvokeContext.CreateStatic(typeof(Issue103Target));

            Dynamic.InvokeSet(tStatic, "StaticAmount", 1);

            Assert.That(Issue103Target.StaticAmount, Is.EqualTo(1m));
        }

        [Test]
        public void StaticSetConvertsIntToDecimalOnField()
        {
            var tStatic = InvokeContext.CreateStatic(typeof(Issue103FieldTarget));

            Dynamic.InvokeSet(tStatic, "StaticAmount", 1);

            Assert.That(Issue103FieldTarget.StaticAmount, Is.EqualTo(1m));
        }

        [Test]
        public void InstanceSetRejectsNullForNonNullableInt()
        {
            var tInstance = new Issue103Target { Number = 42 };

            Assert.That(() => Dynamic.InvokeSet(tInstance, "Number", null),
                Throws.InstanceOf<RuntimeBinderException>());
            Assert.That(tInstance.Number, Is.EqualTo(42));
        }

        [Test]
        public void StaticSetRejectsNullForNonNullableIntProperty()
        {
            var tStatic = InvokeContext.CreateStatic(typeof(Issue103Target));

            Assert.That(() => Dynamic.InvokeSet(tStatic, "StaticNumber", null),
                Throws.InstanceOf<RuntimeBinderException>());
            Assert.That(Issue103Target.StaticNumber, Is.EqualTo(42));
        }

        [Test]
        public void StaticSetRejectsNullForNonNullableIntField()
        {
            var tStatic = InvokeContext.CreateStatic(typeof(Issue103FieldTarget));

            Assert.That(() => Dynamic.InvokeSet(tStatic, "StaticNumber", null),
                Throws.InstanceOf<RuntimeBinderException>());
            Assert.That(Issue103FieldTarget.StaticNumber, Is.EqualTo(42));
        }

        [Test]
        public void StaticSetAllowsNullForString()
        {
            var tStatic = InvokeContext.CreateStatic(typeof(Issue103Target));

            Dynamic.InvokeSet(tStatic, "StaticName", null);

            Assert.That(Issue103Target.StaticName, Is.Null);
        }

        [Test]
        public void StaticSetAllowsNullForNullableInt()
        {
            var tStatic = InvokeContext.CreateStatic(typeof(Issue103Target));

            Dynamic.InvokeSet(tStatic, "StaticMaybe", null);

            Assert.That(Issue103Target.StaticMaybe, Is.Null);
        }

        [Test]
        public void StaticSetAppliesUserDefinedImplicitConversion()
        {
            var tStatic = InvokeContext.CreateStatic(typeof(Issue103ImplicitTarget));

            Dynamic.InvokeSet(tStatic, "StaticNumber", new Issue103Metres(5));

            Assert.That(Issue103ImplicitTarget.StaticNumber, Is.EqualTo(5));
        }

        [Test]
        public void StaticGetSetStillAlternatesAfterConvertedWrite()
        {
            var tStatic = InvokeContext.CreateStatic(typeof(Issue103Target));

            Dynamic.InvokeSet(tStatic, "StaticAmount", 1);
            Assert.That(Dynamic.InvokeGet(tStatic, "StaticAmount"), Is.EqualTo(1m));
            Dynamic.InvokeSet(tStatic, "StaticAmount", 2);
            Assert.That(Dynamic.InvokeGet(tStatic, "StaticAmount"), Is.EqualTo(2m));
        }
    }

    public class Issue103Target
    {
        public decimal Amount { get; set; }
        public static decimal StaticAmount { get; set; }
        public int Number { get; set; } = 42;
        public static int StaticNumber { get; set; } = 42;
        public static string StaticName { get; set; } = "start";
        public static int? StaticMaybe { get; set; } = 42;
    }

    public class Issue103FieldTarget
    {
        public static decimal StaticAmount;
        public static int StaticNumber = 42;
    }

    public class Issue103ImplicitTarget
    {
        public static int StaticNumber { get; set; }
    }

    public struct Issue103Metres
    {
        public Issue103Metres(int value)
        {
            Value = value;
        }

        public int Value { get; }

        public static implicit operator int(Issue103Metres metres) => metres.Value;
    }
}
