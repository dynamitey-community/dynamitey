# Late binding

Late binding is the core of the library: calling a member whose name you have as a string, on a target whose type you may not know, in an accessibility context you choose.

## The invocation surface

| Call | Does |
| --- | --- |
| `Dynamic.InvokeMember(target, name, args)` | Calls a method returning a value |
| `Dynamic.InvokeMemberAction(target, name, args)` | Calls a method returning `void` |
| `Dynamic.InvokeGet(target, name)` | Reads a property or field |
| `Dynamic.InvokeSet(target, name, value)` | Writes a property or field |
| `Dynamic.InvokeGetIndex(target, indexes)` | Reads an indexer |
| `Dynamic.InvokeSetIndex(target, indexesThenValue)` | Writes an indexer |
| `Dynamic.InvokeConstructor(type, args)` | Constructs an instance |
| `Dynamic.InvokeBinaryOperator(lhs, op, rhs)` | Applies a binary operator |
| `Dynamic.InvokeUnaryOperator(op, arg)` | Applies a unary operator |

`InvokeMember` and `InvokeMemberAction` are separate because a call site's return type is part of its signature. Asking for a value from a `void` method fails at bind time rather than quietly returning null.

## Accessibility contexts

By default a call binds in the context of the target's own type. @Dynamitey.InvokeContext changes that, and it is what lets this library reach members `dynamic` cannot.

**Reaching a non-public member:**

```csharp
var context = InvokeContext.CreateContext(target, typeof(TypeThatCanSeeIt));

var value = Dynamic.InvokeMember(context, "InternalMethod", args);
```

The second argument names the type whose accessibility applies. Pass a type inside the target's assembly and internal members become reachable.

**Static members:**

```csharp
var context = InvokeContext.CreateStatic(typeof(SomeType));

var value = Dynamic.InvokeMember(context, "StaticMethod", args);
```

This is why plain `dynamic` cannot substitute for Dynamitey in these cases: `dynamic` always binds in *your* assembly's context, and there is no way to tell it otherwise.

## Named arguments

@Dynamitey.InvokeArg passes an argument by name:

```csharp
Dynamic.InvokeMember(target, "Method", new InvokeArg("paramName", value));
```

`InvokeArg.Create` is a `Func` form of the same thing, useful where a factory is more convenient than a constructor. A `KeyValuePair<string, object>` converts explicitly, so a dictionary of arguments can be projected straight into named arguments.

## Discovering what is there

```csharp
var names = Dynamic.GetMemberNames(target);
var dynamicOnly = Dynamic.GetMemberNames(target, dynamicOnly: true);
```

`dynamicOnly: true` restricts the result to members the target reports dynamically — what a `DynamicObject` or `ExpandoObject` chooses to expose — rather than everything reflection can see.

## Extension methods as instance methods

```csharp
dynamic query = Dynamic.Linq(new List<int> { 1, 2, 3 });

var count = query.Count();

foreach (var item in (System.Collections.IEnumerable)query)
{
    // 1, 2, 3
}
```

`Dynamic.Linq` wraps a sequence so that `System.Linq.Enumerable`'s extension methods can be called as though they were instance members — which is what makes them reachable dynamically at all, since extension methods cannot be dynamically dispatched by the C# binder.

## Limits worth knowing

**Argument-count ceilings.** The generated call-site delegate types cover argument counts up to a fixed ceiling. Beyond it, a different code path runs. This was the cause of a long-standing bug where `InvokeConstructor` failed above 14 arguments, fixed in 4.0.0.

**Trimming and AOT.** Every entry point here carries `[RequiresUnreferencedCode]` and `[RequiresDynamicCode]`. This library resolves members by name at runtime, which is precisely what a trimmer cannot see and an AOT compiler cannot generate for.
