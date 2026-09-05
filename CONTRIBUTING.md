# Contributing

Thanks for looking. This is a community continuation of a dormant project, so
contributions are genuinely how it moves.

## Before you start

- **Small fixes:** just open a pull request.
- **Anything larger:** open an issue first. That is not bureaucracy — several
  areas here have decisions pending that are not obvious from the code, and it
  would be a shame to write something that collides with one.
- The [roadmap](https://github.com/dynamitey-community/dynamitey/issues/10) is
  the sequencing authority and says what is planned in what order.
- Issues labelled [`good first issue`](https://github.com/dynamitey-community/dynamitey/labels/good%20first%20issue)
  are self-contained and have their evidence written down.

## Things that will surprise you

Worth reading before you change anything, because none of it is visible from
the file you happen to be editing.

**Some source files are generated.** `Internal/Optimization/InvokeHelper.cs`,
`InlineLambdas.cs` and `ThisFunctions.cs` come from the `.tt` T4 templates
sitting beside them. Edit the template, not the output.

**`Internal/Compat/Net40.cs` is not dead.** The name says `net40`, which was
removed, but the file's other branch supplies `GetDefaultThreadCurrentCulture`
and is called from `Dynamic.cs`.

**`netstandard2.0` is deliberate.** It is the only target framework reaching
both .NET Framework and modern .NET from one assembly, and upstream has 53
dependent packages. Please do not "simplify" it away.

**Call-site caching is shared across sites.** `BinderHash.cs` and
`CacheableInvocation.cs` mean behaviour can depend on what ran earlier in the
process — see
[#13](https://github.com/dynamitey-community/dynamitey/issues/13). A test
touching static context must control its own ordering or it will pass for the
wrong reason.

**Never push to the upstream repository.** The `upstream` remote here is
fetch-only by design. Please also avoid *clickable* links to upstream issues or
pull requests in comments and commit messages: GitHub posts a cross-reference
onto their timeline. Put them in backticks.

## Building and testing

Requires the .NET 10 SDK.

```bash
dotnet restore
dotnet build -c Release -warnaserror
dotnet test Tests/Tests.csproj -c Release
```

**The full suite must pass with no category filter — 0 failed, 0 skipped.** If
you find yourself adding a `--filter` to make it green, something is wrong. The
wall-clock benchmarks that used to need one now live in `Benchmarks/` and assert
nothing.

`-warnaserror` matches CI. The tree builds clean with .NET analyzers at
`AnalysisLevel=latest`, so a new warning is a regression rather than a backlog
item.

## Tests

New behaviour needs a test. For the ported upstream bugs
([`ported-from-upstream`](https://github.com/dynamitey-community/dynamitey/labels/ported-from-upstream)),
a failing test that reproduces the report is a genuinely useful contribution on
its own, even without a fix — several of those issues have sat for years with
no reproduction anyone could run.

The suite uses NUnit 4. Most assertions currently go through
`NUnit.Framework.Legacy.ClassicAssert` because 291 call sites have not been
converted yet ([#5](https://github.com/dynamitey-community/dynamitey/issues/5)).
**New tests should use the constraint model** — `Assert.That(actual,
Is.EqualTo(expected))` — rather than adding to the pile.

## Versioning

Do not set a version anywhere. GitVersion computes it from git history and tags,
configured by `GitVersion.yml`; there is no version constant in the tree.

If you build from a source archive rather than a clone, GitVersion has no history
to read and the build falls back to `4.0.0-nogit`. That is expected, and it is
why a clone is better for anything you intend to test.

## Pull requests

- Branch from `main`. It is protected: no force pushes, linear history, and
  seven required status checks.
- Keep one concern per pull request. A dependency bump that also rewrites 291
  assertions is not reviewable.
- Write the commit message for someone reading it in five years without the
  context. Say what changed and why the alternative was rejected.
- CI must be green, and review conversations must be resolved before merge.
  Copilot reviews automatically; disagreeing with it is fine, but reply with the
  reasoning rather than resolving silently.

There is no CLA. Contributions are under Apache-2.0, the same as the project.

## Licence

By contributing you agree your work is licensed under the Apache License 2.0.
See [License.txt](License.txt) and [NOTICE](NOTICE). Do not add code you did not
write or that carries an incompatible licence — this project's attribution chain
back to the original authors matters, and is recorded in NOTICE.
