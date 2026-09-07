using System;
using System.Dynamic;
using NUnit.Framework;

namespace Dynamitey.Tests
{
    /// <summary>
    /// MimicTest exercises every Mimic override reachable through ordinary C# dynamic syntax.
    /// Three DynamicObject overrides have no C# syntax that triggers them at all - TryCreateInstance
    /// ("new dynamicVar(args)"), TryDeleteMember and TryDeleteIndex (a "del" statement, as in
    /// IronPython) are never emitted by the C# compiler - so they're called directly here, exactly
    /// as the DLR would, via minimal binder stubs.
    /// </summary>
    [TestFixture]
    public class MimicGapTest
    {
        private sealed class StubCreateInstanceBinder : CreateInstanceBinder
        {
            public StubCreateInstanceBinder(CallInfo callInfo) : base(callInfo)
            {
            }

            public override DynamicMetaObject FallbackCreateInstance(DynamicMetaObject target, DynamicMetaObject[] args, DynamicMetaObject errorSuggestion)
            {
                throw new NotSupportedException("Not needed: Mimic.TryCreateInstance always succeeds without falling back.");
            }
        }

        private sealed class StubDeleteMemberBinder : DeleteMemberBinder
        {
            public StubDeleteMemberBinder(string name, bool ignoreCase) : base(name, ignoreCase)
            {
            }

            public override DynamicMetaObject FallbackDeleteMember(DynamicMetaObject target, DynamicMetaObject errorSuggestion)
            {
                throw new NotSupportedException("Not needed: Mimic.TryDeleteMember always succeeds without falling back.");
            }
        }

        private sealed class StubDeleteIndexBinder : DeleteIndexBinder
        {
            public StubDeleteIndexBinder(CallInfo callInfo) : base(callInfo)
            {
            }

            public override DynamicMetaObject FallbackDeleteIndex(DynamicMetaObject target, DynamicMetaObject[] indexes, DynamicMetaObject errorSuggestion)
            {
                throw new NotSupportedException("Not needed: Mimic.TryDeleteIndex always succeeds without falling back.");
            }
        }

        // Mimic.TryInvoke (calling the dynamic value itself, with no member name) is distinct
        // from TryInvokeMember (MimicTest's Call_Method/Call_Method_With_Parameters, which always
        // name a member) and was never exercised.
        [Test]
        public void DirectInvoke_ReturnsAMimic()
        {
            dynamic mimic = new DynamicObjects.Mimic();

            dynamic result = mimic(1, 2, 3);

            Assert.That((object)result, Is.TypeOf<DynamicObjects.Mimic>());
        }

        [Test]
        public void TryCreateInstance_SucceedsAndReturnsAMimic()
        {
            DynamicObject mimic = new DynamicObjects.Mimic();

            var succeeded = mimic.TryCreateInstance(new StubCreateInstanceBinder(new CallInfo(0)), Array.Empty<object>(), out var result);

            Assert.That(succeeded, Is.True);
            Assert.That(result, Is.TypeOf<DynamicObjects.Mimic>());
        }

        [Test]
        public void TryDeleteMember_Succeeds()
        {
            DynamicObject mimic = new DynamicObjects.Mimic();

            var succeeded = mimic.TryDeleteMember(new StubDeleteMemberBinder("Anything", false));

            Assert.That(succeeded, Is.True);
        }

        [Test]
        public void TryDeleteIndex_Succeeds()
        {
            DynamicObject mimic = new DynamicObjects.Mimic();

            var succeeded = mimic.TryDeleteIndex(new StubDeleteIndexBinder(new CallInfo(1)), new object[] { 0 });

            Assert.That(succeeded, Is.True);
        }
    }
}
