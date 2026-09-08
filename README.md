# Dynamitey

(pronounced dyna-mighty) flexes DLR muscle to do meta-mazing things in .net

[![CI](https://github.com/dynamitey-community/dynamitey/actions/workflows/ci.yml/badge.svg)](https://github.com/dynamitey-community/dynamitey/actions/workflows/ci.yml)
[![CodeQL](https://github.com/dynamitey-community/dynamitey/actions/workflows/codeql.yml/badge.svg)](https://github.com/dynamitey-community/dynamitey/actions/workflows/codeql.yml)
[![License](https://img.shields.io/badge/license-Apache--2.0-blue.svg)](LICENSE)

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

Publishing is deliberately on hold. An outreach message was sent to the original
maintainer on **2026-09-05**, through the NuGet "Contact owners" form on the
`Dynamitey` package — upstream has issue creation restricted, so its own tracker
was not available. Until that is answered this project will not publish a package
or reserve a package ID.

**If there is no reply by 2026-09-26**, the block lifts and 4.0.0 publishes as
`Dynamitey.Community`. The reasoning behind that date, and what happens either
way, is recorded on
[#8](https://github.com/dynamitey-community/dynamitey/issues/8). The package
identity question that comes with it is
[#3](https://github.com/dynamitey-community/dynamitey/issues/3).

If you depend on Dynamitey today, keep using upstream's 3.0.3. This repository
is where the work to move it forward is happening, not yet where you get it.

### Installing, and moving from the original package

This fork is configured to pack as **`Dynamitey.Community`**, not `Dynamitey`. The assembly is
named `Dynamitey.Community` too, so once released it cannot collide with the original package on
nuget.org — two assemblies claiming one identity resolve to a coin flip that
surfaces as a runtime `MissingMethodException`.

**The namespace is deliberately unchanged.** Everything still lives in
`Dynamitey`, so moving from the original package is a one-line change and a
rebuild:

```diff
- <PackageReference Include="Dynamitey" Version="3.0.3" />
+ <PackageReference Include="Dynamitey.Community" Version="4.0.0" />
```

No `using` directive changes, no source edits. The public API is unchanged by the
rename — it is frozen by `PublicAPI.Shipped.txt`, and those files were byte-identical
before and after, which is how that claim was checked rather than asserted.

#### If you end up with both packages

A project that references both — most likely by using **ImpromptuInterface**, which
carries a compile-time reference to the original `Dynamitey` — will get a
compile-time error rather than a silent runtime failure:

```
error CS0433: The type 'Invocation' exists in both
  'Dynamitey.Community, Version=4.0.0.0, ...' and 'Dynamitey, Version=3.0.3.0, ...'
```

Resolve it by aliasing the one you are not using directly:

```xml
<PackageReference Include="Dynamitey" Version="3.0.3" Aliases="upstream" />
```

`Aliases` is honored on a direct `PackageReference` and ignored on a transitive one,
so the package has to be declared explicitly for this to take effect. This
repository's own test project does exactly that.

---

### What has changed since upstream

| | |
| --- | --- |
| Target frameworks | `netstandard2.0;net10.0` — `net40` dropped |
| Tests | `net10.0`, NUnit 4, green on Linux, macOS and Windows |
| CI | Rebuilt: build and test on three platforms, code coverage with enforced floors, CodeQL, dependency review, NuGet audit, OWASP Dependency-Check, and AOT and benchmark smoke jobs |
| Static analysis | .NET analyzers at `AnalysisMode=All`, plus Roslynator, SonarAnalyzer, AsyncFixer, IDisposableAnalyzers and PublicApiAnalyzers. Every remaining suppression carries a written reason |
| Public API | Frozen by `PublicAPI.Shipped.txt` / `PublicAPI.Unshipped.txt` — a change to the public surface fails the build until it is declared |
| Coverage | 90%+ of lines and 80%+ of branches, enforced in CI |
| Benchmarks | The old wall-clock `SpeedTest` fixture is now a BenchmarkDotNet project |
| Dependencies | All current; no known vulnerable or deprecated packages |

`netstandard2.0` is kept deliberately. It is the only target framework that
reaches both .NET Framework 4.6.1–4.8.1 and modern .NET from a single assembly.

The [roadmap](https://github.com/dynamitey-community/dynamitey/issues/10) tracks
what is planned and in what order. The six issues carried over from upstream,
labelled [`ported-from-upstream`](https://github.com/dynamitey-community/dynamitey/labels/ported-from-upstream),
have all been resolved.

---

## Features

**Documentation: <https://dynamitey-community.github.io/dynamitey/>**

Written for this fork and generated from its own source, so it describes 4.0.0
rather than 3.0.3. Nothing is carried over from upstream's wiki.

- Easy fast DLR-based reflection — [Late binding](https://dynamitey-community.github.io/dynamitey/docs/late-binding.html)
- Clean syntax for using types from late-bound libraries — [Late types](https://dynamitey-community.github.io/dynamitey/docs/late-types.html)
- Dynamic currying — [Currying and partial application](https://dynamitey-community.github.io/dynamitey/docs/currying.html)
- Manipulation of tuples — [Tuples](https://dynamitey-community.github.io/dynamitey/docs/tuples.html)
- Inline object graph initialization syntax — [Builders and expandos](https://dynamitey-community.github.io/dynamitey/docs/builders.html)
- `DynamicObject` base types for many things — [API reference](https://dynamitey-community.github.io/dynamitey/api/)
- Extension-to-instance method conversion — [Late binding](https://dynamitey-community.github.io/dynamitey/docs/late-binding.html#extension-methods-as-instance-methods)

Upstream's wiki still exists and describes the same core API, but it predates
this fork's retarget and cannot cover what 4.0.0 changed.

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
will be — that is a property of the DLR, not a defect in Dynamitey.

Trimming or NativeAOT-publishing an app that references Dynamitey now produces
build-time `IL2026`/`IL3050` warnings naming the specific Dynamitey API you
called (`Dynamic.InvokeMember`, `Dynamic.InvokeGet`, `Dynamic.InvokeConstructor`,
the `DynamicObjects` types, and so on), instead of publishing cleanly and
failing at runtime with no warning at all
([#4](https://github.com/dynamitey-community/dynamitey/issues/4)).

Take the warnings seriously: under trimming or AOT, the *runtime* failure a
member's `RuntimeBinderException` reports is not just "unsupported" but
actively misleading, because trimming and the DLR disagree about what
"unreachable" means. Trimming removes a member it cannot prove is used
statically; the DLR then looks for that member by name at runtime and reports
it as never having existed at all — even when the untrimmed source plainly
declares it:

```
InvokeGet on a public 'Name' property   -> RuntimeBinderException:
    'Simple' does not contain a definition for 'Name'
InvokeConstructor on a type with a ctor -> RuntimeBinderException:
    The type 'Poco' has no constructors defined
```

Only the >14-argument call-site path (added in #27) reports an honest,
specific failure — `PlatformNotSupportedException` naming
`System.Reflection.Emit` as the missing capability — because that path fails
on missing runtime capability rather than on a trimmed-away member. The other
two are not edge cases: a plain `InvokeGet` on an ordinary public property
fails exactly the same way. Do not rely on trimming/AOT + Dynamitey working
just because your target member "looks safe"; treat every `IL2026`/`IL3050`
on a Dynamitey call as a real, load-bearing warning, and keep argument counts
at 14 or fewer regardless (Reflection.Emit is unavailable on every AOT/trimmed
runtime, not just occasionally).

---

## Building

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

```bash
dotnet restore
dotnet build -c Release
dotnet test Tests/Tests.csproj -c Release
```

The full suite runs with no category filter and must report 0 failed and 0
skipped. CI additionally builds with `-warnaserror`, so any analyzer warning is
a build failure there even though a local build stays workable.

Coverage is enforced in CI against a floor, so it is worth being able to
reproduce it before opening a pull request:

```bash
dotnet test Tests/Tests.csproj -c Release \
  --settings coverlet.runsettings --collect:"XPlat Code Coverage"
```

`coverlet.runsettings` restricts the report to the `Dynamitey` assembly, which
is the figure CI measures — without it the number also covers `SupportLibrary`,
a fixture that exists only to be called from tests.

Adding a public member fails the build until it is declared in
`Dynamitey/PublicAPI.Unshipped.txt`. That is deliberate: it makes an accidental
change to the public surface impossible to merge quietly. The analyzer's own
code fix will add the entry for you.

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

## License and attribution

Apache License 2.0. See [License.txt](LICENSE) and [NOTICE](NOTICE).

Dynamitey was created and maintained by Ekon Benefits. This fork retains that
copyright and adds its own for changes made after `upstream-baseline`, as
Apache-2.0 sections 4(b) and 4(c) require.
