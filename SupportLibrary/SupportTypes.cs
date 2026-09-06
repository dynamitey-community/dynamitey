using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dynamitey;

namespace Dynamitey.SupportLibrary
{
    public class TestEvent
    {
        public event EventHandler<EventArgs> Event;

        public void OnEvent(object obj, EventArgs args)
        {
            if (Event != null)
                Event(obj, args);
        }
    }

    public static class TestFuncs
    {
        public static Func<int, int> Plus3
        {
            get { return x => x + 3; }
        }
    }

    public class PublicType
    {
        public static object InternalInstance => new InternalType();

        public static object AsyncInternalResultInstance => new InternalAsyncResultPoco();

        // For issue #16's scoped-wrapper fix: a Task<T> whose T IS public (string)
        // must be returned by Dynamic.InvokeMember exactly as before - unwrapped.
        public static object PublicAsyncResultInstance => new PublicAsyncResultPoco();

        // For issue #16's scoped-wrapper fix: a Task<T> that faults or is cancelled,
        // where T is internal (so the wrapper path is actually exercised), must still
        // propagate the original exception/OperationCanceledException, not an
        // AggregateException.
        public static object FaultingAsyncResultInstance => new FaultingAsyncResultPoco();
        public static object CancelingAsyncResultInstance => new CancelingAsyncResultPoco();

        // For issue #16's scoped-wrapper fix: a plain, non-generic Task result must
        // never be wrapped - it has no Result to protect.
        public static object PlainTaskAsyncInstance => new PlainTaskAsyncPoco();

        public bool PrivateMethod(object param)
        {
            return param != null;
        }

        // For issue #16's scoped-wrapper fix: NestedPublicResult below is a public
        // type nested inside this public type. Type.IsPublic is false for it (nested
        // types are never IsPublic even when declared public), but Type.IsVisible is
        // true, because every enclosing type - just PublicType, here - is also
        // public, so the type is genuinely reachable by any caller. Proves the fix
        // must use IsVisible, not IsPublic, to decide what counts as inaccessible.
        public async Task<NestedPublicResult> GetNestedPublicResultAsync(string value)
        {
            await Task.Delay(1);
            return new NestedPublicResult { Value = value };
        }

        public class NestedPublicResult
        {
            public string Value { get; set; }
        }
    }


    internal class InternalType
    {
        public bool InternalMethod(object param)
        {
            return param != null;
        }

        // For issue #16: an internal, async method reached across an assembly
        // boundary, mirroring the reporter's Azure.Data.Tables shape - a
        // genuinely-async ValueTask<T> method with optional parameters
        // (nextPartitionKey, nextRowKey) that a caller using named InvokeArg
        // arguments skips over, relying on their defaults.
        public async ValueTask<string> InternalAsyncMethod(
            string table,
            int? timeout = null,
            string nextPartitionKey = null,
            string nextRowKey = null,
            string queryOptions = null,
            CancellationToken cancellationToken = default)
        {
            await Task.Delay(1, cancellationToken);
            return $"{table}-{timeout}-{nextPartitionKey}-{nextRowKey}-{queryOptions}";
        }
    }

    // For issue #16, the shape that actually fails: the awaited RESULT type
    // is internal to this assembly - exactly like Azure.Data.Tables'
    // TableRestClient.QueryEntitiesAsync, an internal REST-client type
    // reached off a public TableClient, whose method returns
    // Task<ResponseWithHeaders<...>> where the generic closure is internal.
    // A member's declared accessibility is capped by its containing type's,
    // so a public method on this internal class can still return
    // Task<InternalResult> without a CS0050 "inconsistent accessibility"
    // error - same as the real TableRestClient shape.
    //
    // The prior, unreproduced #16 investigation instead made the target
    // internal with a PUBLIC result type - a different shape that does not
    // fail, because the C# runtime binder cares about the accessibility of
    // the value GetResult() must produce, not of the type that owns the
    // invoked method.
    internal class InternalResult
    {
        public string Value { get; set; }
    }

    internal class InternalAsyncResultPoco
    {
        public async Task<InternalResult> GetInternalResultAsync(string value)
        {
            await Task.Delay(1);
            return new InternalResult { Value = value };
        }
    }

    // For issue #16's scoped-wrapper fix: T (string) is public here, so this is the
    // regression-guard shape - Dynamic.InvokeMember must return this method's
    // Task<string> unwrapped, exactly as it did before the fix.
    internal class PublicAsyncResultPoco
    {
        public async Task<string> GetPublicResultAsync(string value)
        {
            await Task.Delay(1);
            return value;
        }
    }

