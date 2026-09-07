using System;
using System.Collections.Generic;
using System.Linq;
using Dynamitey.DynamicObjects;
using NUnit.Framework;

namespace Dynamitey.Tests
{
    /// <summary>
    /// Covers the FauxType/RealType/PropretySpecType/AggreType surface that
    /// FauxTypeTests (carried over from the former Impromptu fixture) does not exercise:
    /// the static factories, the implicit conversions, ContainsType, and AggreType's
    /// merge/append/dedup logic in AddType and MakeTypeAppendable.
    /// </summary>
    [TestFixture]
    public class FauxTypeGapTest : Helper
    {
        [Test]
        public void FauxType_FromType_ReturnsRealTypeWrappingTheGivenType()
        {
            var faux = FauxType.FromType(typeof(string));

            Assert.That(faux, Is.InstanceOf<RealType>());
            Assert.That(faux.GetContainedTypes(), Is.EqualTo(new[] { typeof(string) }));
        }

        [Test]
        public void FauxType_ImplicitFromType_ProducesRealType()
        {
            FauxType faux = typeof(int);

            Assert.That(faux, Is.InstanceOf<RealType>());
            Assert.That(faux.ContainsType(typeof(int)), Is.True);
        }

        [Test]
        public void FauxType_ContainsType_FalseWhenTypeNotAmongContainedTypes()
        {
            FauxType faux = typeof(int);

            Assert.That(faux.ContainsType(typeof(string)), Is.False);
        }

        [Test]
        public void PropretySpecType_GetMember_ReturnsEmptyWhenNameNotFound()
        {
            var spec = new PropretySpecType(new Dictionary<string, Type> { { "test", typeof(bool) } });

            var members = spec.GetMember("missing");

            Assert.That(members, Is.Empty);
        }

        [Test]
        public void PropretySpecType_GetContainedTypes_IsAlwaysEmpty()
        {
            var spec = new PropretySpecType(new Dictionary<string, Type> { { "test", typeof(bool) } });

            Assert.That(spec.GetContainedTypes(), Is.Empty);
        }

        [Test]
        public void RealType_ToTypeAndImplicitConversions_RoundTripTheWrappedType()
        {
            RealType real = typeof(Guid);

            Assert.That(real.ToType(), Is.EqualTo(typeof(Guid)));

            Type back = real;
            Assert.That(back, Is.EqualTo(typeof(Guid)));
        }

        [Test]
        public void RealType_FromType_ReturnsRealTypeWrappingTheGivenType()
        {
            var real = RealType.FromType(typeof(Guid));

            Assert.That(real.ToType(), Is.EqualTo(typeof(Guid)));
        }

        [Test]
        public void RealType_GetMember_FindsMemberByName()
        {
            var real = new RealType(typeof(string));

            var members = real.GetMember(nameof(string.Length));

            Assert.That(members.Any(), Is.True);
        }

        [Test]
        public void AggreType_MakeTypeAppendable_ThrowsOnNullTarget()
        {
            Assert.That(() => AggreType.MakeTypeAppendable(null!), Throws.ArgumentNullException);
        }

        [Test]
        public void AggreType_MakeTypeAppendable_CreatesAggreTypeWhenNoneSet()
        {
            var dict = new DynamicObjects.Dictionary();

            var aggr = AggreType.MakeTypeAppendable(dict);

            Assert.That(aggr, Is.Not.Null);
            Assert.That(((IEquivalentType)dict).EquivalentType, Is.SameAs(aggr));
        }

        [Test]
        public void AggreType_MakeTypeAppendable_WrapsExistingNonAggreEquivalentType()
        {
            var dict = new DynamicObjects.Dictionary();
            FauxType existing = typeof(string);
            ((IEquivalentType)dict).EquivalentType = existing;

            var aggr = AggreType.MakeTypeAppendable(dict);

            Assert.That(aggr.GetContainedTypes(), Does.Contain(typeof(string)));
            Assert.That(((IEquivalentType)dict).EquivalentType, Is.SameAs(aggr));
        }

        [Test]
        public void AggreType_MakeTypeAppendable_ReturnsExistingAggreTypeUnchanged()
        {
            var dict = new DynamicObjects.Dictionary();
            var existingAggr = new AggreType(typeof(string));
            ((IEquivalentType)dict).EquivalentType = existingAggr;

            var aggr = AggreType.MakeTypeAppendable(dict);

            Assert.That(aggr, Is.SameAs(existingAggr));
        }

        [Test]
        public void AggreType_GetInterfaceTypes_ReturnsOnlyInterfaces()
        {
            var aggr = new AggreType(typeof(string), typeof(IComparable));

            var interfaces = aggr.GetInterfaceTypes();

            Assert.That(interfaces, Does.Contain(typeof(IComparable)));
            Assert.That(interfaces, Does.Not.Contain(typeof(string)));
        }

        [Test]
        public void AggreType_AddType_ByType_SkipsDuplicates()
        {
            var aggr = new AggreType(typeof(string));

            aggr.AddType(typeof(string));

            Assert.That(aggr.GetContainedTypes().Count(t => t == typeof(string)), Is.EqualTo(1));
        }

        [Test]
        public void AggreType_AddType_ByType_AddsNewType()
        {
            var aggr = new AggreType(typeof(string));

            aggr.AddType(typeof(int));

            Assert.That(aggr.GetContainedTypes(), Does.Contain(typeof(int)));
        }

        [Test]
        public void AggreType_AddType_ByFauxType_FlattensRealType()
        {
            var aggr = new AggreType();

            aggr.AddType((FauxType)(RealType)typeof(string));

            Assert.That(aggr.GetContainedTypes(), Is.EqualTo(new[] { typeof(string) }));
        }

        [Test]
        public void AggreType_AddType_ByFauxType_MergesNestedAggreType()
        {
            var aggr = new AggreType();
            var nested = new AggreType(typeof(string), typeof(int));

            aggr.AddType(nested);

            Assert.That(aggr.GetContainedTypes(), Is.EquivalentTo(new[] { typeof(string), typeof(int) }));
        }

        [Test]
        public void AggreType_AddType_ByFauxType_AppendsNonRealNonAggreDirectly()
        {
            var aggr = new AggreType();
            var spec = new PropretySpecType(new Dictionary<string, Type> { { "test", typeof(bool) } });

            aggr.AddType(spec);

            Assert.That(aggr.GetMemberNames(), Does.Contain("test"));
        }

        [Test]
        public void AggreType_GetMember_AggregatesAcrossChildTypes()
        {
            var spec = new PropretySpecType(new Dictionary<string, Type> { { "test", typeof(bool) } });
            var aggr = new AggreType(spec, (RealType)typeof(string));

            var members = aggr.GetMember("test");

            Assert.That(members.Any(), Is.True);
        }
    }
}
