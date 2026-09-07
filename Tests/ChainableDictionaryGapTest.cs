using Dynamitey.DynamicObjects;
using Microsoft.CSharp.RuntimeBinder;
using NUnit.Framework;

namespace Dynamitey.Tests
{
    /// <summary>
    /// ChainableDictionary's own TryInvokeMember override (fluent property-setter syntax,
    /// distinct from BaseDictionary's plain get/set) had no direct coverage anywhere in the
    /// suite.
    /// </summary>
    [TestFixture]
    public class ChainableDictionaryGapTest : Helper
    {
        [Test]
        public void InvokeWithMultipleArgs_StoresThemAsAList()
        {
            dynamic chain = new ChainableDictionary();

            var result = chain.Foo(1, 2, 3);

            Assert.That((object)result, Is.SameAs((object)chain));
            Assert.That((object)chain.Foo, Is.InstanceOf<DynamicObjects.List>());
        }

        [Test]
        public void InvokeWithNoArgsAndNoRealMember_Throws()
        {
            dynamic chain = new ChainableDictionary();

            Assert.Throws<RuntimeBinderException>(() => chain.Foo());
        }
    }
}