    // For issue #16's scoped-wrapper fix: an already-faulted Task<InternalResult> -
    // T internal, so Dynamic.InvokeMember wraps it - used to prove the wrapper's
    // GetResult() rethrows the original exception rather than an
    // AggregateException.
    internal class FaultingAsyncResultPoco
    {
        public Task<InternalResult> GetFaultingResultAsync()
        {
            return Task.FromException<InternalResult>(new InvalidTimeZoneException("boom"));
        }
    }

    // For issue #16's scoped-wrapper fix: an already-cancelled Task<InternalResult> -
    // T internal, so Dynamic.InvokeMember wraps it - used to prove the wrapper
    // propagates OperationCanceledException rather than swallowing or misreporting
    // cancellation.
    internal class CancelingAsyncResultPoco
    {
        public Task<InternalResult> GetCancelingResultAsync()
        {
            return Task.FromCanceled<InternalResult>(new CancellationToken(true));
        }
    }

    // For issue #16's scoped-wrapper fix: a plain, non-generic Task - never wrapped,
    // since it has no Result to protect.
    internal class PlainTaskAsyncPoco
    {
        public Task GetPlainTaskAsync()
        {
            return Task.Delay(1);
        }
    }

    public interface IDynamicArg
    {
        dynamic ReturnIt(dynamic arg);

        bool Params(params dynamic[] args);
    }

    public class PocoNonDynamicArg
    {
        public int ReturnIt(int i)
        {
            return i;
        }


        public List<string> ReturnIt(List<string> i)
        {
            return i;
        }

        public bool Params(object fallback)
        {
            return false;
        }

        public bool Params(params int[] args)
        {
            return true;
        }
    }

    public static class StaticType
    {
        public static TReturn Create<TReturn>(int type)
        {
            return default(TReturn);
        }

        public static bool Test => true;

        public static int TestSet { get; set; }
    }

    public interface ISimpeleClassProps
    {
        string Prop1 { get; }

        long Prop2 { get; }

        Guid Prop3 { get; }
    }

    public interface IInheritProp : ISimpeleClassProps
    {
        PropPoco ReturnProp { get; set; }
    }


    public interface IPropPocoProp
    {
        PropPoco ReturnProp { get; set; }
    }

    public interface IEventCollisions
    {
        int Event { get; set; }
    }


    public interface IEvent
    {
        event EventHandler<EventArgs> Event;
        void OnEvent(object obj, EventArgs args);
    }

    public class PocoEvent
    {
        public event EventHandler<EventArgs> Event;

        public void OnEvent(object obj, EventArgs args)
        {
            if (Event != null)
                Event(obj, args);
        }
    }


    public class PocoOptConstructor
    {
        public string One { get; set; }
        public string Two { get; set; }
        public string Three { get; set; }

        public PocoOptConstructor(string one = "-1", string two = "-2", string three = "-3")
        {
            One = one;
            Two = two;
            Three = three;
        }
    }

    public enum TestEnum
    {
        None,
        One,
        Two
    }

    public interface IDynamicDict
    {
        int Test1 { get; }

        long Test2 { get; }

        TestEnum Test3 { get; }

        TestEnum Test4 { get; }

        dynamic TestD { get; }
    }

    public interface INonDynamicDict
    {
        int Test1 { get; }

        long Test2 { get; }

        TestEnum Test3 { get; }

        TestEnum Test4 { get; }

        IDictionary<string, object> TestD { get; }
    }

    public interface ISimpleStringProperty
    {
        int Length { get; }

    }

    public interface IRobot
    {
        string Name { get; }
    }
    public class Robot
    {
        public string Name { get; set; }
    }

    public interface ISimpleStringMethod
    {
        bool StartsWith(string value);

    }

    public interface ISimpleStringMethodCollision
    {
        int StartsWith(string value);

    }

    public interface ISimpeleClassMeth
    {
        void Action1();
        void Action2(bool value);
        string Action3();
    }

    public interface ISimpeleClassMeth2 : ISimpeleClassMeth
    {

        string Action4(int arg);
    }

    public interface IGenericMeth
    {
        string Action<T>(T arg);

        T Action2<T>(T arg);
    }

    public interface IStringIntIndexer
    {
        string this[int index] { get; set; }
    }

    public interface IObjectStringIndexer
    {
        object this[string index] { get; set; }
    }

    public interface IGenericMethWithConstraints
    {
        string Action<T>(T arg) where T : class;
        string Action2<T>(T arg) where T : IComparable;
    }

    public interface IGenericType<T>
    {
        string Funct(T arg);


    }

    public interface IGenericTypeConstraints<T> where T : class
    {
        string Funct(T arg);

    }


