using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Dynamitey.DynamicObjects;
using NUnit.Framework;

namespace Dynamitey.Tests
{
    /// <summary>
    /// FluentRegex and RegexMatch had no coverage at all before this file - every member below
    /// is exercised for the first time.
    /// </summary>
    [TestFixture]
    public class RegexMatchTest : Helper
    {
        [Test]
        public void Match_WhenRegexMatches_ReturnsARegexMatchWrapper()
        {
            var regex = new Regex(@"(?<num>\d+)");

            dynamic result = FluentRegex.Match("abc123", regex);

            Assert.That((object)result, Is.Not.Null);
            Assert.That((string)result.num, Is.EqualTo("123"));
        }

        [Test]
        public void Match_WhenRegexDoesNotMatch_ReturnsNull()
        {
            var regex = new Regex(@"\d+");

            dynamic result = FluentRegex.Match("abc", regex);

            Assert.That((object)result, Is.Null);
        }

        [Test]
        public void Match_ThrowsOnNullRegex()
        {
            Assert.That(() => FluentRegex.Match("abc", null!), Throws.ArgumentNullException);
        }

        [Test]
        public void FluentMatch_ExtensionMethod_BehavesLikeMatch()
        {
            var regex = new Regex(@"(?<word>\w+)");

            dynamic result = regex.FluentMatch("hello");

            Assert.That((object)result, Is.Not.Null);
            Assert.That((string)result.word, Is.EqualTo("hello"));
        }

        [Test]
        public void Matches_ReturnsAWrapperForEverySuccessfulMatch()
        {
            var regex = new Regex(@"\d+");

            var results = FluentRegex.Matches("1 and 22 and 333", regex).Cast<object>().ToList();

            Assert.That(results, Has.Count.EqualTo(3));
        }

        [Test]
        public void Matches_ThrowsOnNullRegex()
        {
            Assert.That(() => FluentRegex.Matches("abc", null!).ToList(), Throws.ArgumentNullException);
        }

        [Test]
        public void FluentMatches_ExtensionMethod_ForwardsToMatches()
        {
            var regex = new Regex(@"\d+");

            var results = regex.FluentMatches("1 and 22").Cast<object>().ToList();

            Assert.That(results, Has.Count.EqualTo(2));
        }

        [Test]
        public void FluentFilter_ReturnsOnlyMatchingStringsAsWrappers()
        {
            var regex = new Regex(@"^\d+$");
            var list = new List<string> { "123", "abc", "456" };

            var results = list.FluentFilter(regex).Cast<object>().ToList();

            Assert.That(results, Has.Count.EqualTo(2));
        }

        [Test]
        public void GetDynamicMemberNames_WithRegex_ReturnsItsGroupNames()
        {
            var regex = new Regex(@"(?<num>\d+)-(?<word>\w+)");
            var match = regex.Match("123-abc");
            var wrapper = new RegexMatch(match, regex);

            var names = wrapper.GetDynamicMemberNames();

            Assert.That(names, Does.Contain("num"));
            Assert.That(names, Does.Contain("word"));
        }

        [Test]
        public void GetDynamicMemberNames_WithoutRegex_IsEmpty()
        {
            var regex = new Regex(@"\d+");
            var match = regex.Match("123");
            var wrapper = new RegexMatch(match);

            var names = wrapper.GetDynamicMemberNames();

            Assert.That(names, Is.Empty);
        }

        [Test]
        public void TryGetMember_GroupNotMatched_DefaultsToNullForReferenceOutType()
        {
            var regex = new Regex(@"(?<num>\d+)?");
            var match = regex.Match(string.Empty);
            dynamic wrapper = new RegexMatch(match, regex);

            var result = wrapper.num;

            Assert.That((object)result, Is.Null);
        }

        [Test]
        public void TryGetMember_GroupNotMatchedWithValueTypeEquivalent_ReturnsDefaultInstance()
        {
            var regex = new Regex(@"(?<num>\d+)?");
            var match = regex.Match(string.Empty);
            var wrapper = new RegexMatch(match, regex);
            ((IEquivalentType)wrapper).EquivalentType =
                new PropretySpecType(new Dictionary<string, System.Type> { { "num", typeof(int) } });

            dynamic dyn = wrapper;
            var result = dyn.num;

            Assert.That((int)result, Is.EqualTo(0));
        }

        [Test]
        public void Indexer_ByPosition_ReturnsGroupValueWhenSuccessful()
        {
            var regex = new Regex(@"(\d+)-(\w+)");
            var match = regex.Match("123-abc");
            var wrapper = new RegexMatch(match, regex);

            Assert.That(wrapper[1], Is.EqualTo("123"));
        }

        [Test]
        public void Indexer_ByPosition_ReturnsNullWhenGroupDidNotParticipate()
        {
            var regex = new Regex(@"(?<num>\d+)?");
            var match = regex.Match(string.Empty);
            var wrapper = new RegexMatch(match, regex);

            Assert.That(wrapper[1], Is.Null);
        }

        [Test]
        public void Indexer_ByName_ReturnsGroupValueWhenSuccessful()
        {
            var regex = new Regex(@"(?<num>\d+)");
            var match = regex.Match("123");
            var wrapper = new RegexMatch(match, regex);

            Assert.That(wrapper["num"], Is.EqualTo("123"));
        }

        [Test]
        public void Indexer_ByName_ReturnsNullWhenGroupDidNotParticipate()
        {
            var regex = new Regex(@"(?<num>\d+)?");
            var match = regex.Match(string.Empty);
            var wrapper = new RegexMatch(match, regex);

            Assert.That(wrapper["num"], Is.Null);
        }

        [Test]
        public void ExplicitValue_ReturnsTheFullMatchText()
        {
            var regex = new Regex(@"\d+");
            var match = regex.Match("abc123def");
            IRegexMatch wrapper = new RegexMatch(match, regex);

            Assert.That(wrapper.Value, Is.EqualTo("123"));
        }

        [Test]
        public void ToString_ReturnsTheFullMatchText()
        {
            var regex = new Regex(@"\d+");
            var match = regex.Match("abc123def");
            var wrapper = new RegexMatch(match, regex);

            Assert.That(wrapper.ToString(), Is.EqualTo("123"));
        }
    }
}
