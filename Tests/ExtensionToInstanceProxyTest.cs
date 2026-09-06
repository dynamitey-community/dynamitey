using System;
using Dynamitey.DynamicObjects;
using NUnit.Framework;

namespace Dynamitey.Tests
{
    public interface IExtProxyTestFoo { }

    public class ExtProxyTestFooImpl : IExtProxyTestFoo { }

    public static class ExtProxyTestFooExtensions
    {
        public static string Bar(this IExtProxyTestFoo f) => "bar";
        public static string BarNull(this IExtProxyTestFoo f) => null;
    }

    // Issue #42 gaps 2 and 3: ExtensionToInstanceProxy dereferenced possibly-null values that
    // the nullable pass (#29) suppressed with `!` rather than fixed.
    [TestFixture]
    public class ExtensionToInstanceProxyTest : Helper
    {
        // Gap 2: the Invoker nested type dereferenced parent.InstanceHints unconditionally when
        // resolving a member by type (needed for generic overload selection), but InstanceHints is
        // null whenever the proxy was constructed without instanceHints - the constructor's own
        // default. Direct invocation (proxy.Bar()) never hits this: it goes straight through
        // TryInvokeMember. Only a member access without immediate invocation reaches TryGetMember
        // and constructs an Invoker. Since a proxy built this way has nothing to reflect over for
        // overload resolution, this is now a clear, documented failure instead of an NRE.
        [Test]
        public void ProxyWithoutInstanceHints_MemberAccessWithoutInvoke_ThrowsInvalidOperation()
        {
            dynamic proxy = new ExtensionToInstanceProxy(new ExtProxyTestFooImpl(), typeof(IExtProxyTestFoo),
                new[] { typeof(ExtProxyTestFooExtensions) });

            Assert.That(() =>
            {
                dynamic bar = proxy.Bar;
                return bar;
            }, Throws.InstanceOf<InvalidOperationException>());
        }

        // Direct invocation is unaffected by gap 2's fix - it never reaches the Invoker path.
        [Test]
        public void ProxyWithoutInstanceHints_DirectInvoke_StillWorks()
        {
            dynamic proxy = new ExtensionToInstanceProxy(new ExtProxyTestFooImpl(), typeof(IExtProxyTestFoo),
                new[] { typeof(ExtProxyTestFooExtensions) });

            Assert.That((string)proxy.Bar(), Is.EqualTo("bar"));
        }

        // Gap 3: InvokeStaticMethod dereferenced its dynamic result (via IsExtendedType/CreateSelf)
        // without checking for null, but a wrapped extension method returning null (as any
        // nullable-returning method can) makes that dereference reachable. A null result is never
        // meaningful to wrap in a self-referential proxy, so it should simply pass through as null.
        [Test]
        public void ProxyInvokingExtensionMethodThatReturnsNull_ReturnsNullInsteadOfThrowing()
        {
            dynamic proxy = new ExtensionToInstanceProxy(new ExtProxyTestFooImpl(), typeof(IExtProxyTestFoo),
                new[] { typeof(ExtProxyTestFooExtensions) });

            Assert.That((string)proxy.BarNull(), Is.Null);
        }
    }
}
