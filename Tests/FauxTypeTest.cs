using System;
using System.Collections.Generic;
using Dynamitey.DynamicObjects;
using Dynamitey.SupportLibrary;
using NUnit.Framework;

namespace Dynamitey.Tests
{
    /// <summary>
    /// Carried over from the former Impromptu fixture, which was removed with the
    /// ImpromptuInterface dependency in #3. This test never used ImpromptuInterface - it
    /// exercises FauxType, PropretySpecType and RealType, all of which are this library's
    /// own types - so it survives the removal unchanged.
    /// </summary>
    [TestFixture]
    public class FauxTypeTests : Helper
    {
        [Test]
        public void FauxTypeTest()
        {
            var testProp = new Dictionary<String,Type>(){
                {"test", typeof(bool)}
            };

            
            var propType = new PropretySpecType(testProp);

            var propMembers = propType.GetMemberNames();
            Assert.That(propMembers, Does.Contain("test"));

            var realType = new RealType(typeof(ISimpeleClassProps));
            var realMembers = realType.GetMemberNames();

            Assert.That(realMembers, Does.Contain("Prop2"));

            

            var aggrType = new AggreType(propType, realType);
            
            var aggrMembers = aggrType.GetMemberNames();

            Assert.That(aggrMembers, Does.Contain("Prop2"));
            Assert.That(aggrMembers, Does.Contain("test"));

        }
    }
}
