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
dotnet test Tests/Tests.csproj -c Release   # no filter, must be 186/186
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

There are **219** `[Test]`/`[TestCase]` attributes in `Tests/*.cs`, and **186**
tests execute. The difference is not lost coverage:

| | |
| --- | --- |
| Attributes in source | 219 |
| Inside `#if NETFRAMEWORK` (`TestCodeDomLateTypeBind`, CodeDom) | −1 |
| Retired with the `SpeedTest` fixture, now `Benchmarks/` | −32 |
| **Execute** | **186** |

CI runs the full suite with **no category filter** and requires 186 passed, 0
failed, 0 skipped. If a filter ever reappears in a test command, something has
gone backwards — the wall-clock benchmarks that used to require one no longer
live in the test project. See #9 and #23.

## Continuous integration

Three workflows, all pinned to current action majors:

| Workflow | Does |
| --- | --- |
| `ci.yml` | Build and test on Linux, macOS, Windows; `-warnaserror`; TRX artifacts; dry-runs every benchmark |
| `codeql.yml` | `security-and-quality` queries, manual build mode, PRs and weekly |
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

Dependabot covers NuGet and Actions weekly, grouping test and benchmark tooling
into one PR. A Dependabot PR bumping NUnit past 3.x will fail until #5 is done —
NUnit 4 removes the classic asserts this suite uses 323 times.

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
