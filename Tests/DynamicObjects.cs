using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using Dynamitey.SupportLibrary;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;

namespace Dynamitey.Tests
{
    [TestFixture]
    public class DynamicObjs : Helper
    {




        [Test]
        public void GetterAnonTest()
        {
            var tAnon = new {Prop1 = "Test", Prop2 = 42L, Prop3 = Guid.NewGuid()};

            dynamic tTest = new DynamicObjects.Get(tAnon);

            Assert.That(tTest.Prop1, Is.EqualTo(tAnon.Prop1));
            Assert.That(tTest.Prop2, Is.EqualTo(tAnon.Prop2));
            Assert.That(tTest.Prop3, Is.EqualTo(tAnon.Prop3));
        }

        [Test]
        public void GetterVoidTest()
        {
            var tPoco = new VoidMethodPoco();

            dynamic tTest = new DynamicObjects.Get(tPoco);

            tTest.Action();
        }

        [Test]
        public void GetterArrayTest()
        {


            var tArray = new int[] {1, 2, 3};

            dynamic tTest = new DynamicObjects.Get(tArray);
            Dynamic.ApplyEquivalentType(tTest, typeof (IStringIntIndexer));

            Assert.That(tTest[2], Is.EqualTo(tArray[2].ToString()));
        }

        [Test]
        public void GetterEventTest()
        {
            dynamic dynEvent = new DynamicObjects.Get(new PocoEvent());
            Dynamic.ApplyEquivalentType(dynEvent, typeof (IEvent));
            var tSet = false;
            EventHandler<EventArgs> tActsLikeOnEvent = (obj, args) => tSet = true;
            dynEvent.Event += tActsLikeOnEvent;

            dynEvent.OnEvent(null, null);
            Assert.That(tSet, Is.EqualTo(true));

        }


        [Test]
        public void GetterEventTest2()
        {
            dynamic dynEvent = new DynamicObjects.Get(new PocoEvent());
            Dynamic.ApplyEquivalentType(dynEvent, typeof (IEvent));
            var tSet = false;
            EventHandler<EventArgs> tActsLikeOnEvent = (obj, args) => tSet = true;
            dynEvent.Event += tActsLikeOnEvent;
            dynEvent.Event -= tActsLikeOnEvent;
            dynEvent.OnEvent(null, null);
            Assert.That(tSet, Is.EqualTo(false));

        }


        [Test]
        public void GetterDynamicTest()
        {
            dynamic tNew = new ExpandoObject();
            tNew.Prop1 = "Test";
            tNew.Prop2 = 42L;
            tNew.Prop3 = Guid.NewGuid();

            dynamic tTest = new DynamicObjects.Get(tNew);


            Assert.That(tTest.Prop1, Is.EqualTo(tNew.Prop1));
            Assert.That(tTest.Prop2, Is.EqualTo(tNew.Prop2));
            Assert.That(tTest.Prop3, Is.EqualTo(tNew.Prop3));
        }

        public class TestForwarder : Dynamitey.DynamicObjects.BaseForwarder
        {
            public TestForwarder(object target)
                : base(target)
            {
            }
        }

        [Test]
        public void ForwardAnonTest()
        {
            var tAnon = new {Prop1 = "Test", Prop2 = 42L, Prop3 = Guid.NewGuid()};

            dynamic tTest = new TestForwarder(tAnon);

            Assert.That(tTest.Prop1, Is.EqualTo(tAnon.Prop1));
            Assert.That(tTest.Prop2, Is.EqualTo(tAnon.Prop2));
            Assert.That(tTest.Prop3, Is.EqualTo(tAnon.Prop3));
        }

        [Test]
        public void ForwardVoidTest()
        {
            var tPoco = new VoidMethodPoco();

            dynamic tTest = new TestForwarder(tPoco);

            tTest.Action();
        }


        [Test]
        public void ForwardGenericMethodsTest()
        {
            dynamic tNew = new ForwardGenericMethodsTestClass();

            dynamic tFwd = new TestForwarder(tNew);

            Assert.That(tFwd.Create<ForwardGenericMethodsTestClass>(99).Value, Is.EqualTo("test99"));
        }


