// Above 14 arguments a call site needs a delegate type that InvokeHelper.tt does not
// generate, so InvokeHelper emits one (#27). For a value-type constructor that delegate
// returns the constructed type itself, which can live in a collectible assembly.
using NUnit.Framework;
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Dynamitey.Tests
{
    public class EmitPathCollectibleTest : Helper
    {
        private static Type DefineCollectibleStruct(string name, int ctorArgs)
        {
            var tBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(name), AssemblyBuilderAccess.RunAndCollect)
                .DefineDynamicModule("Main")
                .DefineType("Collectible", TypeAttributes.Public | TypeAttributes.Sealed, typeof(ValueType));
            tBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard,
                    Enumerable.Repeat(typeof(object), ctorArgs).ToArray())
                .GetILGenerator().Emit(OpCodes.Ret);
            return tBuilder.CreateType();
        }

        private static object[] Args(int count) => Enumerable.Range(0, count).Cast<object>().ToArray();

        // An emitted delegate in a non-collectible assembly cannot name a collectible
        // return type: the runtime threw NotSupportedException ("A non-collectible
        // assembly may not reference a collectible assembly").
        [Test]
        public void TestInvokeConstructorOnCollectibleStructAbove14Args()
        {
            var tType = DefineCollectibleStruct("EmitPathCollectible15", 15);

            var tResult = Dynamic.InvokeConstructor(tType, Args(15));

            Assert.That(tResult.GetType(), Is.EqualTo(tType));
        }

        // The emitted-delegate cache keys on the signature, return type included, so it
        // holds collectible types too. ClearCaches is the documented way to release them.
        [Test]
        public void TestClearCachesEmptiesEmittedDelegateTypes()
        {
            Dynamic.InvokeConstructor(DefineCollectibleStruct("EmitPathCollectibleClear", 15), Args(15));
            Assert.That(EmittedDelegateTypes(), Is.Not.Empty);

            Dynamic.ClearCaches();

            Assert.That(EmittedDelegateTypes(), Is.Empty);
        }

        private static IEnumerable EmittedDelegateTypes() =>
            (IEnumerable)typeof(Dynamic).Assembly
                .GetType("Dynamitey.Internal.Optimization.InvokeHelper", throwOnError: true)!
                .GetField("_emittedDelegateTypes", BindingFlags.NonPublic | BindingFlags.Static)!
                .GetValue(null)!;
    }
}
