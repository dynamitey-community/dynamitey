using System.Dynamic;
using NUnit.Framework;

namespace Dynamitey.Tests
{
    [TestFixture]
    public class ExpandoObjs : Helper
    {
        [Test]
        public void TestExpando()
        {
            var New = Builder.New<ExpandoObject>();

            var tExpando = New.Object(
                Test: "test1",
                Test2: "Test 2nd"
                );            

            var tExpandoNew = Expando.New(
                Test: "test1",
                Test2: "Test 2nd"
                );


            Assert.That(tExpandoNew.Test, Is.EqualTo("test1"));
            Assert.That(tExpandoNew.Test2, Is.EqualTo("Test 2nd"));

            Assert.That(tExpandoNew.Test, Is.EqualTo(tExpando.Test));
            Assert.That(tExpandoNew.Test2, Is.EqualTo(tExpando.Test2));
            Assert.That(tExpandoNew.GetType(), Is.EqualTo(tExpando.GetType()));
        }


        [Test]
        public void TestExpando2()
        {            
            dynamic NewD = new DynamicObjects.Builder<ExpandoObject>();

            var tExpandoNamedTest = NewD.Robot(
                LeftArm: "Rise",
                RightArm: "Clamp"
                );

            dynamic NewE = new Expando();

            var tExpandoNamedTestShortcut = NewE.Robot(
               LeftArm: "Rise",
               RightArm: "Clamp"
               );

            Assert.That(tExpandoNamedTestShortcut.LeftArm, Is.EqualTo("Rise"));
            Assert.That(tExpandoNamedTestShortcut.RightArm, Is.EqualTo("Clamp"));

            Assert.That(tExpandoNamedTestShortcut.LeftArm, Is.EqualTo(tExpandoNamedTest.LeftArm));
            Assert.That(tExpandoNamedTestShortcut.RightArm, Is.EqualTo(tExpandoNamedTest.RightArm));
            Assert.That(tExpandoNamedTestShortcut.GetType(), Is.EqualTo(tExpandoNamedTest.GetType()));
        }
    }
}