        [Test]
        public void ForwardDynamicTest()
        {
            dynamic tNew = new ExpandoObject();
            tNew.Prop1 = "Test";
            tNew.Prop2 = 42L;
            tNew.Prop3 = Guid.NewGuid();

            dynamic tTest = new TestForwarder(tNew);


            Assert.That(tTest.Prop1, Is.EqualTo(tNew.Prop1));
            Assert.That(tTest.Prop2, Is.EqualTo(tNew.Prop2));
            Assert.That(tTest.Prop3, Is.EqualTo(tNew.Prop3));
        }

        [Test]
        public void DictionaryMethodsTest()
        {

            dynamic tNew = new DynamicObjects.Dictionary();
            tNew.Action1 = new Action(Assert.Fail);
            tNew.Action2 = new Action<bool>(actual => Assert.That(actual, Is.False));
            tNew.Action3 = new Func<string>(() => "test");
            tNew.Action4 = new Func<int, string>(arg => "test" + arg);





            Assert.That(() => tNew.Action1(), Throws.InstanceOf<AssertionException>());
            Assert.That(() => tNew.Action2(true), Throws.InstanceOf<AssertionException>());

            Assert.That((object)tNew.Action3(), Is.EqualTo("test"));

            Assert.That((object)tNew.Action4(4), Is.EqualTo("test4"));
        }

        // Issue #50 (cs/reference-equality-with-object). BaseDictionary.SetProperty used to decide
        // whether a set actually changed the value with `!=`, which on `object` is reference
        // equality: re-assigning a property to an independently-boxed value with the same content
        // (not the same box) would still look "changed" and fire a spurious PropertyChanged.
        [Test]
        public void SetPropertyDoesNotRaiseChangeForContentEqualDifferentReferenceValue()
        {
            dynamic tNew = new DynamicObjects.Dictionary();
            var tChanges = new List<string>();
            ((INotifyPropertyChanged)tNew).PropertyChanged += (s, e) => tChanges.Add(e.PropertyName!);

            tNew.Value = 5;
            tChanges.Clear();

            object tStoredValue = tNew.Value;
            object tSameContentDifferentBox = 5;
            // Sanity check on the premise: boxing always allocates, so these are genuinely two
            // different objects with the same content, not the same reference.
            Assert.That(ReferenceEquals(tStoredValue, tSameContentDifferentBox), Is.False);

            tNew.Value = tSameContentDifferentBox;
            Assert.That(tChanges, Is.Empty, "Setting an equal-by-value, different-reference value must not raise PropertyChanged.");

            tNew.Value = 6;
            Assert.That(tChanges, Is.EqualTo(new[] { "Value", "Item[]" }), "Setting a genuinely different value must still raise PropertyChanged.");
        }

        // Issue #50 (cs/reference-equality-with-object). BaseDictionary.Remove(KeyValuePair) used
        // `==` on the value, which on `object` is reference equality: removing by a
        // content-equal-but-differently-boxed value would silently leave the entry in place.
        [Test]
        public void RemoveKeyValuePairUsesValueEquality()
        {
            IDictionary<string, object> tDict = new DynamicObjects.Dictionary();
            tDict["Test"] = 5;

            object tSameContentDifferentBox = 5;
            Assert.That(ReferenceEquals(tDict["Test"], tSameContentDifferentBox), Is.False);

            var tRemoved = tDict.Remove(new KeyValuePair<string, object>("Test", tSameContentDifferentBox));

            Assert.That(tDict.ContainsKey("Test"), Is.False);
            // Every path of this method used to return false, so even a successful removal
            // reported failure. ICollection<T>.Remove is documented to return whether the item
            // was removed, and callers branch on it.
            Assert.That(tRemoved, Is.True, "A successful removal must report true.");
        }

