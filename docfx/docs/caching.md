# Caching invocations

@Dynamitey.CacheableInvocation is a pre-resolved invocation you hold and reuse. Where a `dynamic` call site is generated per source location and belongs to the compiler, a `CacheableInvocation` is an object, so the same resolved binder can serve many call sites.

## Basic use

```csharp
using Dynamitey;

var setProp = new CacheableInvocation(InvocationKind.Set, "Prop1");

setProp.Invoke(target, "hello");
setProp.Invoke(anotherTarget, "world");
```

The name and kind are resolved once, at construction. Each `Invoke` reuses that work.

## Kinds

`InvocationKind` selects what the invocation does — `Get`, `Set`, `InvokeMember`, `InvokeMemberAction`, `GetIndex`, `SetIndex`, `Constructor`, and the operator kinds among them.

Some kinds constrain argument count, and the constraint is enforced at construction rather than at the call:

```csharp
new CacheableInvocation(InvocationKind.GetIndex, argCount: 0);  // throws ArgumentException
new CacheableInvocation(InvocationKind.SetIndex, argCount: 1);  // throws ArgumentException
```

An indexer read needs at least one argument and an indexer write at least two. Failing at construction means a misconfigured invocation cannot sit dormant in a field until the first call.

## When to use it

Reach for `CacheableInvocation` when the same operation runs many times — a mapper reading the same property off thousands of objects, a serializer, a dispatch loop. For a call that happens once, `Dynamic.InvokeGet` is simpler and the caching buys nothing.

## Shared cache state

> [!WARNING]
> Binder caches are shared across call sites. That is what makes reuse fast, and it also means a call's behavior can depend on what ran earlier in the process — particularly for static contexts.
>
> This is not theoretical: it is the mechanism behind a reported bug where results changed depending on execution order. Any test that touches static context must control its ordering, or it may pass for the wrong reason.
