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