        [Test]
        public void RemoveKeyValuePairReportsFalseWhenValueDoesNotMatch()
        {
            IDictionary<string, object> tDict = new DynamicObjects.Dictionary();
            tDict["Test"] = 5;

            var tRemoved = tDict.Remove(new KeyValuePair<string, object>("Test", 6));

            Assert.That(tRemoved, Is.False, "A value mismatch must report false.");
            Assert.That(tDict.ContainsKey("Test"), Is.True, "A value mismatch must leave the entry in place.");
            Assert.That(
                tDict.Remove(new KeyValuePair<string, object>("Absent", 5)), Is.False,
                "A missing key must report false.");
        }

        [Test]
        public void ForwardMethodsTest()
        {

            dynamic tNew = new DynamicObjects.Dictionary();
            tNew.Action1 = new Action(Assert.Fail);
            tNew.Action2 = new Action<bool>(actual => Assert.That(actual, Is.False));
            tNew.Action3 = new Func<string>(() => "test");
            tNew.Action4 = new Func<int, string>(arg => "test" + arg);


            dynamic tFwd = new TestForwarder(tNew);



            Assert.That(() => tFwd.Action1(), Throws.InstanceOf<AssertionException>());
            Assert.That(() => tFwd.Action2(true), Throws.InstanceOf<AssertionException>());

            Assert.That((object)tFwd.Action3(), Is.EqualTo("test"));

            Assert.That((object)tFwd.Action4(4), Is.EqualTo("test4"));
        }

        [Test]
        public void DictionaryMethodsOutTest()
        {

            dynamic tNew = new DynamicObjects.Dictionary();
            tNew.Func = new DynamicTryString(TestOut);

            Assert.That(tNew.Func(null, "Test", out string tOut), Is.EqualTo(true));
            Assert.That(tOut, Is.EqualTo("Test"));

            Assert.That(tNew.Func(null, 1, out string tOut2), Is.EqualTo(false));
            Assert.That(tOut2, Is.EqualTo(null));
        }

        private static object TestOut(CallSite dummy, object @in, out string @out)
        {
            @out = @in as string;

            return @out != null;
        }


        [Test]
        public void DictionaryMethodsTestWithPropertyAccess()
        {

            dynamic tNew = new DynamicObjects.Dictionary();
            tNew.PropCat = "Cat-";
            tNew.Action1 = new Action(Assert.Fail);
            tNew.Action2 = new Action<bool>(actual => Assert.That(actual, Is.False));
            tNew.Action3 = new ThisFunc<string>(@this => @this.PropCat + "test");



            Assert.That(() => tNew.Action1(), Throws.InstanceOf<AssertionException>());
            Assert.That(() => tNew.Action2(true), Throws.InstanceOf<AssertionException>());

            Assert.That(tNew.Action3(), Is.EqualTo("Cat-test"));


        }

        [Test]
        public void DictionaryNullMethodsTest()
        {

            dynamic tNew = new DynamicObjects.Dictionary();
            Dynamic.ApplyEquivalentType(tNew, typeof (ISimpleStringMethod));

            Assert.That((object)tNew.StartsWith("Te"), Is.False);



        }


        [Test]
        public void DynamicDictionaryWrappedTest()
        {

            var tDictionary = new Dictionary<string, object>
                                  {
                                      {"Test1", 1},
                                      {"Test2", 2},
                                      {
                                          "TestD", new Dictionary<string, object>()
                                                       {
                                                           {"TestA", "A"},
                                                           {"TestB", "B"}
                                                       }
                                      }
                                  };

            dynamic tNew = new DynamicObjects.Dictionary(tDictionary);

            Assert.That(tNew.Test1, Is.EqualTo(1));
            Assert.That(tNew.Test2, Is.EqualTo(2));
            Assert.That(tNew.TestD.TestA, Is.EqualTo("A"));
            Assert.That(tNew.TestD.TestB, Is.EqualTo("B"));
        }

