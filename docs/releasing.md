# Releasing

How a release is cut, and what currently prevents one.

## Status: releases are blocked

**Nothing publishes today.** An outreach message went to the upstream maintainer
on 2026-09-05 through the NuGet "Contact owners" form on the `Dynamitey`
package — upstream has issue creation restricted, so its own tracker was not
available. Until that is answered, or until **2026-09-26** passes without a
reply, this project publishes no package and reserves no package ID.

Issue #8 records the deadline, the reasoning behind it, and what happens either
way. It is the authority; this document describes mechanics.

The `Release` workflow reflects that block in its shape rather than in a
comment: it is `workflow_dispatch` only, it has no tag trigger, it holds no
credential, and it contains no push step at all.

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

`.github/workflows/release.yml`, run manually from the Actions tab. It restores,
builds with `-warnaserror`, runs the full suite with no category filter, packs
both target frameworks, asserts the package shape, and uploads the `.nupkg` and
`.snupkg` as build artifacts.

It publishes nothing. Run it freely — it is the dry run.

## Cutting a real release, once #8 unblocks

In order:

1. **Confirm the block has lifted** — a reply arrived and the path is agreed, or
   2026-09-26 passed. Update #8, `README.md`, `CLAUDE.md` and
   `docs/dynamitey-migration-handover.md`, all of which state the block.
2. **Configure Trusted Publishing** on the NuGet account for this repository.
   Not an API key in a repository secret: OIDC exchanges a short-lived GitHub
   Actions token for a scoped, short-lived NuGet credential, so no durable
   secret exists to leak, rotate or inherit. This repository arrived carrying
   two inherited publish credentials — an encrypted MyGet key in
   `.appveyor.yml` and a `GITHUB_TOKEN` publish step aimed at another
   organization's feed — so that is not a hypothetical concern.
3. **Add the push step** to `release.yml`, following the comment block that
   already sits where it goes: add `id-token: write` to the workflow
   permissions, exchange the OIDC token, then
   `dotnet nuget push artifacts/*.nupkg`. The `.snupkg` is pushed alongside it
   automatically.
4. **Add the tag trigger**, so a release is cut by pushing a tag rather than by
   a manual dispatch. Deliberately absent until step 2 exists, because a tag
   trigger on a workflow that can publish turns `git tag` into a release.
5. **Tag `4.0.0`** and push the tag.
6. **Discharge the `notify-on-close` obligations.** Six ported issues carry that
   label and their original reporters have deliberately never been contacted —
   they get told once, when there is something installable, not when a commit
   lands on `main`. #11 has **two** people on it, not one.

   ```bash
   gh issue list --label notify-on-close --state all
   ```

   The messages are already drafted in `docs/release-notifications.md`.
7. **Close #8 and #10.** #10 is the roadmap and has nowhere to go after 4.0.0
   ships.

## Standing constraints

Not up for casual revision, and all of them predate this document:

- **Never push to `ekonbenefits`** — not a branch, not a tag, not a pull
  request. The `upstream` remote's push URL is set to `DISABLED` deliberately.
- **Do not publish a package or reserve a package ID** until #8 clears.
- **Do not move or delete the `upstream-baseline` tag.** It marks the last
  purely-upstream commit, which is what the Apache-2.0 "state your changes"
  requirement points at.
- **Do not pin the executed test count** in this or any other document. The bar
  is 0 failed, 0 skipped, with no category filter.
