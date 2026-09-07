using System;
using System.Collections.Generic;
using Dynamitey.DynamicObjects;
using Microsoft.CSharp.RuntimeBinder;
using NUnit.Framework;

namespace Dynamitey.Tests
{
    /// <summary>
    /// Covers BaseDictionary members that the rest of the suite reaches only through Dictionary/
    /// List's own tests, or not at all: the ICollection&lt;KeyValuePair&gt;-shaped Add/Contains
    /// overloads, IsReadOnly/Values/ToString, TryGetMember's missing-key branch, and
    /// TryInvokeMember's non-fast-path branch (named arguments, taken regardless of whether the
    /// stored value is a delegate) including its own RuntimeBinderException fallback.
    /// </summary>
    [TestFixture]
    public class BaseDictionaryGapTest : Helper
    {
        [Test]
        public void IsReadOnly_IsAlwaysFalse()
        {
            var dict = new DynamicObjects.Dictionary();

            Assert.That(dict.IsReadOnly, Is.False);
        }

        [Test]
        public void Values_ReflectsStoredValues()
        {
            var dict = new DynamicObjects.Dictionary();
            ((IDictionary<string, object>)dict).Add("A", 1);

            Assert.That(dict.Values, Does.Contain(1));
        }

        [Test]
        public void ToString_DelegatesToTheBackingDictionary()
        {
            var backing = new Dictionary<string, object>();
            var dict = new DynamicObjects.Dictionary(backing);

            Assert.That(dict.ToString(), Is.EqualTo(backing.ToString()));
        }

        [Test]
        public void Add_KeyValuePairOverload_StoresTheValue()
        {
            ICollection<KeyValuePair<string, object>> dict = new DynamicObjects.Dictionary();

            dict.Add(new KeyValuePair<string, object>("A", 1));

            Assert.That(((IDictionary<string, object>)dict).TryGetValue("A", out var value), Is.True);
            Assert.That(value, Is.EqualTo(1));
        }

        [Test]
        public void Add_KeyValueOverload_StoresTheValue()
        {
            IDictionary<string, object> dict = new DynamicObjects.Dictionary();

            dict.Add("A", 1);

            Assert.That(dict.TryGetValue("A", out var value), Is.True);
            Assert.That(value, Is.EqualTo(1));
        }

        [Test]
        public void Contains_KeyValuePairOverload_TrueWhenPresent()
        {
            ICollection<KeyValuePair<string, object>> dict = new DynamicObjects.Dictionary();
            dict.Add(new KeyValuePair<string, object>("A", 1));

            Assert.That(dict.Contains(new KeyValuePair<string, object>("A", 1)), Is.True);
        }

        [Test]
        public void Contains_KeyValuePairOverload_FalseWhenAbsent()
        {
            ICollection<KeyValuePair<string, object>> dict = new DynamicObjects.Dictionary();

            Assert.That(dict.Contains(new KeyValuePair<string, object>("A", 1)), Is.False);
        }

        [Test]
        public void TryGetMember_MissingKeyWithoutEquivalentType_Throws()
        {
            dynamic dict = new DynamicObjects.Dictionary();

            Assert.Throws<RuntimeBinderException>(() =>
            {
                _ = dict.NoSuchKey;
            });
        }

        [Test]
        public void TryInvokeMember_NamedArguments_InvokesStoredDelegateByName()
        {
            dynamic dict = new DynamicObjects.Dictionary();
            dict.Adder = new Func<int, int, int>((a, b) => a + b);

            // Func<int,int,int>.Invoke's own parameters are named arg1/arg2 (the compiler's
            // names for a Func<> delegate type), not the lambda's a/b - named-argument
            // invocation resolves against the delegate's Invoke signature.
            var result = dict.Adder(arg1: 2, arg2: 3);

            Assert.That((int)result, Is.EqualTo(5));
        }

        [Test]
        public void TryInvokeMember_NamedArguments_MismatchedArgsThrows()
        {
            dynamic dict = new DynamicObjects.Dictionary();
            dict.Adder = new Func<int, int, int>((a, b) => a + b);

            Assert.Throws<RuntimeBinderException>(() =>
            {
                _ = dict.Adder(arg1: 2);
            });
        }
    }
}