        [Test]
        public void InterfaceDictionaryWrappedTest()
        {

            var tDictionary = new Dictionary<string, object>
                                  {
                                      {"Test1", 1},
                                      {"Test2", 2L},
                                      {"Test3", 1},
                                      {"Test4", "Two"},
                                      {
                                          "TestD", new Dictionary<string, object>()
                                                       {
                                                           {"TestA", "A"},
                                                           {"TestB", "B"}
                                                       }
                                      }
                                  };

            dynamic tDynamic = new DynamicObjects.Dictionary(tDictionary);
            dynamic tNotDynamic = new DynamicObjects.Dictionary(tDictionary);


            Dynamic.ApplyEquivalentType(tDynamic, typeof (IDynamicDict));
            Dynamic.ApplyEquivalentType(tNotDynamic, typeof (INonDynamicDict));


            Assert.That(tNotDynamic, Is.EqualTo(tDynamic));

            Assert.That(tDynamic.Test1, Is.EqualTo(1));
            Assert.That(tDynamic.Test2, Is.EqualTo(2L));
            Assert.That(tDynamic.Test3, Is.EqualTo(TestEnum.One));
            Assert.That(tDynamic.Test4, Is.EqualTo(TestEnum.Two));

            Assert.That(tDynamic.TestD.TestA, Is.EqualTo("A"));
            Assert.That(tDynamic.TestD.TestB, Is.EqualTo("B"));

            Assert.That(tNotDynamic.Test1, Is.EqualTo(1));
            Assert.That(tNotDynamic.Test2, Is.EqualTo(2L));
            Assert.That(tNotDynamic.Test3, Is.EqualTo(TestEnum.One));
            Assert.That(tNotDynamic.Test4, Is.EqualTo(TestEnum.Two));

            Assert.That(tNotDynamic.TestD.GetType(), Is.EqualTo(typeof (Dictionary<string, object>)));
            Assert.That(tDynamic.TestD.GetType(), Is.EqualTo(typeof (DynamicObjects.Dictionary)));
        }

        [Test]
        public void DynamicObjectEqualsTest()
        {
            var tDictionary = new Dictionary<string, object>
                                  {
                                      {"Test1", 1},
                                      {"Test2", 2},
                                      {
                                          "TestD", new Dictionary<string, object>()
                                                       {
                                                           {"TestA", "A"},
                                                           {"TestB", "B"}
                                                       }
                                      }
                                  };

            dynamic tDynamic = new DynamicObjects.Dictionary(tDictionary);
            dynamic tNotDynamic = new DynamicObjects.Dictionary(tDictionary);


            Dynamic.ApplyEquivalentType(tDynamic, typeof (IDynamicDict));
            Dynamic.ApplyEquivalentType(tNotDynamic, typeof (INonDynamicDict));

            Assert.That(tNotDynamic, Is.EqualTo(tDynamic));

            Assert.That(tDictionary, Is.EqualTo(tDynamic));

            Assert.That(tDictionary, Is.EqualTo(tNotDynamic));
        }

        [Test]
        public void DynamicAnnonymousWrapper()
        {
            var tData = new Dictionary<int, string> {{1, "test"}};
            var tDyn = DynamicObjects.Get.Create(new
                                                     {
                                                         Test1 = 1,
                                                         Test2 = "2",
                                                         IsGreaterThan5 = Return<bool>.Arguments<int>(it => it > 5),
                                                         ClearData = ReturnVoid.Arguments(() => tData.Clear())
                                                     });

            Assert.That(tDyn.Test1, Is.EqualTo(1));
            Assert.That(tDyn.Test2, Is.EqualTo("2"));
            Assert.That(tDyn.IsGreaterThan5(6), Is.EqualTo(true));
            Assert.That(tDyn.IsGreaterThan5(4), Is.EqualTo(false));

            Assert.That(tData.Count, Is.EqualTo(1));
            tDyn.ClearData();
            Assert.That(tData.Count, Is.EqualTo(0));

        }

