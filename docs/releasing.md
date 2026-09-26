# Releasing

How a release of **`Dynamitey.Community`** is cut.

## Status

The package id is `Dynamitey.Community`. The original `Dynamitey` package on
nuget.org stays upstream's. This repository never pushes a package whose id is
`Dynamitey`.

The 2026-09-05 NuGet "Contact owners" message went unanswered through
2026-09-26. That is the deadline recorded on #8, and it is why 4.0.0 publishes
under this id rather than upstream's. #8 is the authority for that decision;
this document describes mechanics.

A version tag runs `.github/workflows/release.yml`, which builds, tests, packs,
checks that the package id is `Dynamitey.Community`, and pushes via NuGet
Trusted Publishing. `workflow_dispatch` does the same work and does not push.
There is no NuGet API key in the repository secrets.

## The version comes from git, not from a file

There is no version constant to edit. `Version.props` used to hold one and was
deleted.

- `GitVersion.yml` at the repository root is the configuration: GitHubFlow
  workflow, `ContinuousDelivery` mode, `next-version: 4.0.0` as the floor.
- `Dynamitey.csproj` references `GitVersion.MsBuild` with `PrivateAssets="all"`,
  so it is build-time only and never reaches a consumer.
- A build on `main` produces `4.0.0-preview.N`. A branch build produces its own
  prerelease label.

**A release is made by pushing a tag, not by editing a file.**

Two consequences that bite if forgotten:

- **Every checkout needs `fetch-depth: 0`.** GitVersion reads history and tags,
  and the default shallow clone has neither — it fails rather than guessing.
- **Building without a `.git` directory** — from a source archive rather than a
  clone — disables the task and falls back to `4.0.0-nogit`. That exists so a
  tarball build works, not as a version anyone should ship.

## Why 4.0.0 and not 3.0.4

Upstream stopped at 3.0.3, and this tree drops `net40`, removing support for any
consumer on .NET Framework 4.0. That is breaking, so the major version moves.
Recorded on #6.

## What the package carries

Set in `Dynamitey/Dynamitey.csproj`:

| Property | Why |
| --- | --- |
| `PackageId` / `AssemblyName` = `Dynamitey.Community` | The assembly identity must differ from upstream's, or a project referencing both resolves to a coin flip that surfaces as a runtime `MissingMethodException`. `RootNamespace` deliberately stays `Dynamitey` so a consumer swaps one `PackageReference` line and rebuilds with no source change |
| `IncludeSymbols` + `SymbolPackageFormat=snupkg` | Both are required. `IncludeSymbols` alone produces the legacy `.symbols.nupkg`, which nuget.org **rejects on push** |
| `PublishRepositoryUrl`, `EmbedUntrackedSources` | Source Link. Lets a consumer step into this library's real sources, fetched from GitHub on demand. `Microsoft.SourceLink.GitHub` needs no `PackageReference` — the .NET SDK has bundled it since .NET 8 |
| `ContinuousIntegrationBuild`, CI only | Normalizes source paths, which Source Link requires but which makes a local build's paths useless for local debugging. Conditioned on `GITHUB_ACTIONS` |

`IncludeSource` was removed rather than kept alongside these. It produced a
legacy `.source.nupkg`, a pre-Source-Link mechanism no modern debugger looks
for.

## Verifying packaging locally

```bash
dotnet pack Dynamitey/Dynamitey.csproj -c Release -o /tmp/pack
```

Expect exactly two files: a `.nupkg` and a `.snupkg`. A `.symbols.nupkg` or a
`.source.nupkg` means the properties above have regressed.

To confirm Source Link actually landed, check that the commit is embedded in the
package metadata:

```bash
unzip -o -q /tmp/pack/Dynamitey.Community.*.nupkg -d /tmp/pack/nupkg
grep -oE '<repository[^>]*>' /tmp/pack/nupkg/Dynamitey.Community.nuspec
```

That should print a `<repository …>` element carrying the commit SHA. The symbol
package's PDB should also contain a `raw.githubusercontent.com` URL pointing at
that same SHA.

## The Release workflow

`.github/workflows/release.yml`. A version tag publishes. A manual run from the
Actions tab does not.

Before the push, the workflow requires exactly one
`Dynamitey.Community.*.nupkg`, rejects every other nupkg in the artifact
directory, and checks that the nuspec `<id>` is `Dynamitey.Community`. The push
argument is that filename, not a wildcard over every nupkg. The `.snupkg` is
pushed alongside it.

## Cutting a release

In order:

1. **Trusted Publishing is already required.** On nuget.org, the policy names
   repository owner `dynamitey-community`, repository `dynamitey`, workflow file
   `release.yml`, and no Actions environment. Its package scope is
   `Dynamitey.Community` only. Do not use `Dynamitey` or `Dynamitey*`: the first
   is the original package, and the second matches it. The repository variable
   `NUGET_USER` is the nuget.org profile name that owns the policy. It is not a
   secret and it is not an API key.
2. **Land the commit on `main`.**
3. **Tag it and push the tag.** `4.0.0`, or `v4.0.0`. GitVersion accepts either.
   That runs Release. Non-version tags such as `upstream-baseline` do not match
   the trigger.
4. **Discharge the `notify-on-close` obligations** once `Dynamitey.Community` is
   actually on nuget.org. Six ported issues carry that label. #11 has **two**
   people on it, not one.

   ```bash
   gh issue list --label notify-on-close --state all
   ```

   The messages are drafted in `docs/release-notifications.md`. Add the `@` when
   sending. The drafts name `Dynamitey.Community` as the package to install.
5. **Close #8 and #10.** #10 is the roadmap. #95 stays open; it is the 5.0.0
   targeting change and says so in the issue.

## Standing constraints

- **Never push to `ekonbenefits`** — not a branch, not a tag, not a pull
  request. The `upstream` remote's push URL is set to `DISABLED` deliberately.
- **Never publish a package whose id is `Dynamitey`.** The id is
  `Dynamitey.Community`.
- **Do not store a NuGet API key** in the repository secrets. Trusted Publishing
  is the only publish path.
- **Do not move or delete the `upstream-baseline` tag.** It marks the last
  purely-upstream commit, which is what the Apache-2.0 "state your changes"
  requirement points at.
- **Do not pin the executed test count** in this or any other document. The bar
  is 0 failed, 0 skipped, with no category filter.
