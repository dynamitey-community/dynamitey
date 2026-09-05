using System;
using BenchmarkDotNet.Attributes;

namespace Dynamitey.Benchmarks
{
    /// <summary>Was SpeedTest.FastDynamicInvoke.</summary>
    [MemoryDiagnoser]
    public class DelegateInvokeFunc
    {
        private Func<int, bool> _func;

        [GlobalSetup]
        public void Setup() => _func = it => it > 10;

        [Benchmark(Baseline = true)]
        public object DynamicInvoke() => _func.DynamicInvoke(5);

        [Benchmark]
        public object FastDynamicInvoke() => _func.FastDynamicInvoke(5);
    }

    /// <summary>Was SpeedTest.FastDynamicInvokeAction.</summary>
    [MemoryDiagnoser]
    public class DelegateInvokeAction
    {
        private Action<int> _action;

        [GlobalSetup]
        public void Setup() => _action = it => it.ToString();

        [Benchmark(Baseline = true)]
        public object DynamicInvoke() => _action.DynamicInvoke(5);

        [Benchmark]
        public object FastDynamicInvoke() => _action.FastDynamicInvoke(5);
    }
}
