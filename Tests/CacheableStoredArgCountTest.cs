// Issue #104. CacheableInvocation set _argCount from storedArgs.Length, then the
// Kind switch overwrote it from the argCount parameter (default 0) and from
// argument-name count. Positional stored arguments then failed at
// InvokeWithStoredArgs, and GetIndex/SetIndex validated the parameter rather
// than the stored length. Named stored InvokeArgs still worked because they
// populated _argNames. Types here exist only for this fixture.
using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Dynamitey.Tests
{
    public class CacheableStoredArgCountTest : Helper
    {
        [Test]
        public void PositionalStoredArgsInvokeMemberWithoutArgCount()
        {
            var tInvocation = new CacheableInvocation(InvocationKind.InvokeMember, "Join",
                storedArgs: new object[] { "a", "b" });

            Assert.That(tInvocation.InvokeWithStoredArgs(new Issue104Target()), Is.EqualTo("ab"));
        }

        [Test]
        public void PositionalStoredArgsInvokeDelegateWithoutArgCount()
        {
            Func<string, string, string> tJoin = (a, b) => a + b;
            var tInvocation = new CacheableInvocation(InvocationKind.Invoke,
                storedArgs: new object[] { "a", "b" });

            Assert.That(tInvocation.InvokeWithStoredArgs(tJoin), Is.EqualTo("ab"));
        }

        [Test]
        public void PositionalStoredArgsConstructorWithoutArgCount()
        {
            var tInvocation = new CacheableInvocation(InvocationKind.Constructor,
                storedArgs: new object[] { 2009, 1, 20 });

            var tResult = (DateTime)tInvocation.InvokeWithStoredArgs(typeof(DateTime));

            Assert.That(tResult.Day, Is.EqualTo(20));
        }

        [Test]
        public void PositionalStoredArgsGetIndexWithoutArgCount()
        {
            var tList = new List<string> { "x", "y" };
            var tInvocation = new CacheableInvocation(InvocationKind.GetIndex,
                storedArgs: new object[] { 1 });

            Assert.That(tInvocation.InvokeWithStoredArgs(tList), Is.EqualTo("y"));
        }

        [Test]
        public void PositionalStoredArgsSetIndexWithoutArgCount()
        {
            var tList = new List<string> { "x", "y" };
            var tInvocation = new CacheableInvocation(InvocationKind.SetIndex,
                storedArgs: new object[] { 0, "z" });

            tInvocation.InvokeWithStoredArgs(tList);

            Assert.That(tList[0], Is.EqualTo("z"));
        }

        [Test]
        public void NamedStoredArgsStillBindByName()
        {
            var tInvocation = new CacheableInvocation(InvocationKind.InvokeMember, "Join",
                storedArgs: new object[] { InvokeArg.Create("b", "B"), InvokeArg.Create("a", "A") });

            Assert.That(tInvocation.InvokeWithStoredArgs(new Issue104Target()), Is.EqualTo("AB"));
        }

        [Test]
        public void MatchingExplicitArgCountAndStoredArgsStillWorks()
        {
            var tInvocation = new CacheableInvocation(InvocationKind.InvokeMember, "Join",
                argCount: 2, storedArgs: new object[] { "a", "b" });

            Assert.That(tInvocation.InvokeWithStoredArgs(new Issue104Target()), Is.EqualTo("ab"));
        }

        [Test]
        public void ConflictingExplicitArgCountAndStoredArgsThrows()
        {
            Assert.That(
                () => new CacheableInvocation(InvocationKind.InvokeMember, "Join",
                    argCount: 3, storedArgs: new object[] { "a", "b" }),
                Throws.ArgumentException);
        }

        [Test]
        public void WrongInvokeArgCountStillThrows()
        {
            var tInvocation = new CacheableInvocation(InvocationKind.InvokeMember, "Join",
                storedArgs: new object[] { "a", "b" });

            Assert.That(() => tInvocation.Invoke(new Issue104Target(), "only-one"),
                Throws.ArgumentException);
        }

        [Test]
        public void GetIndexWithoutStoredArgsStillRequiresAtLeastOne()
        {
            Assert.That(() => new CacheableInvocation(InvocationKind.GetIndex, argCount: 0),
                Throws.ArgumentException);
        }
    }

    public class Issue104Target
    {
        public string Join(string a, string b) => a + b;
    }
}
