// Dynamic.CoerceConvert has a large branch tree the rest of the suite barely
// touches: the interface/ActLike path (with its Dictionary-vs-Get wrapping
// choice), the DLR-explicit-convert-fails fallback chain (Nullable<T>
// unwrapping, Enum.Parse, IConvertible/Convert.ChangeType, and the
// TypeDescriptor "hail mary"), and the null/DBNull short-circuits at the end
// of the method. These tests walk each branch directly.
using System;
using System.Collections.Generic;
using Dynamitey.SupportLibrary;
using NUnit.Framework;

namespace Dynamitey.Tests
{
    public class CoerceConvertTest : Helper
    {
        [Test]
        public void TestCoerceConvertWrapsPlainObjectInGetForInterfaceTarget()
        {
            var tResult = (ISimpleStringMethod)Dynamic.CoerceConvert("hello", typeof(ISimpleStringMethod))!;

            Assert.That(tResult.StartsWith("he"), Is.True);
        }

        [Test]
        public void TestCoerceConvertWrapsDictionaryForInterfaceTarget()
        {
            var tDict = new Dictionary<string, object> { { "Foo", "Bar" } };

            var tResult = (IObjectStringIndexer)Dynamic.CoerceConvert(tDict, typeof(IObjectStringIndexer))!;

            Assert.That(tResult["Foo"], Is.EqualTo("Bar"));

            tResult["Baz"] = "Qux";
            Assert.That(tDict["Baz"], Is.EqualTo("Qux"));
        }

        [Test]
        public void TestCoerceConvertDlrExplicitConvertSucceedsDirectly()
        {
            var tResult = Dynamic.CoerceConvert(5, typeof(long));

            Assert.That(tResult, Is.EqualTo(5L));
        }

        [Test]
        public void TestCoerceConvertEnumParseFallback()
        {
            var tResult = Dynamic.CoerceConvert("Monday", typeof(DayOfWeek));

            Assert.That(tResult, Is.EqualTo(DayOfWeek.Monday));
        }

        [Test]
        public void TestCoerceConvertNullableEnumParseFallback()
        {
            var tResult = Dynamic.CoerceConvert("Tuesday", typeof(DayOfWeek?));

            Assert.That(tResult, Is.EqualTo(DayOfWeek.Tuesday));
        }

        [Test]
        public void TestCoerceConvertIConvertibleFallback()
        {
            var tResult = Dynamic.CoerceConvert(5, typeof(string));

            Assert.That(tResult, Is.EqualTo("5"));
        }

        private class UnconvertibleSource { }
        private class UnrelatedDestination { }

        [Test]
        public void TestCoerceConvertTypeDescriptorHailMaryLeavesTargetUnchangedWhenNoConverterApplies()
        {
            var tSource = new UnconvertibleSource();

            var tResult = Dynamic.CoerceConvert(tSource, typeof(UnrelatedDestination));

            // None of the fallbacks apply (not an enum/string pair, not IConvertible), so the
            // TypeDescriptor hail-mary runs, finds no applicable converter, and the original
            // target is returned unchanged.
            Assert.That(ReferenceEquals(tResult, tSource), Is.True);
        }

        [Test]
        public void TestCoerceConvertNullValueTypeConstructsDefault()
        {
            var tResult = Dynamic.CoerceConvert(null, typeof(int));

            Assert.That(tResult, Is.EqualTo(0));
        }

        [Test]
        public void TestCoerceConvertNullReferenceTypeReturnsNull()
        {
            object tResult = Dynamic.CoerceConvert(null, typeof(string));

            Assert.That(tResult, Is.Null);
        }

        [Test]
        public void TestCoerceConvertDbNullMismatchReturnsNull()
        {
            object tResult = Dynamic.CoerceConvert(DBNull.Value, typeof(string));

            Assert.That(tResult, Is.Null);
        }
    }
}
