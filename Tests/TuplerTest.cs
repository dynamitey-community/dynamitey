using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dynamitey;
using NUnit.Framework;

namespace Dynamitey.Tests
{
    [TestFixture]
    public class TuplerTest
    {

        [Test]
        public void DynamicCreateTypedTuple()
        {
            object tup = Tupler.Create(1, "2", "3", 4);

            var tup2 = Tuple.Create(1, "2", "3", 4);

            Assert.That(tup, Is.TypeOf(tup2.GetType()));

            Assert.That(tup, Is.EqualTo(tup2));
        }

        [Test]
        public void DynamicCreateTypedTuple8()
        {
            object tup = Tupler.Create(1, "2", "3", 4,
                    5, 6, 7, "8");

            var tup2 = Tuple.Create(1, "2", "3", 4, 5, 6, 7, "8");

            Assert.That(tup,Is.TypeOf(tup2.GetType()));

            Assert.That(tup, Is.EqualTo(tup2));
        }

        [Test]
        public void DynamicCreateLongTypedTuple()
        {
            object tup = Tupler.Create(1, "2", "3", 4,
                    5, 6, 7, "8", "9", 10, "11", 12);

            var tup2 = new Tuple<int, string, string, int, int, int, int, Tuple<string, string, int, string, int>>(
                1, "2", "3", 4,
                5, 6, 7, Tuple.Create("8", "9", 10, "11", 12)
                );

            Assert.That(tup, Is.TypeOf(tup2.GetType()));

            Assert.That(tup, Is.EqualTo(tup2));
        }

        [Test]
        public void DynamicTupleSize()
        {
            var tup = Tuple.Create(1, 2, 3, 4, 5);

            Assert.That((object)Tupler.Size(tup),Is.EqualTo(5));
        }

        // Guards the TupleArgs arity map in InvokeHelper (T4-generated). That dictionary keyed its
        // index range off ActionKinds.Length rather than TupleKinds.Length, and produced the right
        // answer only because ActionKinds happens to be longer (17 against 8) so Zip truncated to
        // the correct range. Nothing asserted the mapping itself, so shortening ActionKinds - or
        // lengthening TupleKinds past it - would have silently dropped arities from the map.
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        [TestCase(7)]
        public void SizeReportsEveryTupleArity(int tArity)
        {
            var tArgs = Enumerable.Range(0, tArity).Select(it => (object)it).ToArray();
            var tTypes = Enumerable.Repeat(typeof(int), tArity).ToArray();

            var tTuple = typeof(Tuple).GetMethods()
                .Single(it => it.Name == nameof(Tuple.Create) && it.GetGenericArguments().Length == tArity)
                .MakeGenericMethod(tTypes)
                .Invoke(null, tArgs);

            Assert.That((object)Tupler.Size(tTuple), Is.EqualTo(tArity));
        }
        [Test]
        public void DynamicTupleSize8()
        {
            var tup = Tuple.Create(1, 2, 3, 4, 5,6,7,8);

            Assert.That((object)Tupler.Size(tup), Is.EqualTo(8));
        }
        [Test]
        public void DynamicTupleSize20()
        {
            var tup = Tupler.Create(1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20);

            Assert.That((object)Tupler.Size(tup), Is.EqualTo(20));
        }

        [Test]
        public void DynamicTupleToList()
        {
            var tup =Tuple.Create(1, 2, 3, 4, 5);
            var exp=Enumerable.Range(1,5).ToList();
            Assert.That((object)Tupler.ToList(tup),Is.EqualTo(exp));

        }

        [Test]
        public void DynamicTupleToList8()
        {
            var tup = Tuple.Create(1, 2, 3, 4, 5, 6, 7, 8);
            var exp = Enumerable.Range(1, 8).ToList();
            Assert.That((object)Tupler.ToList(tup), Is.EqualTo(exp));
        }

        [Test]
        public void DynamicTupleToList20()
        {
            var tup = Tupler.Create(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20);
            var exp = Enumerable.Range(1, 20).ToList();
            Assert.That((object)Tupler.ToList(tup), Is.EqualTo(exp));
        }



        [Test]
        public void DynamicListToTuple()
        {
            var exp = Enumerable.Range(1, 5).ToList();
            var tup = exp.ToTuple();
            Assert.That((object)Tupler.IsTuple(tup), Is.True);
            Assert.That((object)Tupler.ToList(tup), Is.EqualTo(exp));

        }

        [Test]
        public void DynamicListToTuplet8()
        {
            var exp = Enumerable.Range(1, 8).ToList();
            var tup = exp.ToTuple();
            Assert.That((object)Tupler.IsTuple(tup), Is.True);
            Assert.That((object)Tupler.ToList(tup), Is.EqualTo(exp));
        }

