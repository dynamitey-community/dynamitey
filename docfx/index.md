---
_layout: landing
title: Dynamitey.Community
---

# Dynamitey

Dynamitey is a .NET library that wraps the Dynamic Language Runtime to do runtime dispatch: late binding, currying, partial application, expando objects, tuple manipulation, and duck-typed proxies.

This documentation covers **Dynamitey.Community**, the community continuation of `ekonbenefits/dynamitey`, which has been dormant since its 3.0.3 release in November 2023.

> [!IMPORTANT]
> **Nothing has been released yet.** There is no `Dynamitey.Community` package on NuGet. Publishing is on hold pending a reply from the original maintainer; if none arrives by 2026-09-26, 4.0.0 publishes. Until then, keep using upstream's `Dynamitey` 3.0.3. See [issue #8](https://github.com/dynamitey-community/dynamitey/issues/8).

## Where to start

| If you want to | Read |
| --- | --- |
| Call a member you cannot see at compile time | [Late binding](docs/late-binding.md) |
| Use a type from an assembly you have no reference to | [Late types](docs/late-types.md) |
| Move from the original `Dynamitey` package | [Migrating](docs/migrating.md) |
| Build object graphs inline | [Builders and expandos](docs/builders.md) |
| Curry or partially apply a function | [Currying](docs/currying.md) |
| Create or manipulate tuples at runtime | [Tuples](docs/tuples.md) |
| Await a method whose return type you cannot see | [Async](docs/async.md) |
| Make repeated dynamic calls faster | [Caching](docs/caching.md) |
| Look up a specific type or member | [API reference](api/index.md) |

## The shortest example

```csharp
using Dynamitey;

var target = "a,b,c";
var result = Dynamic.InvokeMember(target, "Split", new object[] { ',' });
```

That resolves `Split` at runtime against whatever `target` actually is. The compiler is not asked to know, which is the point: it works against internal types, types from assemblies you have no reference to, and shapes decided at runtime.

## What this library is for

The DLR gives C# the `dynamic` keyword, and `dynamic` alone covers most everyday cases. Dynamitey exists for the cases it does not:

- **Accessibility.** `dynamic` resolves members in *your* assembly's accessibility context, so it cannot reach a private or internal member, and cannot hand you a value whose type you cannot see. Dynamitey can.
- **Names decided at runtime.** `target.Foo` requires writing `Foo`. `Dynamic.InvokeMember(target, name, args)` does not.
- **Reuse.** A `dynamic` call site is compiler-generated and tied to one location. [`CacheableInvocation`](docs/caching.md) makes the cached binder an object you hold and reuse across call sites.
- **Function manipulation.** Currying, partial application, and delegate conversion have no `dynamic` equivalent at all.

## Supported frameworks

`netstandard2.0` and `net10.0`. The `netstandard2.0` target is kept deliberately — it is the only target reaching both .NET Framework 4.6.1–4.8.1 and modern .NET from a single assembly.

## A word on trimming and AOT

This library is DLR-based and will never be trim-safe or NativeAOT-safe. Rather than fail at runtime, the public surface carries `[RequiresUnreferencedCode]` and `[RequiresDynamicCode]`, so a trimmed or AOT-compiled project gets build-time warnings at the exact call sites that will not survive.
