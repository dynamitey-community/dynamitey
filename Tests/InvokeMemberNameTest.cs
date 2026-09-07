// InvokeMemberName/String_OR_InvokeMemberName's conversions, factories, and
// Equals/GetHashCode were only ever exercised incidentally (as a parameter
// passed through, never compared or converted directly). These tests cover
// the conversions, factories, and every branch of EqualsHelper.
using System;
using NUnit.Framework;

namespace Dynamitey.Tests
{
    public class InvokeMemberNameTest : Helper
    {
        [Test]
        public void TestImplicitConversionFromStringToBaseType()
        {
            String_OR_InvokeMemberName tName = "Foo";

            Assert.That(tName.Name, Is.EqualTo("Foo"));
            Assert.That(tName.GenericArgs, Is.Null);
        }

        [Test]
        public void TestBaseTypeFromStringFactory()
        {
            var tName = String_OR_InvokeMemberName.FromString("Bar");

            Assert.That(tName.Name, Is.EqualTo("Bar"));
        }

        [Test]
        public void TestImplicitConversionFromStringToInvokeMemberName()
        {
            InvokeMemberName tName = "Baz";

            Assert.That(tName.Name, Is.EqualTo("Baz"));
        }

        [Test]
        public void TestInvokeMemberNameFromStringFactory()
        {
            var tName = InvokeMemberName.FromString("Qux");

            Assert.That(tName.Name, Is.EqualTo("Qux"));
        }

        [Test]
        public void TestCreateFactoryWithGenericArgs()
        {
            var tName = InvokeMemberName.Create("Method", new[] { typeof(int) });

            Assert.That(tName.Name, Is.EqualTo("Method"));
            Assert.That(tName.GenericArgs, Is.EqualTo(new[] { typeof(int) }));
            Assert.That(tName.IsSpecialName, Is.False);
        }

        [Test]
        public void TestCreateSpecialNameFactory()
        {
            var tName = InvokeMemberName.CreateSpecialName("add_Event");

            Assert.That(tName.Name, Is.EqualTo("add_Event"));
            Assert.That(tName.IsSpecialName, Is.True);
            Assert.That(tName.GenericArgs, Is.Empty);
        }

        [Test]
        public void TestEqualsSameNameAndGenericArgs()
        {
            var tA = InvokeMemberName.Create("Method", new[] { typeof(int), typeof(string) });
            var tB = InvokeMemberName.Create("Method", new[] { typeof(int), typeof(string) });

            Assert.That(tA.Equals(tA), Is.True);
            Assert.That(tA.Equals(tB), Is.True);
        }

        [Test]
        public void TestEqualsDifferentName()
        {
            var tA = InvokeMemberName.Create("MethodA", null);
            var tB = InvokeMemberName.Create("MethodB", null);

            Assert.That(tA.Equals(tB), Is.False);
        }

        [Test]
        public void TestEqualsDifferentSpecialNameFlag()
        {
            var tA = new InvokeMemberName("Event", true);
            var tB = new InvokeMemberName("Event", false);

            Assert.That(tA.Equals(tB), Is.False);
        }

        [Test]
        public void TestEqualsOneNullGenericArgsOtherNot()
        {
            var tA = InvokeMemberName.Create("Method", null);
            var tB = InvokeMemberName.Create("Method", Array.Empty<Type>());

            Assert.That(tA.Equals(tB), Is.False);
        }

        [Test]
        public void TestEqualsDifferentGenericArgsSequence()
        {
            var tA = InvokeMemberName.Create("Method", new[] { typeof(int) });
            var tB = InvokeMemberName.Create("Method", new[] { typeof(string) });

            Assert.That(tA.Equals(tB), Is.False);
        }

        [Test]
        public void TestEqualsNullOtherReturnsFalse()
        {
            var tA = InvokeMemberName.Create("Method", null);

            Assert.That(tA.Equals((InvokeMemberName)null), Is.False);
        }

        [Test]
        public void TestEqualsObjectOverload()
        {
            var tA = InvokeMemberName.Create("Method", null);
            var tB = InvokeMemberName.Create("Method", null);

            Assert.That(tA.Equals((object)tA), Is.True);
            Assert.That(tA.Equals((object)tB), Is.True);
            Assert.That(tA.Equals((object)null), Is.False);
            Assert.That(tA.Equals(new object()), Is.False);
        }

        [Test]
        public void TestGetHashCodeConsistentWithEquals()
        {
            var tA = InvokeMemberName.Create("Method", new[] { typeof(int) });
            var tB = InvokeMemberName.Create("Method", new[] { typeof(int) });

            Assert.That(tA.Equals(tB), Is.True);
            Assert.That(tA.GetHashCode(), Is.EqualTo(tB.GetHashCode()));
        }
    }
}
