using NUnit.Framework;
using System;
using System.Dynamic;
using System.Reflection;

namespace Dynamitey.Tests
{
    /// <summary>
    /// Dynamic.IsDBNull's success path is hit by conversion tests. These cover the catch:
    /// a binder failure returns false, and any other exception propagates.
    /// </summary>
    [TestFixture]
    public class IsDBNullGapTest : Helper
    {
        private static readonly FieldInfo LateConvert = typeof(Dynamic).GetField(
            "_lateConvert",
            BindingFlags.Static | BindingFlags.NonPublic)!;

        [Test]
        public void RealConvert_RecognizesDbNull()
        {
            Assert.That(Dynamic.IsDBNull(DBNull.Value), Is.True);
            Assert.That(Dynamic.IsDBNull(new object()), Is.False);
        }

        [Test]
        public void BinderFailure_ReturnsFalse()
        {
            WithLateConvert(new MissingMember(), () =>
            {
                Assert.That(Dynamic.IsDBNull(new object()), Is.False);
            });
        }

        [Test]
        public void OtherException_Propagates()
        {
            WithLateConvert(new ThrowsFromMember(), () =>
            {
                var ex = Assert.Throws<InvalidOperationException>(() => Dynamic.IsDBNull(new object()));
                Assert.That(ex!.Message, Is.EqualTo("probe failed"));
            });
        }

        private static void WithLateConvert(object standIn, Action body)
        {
            var prior = LateConvert.GetValue(null);
            try
            {
                LateConvert.SetValue(null, standIn);
                body();
            }
            finally
            {
                LateConvert.SetValue(null, prior);
            }
        }

        private sealed class MissingMember : DynamicObject
        {
        }

        private sealed class ThrowsFromMember : DynamicObject
        {
            public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
            {
                result = null;
                throw new InvalidOperationException("probe failed");
            }
        }
    }
}
