using System;
using System.Reflection;
using BenchmarkDotNet.Attributes;

namespace Dynamitey.Benchmarks
{
    /// <summary>Was SpeedTest.GetStaticTimed and SpeedTest.CacheableGetStaticTimed.</summary>
    [MemoryDiagnoser]
    public class StaticPropertyGet
    {
        private static readonly Type Target = typeof(DateTime);

        private object _context;
        private MethodInfo _getter;
        private CacheableInvocation _cached;

        [GlobalSetup]
        public void Setup()
        {
            _context = InvokeContext.CreateStatic(Target);
            _getter = Target.GetProperty("Today").GetGetMethod();
            _cached = new CacheableInvocation(InvocationKind.Get, "Today", context: _context);
        }

        [Benchmark(Baseline = true)]
        public object Reflection() => _getter.Invoke(Target, new object[] { });

        [Benchmark]
        public object Dynamitey() => Dynamic.InvokeGet(_context, "Today");

        [Benchmark]
        public object DynamiteyCached() => _cached.Invoke(Target);
    }

    /// <summary>
    /// A public top-level type with a public static settable property, purely
    /// so <see cref="StaticPropertySet"/> below has something to benchmark
    /// against - <c>DateTime</c> has nothing static and public that's
    /// settable. Static so each run gets a fresh binder history for its
    /// accessors (see issue #31).
    /// </summary>
    public static class BenchmarkStaticHolder
    {
        public static int Value { get; set; }
    }

    /// <summary>Issue #31: measures the cost of the static-context SET path,
    /// for comparison against reflection when deciding whether to keep the
    /// DLR accessor-method trick or replace it with reflection unconditionally.</summary>
    [MemoryDiagnoser]
    public class StaticPropertySet
    {
        private static readonly Type Target = typeof(BenchmarkStaticHolder);

        private object _context;
        private PropertyInfo _property;
        private CacheableInvocation _cached;

        [GlobalSetup]
        public void Setup()
        {
            _context = InvokeContext.CreateStatic(Target);
            _property = Target.GetProperty("Value");
            _cached = new CacheableInvocation(InvocationKind.Set, "Value", context: _context);
        }

        [Benchmark(Baseline = true)]
        public void Reflection() => _property.SetValue(null, 42);

        [Benchmark]
        public void Dynamitey() => Dynamic.InvokeSet(_context, "Value", 42);

        [Benchmark]
        public void DynamiteyCached() => _cached.Invoke(Target, 42);
    }

    /// <summary>Was SpeedTest.MethodStaticMethodValueTimed and its cacheable twin.</summary>
    [MemoryDiagnoser]
    public class StaticMethodInvoke
    {
        private const string Date = "01/20/2009";
        private static readonly Type Target = typeof(DateTime);

        private object _context;
        private MethodInfo _method;
        private CacheableInvocation _cached;

        [GlobalSetup]
        public void Setup()
        {
            _context = InvokeContext.CreateStatic(Target);
            _method = Target.GetMethod("Parse", new[] { typeof(string) });
            _cached = new CacheableInvocation(
                InvocationKind.InvokeMember, "Parse", argCount: 1, context: _context);
        }

        [Benchmark(Baseline = true)]
        public object Reflection() => _method.Invoke(Target, new object[] { Date });

        [Benchmark]
        public object Dynamitey() => Dynamic.InvokeMember(_context, "Parse", Date);

        [Benchmark]
        public object DynamiteyCached() => _cached.Invoke(Target, Date);
    }
}
