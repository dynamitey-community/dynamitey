using System;
using System.Collections.Generic;
using Dynamitey.DynamicObjects;
using NUnit.Framework;

namespace Dynamitey.Tests
{
    // CA2225 batch (see the csproj backlog comment): eight of the library's twelve operator
    // overloads got a purely-additive named alternate for callers in a language that cannot
    // consume operator overloads. Each test below proves the named alternate produces the same
    // result as the operator it mirrors, not just that it compiles; each was also proven
    // load-bearing by breaking the named alternate's body in an isolated worktree and confirming
    // the matching test fails.
    [TestFixture]
    public class Ca2225NamedAlternateTest : Helper
    {
        [Test]
        public void FauxType_FromType_MatchesImplicitOperator()
        {
            FauxType tOperator = typeof(string);
            var tNamed = FauxType.FromType(typeof(string));

            Assert.That(tNamed, Is.TypeOf<RealType>());
            Assert.That(tNamed.GetMemberNames(), Is.EqualTo(tOperator.GetMemberNames()));
        }

        [Test]
        public void RealType_ToType_MatchesImplicitOperator()
        {
            var tRealType = new RealType(typeof(string));

            Type tOperator = tRealType;
            var tNamed = tRealType.ToType();

            Assert.That(tNamed, Is.EqualTo(tOperator));
            Assert.That(tNamed, Is.EqualTo(typeof(string)));
        }

        [Test]
        public void RealType_FromType_MatchesImplicitOperator()
        {
            RealType tOperator = typeof(string);
            var tNamed = RealType.FromType(typeof(string));

            Assert.That((Type)tNamed, Is.EqualTo((Type)tOperator));
        }

        [Test]
        public void InvokeArg_FromKeyValuePair_MatchesExplicitOperator()
        {
            var tPair = new KeyValuePair<string, object>("arg", 42);

            var tOperator = (InvokeArg)tPair;
            var tNamed = InvokeArg.FromKeyValuePair(tPair);

            Assert.That(tNamed.Name, Is.EqualTo(tOperator.Name));
            Assert.That(tNamed.Value, Is.EqualTo(tOperator.Value));
        }

        [Test]
        public void InvokeArgOfT_FromKeyValuePair_MatchesExplicitOperator()
        {
            var tPair = new KeyValuePair<string, int>("arg", 42);

            var tOperator = (InvokeArg<int>)tPair;
            var tNamed = InvokeArg<int>.FromKeyValuePair(tPair);

            Assert.That(tNamed.Name, Is.EqualTo(tOperator.Name));
            Assert.That(tNamed.Value, Is.EqualTo(tOperator.Value));
        }

        [Test]
        public void StaticContext_FromType_MatchesExplicitOperator()
        {
            var tOperator = (StaticContext)typeof(string);
            var tNamed = StaticContext.FromType(typeof(string));

            Assert.That(tNamed.Context, Is.EqualTo(tOperator.Context));
        }

        [Test]
        public void String_OR_InvokeMemberName_FromString_MatchesImplicitOperator()
        {
            String_OR_InvokeMemberName tOperator = "MethodName";
            var tNamed = String_OR_InvokeMemberName.FromString("MethodName");

            Assert.That(tNamed.Name, Is.EqualTo(tOperator.Name));
            Assert.That(tNamed.GenericArgs, Is.EqualTo(tOperator.GenericArgs));
        }

        [Test]
        public void InvokeMemberName_FromString_MatchesImplicitOperator()
        {
            InvokeMemberName tOperator = "MethodName";
            var tNamed = InvokeMemberName.FromString("MethodName");

            Assert.That(tNamed.Name, Is.EqualTo(tOperator.Name));
            Assert.That(tNamed.GenericArgs, Is.EqualTo(tOperator.GenericArgs));
        }
    }
}