    public interface IOverloadingMethod
    {
        string Func(int arg);

        string Func(object arg);
    }


    public class PropPoco
    {
        public string Prop1 { get; set; }

        public long Prop2 { get; set; }

        public Guid Prop3 { get; set; }

        public int Event { get; set; }
    }

    public struct PropStruct
    {
        public string Prop1 { get; set; }

        public long Prop2 { get; set; }

        public Guid Prop3 { get; set; }

        public int Event { get; set; }
    }


    public interface IVoidMethod
    {
        void Action();
    }

    public class VoidMethodPoco
    {
        public void Action()
        {

        }
    }

    public class OverloadingMethPoco
    {
        public string Func(int arg)
        {
            return "int";
        }

        public string Func(object arg)
        {
            return "object";
        }
        public string Func(object arg, object arg2, object arg3, object arg4, object arg5, object arg6)
        {
            return "object 6";
        }

        public string Func(object one = null, object two = null, object three = null)
        {
            return "object named";
        }
    }

    /// <summary>
    /// Dynamic Delegates need to return object or void, first parameter should be a CallSite, second object, followed by the expected arguments
    /// </summary>
    public delegate object DynamicTryString(CallSite callsite, object target, out string result);

    public class MethOutPoco
    {
        public bool Func(out string result)
        {
            result = "success";
            return true;
        }
    }


    public class Thing { }

    public interface IGenericTest
    {
        List<T> GetThings<T>(Guid test) where T : Thing;
    }
    public class OtherThing
    {


        List<T> GetThings<T>(Guid test) where T : Thing
        {
            return new List<T>();
        }

    }

    public class ForwardGenericMethodsTestClass
    {
        public string Value { get; set; }

        public T Create<T>(int arg) where T : ForwardGenericMethodsTestClass, new()
        {
            return new T { Value = "test" + arg };
        }
    }


    public class GenericMethOutPoco
    {
        public bool Func<T>(out T result)
        {
            result = default(T);
            return true;
        }
    }

    public interface IGenericMethodOut
    {
        bool Func<T>(out T result);
    }

    /// <summary>
    /// For issue #14's investigation matrix. A base class used as a generic
    /// constraint (<c>where T : GenericConstraintBase</c>) below.
    /// </summary>
    public class GenericConstraintBase
    {
        public virtual string Describe()
        {
            return "base";
        }
    }

    /// <summary>
    /// For issue #14's investigation matrix - a concrete subclass to satisfy
    /// <c>where T : GenericConstraintBase</c>.
    /// </summary>
    public class GenericConstraintDerived : GenericConstraintBase
    {
        public override string Describe()
        {
            return "derived";
        }
    }

    /// <summary>
    /// For issue #14: upstream #31 reported that invoking a generic method threw,
    /// and that raw reflection's MakeGenericMethod worked around it. The reporter
    /// never sent a repro. The maintainer's reply said inference from argument
    /// types works, which by omission suggests the untested case is explicit
    /// generic arguments supplied via <see cref="InvokeMemberName"/> - especially
    /// where inference cannot determine the type argument at all (e.g. a method
    /// generic only in its return type). This class and
    /// <see cref="GenericMethodsGenericTypeTestClass{T}"/> below cover that matrix:
    /// return-type-only generics, uninferable generics, multiple type parameters
    /// where only some are inferable, constrained type parameters, value vs.
    /// reference type arguments, and a generic method with params.
    /// </summary>
    public class GenericMethodsTestClass
    {
        // Reaches InternalGenericMethodsTestClass across the assembly boundary
        // without widening its accessibility - same pattern as
        // PublicType.InternalInstance in Tests/PrivateTest.cs's coverage.
        public static object InternalGenericInstance => new InternalGenericMethodsTestClass();

        // Generic only in the return type - inference is impossible; the type
        // argument MUST be supplied explicitly.
        public T Create<T>() where T : new()
        {
            return new T();
        }

        public static T StaticCreate<T>() where T : new()
        {
            return new T();
        }

        // No arguments at all to infer from.
        public T Default<T>()
        {
            return default(T);
        }

        // Inference CAN determine T from the argument - the "works" case the
        // maintainer described - but the type argument can also be supplied
        // explicitly, which should agree with inference.
        public T Echo<T>(T arg)
        {
            return arg;
        }

        // Two type parameters: T1 is inferable from the argument, TR is not
        // (return-type only) and must be supplied explicitly.
        public TR Combine<T1, TR>(T1 arg) where TR : new()
        {
            return new TR();
        }

        // where T : class
        public T EchoClass<T>(T arg) where T : class
        {
            return arg;
        }

