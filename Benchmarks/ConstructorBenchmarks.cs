using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;

namespace Dynamitey.Benchmarks
{
    /// <summary>Was SpeedTest.ConstructorTimed and SpeedTest.CacheableConstructorTimed.</summary>
    [MemoryDiagnoser]
    public class ConstructorOneArg
    {
        private static readonly Type Target = typeof(Tuple<string>);

        private CacheableInvocation _cached;

        [GlobalSetup]
        public void Setup() => _cached = new CacheableInvocation(InvocationKind.Constructor, argCount: 1);

        [Benchmark(Baseline = true)]
        public object Activator_CreateInstance() => System.Activator.CreateInstance(Target, "Test");

        [Benchmark]
        public object Dynamitey() => Dynamic.InvokeConstructor(Target, "Test");

        [Benchmark]
        public object DynamiteyCached() => _cached.Invoke(Target, "Test");
    }

    /// <summary>
    /// Was SpeedTest.ConstructorNoARgTimed and SpeedTest.CachableConstructorNoARgTimed.
    /// Both of those carried Assert.Ignore("I don't think this is beatable at the
    /// moment") ahead of their assertion, so they never actually gated anything.
    /// </summary>
    [MemoryDiagnoser]
    public class ConstructorNoArg
    {
        private static readonly Type Target = typeof(List<string>);

        private CacheableInvocation _cached;

        [GlobalSetup]
        public void Setup() => _cached = new CacheableInvocation(InvocationKind.Constructor);

        [Benchmark(Baseline = true)]
        public object Activator_CreateInstance() => System.Activator.CreateInstance(Target);

        [Benchmark]
        public object Activator_CreateInstanceGeneric() => System.Activator.CreateInstance<List<string>>();

        [Benchmark]
        public object Dynamitey() => Dynamic.InvokeConstructor(Target);

        [Benchmark]
        public object DynamiteyCached() => _cached.Invoke(Target);
    }

    /// <summary>Was SpeedTest.ConstructorValueTypeTimed and SpeedTest.CachedConstructorValueTypeTimed.</summary>
    [MemoryDiagnoser]
    public class ConstructorValueType
    {
        private static readonly Type Target = typeof(DateTime);

        private CacheableInvocation _cached;

        [GlobalSetup]
        public void Setup() => _cached = new CacheableInvocation(InvocationKind.Constructor, argCount: 3);

        [Benchmark(Baseline = true)]
        public object Activator_CreateInstance() => System.Activator.CreateInstance(Target, 2010, 1, 20);

        [Benchmark]
        public object Dynamitey() => Dynamic.InvokeConstructor(Target, 2010, 1, 20);

        [Benchmark]
        public object DynamiteyCached() => _cached.Invoke(Target, 2010, 1, 20);
    }
}
