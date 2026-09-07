using System;
using Dynamitey.DynamicObjects;
using NUnit.Framework;

namespace Dynamitey.Tests
{
    /// <summary>
    /// Every existing LateType test (Tests/DynamicObjects.cs) resolves a real type - none cover
    /// what happens when the name doesn't resolve at all, which is the only reason
    /// MissingTypeException exists.
    /// </summary>
    [TestFixture]
    public class LateTypeGapTest : Helper
    {
        [Test]
        public void ConstructingWithAnUnresolvableTypeName_ThrowsOnFirstUse()
        {
            dynamic tLate = new LateType("Dynamitey.Tests.NoSuchTypeAtAll, Dynamitey.Tests");

            var ex = Assert.Throws<LateType.MissingTypeException>(() =>
            {
                _ = tLate.AnyMember;
            });

            Assert.That(ex.Message, Does.Contain("Dynamitey.Tests.NoSuchTypeAtAll"));
        }

        // LateType.FindType's documented contract is to return null for any resolution
        // failure, not just "not found" - Type.GetType(typeName, false) still throws for some
        // malformed names (here, an assembly-qualified name with too many comma-separated
        // components raises FileLoadException) even though throwOnError only suppresses the
        // plain "not found" case. That's what FindType's catch-all exists to convert to null.
        [Test]
        public void FindType_WithMalformedAssemblyQualifiedName_ReturnsNullInsteadOfThrowing()
        {
            var result = LateType.FindType("A, B, C, D, E, F");

            Assert.That(result, Is.Null);
        }

        // MissingTypeException's (message, innerException) constructor is public API
        // (PublicAPI.Unshipped.txt) but nothing in the library actually calls it - LateType
        // itself only ever uses the (typename) overload. Tested directly since it would
        // otherwise have no coverage at all.
        [Test]
        public void MessageAndInnerExceptionConstructor_SetsBothProperties()
        {
            var inner = new InvalidOperationException("inner");

            var ex = new LateType.MissingTypeException("custom message", inner);

            Assert.That(ex.Message, Is.EqualTo("custom message"));
            Assert.That(ex.InnerException, Is.SameAs(inner));
        }
    }
}
