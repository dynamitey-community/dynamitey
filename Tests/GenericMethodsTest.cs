using System;
using Dynamitey.SupportLibrary;
using NUnit.Framework;

namespace Dynamitey.Tests
{
    // Issue #14 investigation. Reported as upstream #31 (2020), never resolved.
    // The reporter said invoking a generic typed method threw, and that raw
    // reflection's MakeGenericMethod worked around it - but never sent a repro:
    // no code, no exception text, no signature. The maintainer replied that
    // inference from argument types works "just like in C#", which by omission
    // points at the untested case: explicit generic arguments supplied via
    // InvokeMemberName(name, typeof(T), ...), particularly where inference
    // cannot determine the type argument at all (a method generic only in its
    // return type).
    //
    // This file is the matrix from the issue brief, run against
    // Dynamitey.SupportLibrary.GenericMethodsTestClass (and friends) declared
    // in SupportTypes.cs for this investigation. Every cell below passed -
    // Dynamic.InvokeMember with an explicit InvokeMemberName(name, typeof(T))
    // agrees with raw reflection's MakeGenericMethod in every shape tried.
    // Nothing reproduced; these tests stand as permanent coverage of an
    // otherwise-untested public code path (InvokeMemberName's generic-args
    // constructor).
    [TestFixture]
    public class GenericMethodsTest : Helper
    {
        [Test]
        public void ExplicitGenericArg_ReturnTypeOnly_ReferenceType()
        {
            var target = new GenericMethodsTestClass();

            var result = Dynamic.InvokeMember(target, new InvokeMemberName("Create", typeof(GenericMethodsTestClass)));

            Assert.That(result, Is.InstanceOf<GenericMethodsTestClass>());
        }

        [Test]
        public void ExplicitGenericArg_ReturnTypeOnly_ValueType()
        {
            var target = new GenericMethodsTestClass();

            var result = Dynamic.InvokeMember(target, new InvokeMemberName("Create", typeof(int)));

            Assert.That((object)result, Is.EqualTo(0));
        }

        [Test]
        public void ExplicitGenericArg_MatchesReflectionMakeGenericMethod()
        {
            var target = new GenericMethodsTestClass();

            var viaDynamitey = Dynamic.InvokeMember(target, new InvokeMemberName("Create", typeof(GenericConstraintDerived)));

            var method = typeof(GenericMethodsTestClass).GetMethod("Create").MakeGenericMethod(typeof(GenericConstraintDerived));
            var viaReflection = method.Invoke(target, null);

            Assert.That(viaDynamitey, Is.InstanceOf(viaReflection.GetType()));
        }

        [Test]
        public void ExplicitGenericArg_StaticMethod()
        {
            var result = Dynamic.InvokeMember(new StaticContext(typeof(GenericMethodsTestClass)), new InvokeMemberName("StaticCreate", typeof(int)));

            Assert.That((object)result, Is.EqualTo(0));
        }

        [Test]
        public void ExplicitGenericArg_NoArgsToInferFrom_ValueType()
        {
            var target = new GenericMethodsTestClass();

            var result = Dynamic.InvokeMember(target, new InvokeMemberName("Default", typeof(int)));

            Assert.That((object)result, Is.EqualTo(0));
        }

        [Test]
        public void ExplicitGenericArg_NoArgsToInferFrom_ReferenceType()
        {
            var target = new GenericMethodsTestClass();

            var result = Dynamic.InvokeMember(target, new InvokeMemberName("Default", typeof(string)));

            Assert.That((object)result, Is.Null);
        }

        [Test]
        public void ExplicitGenericArg_AgreesWithInference()
        {
            var target = new GenericMethodsTestClass();

            var viaInference = Dynamic.InvokeMember(target, "Echo", 42);
            var viaExplicit = Dynamic.InvokeMember(target, new InvokeMemberName("Echo", typeof(int)), 42);

            Assert.That((object)viaExplicit, Is.EqualTo(42));
            Assert.That((object)viaExplicit, Is.EqualTo((object)viaInference));
        }

        [Test]
        public void ExplicitGenericArg_WhereInferenceCouldNotResolve()
        {
            // Only the inferable argument is passed; the non-inferable type
            // parameter is supplied explicitly.
            var target = new GenericMethodsTestClass();

            var result = Dynamic.InvokeMember(target, new InvokeMemberName("Combine", typeof(int), typeof(GenericConstraintDerived)), 5);

            var method = typeof(GenericMethodsTestClass).GetMethod("Combine").MakeGenericMethod(typeof(int), typeof(GenericConstraintDerived));
            var viaReflection = method.Invoke(target, new object[] { 5 });

            Assert.That(result, Is.InstanceOf<GenericConstraintDerived>());
            Assert.That(result, Is.InstanceOf(viaReflection.GetType()));
        }

        [Test]
        public void ExplicitGenericArg_ConstraintWhereClass()
        {
            var target = new GenericMethodsTestClass();

            var result = Dynamic.InvokeMember(target, new InvokeMemberName("EchoClass", typeof(string)), "hi");

            Assert.That((object)result, Is.EqualTo("hi"));
        }

        [Test]
        public void ExplicitGenericArg_ConstraintWhereNew()
        {
            var target = new GenericMethodsTestClass();

            var result = Dynamic.InvokeMember(target, new InvokeMemberName("CreateNew", typeof(GenericMethodsTestClass)));

            Assert.That(result, Is.InstanceOf<GenericMethodsTestClass>());
        }

        [Test]
        public void ExplicitGenericArg_ConstraintWhereBaseType()
        {
            var target = new GenericMethodsTestClass();

            var result = Dynamic.InvokeMember(target, new InvokeMemberName("DescribeConstrained", typeof(GenericConstraintDerived)));

            Assert.That((object)result, Is.EqualTo("derived"));
        }

        [Test]
        public void ExplicitGenericArg_WithParams()
        {
            var target = new GenericMethodsTestClass();

            var result = Dynamic.InvokeMember(target, new InvokeMemberName("Join", typeof(string)), "a", "b", "c");

            Assert.That((object)result, Is.EqualTo("a,b,c"));
        }

        [Test]
        public void ExplicitGenericArg_GenericMethodOnGenericType()
        {
            var target = new GenericMethodsGenericTypeTestClass<string> { Value = "outer" };

            var result = Dynamic.InvokeMember(target, new InvokeMemberName("Cast", typeof(int)));

            Assert.That((object)result, Is.EqualTo(0));
        }

        [Test]
        public void ExplicitGenericArg_NonPublicType()
        {
            var target = GenericMethodsTestClass.InternalGenericInstance;

            var result = Dynamic.InvokeMember(target, new InvokeMemberName("Create", typeof(int)));

            Assert.That((object)result, Is.EqualTo(0));
        }

        [Test]
        public void ExplicitGenericArg_InvokeMemberAction_VoidReturning()
        {
            var target = new GenericMethodsTestClass();

            Dynamic.InvokeMemberAction(target, new InvokeMemberName("SetDefault", typeof(string)));

            Assert.That(target.LastSetValue, Is.EqualTo("String"));
        }

        [Test]
        public void ExplicitGenericArg_CacheableInvocation()
        {
            var target = new GenericMethodsTestClass();

            var invocation = CacheableInvocation.CreateCall(InvocationKind.InvokeMemberUnknown,
                new InvokeMemberName("Create", typeof(int)));

            var result = invocation.Invoke(target);

            Assert.That((object)result, Is.EqualTo(0));
        }
    }
}
