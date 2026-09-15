// Issue #99. Three process-wide maps were unlocked Dictionary instances with
// check-then-write. Parallel first-use of the same or distinct keys could
// throw during rehash or drop an entry. ClearCaches now empties them so this
// fixture can start cold. NonParallelizable: ClearCaches must not race the rest
// of the suite.
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using NUnit.Framework;

namespace Dynamitey.Tests
{
    [NonParallelizable]
    public class ConcurrentCallSiteCacheTest : Helper
    {
        [SetUp]
        public void ColdCache()
        {
            Dynamic.ClearCaches();
        }

        [Test]
        public void ParallelValueTypeConstructorsSameKeyDoNotThrow()
        {
            var tExpected = new DateTime(2009, 1, 20);
            var tResults = new object[32];
            var tErrors = RunContended(32, i =>
                tResults[i] = Dynamic.InvokeConstructor(typeof(DateTime), 2009, 1, 20));

            Assert.That(tErrors, Is.Empty);
            Assert.That(tResults, Is.All.EqualTo(tExpected));
        }

        [Test]
        public void ParallelValueTypeConstructorsDistinctKeysDoNotThrow()
        {
            var tGuid = new Guid("00112233-4455-6677-8899-aabbccddeeff");
            var tCases = new[]
            {
                (Type: typeof(DateTime), Args: new object[] { 2009, 1, 20 }, Expected: (object)new DateTime(2009, 1, 20)),
                (Type: typeof(TimeSpan), Args: new object[] { 1, 2, 3 }, Expected: (object)new TimeSpan(1, 2, 3)),
                (Type: typeof(Guid), Args: new object[] { tGuid.ToString() }, Expected: (object)tGuid),
                (Type: typeof(decimal), Args: new object[] { 42 }, Expected: (object)42m)
            };
            var tResults = new object[tCases.Length * 8];
            var tErrors = RunContended(tResults.Length, i =>
            {
                var tCase = tCases[i % tCases.Length];
                tResults[i] = Dynamic.InvokeConstructor(tCase.Type, tCase.Args);
                return tResults[i];
            });

            Assert.That(tErrors, Is.Empty);
            for (var i = 0; i < tResults.Length; i++)
            {
                Assert.That(tResults[i], Is.EqualTo(tCases[i % tCases.Length].Expected));
            }
        }

        [Test]
        public void ParallelCoerceToDelegateSameKeyDoNotThrow()
        {
            Func<object, object> tSource = x => (int)x + 1;
            var tResults = new Func<int, int>[32];
            var tErrors = RunContended(32, i =>
            {
                tResults[i] = (Func<int, int>)Dynamic.CoerceToDelegate(tSource, typeof(Func<int, int>))!;
                return tResults[i](3);
            });

            Assert.That(tErrors, Is.Empty);
            Assert.That(tResults.Select(d => d(10)), Is.All.EqualTo(11));
        }

        [Test]
        public void ParallelCoerceToDelegateDistinctKeysDoNotThrow()
        {
            var tDelegateTypes = new[]
            {
                typeof(Func<int, int>),
                typeof(Func<long, long>),
                typeof(Func<byte, byte>),
                typeof(Func<short, short>),
                typeof(Func<uint, uint>),
                typeof(Func<decimal, decimal>),
                typeof(Func<bool, bool>),
                typeof(Func<char, char>),
                typeof(Func<float, float>),
                typeof(Func<double, double>),
                typeof(Func<Guid, Guid>),
                typeof(Func<DateTime, DateTime>)
            };
            Func<object, object> tSource = x => x;
            var tDelegates = new object[tDelegateTypes.Length * 4];
            var tErrors = RunContended(tDelegates.Length, i =>
            {
                var tType = tDelegateTypes[i % tDelegateTypes.Length];
                tDelegates[i] = Dynamic.CoerceToDelegate(tSource, tType)!;
                return InvokeCoerced(tDelegates[i], tType);
            });

            Assert.That(tErrors, Is.Empty);
            for (var i = 0; i < tDelegates.Length; i++)
            {
                var tType = tDelegateTypes[i % tDelegateTypes.Length];
                Assert.That(InvokeCoerced(tDelegates[i], tType), Is.EqualTo(SampleFor(tType)));
            }
        }

        private static object InvokeCoerced(object del, Type delegateType)
        {
            if (delegateType == typeof(Func<int, int>)) return ((Func<int, int>)del)(7);
            if (delegateType == typeof(Func<long, long>)) return ((Func<long, long>)del)(7L);
            if (delegateType == typeof(Func<byte, byte>)) return ((Func<byte, byte>)del)((byte)7);
            if (delegateType == typeof(Func<short, short>)) return ((Func<short, short>)del)((short)7);
            if (delegateType == typeof(Func<uint, uint>)) return ((Func<uint, uint>)del)(7u);
            if (delegateType == typeof(Func<decimal, decimal>)) return ((Func<decimal, decimal>)del)(7m);
            if (delegateType == typeof(Func<bool, bool>)) return ((Func<bool, bool>)del)(true);
            if (delegateType == typeof(Func<char, char>)) return ((Func<char, char>)del)('x');
            if (delegateType == typeof(Func<float, float>)) return ((Func<float, float>)del)(7f);
            if (delegateType == typeof(Func<double, double>)) return ((Func<double, double>)del)(7d);
            if (delegateType == typeof(Func<Guid, Guid>)) return ((Func<Guid, Guid>)del)(Guid.Empty);
            if (delegateType == typeof(Func<DateTime, DateTime>)) return ((Func<DateTime, DateTime>)del)(new DateTime(2009, 1, 20));
            throw new ArgumentException(delegateType.ToString());
        }

        private static object SampleFor(Type delegateType)
        {
            if (delegateType == typeof(Func<int, int>)) return 7;
            if (delegateType == typeof(Func<long, long>)) return 7L;
            if (delegateType == typeof(Func<byte, byte>)) return (byte)7;
            if (delegateType == typeof(Func<short, short>)) return (short)7;
            if (delegateType == typeof(Func<uint, uint>)) return 7u;
            if (delegateType == typeof(Func<decimal, decimal>)) return 7m;
            if (delegateType == typeof(Func<bool, bool>)) return true;
            if (delegateType == typeof(Func<char, char>)) return 'x';
            if (delegateType == typeof(Func<float, float>)) return 7f;
            if (delegateType == typeof(Func<double, double>)) return 7d;
            if (delegateType == typeof(Func<Guid, Guid>)) return Guid.Empty;
            if (delegateType == typeof(Func<DateTime, DateTime>)) return new DateTime(2009, 1, 20);
            throw new ArgumentException(delegateType.ToString());
        }

        private static ConcurrentBag<Exception> RunContended(int workers, Func<int, object> work)
        {
            var tErrors = new ConcurrentBag<Exception>();
            var tBarrier = new Barrier(workers);
            var tThreads = Enumerable.Range(0, workers).Select(i => new Thread(() =>
            {
                try
                {
                    tBarrier.SignalAndWait();
                    work(i);
                }
                catch (Exception tEx)
                {
                    tErrors.Add(tEx);
                }
            })).ToArray();

            foreach (var tThread in tThreads)
            {
                tThread.Start();
            }

            foreach (var tThread in tThreads)
            {
                tThread.Join();
            }

            return tErrors;
        }
    }
}