        [Test]
        public void TestAnonInterface()
        {
            dynamic tInterface = new DynamicObjects.Get(new
                                                            {
                                                                CopyArray =
                                                            ReturnVoid.Arguments<Array, int>(
                                                                (ar, i) => Enumerable.Range(1, 10)),
                                                                Count = 10,
                                                                IsSynchronized = false,
                                                                SyncRoot = this,
                                                                GetEnumerator =
                                                            Return<IEnumerator>.Arguments(
                                                                () => Enumerable.Range(1, 10).GetEnumerator())
                                                            });

            Dynamic.ApplyEquivalentType(tInterface, typeof (ICollection), typeof (IEnumerable));

            Assert.That(tInterface.Count, Is.EqualTo(10));
            Assert.That(tInterface.IsSynchronized, Is.EqualTo(false));
            Assert.That(tInterface.SyncRoot, Is.EqualTo(this));
            Assert.That((object)tInterface.GetEnumerator(), Is.InstanceOf<IEnumerator>());
        }

        [Test]
        public void TestBuilder()
        {
            var New = Builder.New<ExpandoObject>();

            var tExpando = New.Object(
                Test: "test1",
                Test2: "Test 2nd"
                );
            Assert.That(tExpando.Test, Is.EqualTo("test1"));
            Assert.That(tExpando.Test2, Is.EqualTo("Test 2nd"));

            dynamic NewD = new DynamicObjects.Builder<ExpandoObject>();


            var tExpandoNamedTest = NewD.Robot(
                LeftArm: "Rise",
                RightArm: "Clamp"
                );

            Assert.That(tExpandoNamedTest.LeftArm, Is.EqualTo("Rise"));
            Assert.That(tExpandoNamedTest.RightArm, Is.EqualTo("Clamp"));
        }

        // Test-only type for the two catch-narrowing tests below: its parameterless constructor
        // always throws, so it can prove a genuine constructor failure is neither swallowed nor
        // (via a stray fallback re-invocation) run twice.
        public class ThrowingParameterlessCtorPoco
        {
            public static int ConstructAttempts;

            public ThrowingParameterlessCtorPoco()
            {
                ConstructAttempts++;
                throw new InvalidOperationException("boom");
            }
        }

        // Issue #50 (cs/catch-of-all-exceptions). Activate<T>.Create() used to catch(Exception)
        // around Activator.CreateInstance<T>(), narrowed to catch(MissingMemberException) - the one
        // documented failure of that call, and exactly the "optional-parameter constructor" case the
        // fallback exists for (see PocoOptConstructor: only a (string,string,string) ctor, all
        // defaulted).
        [Test]
        public void ActivateStillFallsBackForOptionalParameterConstructor()
        {
            PocoOptConstructor tResult = new Activate<PocoOptConstructor>().Create();

            Assert.That(tResult.One, Is.EqualTo("-1"));
            Assert.That(tResult.Two, Is.EqualTo("-2"));
            Assert.That(tResult.Three, Is.EqualTo("-3"));
        }

        [Test]
        public void ActivateDoesNotDoubleInvokeConstructorOnUnrelatedException()
        {
            ThrowingParameterlessCtorPoco.ConstructAttempts = 0;

            // Activator.CreateInstance<T>() wraps a throwing constructor's exception in a
            // TargetInvocationException, which is nowhere in the MissingMemberException hierarchy,
            // so the narrowed catch must let it propagate rather than treat it as "no
            // parameterless constructor".
            Assert.That(() => new Activate<ThrowingParameterlessCtorPoco>().Create(),
                Throws.InstanceOf<TargetInvocationException>()
                    .With.InnerException.InstanceOf<InvalidOperationException>());

            // Before the fix, catch(Exception) would swallow that exception and retry via
            // Dynamic.InvokeConstructor, running the (still-failing) constructor a second time - a
            // real problem for any constructor with side effects.
            Assert.That(ThrowingParameterlessCtorPoco.ConstructAttempts, Is.EqualTo(1));
        }

