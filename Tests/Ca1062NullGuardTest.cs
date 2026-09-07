using System;
using System.Dynamic;
using NUnit.Framework;

namespace Dynamitey.Tests
{
    // CA1062 batch (see the csproj backlog comment): a reflection probe found 34 public
    // methods that threw NullReferenceException on a null argument they never checked.
    // Guarding those turns the same failure into an ArgumentNullException naming the
    // offending parameter - a pure diagnostic improvement, not a behaviour change. This
    // fixture is a representative sample of that guard work, not exhaustive coverage of
    // every guarded member; each case here was also proven load-bearing by removing the
    // guard in an isolated worktree and confirming the test fails.
    [TestFixture]
    public class Ca1062NullGuardTest : Helper
    {
        [Test]
        public void GetMemberNames_NullTarget_ThrowsArgumentNullException()
        {
            Assert.That(() => Dynamic.GetMemberNames(null!),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("target"));
        }

        [Test]
        public void Linq_NullEnumerable_ThrowsArgumentNullException()
        {
            Assert.That(() => Dynamic.Linq(null!),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("enumerable"));
        }

        [Test]
        public void FluentRegex_Matches_NullRegex_ThrowsArgumentNullException()
        {
            Assert.That(() => FluentRegex.Matches("abc", null!),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("regex"));
        }

        [Test]
        public void InvokeSetIndex_NullIndexesThenValue_ThrowsArgumentNullException()
        {
            Assert.That(() => Dynamic.InvokeSetIndex(new ExpandoObject(), (object[])null!),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("indexesThenValue"));
        }

        [Test]
        public void AggreType_MakeTypeAppendable_NullType_ThrowsArgumentNullException()
        {
            Assert.That(() => DynamicObjects.AggreType.MakeTypeAppendable(null!),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("type"));
        }
    }
}
