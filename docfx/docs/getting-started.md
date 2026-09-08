# Getting started

## Installing

> [!IMPORTANT]
> Nothing has been published yet. This section describes what installation will look like once 4.0.0 ships; today there is no `Dynamitey.Community` package on NuGet. See [issue #8](https://github.com/dynamitey-community/dynamitey/issues/8) for the current state.

```xml
<PackageReference Include="Dynamitey.Community" Version="4.0.0" />
```

The namespace is `Dynamitey`, not `Dynamitey.Community`. That split is deliberate: the package and assembly are renamed so they cannot collide with the original package, while the namespace stays put so existing code needs no source change. See [Migrating](migrating.md).

```csharp
using Dynamitey;
```

## Calling a member by name

The core operation is invoking something whose name you have as a string, on a target whose type you may not know:

```csharp
var target = "a,b,c";

var parts = Dynamic.InvokeMember(target, "Split", new object[] { ',' });
```

The same call works whether `target` is a `string`, an internal type from another assembly, or an object whose shape was decided at runtime. Nothing is resolved until the call runs.

The family is consistent:

| Call | Does |
| --- | --- |
| `Dynamic.InvokeMember(target, name, args)` | Calls a method that returns a value |
| `Dynamic.InvokeMemberAction(target, name, args)` | Calls a method that returns `void` |
| `Dynamic.InvokeGet(target, name)` | Reads a property or field |
| `Dynamic.InvokeSet(target, name, value)` | Writes a property or field |
| `Dynamic.InvokeConstructor(type, args)` | Constructs an instance |

The split between `InvokeMember` and `InvokeMemberAction` is not cosmetic. A call site's return type is part of its signature, so a `void`-returning method genuinely needs a different one, and asking for a value from a method that returns none fails at bind time rather than silently yielding null.

## Why not just use `dynamic`?

For a member you can already see, `dynamic` is simpler and you should use it:

```csharp
dynamic d = target;
var parts = d.Split(',');
```

Reach for Dynamitey when one of these applies:

**The member name is not a literal.** `d.Split` requires writing `Split` in source. `Dynamic.InvokeMember(target, name, args)` takes it as data.

**The member is not visible to you.** This is the one that surprises people. `dynamic` resolves in *your* assembly's accessibility context, so it cannot call a private method, and it cannot hand you back a value whose type is internal to another assembly. Dynamitey can do both:

```csharp
var result = Dynamic.InvokeMember(
    InvokeContext.CreateContext(target, typeof(SomeTypeInThatAssembly)),
    "InternalMethod",
    args);
```

@Dynamitey.InvokeContext is what selects the accessibility context a call binds in. See [Late binding](late-binding.md) for the detail.

**You are calling the same shape repeatedly.** A `dynamic` call site is generated per source location. @Dynamitey.CacheableInvocation lets you resolve once and reuse across sites. See [Caching](caching.md).

## Named arguments

Pass @Dynamitey.InvokeArg where a positional argument would go:

```csharp
Dynamic.InvokeMember(target, "Method", new InvokeArg("paramName", value));
```

## Static members

Wrap the type rather than an instance:

```csharp
var context = InvokeContext.CreateStatic(typeof(SomeType));
var value = Dynamic.InvokeMember(context, "StaticMethod", args);
```

> [!WARNING]
> Binder caches are shared across call sites, so a static-context call can behave differently depending on what ran earlier in the process. Any test touching static context must control execution order or it can pass for the wrong reason.

## Where to go next

- [Late binding](late-binding.md) — the full invocation surface, and accessibility contexts
- [Late types](late-types.md) — working with a type you have no reference to
- [Migrating](migrating.md) — moving from the original `Dynamitey` package
