# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

Dynamitey is an Apache-2.0 licensed .NET library that wraps the DLR to do
runtime dispatch — late binding, currying, partial application, expando objects,
and duck-typed proxies. Upstream (`ekonbenefits/dynamitey`) has been dormant
since its 3.0.3 release in November 2023. This repository is a community
continuation in the `dynamitey-community` GitHub org.

It is a **detached** fork, not a GitHub network fork — created by mirror-clone
and push to a fresh empty repo. A network fork would disable issues by default,
hide the repo from search, and cause contributor PRs to default to targeting
`ekonbenefits`. Full git history is preserved deliberately: under Apache-2.0 it
is the attribution trail, and it allows cherry-picking from upstream later.

`docs/dynamitey-migration-handover.md` records how the repository was created.
It is history, not a task list.

## Hard constraints

- **Never push to `ekonbenefits`** — not a branch, not a tag, not a PR. That repo
  has issue creation restricted by its owner and an outreach message is still
  pending a reply. The `upstream` remote's push URL is set to `DISABLED` for
  exactly this reason; do not undo it. Fetching is fine and is how upstream work
  gets pulled in.
- **Also avoid incidental writes to upstream.** A clickable link to an
  `ekonbenefits` issue or PR posts a cross-reference event onto their timeline,
  and an `@mention` copied out of upstream text notifies a real person. Put both
  in backticks. The ported issues (#11–#16) follow this and say so inline.
- **Never force-push.** `main` is protected against it, admins included.
- **Do not move or delete the `upstream-baseline` tag.** It marks commit
  `c44f5c5`, the last purely-upstream commit, which is what the Apache-2.0
  "state your changes" requirement points at.
- **Do not publish** a NuGet package or reserve a package ID. Gated on the
  upstream maintainer's pending reply. See issue #8.

## Repo conventions

**Commit identity** is set repo-locally to `Tom Atwood
<1312113+AtwoodTM@users.noreply.github.com>` so the maintainer's real address
does not appear in a public history. Do not override it, and do not reach for
the global config here.

**No FCG house rules apply.** This is a public OSS repo, so: no pre-commit
hooks, no `prek` config, no `type/KEY: message` commit subjects, no DEV/PLAT
Jira keys. Work is tracked in GitHub issues.

**`main` is protected.** PRs required (0 approvals, so a solo maintainer is not
locked out), linear history, no force pushes, no deletions, conversation
resolution required, admins included, and seven required status checks. A merge
is blocked until every review thread is resolved — including Copilot's, which
reviews PRs automatically.

**Issue labels that carry meaning beyond the default set:**

- `ported-from-upstream` — carried over from `ekonbenefits/dynamitey`, not yet
  reproduced against this codebase.
- `notify-on-close` — the original reporter must be `@`-mentioned in the closing
  comment when the fix ships. They were deliberately not notified when the issue
  was ported. `gh issue list --label notify-on-close --state all` lists them.
  Issue #11 has two people to notify, not one.
- `blocked` — correct to do, not permitted yet.
- `dependencies` — Dependabot's.

Issue #10 is the pinned roadmap and the sequencing authority.

## Build, test, benchmark

Requires the .NET 10 SDK.

```bash
dotnet restore
dotnet build -c Release                     # add -warnaserror to match CI
dotnet test Tests/Tests.csproj -c Release   # no filter, 0 failed, 0 skipped
```

| Project | Target frameworks |
| --- | --- |
| `Dynamitey` | `netstandard2.0;net10.0` |
| `SupportLibrary` | `netstandard2.0` |
| `Tests` | `net10.0` |
| `Benchmarks` | `net10.0` |

`netstandard2.0` is kept deliberately — it is the only target reaching both
.NET Framework 4.6.1–4.8.1 and modern .NET from one assembly, and upstream has
53 dependent packages. Do not "simplify" it away.

Benchmarks never run in CI beyond a dry pass. To run them for real:

```bash
dotnet run -c Release --project Benchmarks -- --list flat
dotnet run -c Release --project Benchmarks -- --filter '*Tuple*'
```

### The test count, because it looks wrong and is not

Two numbers will not match, and both are fine.

`Tests/*.cs` carries **187** `[Test]` attributes and **10** `[TestCase]`
attributes. NUnit expands each `[TestCase]` into its own test, so the executed
count is higher than the `[Test]` count, not lower.

One test does not run at all: `TestCodeDomLateTypeBind` in
`Tests/DynamicObjects.cs`, inside `#if NETFRAMEWORK`. It compiles an assembly at
runtime with `CSharpCodeProvider`, so it only ever ran on .NET Framework and has
been unreachable since `net48` was dropped. Tracked in #23.

**Do not pin the executed count in documentation.** It was pinned at 186 in four
files, and the first bug fix that added tests made all four wrong at once. The
bar is *0 failed, 0 skipped, with no category filter* — that survives new tests,
and a filter reappearing in a test command is the thing actually worth catching.

**Where older numbers came from.** Before the `SpeedTest` fixture moved to
`Benchmarks/` in #9, the source carried 219 `[Test]` attributes and CI needed
`--filter TestCategory!=Performance` to stay green, which produced 186. So 219
and "186 under a filter" both describe the tree before #9.

## Continuous integration

Three workflows, all pinned to current action majors:

| Workflow | Does |
| --- | --- |
| `ci.yml` | Build and test on Linux, macOS, Windows; `-warnaserror`; TRX artifacts; dry-runs every benchmark |
| `codeql.yml` | `security-and-quality` queries, manual build mode, PRs and weekly. **Builds `Dynamitey/Dynamitey.csproj` only** — see below |
| `dependencies.yml` | Dependency review on PRs; weekly `dotnet list package --vulnerable --include-transitive` |

`push` only triggers CI on `main`; `pull_request` covers everything else, which
is what stops every branch push producing a duplicate run. Do not add branches
to the `push` trigger without a reason.

**`-warnaserror` lives in the workflow, not the project files.** The tree builds
clean with .NET analyzers at `AnalysisLevel=latest`, so any new warning is a
regression — but a local build stays workable. If a change needs a warning
suppressed, suppress it narrowly and say why.

`Directory.Build.props` carries the analyzer settings and NuGet audit config
(`NuGetAuditMode=all`, `NuGetAuditLevel=low`).

**CodeQL analyses the shipped library only.** `.github/codeql/codeql-config.yml`
declares the intent, but for a compiled language `paths-ignore` cannot exclude
code that was compiled — CodeQL analyses whatever the build extracts. So the
workflow builds `Dynamitey/Dynamitey.csproj` alone rather than the solution. The
test project deliberately does things static analysis must flag: dynamic calls
it believes cannot succeed, and casts like `(object)tOut` that look useless but
move a call from dynamic dispatch to the runtime type. If the CodeQL build step
is ever widened back to the solution, roughly thirty false positives return.

Dependabot covers NuGet and Actions weekly, grouping test and benchmark tooling
into one PR.

### Dependency upgrades that needed code changes

Recorded because the next person to hit them should not have to re-derive them:

- **NUnit 4** removes the classic assertions from `Assert` and moves them to
  `NUnit.Framework.Legacy.ClassicAssert`. The failure mode here is unusual: most
  assertions take `dynamic` arguments, so the compiler reports **CS1973**
  — "extension methods cannot be dynamically dispatched" — rather than a missing
  method. 291 call sites now use `ClassicAssert`. It also removed
  `AssertionHelper`, so `Helper` no longer derives from it and the four
  `Expect(actual, constraint)` sites became `Assert.That`.
- **IronPython 3** maps Python's `int` to `System.Numerics.BigInteger`, not
  `System.Int32`, because Python 3 integers are arbitrary precision. Embedded
  scripts must name .NET types explicitly — `System.Func[System.Int32,
  System.Boolean]`, never `System.Func[int, bool]` — or overload selection
  silently picks the wrong overload.

The suite is on NUnit 4 and uses the constraint model (`Assert.That(actual,
Is.EqualTo(expected))`) throughout; the one-time `ClassicAssert` migration was
#5. `NUnit.Analyzers` is referenced by `Tests.csproj` as a
`PrivateAssets="all"` dev dependency so any new `ClassicAssert` usage is
flagged (NUnit2005 and siblings) at build time — CI builds with
`-warnaserror`, so a reintroduced classic-model call fails the build.

## Versioning and releases

**The version is computed by GitVersion from git history and tags. There is no
version constant to edit.** `Version.props` used to hold one and is gone.

- `GitVersion.yml` at the repository root is the config: GitHubFlow workflow,
  `ContinuousDelivery` mode, and `next-version: 4.0.0` as the floor.
- `Dynamitey.csproj` references `GitVersion.MsBuild` with `PrivateAssets="all"`,
  so it is build-time only and does not reach consumers.
- A build on `main` produces `4.0.0-preview.N`. **A release is made by pushing a
  tag, not by editing a file.**

**4.0.0, not 3.0.4.** Upstream stopped at 3.0.3, and this tree drops `net40`,
which removes support for any consumer on .NET Framework 4.0. That is breaking,
so the major version moves. Recorded on #6.

Two consequences that will bite if forgotten:

- **CI checkouts need `fetch-depth: 0`.** GitVersion reads history and tags; the
  default shallow clone has neither and it fails rather than guessing. `ci.yml`
  and `codeql.yml` set it.
- **Building without a `.git` directory** — from a source archive rather than a
  clone — disables the task and falls back to `4.0.0-nogit` via
  `Directory.Build.props`. That fallback exists so a tarball build works, not as
  a version anyone should ship.

**Do not tag a release yet.** Publishing is blocked on the upstream maintainer's
reply (#8), and the `notify-on-close` obligation on the ported issues is
deliberately held until there is something installable.

## Architecture

Four projects: `Dynamitey/` (the library), `SupportLibrary/` (a fixture assembly
used to exercise cross-assembly access), `Tests/`, and `Benchmarks/`.

The public surface is mostly `Dynamitey/Dynamic.cs` — `InvokeMember`,
`InvokeGet`, `InvokeSet`, `InvokeConstructor` and friends. Everything else in
the root is a feature area: `Builder.cs`, `Expando.cs`, `Tupler.cs`,
`PartialApply.cs`, `FluentRegex.cs`, `InvokeContext.cs` (static vs instance
context), `InvokeArg.cs` (named arguments), `CacheableInvocation.cs`.
`DynamicObjects/` holds the proxy and forwarder types.

Two internals matter more than the file count, and both explain open bugs:

**Generated, arity-limited call sites.** `Internal/Optimization/InvokeHelper.cs`,
`InlineLambdas.cs`, and `ThisFunctions.cs` are **generated by T4 templates** —
the `.tt` files sit beside them. They emit call-site delegate types for each
argument count up to a fixed ceiling. Edit the `.tt`, not the `.cs`. That ceiling
is the suspected cause of #11, where `InvokeConstructor` fails above 14
arguments.

**Call-site caching.** `Internal/Optimization/BinderHash.cs` keys cached binders
and `CacheableInvocation.cs` exposes reuse deliberately. Cache state is shared
across call sites, which is why #13 reports behaviour that changes depending on
what ran earlier in the process. Any test touching static context must control
execution order or it will pass for the wrong reason.

`Internal/Compat/Net40.cs` survived the `net40` removal because its non-Framework
arm supplies `GetDefaultThreadCurrentCulture`, called from `Dynamic.cs:869`. The
filename is misleading; do not delete it on the strength of the name.

`Dynamitey/sn.snk` is upstream's strong-name key, committed. A renamed fork needs
its own — see #3.

## Scope reminders

The library still sets no `PackageId`, `AssemblyName`, or `RootNamespace`, so all
three default to `Dynamitey` and collide with the original package on nuget.org.
Its `Company` and `Copyright` still read `Ekon Benefits`. Neither is an oversight
to fix casually in passing — they are #3 and #6, both of which must land before
anything is ever published, and #6 carries an Apache-2.0 attribution question
(upstream's copyright is *retained*, not replaced).