        [Test]
        public void DynamicListToTuple20()
        {
    
            var exp = Enumerable.Range(1, 20).ToList();
            var tup = exp.ToTuple();
            Assert.That((object)Tupler.IsTuple(tup), Is.True);
            Assert.That((object)Tupler.ToList(tup), Is.EqualTo(exp));
        }



        [Test]
        public void DynamicTupleIndex()
        {
            var tup = Tupler.Create(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20);
            Assert.That((object)Tupler.Index(tup,5), Is.EqualTo(6));
        }

        [Test]
        public void DynamicTupleIndex7()
        {
            var tup = Tupler.Create(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20);
            Assert.That((object)Tupler.Index(tup, 7), Is.EqualTo(8));
        }

        [Test]
        public void DynamicTupleIndex19()
        {
            var tup = Tupler.Create(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20);
            Assert.That((object)Tupler.Index(tup, 19), Is.EqualTo(20));
        }

        // Issue #62: Tupler.HelperIsTuple initialized its `size` out-param to 1 before the
        // null check, so the early-return-on-null path never overwrote it - Size(null) came
        // back 1, and Last(null) computed a valid-looking index from that wrong size instead
        // of being rejected. Every public entry point below now guards its `tuple` parameter
        // with Guard.NotNull, and the initializer was changed to 0 (what every non-null
        // non-tuple already reported via TupleArgs.TryGetValue's failure case). These tests
        // cover the three cases that used to give three different kinds of answer: null,
        // a non-null non-tuple, and a real tuple.

        [Test]
        public void SizeNullThrowsArgumentNullException()
        {
            Assert.That(() => Tupler.Size(null!),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("tuple"));
        }

        [Test]
        public void SizeNonTupleReturnsZero()
        {
            Assert.That((object)Tupler.Size(new object()), Is.EqualTo(0));
        }

        [Test]
        public void SizeRealTupleReturnsCorrectSize()
        {
            var tup = Tuple.Create(1, 2, 3);
            Assert.That((object)Tupler.Size(tup), Is.EqualTo(3));
        }

        [Test]
        public void IndexNullThrowsArgumentNullException()
        {
            Assert.That(() => Tupler.Index(null!, 0),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("tuple"));
        }

        [Test]
        public void IndexNonTupleThrowsArgumentException()
        {
            Assert.That(() => Tupler.Index(new object(), 0), Throws.ArgumentException);
        }

        [Test]
        public void IndexRealTupleReturnsCorrectValue()
        {
            var tup = Tuple.Create(1, 2, 3);
            Assert.That((object)Tupler.Index(tup, 1), Is.EqualTo(2));
        }

        [Test]
        public void FirstNullThrowsArgumentNullException()
        {
            Assert.That(() => Tupler.First(null!),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("tuple"));
        }

        [Test]
        public void FirstNonTupleThrowsArgumentException()
        {
            Assert.That(() => Tupler.First(new object()), Throws.ArgumentException);
        }

        [Test]
        public void FirstRealTupleReturnsCorrectValue()
        {
            var tup = Tuple.Create(1, 2, 3);
            Assert.That((object)Tupler.First(tup), Is.EqualTo(1));
        }

        [Test]
        public void SecondNullThrowsArgumentNullException()
        {
            // Second takes the same bare `object tuple` shape as First/Last and is guarded
            // for the same reason, even though the issue only names Size/Index/First/Last/ToList.
            Assert.That(() => Tupler.Second(null!),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("tuple"));
        }

        [Test]
        public void LastNullThrowsArgumentNullException()
        {
            Assert.That(() => Tupler.Last(null!),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("tuple"));
        }

        [Test]
        public void LastNonTupleThrowsArgumentException()
        {
            Assert.That(() => Tupler.Last(new object()), Throws.ArgumentException);
        }

        [Test]
        public void LastRealTupleReturnsCorrectValue()
        {
            var tup = Tuple.Create(1, 2, 3);
            Assert.That((object)Tupler.Last(tup), Is.EqualTo(3));
        }

        [Test]
        public void ToListNullThrowsArgumentNullException()
        {
            Assert.That(() => Tupler.ToList(null!),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("tuple"));
        }

        [Test]
        public void ToListNonTupleReturnsEmptyList()
        {
            Assert.That((object)Tupler.ToList(new object()), Is.Empty);
        }

        [Test]
        public void ToListRealTupleReturnsCorrectList()
        {
            var tup = Tuple.Create(1, 2, 3);
            Assert.That((object)Tupler.ToList(tup), Is.EqualTo(new List<int> { 1, 2, 3 }));
        }
    }
}
