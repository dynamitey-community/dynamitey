# Async and awaitable results

Invoking an async method dynamically works the way you would expect:

```csharp
using Dynamitey;

var result = await Dynamic.InvokeMember(target, "SomeAsyncMethod", args);
```

That holds even in the case this library exists for: a method whose `Task<T>` has a `T` that is **internal to another assembly**.

## Why that case is hard

`await` on a `dynamic` expression compiles to dynamic calls to `GetAwaiter`, `IsCompleted` and `GetResult`. The C# runtime binder resolves those in *your* assembly's accessibility context — so it cannot hand you a value of a type you are not allowed to see. Ordinarily that means awaiting such a call fails, even though the task itself completed perfectly well.

## What Dynamitey does about it

`Dynamic.InvokeMember` detects this specific case — a `Task<T>` whose `T` is not visible outside its declaring assembly — and returns the result wrapped in an @Dynamitey.AwaitableResult instead of the raw task.

Every member the dynamic `await` pattern needs is declared publicly on that wrapper, with `GetResult` returning `object` rather than `T`, so the binder never needs to name the inaccessible type. You get the value; the type stays invisible.

Faults and cancellation propagate normally — the original exception, never wrapped in an `AggregateException`.

A `Task<T>` whose `T` is public, a nested public type, or a plain non-generic `Task` is returned **completely unchanged**. The wrapper only appears where it is needed.

## Without an intermediate dynamic await

If you would rather have a single non-dynamic call returning `Task<object>`:

```csharp
var result = await Dynamic.InvokeMemberAsync(target, "SomeAsyncMethod", args);
```

Both are supported. `InvokeMemberAsync` avoids a `dynamic` await expression in your source; `InvokeMember` plus `await` reads more like the code you would write against a visible type.
