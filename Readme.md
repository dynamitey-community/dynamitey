# Dynamitey

(pronounced dyna-mighty) flexes DLR muscle to do meta-mazing things in .net

[![CI](https://github.com/dynamitey-community/dynamitey/actions/workflows/ci.yml/badge.svg)](https://github.com/dynamitey-community/dynamitey/actions/workflows/ci.yml)
[![CodeQL](https://github.com/dynamitey-community/dynamitey/actions/workflows/codeql.yml/badge.svg)](https://github.com/dynamitey-community/dynamitey/actions/workflows/codeql.yml)
[![License](https://img.shields.io/badge/license-Apache--2.0-blue.svg)](License.txt)

Dynamitey is a .NET library that wraps the Dynamic Language Runtime to do
runtime dispatch: late binding, currying, partial application, expando objects,
tuple manipulation, and duck-typed proxies.

---

## What this repository is

**This is a community continuation of [`ekonbenefits/dynamitey`](https://github.com/ekonbenefits/dynamitey), which has been dormant since its 3.0.3 release in November 2023.**

It is a *detached* fork rather than a GitHub network fork — created by
mirror-cloning upstream and pushing to a fresh repository, so that issues work,
the repository is findable, and pull requests default to targeting here. The
full git history is preserved deliberately: under Apache-2.0 it is the
attribution trail, and it means changes can still be taken from upstream.

The tag [`upstream-baseline`](https://github.com/dynamitey-community/dynamitey/releases/tag/upstream-baseline)
marks commit `c44f5c5`, the last commit that is purely upstream's work.
Everything after it belongs to this project.

### Status: not published

> **There is no `dynamitey-community` package on NuGet, and nothing here has
> been released.** The `Dynamitey` package on nuget.org is upstream's, at 3.0.3.
> Installing it does not get you this code.

Publishing is deliberately on hold. An outreach message to the original
maintainer is unanswered, and until that resolves this project will not publish
a package or reserve a package ID — see
[#8](https://github.com/dynamitey-community/dynamitey/issues/8). The package
identity question that comes with it is
[#3](https://github.com/dynamitey-community/dynamitey/issues/3).

If you depend on Dynamitey today, keep using upstream's 3.0.3. This repository
is where the work to move it forward is happening, not yet where you get it.

### What has changed since upstream

| | |
| --- | --- |
| Target frameworks | `netstandard2.0;net10.0` — `net40` dropped |
| Tests | `net10.0`, NUnit 4, green on Linux, macOS and Windows |
| CI | Rebuilt: build and test on three platforms, CodeQL, dependency review and NuGet audit |
| Benchmarks | The old wall-clock `SpeedTest` fixture is now a BenchmarkDotNet project |
| Dependencies | All current; no known vulnerable or deprecated packages |

`netstandard2.0` is kept deliberately. It is the only target framework that
reaches both .NET Framework 4.6.1–4.8.1 and modern .NET from a single assembly.

The [roadmap](https://github.com/dynamitey-community/dynamitey/issues/10) tracks
what is planned and in what order. Six issues carried over from upstream are
labelled [`ported-from-upstream`](https://github.com/dynamitey-community/dynamitey/labels/ported-from-upstream).

---

## Features

Documentation still lives on upstream's wiki. Those pages describe the same API
this fork carries, and there is no equivalent here yet.

- Easy fast DLR-based reflection — [Really Late Binding](https://github.com/ekonbenefits/dynamitey/wiki/UsageReallyLateBinding)
- Clean syntax for using types from late-bound libraries — [LateType](https://github.com/ekonbenefits/dynamitey/wiki/LateType)
- Dynamic currying — [Curry](https://github.com/ekonbenefits/dynamitey/wiki/UsageCurry)
- Manipulation of tuples — [`Tests/TuplerTest.cs`](Tests/TuplerTest.cs)
- Inline object graph initialisation syntax — [Builder](https://github.com/ekonbenefits/dynamitey/wiki/UsageBuilder)
- `DynamicObject` base types for many things — [Dynamic](https://github.com/ekonbenefits/dynamitey/wiki/UsageDynamic)
- Extension-to-instance method conversion — [`Tests/Linq.cs`](Tests/Linq.cs)

### Awaiting a result whose type you cannot see

If you invoke an async method whose `Task<T>` has a `T` that is internal to
another assembly — the exact situation this library exists to reach into —
`await Dynamic.InvokeMember(...)` just works:

```csharp
// Works
var result = await Dynamic.InvokeMember(target, "SomeInternalAsyncMethod", args);
```

`await` on a `dynamic` compiles to dynamic calls to `GetAwaiter`, `IsCompleted`
and `GetResult`, which the C# runtime binder resolves in *your* assembly's
accessibility context — it cannot hand you a value of a type you cannot see.
`Dynamic.InvokeMember` detects this case (a `Task<T>` whose `T` is not visible
outside its declaring assembly) and returns the result wrapped in an
`AwaitableResult` instead of the raw task. Every member the dynamic `await`
pattern needs on that wrapper is declared publicly, with `GetResult` returning
`object` rather than `T`, so the binder never needs to see the inaccessible
type. Faults and cancellation still propagate normally — the original
exception, never wrapped in an `AggregateException`. A `Task<T>` whose `T` is
public (or a nested public type, or a plain non-generic `Task`) is returned
completely unchanged.

`Dynamic.InvokeMemberAsync` is still supported for callers who prefer a single
non-dynamic `Task<object>`-returning call, without an intermediate `dynamic`
await expression:

```csharp
object result = await Dynamic.InvokeMemberAsync(target, "SomeInternalAsyncMethod", args);
```

`Dynamic.AwaitResult(task)` does the same job for a `Task` — or an
`AwaitableResult` — you already hold. Both return `Task<object>`, and `object`
is always accessible.

### A note on trimming and AOT

This library is built on the DLR. It resolves calls at runtime that cannot be
seen statically, so it is not trim-safe and not AOT-compatible, and it never
will be. Annotating the public surface so consumers get build-time warnings
instead of runtime failures is
[#4](https://github.com/dynamitey-community/dynamitey/issues/4).

---

## Building

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

```bash
dotnet restore
dotnet build -c Release
dotnet test Tests/Tests.csproj -c Release
```

The full suite runs with no category filter and must report 0 failed and 0
skipped. CI additionally builds with `-warnaserror`.

Benchmarks are a separate project and never run in CI:

```bash
dotnet run -c Release --project Benchmarks -- --list flat
dotnet run -c Release --project Benchmarks -- --filter '*Tuple*'
```

See [`Benchmarks/README.md`](Benchmarks/README.md) for what they measure.

---

## Contributing

Issues and pull requests are welcome — see [CONTRIBUTING.md](CONTRIBUTING.md).
Issues labelled [`good first issue`](https://github.com/dynamitey-community/dynamitey/labels/good%20first%20issue)
are a reasonable starting point.

To report a security problem, do **not** open a public issue. See
[SECURITY.md](SECURITY.md).

---

## Licence and attribution

Apache License 2.0. See [License.txt](License.txt) and [NOTICE](NOTICE).

Dynamitey was created and maintained by Ekon Benefits. This fork retains that
copyright and adds its own for changes made after `upstream-baseline`, as
Apache-2.0 sections 4(b) and 4(c) require.
