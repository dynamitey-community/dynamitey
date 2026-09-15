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
            var tErrors = RunContended(32, _ => Dynamic.InvokeConstructor(typeof(int)));

            Assert.That(tErrors, Is.Empty);
        }

        [Test]
        public void ParallelValueTypeConstructorsDistinctKeysDoNotThrow()
        {
            var tTypes = new[]
            {
                typeof(int), typeof(uint), typeof(long), typeof(ulong),
                typeof(short), typeof(ushort), typeof(byte), typeof(sbyte),
                typeof(bool), typeof(char), typeof(float), typeof(double),
                typeof(decimal), typeof(DateTime), typeof(TimeSpan), typeof(Guid)
            };

            var tErrors = RunContended(tTypes.Length * 4, i =>
                Dynamic.InvokeConstructor(tTypes[i % tTypes.Length]));

            Assert.That(tErrors, Is.Empty);
            Assert.That(Dynamic.InvokeConstructor(typeof(DateTime)), Is.EqualTo(new DateTime()));
            Assert.That(Dynamic.InvokeConstructor(typeof(int)), Is.EqualTo(0));
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

            var tErrors = RunContended(tDelegateTypes.Length * 4, i =>
                Dynamic.CoerceToDelegate(tSource, tDelegateTypes[i % tDelegateTypes.Length]));

            Assert.That(tErrors, Is.Empty);
            var tPlus = (Func<int, int>)Dynamic.CoerceToDelegate(
                (Func<object, object>)(x => (int)x + 2), typeof(Func<int, int>))!;
            Assert.That(tPlus(5), Is.EqualTo(7));
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