        // Same two cases again for DynamicObjects.Builder<T>.InvokeHelper, which has the identical
        // catch(Exception)-around-Activator.CreateInstance<T>() pattern.
        [Test]
        public void DynamicBuilderStillFallsBackForOptionalParameterConstructor()
        {
            dynamic tNewD = new DynamicObjects.Builder<PocoOptConstructor>();

            PocoOptConstructor tResult = tNewD.Object();

            Assert.That(tResult.One, Is.EqualTo("-1"));
            Assert.That(tResult.Two, Is.EqualTo("-2"));
            Assert.That(tResult.Three, Is.EqualTo("-3"));
        }

        [Test]
        public void DynamicBuilderDoesNotDoubleInvokeConstructorOnUnrelatedException()
        {
            ThrowingParameterlessCtorPoco.ConstructAttempts = 0;
            dynamic tNewD = new DynamicObjects.Builder<ThrowingParameterlessCtorPoco>();

            Assert.That(() => tNewD.Object(),
                Throws.InstanceOf<TargetInvocationException>()
                    .With.InnerException.InstanceOf<InvalidOperationException>());

            Assert.That(ThrowingParameterlessCtorPoco.ConstructAttempts, Is.EqualTo(1));
        }

        [Test]
        public void TestSetupOtherTypes()
        {
            var New = Builder.New().Setup(
                Expando: typeof (ExpandoObject),
                Dict: typeof (DynamicObjects.Dictionary)
                );

            var tExpando = New.Expando(
                LeftArm: "Rise",
                RightArm: "Clamp"
                );

            var tDict = New.Dict(
                LeftArm: "RiseD",
                RightArm: "ClampD"
                );

            Assert.That(tExpando.LeftArm, Is.EqualTo("Rise"));
            Assert.That(tExpando.RightArm, Is.EqualTo("Clamp"));
            Assert.That(tExpando.GetType(), Is.EqualTo(typeof (ExpandoObject)));

            Assert.That(tDict.LeftArm, Is.EqualTo("RiseD"));
            Assert.That(tDict.RightArm, Is.EqualTo("ClampD"));
            Assert.That(tDict.GetType(), Is.EqualTo(typeof (DynamicObjects.Dictionary)));

        }

        [Test]

        //This test data is modified from MS-PL Clay project http://clay.codeplex.com
        public void TestClayFactorySyntax()
        {
            dynamic New = Builder.New();

            {
                var person = New.Person();
                person.FirstName = "Louis";
                person.LastName = "Dejardin";
                Assert.That(person.FirstName, Is.EqualTo("Louis"));
                Assert.That(person.LastName, Is.EqualTo("Dejardin"));
            }
            {
                var person = New.Person();
                person["FirstName"] = "Louis";
                person["LastName"] = "Dejardin";
                Assert.That(person.FirstName, Is.EqualTo("Louis"));
                Assert.That(person.LastName, Is.EqualTo("Dejardin"));
            }
            {
                var person = New.Person(
                    FirstName: "Bertrand",
                    LastName: "Le Roy"
                    ).Aliases("bleroy", "boudin");

                Assert.That(person.FirstName, Is.EqualTo("Bertrand"));
                Assert.That(person.LastName, Is.EqualTo("Le Roy"));
                Assert.That(person.Aliases[1], Is.EqualTo("boudin"));
            }

            {
                var person = New.Person()
                                .FirstName("Louis")
                                .LastName("Dejardin")
                                .Aliases(new[] {"Lou"});

                Assert.That(person.FirstName, Is.EqualTo("Louis"));
                Assert.That(person.Aliases[0], Is.EqualTo("Lou"));
            }

            {
                var person = New.Person(new
                                            {
                                                FirstName = "Louis",
                                                LastName = "Dejardin"
                                            });
                Assert.That(person.FirstName, Is.EqualTo("Louis"));
                Assert.That(person.LastName, Is.EqualTo("Dejardin"));
            }

        }





