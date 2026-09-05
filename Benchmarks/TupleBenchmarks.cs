using System;
using System.Linq;
using BenchmarkDotNet.Attributes;
using Microsoft.FSharp.Reflection;

namespace Dynamitey.Benchmarks
{
    /// <summary>
    /// The tuple comparisons measure Dynamitey's Tupler against F# reflection,
    /// which is the other way to take a tuple apart at runtime. All four use a
    /// 20-element tuple, which is nested past the 7-element boundary and so
    /// exercises the TRest chain rather than a flat lookup.
    /// </summary>
    public static class TupleFixture
    {
        public static object Create() =>
            Tupler.Create(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20);

        public static object[] Values() =>
            new object[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 };
    }

    /// <summary>Was SpeedTest.IsTupleTimed.</summary>
    [MemoryDiagnoser]
    public class TupleIsTuple
    {
        private object _tuple;

        [GlobalSetup]
        public void Setup() => _tuple = TupleFixture.Create();

        [Benchmark(Baseline = true)]
        public bool FSharpReflection() => FSharpType.IsTuple(_tuple.GetType());

        [Benchmark]
        public bool Dynamitey() => Tupler.IsTuple(_tuple);
    }

    /// <summary>Was SpeedTest.TupleIndexTimed.</summary>
    [MemoryDiagnoser]
    public class TupleIndex
    {
        private object _tuple;

        [GlobalSetup]
        public void Setup() => _tuple = TupleFixture.Create();

        [Benchmark(Baseline = true)]
        public object FSharpReflection() => FSharpValue.GetTupleField(_tuple, 14);

        [Benchmark]
        public object Dynamitey() => Tupler.Index(_tuple, 14);
    }

    /// <summary>Was SpeedTest.TupleToListTimed.</summary>
    [MemoryDiagnoser]
    public class TupleToList
    {
        private object _tuple;

        [GlobalSetup]
        public void Setup() => _tuple = TupleFixture.Create();

        [Benchmark(Baseline = true)]
        public object FSharpReflection() => FSharpValue.GetTupleFields(_tuple).ToList();

        [Benchmark]
        public object Dynamitey() => Tupler.ToList(_tuple);
    }

    /// <summary>Was SpeedTest.ListToTupleTimed.</summary>
    [MemoryDiagnoser]
    public class ListToTuple
    {
        private object[] _values;

        [GlobalSetup]
        public void Setup() => _values = TupleFixture.Values();

        [Benchmark(Baseline = true)]
        public object FSharpReflection()
        {
            var types = _values.Select(it => it.GetType()).ToArray();
            var tupleType = FSharpType.MakeTupleType(types);
            return FSharpValue.MakeTuple(_values, tupleType);
        }

        [Benchmark]
        public object Dynamitey() => Tupler.ToTuple(_values);
    }
}