        // where T : new()
        public T CreateNew<T>() where T : new()
        {
            return new T();
        }

        // where T : SomeBase
        public string DescribeConstrained<T>() where T : GenericConstraintBase, new()
        {
            return new T().Describe();
        }

        // Generic method with params.
        public string Join<T>(params T[] items)
        {
            return string.Join(",", items);
        }

        // Void-returning, generic only in a type parameter that appears nowhere
        // in the arguments - exercises InvokeMemberActionCallSite's own copy of
        // the GenericArgs plumbing (Dynamitey/Internal/Optimization/InvokeHelper-
        // Regular.cs), a separate binder call site from InvokeMemberCallSite's.
        public string LastSetValue;

        public void SetDefault<T>()
        {
            LastSetValue = typeof(T).Name;
        }
    }

    /// <summary>
    /// For issue #14's investigation matrix: a generic method on a generic type.
    /// </summary>
    public class GenericMethodsGenericTypeTestClass<T>
    {
        public T Value { get; set; }

        public T2 Cast<T2>() where T2 : new()
        {
            return new T2();
        }
    }

    /// <summary>
    /// For issue #14's investigation matrix: a generic method on a non-public
    /// type, reached the same way <c>Tests/PrivateTest.cs</c> reaches
    /// <see cref="InternalType"/> - across the assembly boundary, without
    /// widening the type's accessibility.
    /// </summary>
    internal class InternalGenericMethodsTestClass
    {
        public T Create<T>() where T : new()
        {
            return new T();
        }
    }

    public interface IMethodOut2
    {
        bool Func(out int result);
    }


    public class MethRefPoco
    {
        public bool Func(ref int result)
        {
            result = result + 2;
            return true;
        }

    }

    public class PocoAdder
    {
        public int Add(int x, int y)
        {
            return x + y;
        }
    }

    public class PocoDoubleProp : IInheritProp, IPropPocoProp, IEnumerable
    {
        public string Prop1 => throw new NotImplementedException();

        public long Prop2 => throw new NotImplementedException();

        public Guid Prop3 => throw new NotImplementedException();

        public PropPoco ReturnProp
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();  //lgtm [cs/unused-property-value]
        }

        PropPoco IPropPocoProp.ReturnProp
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException(); //lgtm [cs/unused-property-value]
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }

    public class PocoCollection : IList
    {
        public IEnumerator GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public void CopyTo(Array array, int index)
        {
            throw new NotImplementedException();
        }

        public int Count => throw new NotImplementedException();

        public object SyncRoot => throw new NotImplementedException();

        public bool IsSynchronized => throw new NotImplementedException();

        public int Add(object value)
        {
            throw new NotImplementedException();
        }

        public bool Contains(object value)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public int IndexOf(object value)
        {
            throw new NotImplementedException();
        }

        public void Insert(int index, object value)
        {
            throw new NotImplementedException();
        }

        public void Remove(object value)
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        public object this[int index]
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public bool IsReadOnly => throw new NotImplementedException();

        public bool IsFixedSize => throw new NotImplementedException();
    }


    /// <summary>
    /// For issue #11. A params constructor, which is what the original upstream
    /// report used: 14 arguments worked and 15 threw InvalidCastException.
    /// </summary>
    public class ParamsConstructorPoco
    {
        public ParamsConstructorPoco(params string[] args)
        {
            Args = string.Join(",", args);
        }

        public string Args { get; }
    }

    /// <summary>
    /// For issue #11. The reporter also needed a fixed leading parameter ahead of
    /// the params array, and said the suggested workaround did not cover it.
    /// </summary>
    public class LeadingArgParamsConstructorPoco
    {
        public LeadingArgParamsConstructorPoco(string first, params string[] rest)
        {
            First = first;
            Rest = string.Join(",", rest);
        }

        public string First { get; }

        public string Rest { get; }
    }

    /// <summary>
    /// For issue #11. Exercises the same many-argument path through ordinary member
    /// invocation rather than a constructor.
    /// </summary>
    public class ParamsMethodPoco
    {
        public string Join(params string[] args)
        {
            return string.Join(",", args);
        }
    }

    /// <summary>
    /// For issue #27. InvokeMemberActionCallSite/InvokeMemberAction falls to the
    /// same >14-argument default branch as the value-returning path above, but
    /// through a void-returning delegate rather than a Func-shaped one - this
    /// exercises that instead of the string-returning ParamsMethodPoco.Join.
    /// </summary>
    public class ParamsActionMethodPoco
    {
        public string Joined { get; private set; }

        public void Join(params string[] args)
        {
            Joined = string.Join(",", args);
        }
    }

}
