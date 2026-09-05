using System;
using System.Collections.Generic;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using Dynamitey.SupportLibrary;

namespace Dynamitey.Benchmarks
{
    /// <summary>Was SpeedTest.MethodPocoGetValueTimed and SpeedTest.CacheableMethodPocoGetValueTimed.</summary>
    [MemoryDiagnoser]
    public class MethodNoArgs
    {
        private const int Target = 1;

        private MethodInfo _method;
        private CacheableInvocation _cached;

        [GlobalSetup]
        public void Setup()
        {
            _method = Target.GetType().GetMethod("ToString", new Type[] { });
            _cached = new CacheableInvocation(InvocationKind.InvokeMember, "ToString");
        }

        [Benchmark(Baseline = true)]
        public object Reflection() => _method.Invoke(Target, new object[] { });

        [Benchmark]
        public object Dynamitey() => Dynamic.InvokeMember(Target, "ToString");

        [Benchmark]
        public object DynamiteyCached() => _cached.Invoke(Target);
    }

    /// <summary>
    /// Was SpeedTest.MethodPocoGetValuePassNullTimed and
    /// SpeedTest.CacheableMethodPocoGetValuePassNullTimed. Passing null is the
    /// interesting part: the binder has no argument type to work from.
    /// </summary>
    [MemoryDiagnoser]
    public class MethodNullArg
    {
        private OverloadingMethPoco _target;
        private MethodInfo _method;
        private CacheableInvocation _cached;

        [GlobalSetup]
        public void Setup()
        {
            _target = new OverloadingMethPoco();
            _method = _target.GetType().GetMethod("Func", new[] { typeof(object) });
            _cached = new CacheableInvocation(InvocationKind.InvokeMember, "Func", argCount: 1);
        }

        [Benchmark(Baseline = true)]
        public object Reflection() => _method.Invoke(_target, new object[] { null });

        [Benchmark]
        public object Dynamitey() => Dynamic.InvokeMember(_target, "Func", null);

        [Benchmark]
        public object DynamiteyCached() => _cached.Invoke(_target, null);
    }

    /// <summary>
    /// Was SpeedTest.MethodPocoGetValuePassNullDoubleCallTimed and its cacheable
    /// twin. Two different overloads through the same call site, which is the
    /// case that defeats a naively cached binder.
    /// </summary>
    [MemoryDiagnoser]
    public class MethodOverloadDoubleCall
    {
        private OverloadingMethPoco _target;
        private MethodInfo _objectOverload;
        private MethodInfo _intOverload;
        private CacheableInvocation _cached;

        [GlobalSetup]
        public void Setup()
        {
            _target = new OverloadingMethPoco();
            _objectOverload = _target.GetType().GetMethod("Func", new[] { typeof(object) });
            _intOverload = _target.GetType().GetMethod("Func", new[] { typeof(int) });
            _cached = new CacheableInvocation(InvocationKind.InvokeMember, "Func", 1);
        }

        [Benchmark(Baseline = true)]
        public void Reflection()
        {
            _objectOverload.Invoke(_target, new object[] { null });
            _intOverload.Invoke(_target, new object[] { 2 });
        }

        [Benchmark]
        public void Dynamitey()
        {
            Dynamic.InvokeMember(_target, "Func", null);
            Dynamic.InvokeMember(_target, "Func", 2);
        }

        [Benchmark]
        public void DynamiteyCached()
        {
            _cached.Invoke(_target, null);
            _cached.Invoke(_target, 2);
        }
    }

    /// <summary>Was SpeedTest.MethodPocoGetValue4argsTimed and its cacheable twin.</summary>
    [MemoryDiagnoser]
    public class MethodFourArgs
    {
        private const string Target = "test 123 45 string";

        private MethodInfo _method;
        private CacheableInvocation _cached;

        [GlobalSetup]
        public void Setup()
        {
            _method = Target.GetType().GetMethod(
                "IndexOf",
                new[] { typeof(string), typeof(int), typeof(int), typeof(StringComparison) });
            _cached = new CacheableInvocation(InvocationKind.InvokeMember, "IndexOf", 4);
        }

        [Benchmark(Baseline = true)]
        public object Reflection() =>
            _method.Invoke(Target, new object[] { "45", 0, 14, StringComparison.InvariantCulture });

        [Benchmark]
        public object Dynamitey() =>
            Dynamic.InvokeMember(Target, "IndexOf", "45", 0, 14, StringComparison.InvariantCulture);

        [Benchmark]
        public object DynamiteyCached() =>
            _cached.Invoke(Target, "45", 0, 14, StringComparison.InvariantCulture);
    }

    /// <summary>Was SpeedTest.MethodPocoVoidTimed and SpeedTest.CacheableMethodPocoVoidTimed.</summary>
    [MemoryDiagnoser]
    public class MethodVoid
    {
        private Dictionary<object, object> _target;
        private MethodInfo _method;
        private CacheableInvocation _cached;

        [GlobalSetup]
        public void Setup()
        {
            _target = new Dictionary<object, object>();
            _method = _target.GetType().GetMethod("Clear", new Type[] { });
            _cached = new CacheableInvocation(InvocationKind.InvokeMemberAction, "Clear");
        }

        [Benchmark(Baseline = true)]
        public void Reflection() => _method.Invoke(_target, new object[] { });

        [Benchmark]
        public void Dynamitey() => Dynamic.InvokeMemberAction(_target, "Clear");

        [Benchmark]
        public void DynamiteyCached() => _cached.Invoke(_target);
    }
}