        [Test]
        //This test data is modified from MS-PL Clay project http://clay.codeplex.com
        public void TestFactoryListSyntax()
        {
            dynamic New = Builder.New();

            //Test using Clay Syntax
            var people = New.Array(
                New.Person().FirstName("Louis").LastName("Dejardin"),
                New.Person().FirstName("Bertrand").LastName("Le Roy")
                );

            Assert.That(people[0].LastName, Is.EqualTo("Dejardin"));
            Assert.That(people[1].LastName, Is.EqualTo("Le Roy"));

            var people2 = new DynamicObjects.List()
                              {
                                  New.Robot(Name: "Bender"),
                                  New.Robot(Name: "RobotDevil")
                              };


            Assert.That(people2[0].Name, Is.EqualTo("Bender"));
            Assert.That(people2[1].Name, Is.EqualTo("RobotDevil"));

        }

        [Test]
        public void TestQuicListSyntax()
        {
            var tList = Build.NewList("test", "one", "two");
            Assert.That(tList[1], Is.EqualTo("one"));

            var tList2 = Build.NewList("test", "one", "two", "three");
            Assert.That(tList2[3], Is.EqualTo("three"));
        }


        [Test]
        public void TestRecorder()
        {
            dynamic New = Builder.New<DynamicObjects.Recorder>();

            DynamicObjects.Recorder tRecording = New.Watson(Test: "One", Test2: 2, NameLast: "Watson");


            dynamic tVar = tRecording.ReplayOn(new ExpandoObject());

            Assert.That(tVar.Test, Is.EqualTo("One"));
            Assert.That(tVar.Test2, Is.EqualTo(2));
            Assert.That(tVar.NameLast, Is.EqualTo("Watson"));
        }

        [Test]
        public void TestRecorderReplaysIndexAssignment()
        {
            dynamic recorder = new DynamicObjects.Recorder();
            recorder[1] = "written";

            var target = new List<string> { "zero", "one" };
            recorder.ReplayOn(target);

            Assert.That(target[1], Is.EqualTo("written"));
        }


        [Test]
        public void TestRoslynLateTypeBind()
        {
            // Runtime-compiles an assembly, then late-binds to a type in it - the same
            // scenario the old CodeDom/CSharpCodeProvider version of this test covered
            // (see #23 item 3). CodeDom's runtime compilation is .NET Framework only and
            // has no modern equivalent, so this uses Roslyn instead.
            string code = @"
                namespace CodeInjection
                {
                    public static class DynConcatenateString
                    {
                        public static string Concatenate(string s1, string s2){
                            return s1 + "" ! "" + s2;
                        }
                    }
                }";

            var syntaxTree = CSharpSyntaxTree.ParseText(code);

            var references = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
                .Select(a => (MetadataReference)MetadataReference.CreateFromFile(a.Location));

            var compilation = CSharpCompilation.Create(
                "DynamiteyTestCodeInjection_" + Guid.NewGuid().ToString("N"),
                new[] { syntaxTree },
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            using var assemblyStream = new MemoryStream();
            var emitResult = compilation.Emit(assemblyStream);

            Assert.That(emitResult.Success, Is.True,
                () => string.Join(Environment.NewLine, emitResult.Diagnostics));

            var compiledAssembly = Assembly.Load(assemblyStream.ToArray());

            dynamic DynConcatenateString = new DynamicObjects.LateType(compiledAssembly, "CodeInjection.DynConcatenateString");

            Assert.That(DynConcatenateString.Concatenate("1","2"), Is.EqualTo("1 ! 2"));
        }


    [Test]
        public void TestLateLibrarybind()
        {

            dynamic tBigIntType =
                new DynamicObjects.LateType(
                    "System.Numerics.BigInteger, System.Numerics, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");

            if (tBigIntType.IsAvailable)
            {

                var one = tBigIntType.@new(1);
                var two = tBigIntType.@new(2);

                Assert.That(one.IsEven, Is.False);
                Assert.That(two.IsEven, Is.EqualTo(true));

                var tParsed = tBigIntType.Parse("4");

                Assert.That(tParsed.IsEven, Is.EqualTo(true));



            }
            else
            {

                Assert.Fail("Big Int Didn't Load");


            }
        }
    }
}
