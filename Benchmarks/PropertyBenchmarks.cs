using System.Reflection;
using BenchmarkDotNet.Attributes;
using Dynamitey.SupportLibrary;

namespace Dynamitey.Benchmarks
{
    /// <summary>Was SpeedTest.PropPocoGetValueTimed.</summary>
    [MemoryDiagnoser]
    public class PropertyGetAnonymous
    {
        private object _target;
        private PropertyInfo _property;

        [GlobalSetup]
        public void Setup()
        {
            _target = new { TestGet = "1" };
            _property = _target.GetType().GetProperty("TestGet");
        }

        [Benchmark(Baseline = true)]
        public object Reflection() => _property.GetValue(_target, null);

        [Benchmark]
        public object Dynamitey() => Dynamic.InvokeGet(_target, "TestGet");
    }

    /// <summary>Was SpeedTest.CacheableGetValueTimed.</summary>
    [MemoryDiagnoser]
    public class PropertyGetPoco
    {
        private PropPoco _target;
        private PropertyInfo _property;
        private CacheableInvocation _cached;

        [GlobalSetup]
        public void Setup()
        {
            _target = new PropPoco { Prop1 = "1" };
            _property = _target.GetType().GetProperty("Prop1");
            _cached = new CacheableInvocation(InvocationKind.Get, "Prop1");
        }

        [Benchmark(Baseline = true)]
        public object Reflection() => _property.GetValue(_target, null);

        [Benchmark]
        public object DynamiteyCached() => _cached.Invoke(_target);
    }

    /// <summary>Was SpeedTest.SetTimed and SpeedTest.CacheableSetTimed.</summary>
    [MemoryDiagnoser]
    public class PropertySet
    {
        private const string Value = "1";

        private PropPoco _reflectionTarget;
        private PropPoco _dynamiteyTarget;
        private PropPoco _cachedTarget;
        private PropertyInfo _property;
        private CacheableInvocation _cached;

        [GlobalSetup]
        public void Setup()
        {
            _reflectionTarget = new PropPoco();
            _dynamiteyTarget = new PropPoco();
            _cachedTarget = new PropPoco();
            _property = typeof(PropPoco).GetProperty("Prop1");
            _cached = new CacheableInvocation(InvocationKind.Set, "Prop1");
        }

        [Benchmark(Baseline = true)]
        public void Reflection() => _property.SetValue(_reflectionTarget, Value, new object[] { });

        [Benchmark]
        public void Dynamitey() => Dynamic.InvokeSet(_dynamiteyTarget, "Prop1", Value);

        [Benchmark]
        public void DynamiteyCached() => _cached.Invoke(_cachedTarget, Value);
    }

    /// <summary>Was SpeedTest.SetNullTimed and SpeedTest.CacheableSetNullTimed.</summary>
    [MemoryDiagnoser]
    public class PropertySetNull
    {
        private const string Value = null;

        private PropPoco _reflectionTarget;
        private PropPoco _dynamiteyTarget;
        private PropPoco _cachedTarget;
        private PropertyInfo _property;
        private CacheableInvocation _cached;

        [GlobalSetup]
        public void Setup()
        {
            _reflectionTarget = new PropPoco();
            _dynamiteyTarget = new PropPoco();
            _cachedTarget = new PropPoco();
            _property = typeof(PropPoco).GetProperty("Prop1");
            _cached = new CacheableInvocation(InvocationKind.Set, "Prop1");
        }

        [Benchmark(Baseline = true)]
        public void Reflection() => _property.SetValue(_reflectionTarget, Value, new object[] { });

        [Benchmark]
        public void Dynamitey() => Dynamic.InvokeSet(_dynamiteyTarget, "Prop1", Value);

        [Benchmark]
        public void DynamiteyCached() => _cached.Invoke(_cachedTarget, Value);
    }
}
