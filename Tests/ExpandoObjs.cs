using System.Dynamic;
using NUnit.Framework;
using NUnit.Framework.Legacy;

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


            ClassicAssert.AreEqual("test1", tExpandoNew.Test);
            ClassicAssert.AreEqual("Test 2nd", tExpandoNew.Test2);

            ClassicAssert.AreEqual(tExpando.Test, tExpandoNew.Test);
            ClassicAssert.AreEqual(tExpando.Test2, tExpandoNew.Test2);
            ClassicAssert.AreEqual(tExpando.GetType(), tExpandoNew.GetType());
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

            ClassicAssert.AreEqual("Rise", tExpandoNamedTestShortcut.LeftArm);
            ClassicAssert.AreEqual("Clamp", tExpandoNamedTestShortcut.RightArm);

            ClassicAssert.AreEqual(tExpandoNamedTest.LeftArm, tExpandoNamedTestShortcut.LeftArm);
            ClassicAssert.AreEqual(tExpandoNamedTest.RightArm, tExpandoNamedTestShortcut.RightArm);
            ClassicAssert.AreEqual(tExpandoNamedTest.GetType(), tExpandoNamedTestShortcut.GetType());
        }
    }
}
