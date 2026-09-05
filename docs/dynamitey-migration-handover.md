# Handover: Migrate Dynamitey to dynamitey-community

> **Status: completed on 2026-09-05. This is an archival record, not a runbook.**
>
> Every step below has been carried out. **Do not re-run any of it** — the
> repository exists, the history is pushed, and the `upstream-baseline` tag is
> in place at `c44f5c5`. Re-running Step 1 would fail, and Step 4 would attempt
> to push over a repository that is no longer empty.
>
> It is kept because it records *why* this is a detached fork rather than a
> GitHub network fork, and because the hard constraints in it still apply —
> in particular that nothing may ever be pushed to `ekonbenefits`.
>
> Three things did not go exactly as written, and the deviations are recorded
> in the pull request that established the baseline:
>
> - Step 4 needed the documented fallback push; 22 hidden `refs/pull/` refs made
>   a mirror push impossible.
> - Step 6 failed on first attempt. GitHub made a stray feature branch the
>   default of the empty repository, so the clone checked out the wrong branch
>   and reported 225 commits instead of 229.
> - Step 9's command order is wrong as written: `main` has to exist on the
>   remote before it can be set as the default branch.
>
> Current state of the build has moved on from Step 7 and Step 8 — see
> `CLAUDE.md` for what the targets and test counts are now.

**For:** Claude Code, running in WSL
**Scope:** Move `ekonbenefits/dynamitey` into `dynamitey-community/dynamitey` with full history and no fork-network relationship, then establish a build baseline. Nothing else.

---

## Context

Dynamitey is an Apache-2.0 licensed .NET library that has been dormant since its 3.0.3 release in November 2023. We are standing up a community continuation in the `dynamitey-community` GitHub org, which already exists and is empty.

This is a *detached* fork, not a GitHub network fork. A network fork would disable issues by default, hide the repo from search, and cause contributor PRs to default to targeting `ekonbenefits`. So we mirror-clone and push to a fresh repo instead. Full git history is preserved deliberately: under Apache-2.0 it is the attribution trail, and it lets us cherry-pick from upstream later.

The GitHub fork that previously existed at `AtwoodTM/dynamitey` has been deleted. Do not recreate it.

---

## Hard constraints

**Never push to `ekonbenefits`.** Not a branch, not a tag, not a PR. This repo has issue creation restricted by its owner and an outreach message is currently pending a reply. An accidental push would be a serious problem. Step 5 sets the upstream push URL to `DISABLED` for exactly this reason. Do not undo that.

**Do not force-push anything.**

**Do not** publish a NuGet package, reserve a package ID, rename the assembly or root namespace, change target frameworks, or edit source files. Those are separate decisions that follow this migration.

**Do not** add pre-commit hooks, `prek` config, or a commit-message convention. The FCG `type/KEY: message` format with DEV/PLAT Jira keys does not apply to a public OSS repo.

**Stop and ask** at any point marked **CHECKPOINT**. Do not improvise past a failed verification.

---

## Variables

| Name | Value |
|---|---|
| `ORG` | `dynamitey-community` |
| `REPO` | `dynamitey` |
| `UPSTREAM` | `https://github.com/ekonbenefits/dynamitey.git` |
| `WORKDIR` | Ask the user. `~/repos` unless told otherwise. |

---

## Step 0 — Prerequisites

```bash
gh auth status
git --version
dotnet --list-sdks
git config --get user.name
git config --get user.email
```

**CHECKPOINT.** Report all five results before continuing.

- `gh auth status` must show an account with write access to the `dynamitey-community` org.
- If no .NET 10 SDK is present, install it and stop for confirmation before proceeding:
  ```bash
  wget https://dot.net/v1/dotnet-install.sh -O ~/dotnet-install.sh
  chmod +x ~/dotnet-install.sh
  ~/dotnet-install.sh --channel 10.0
  echo 'export PATH="$HOME/.dotnet:$PATH"' >> ~/.bashrc && source ~/.bashrc
  ```
- Report the configured git identity verbatim. Every commit and release from here carries it, and the user has not yet confirmed which handle he wants on this project. **Do not change it. Just report it.**

---

## Step 1 — Create the empty target repo

The repo must be created with no README, no license file, and no `.gitignore`. A mirror push to a non-empty repo fails.

```bash
gh repo create "$ORG/$REPO" \
  --public \
  --description "Community-maintained continuation of ekonbenefits/dynamitey"
```

Verify it is empty:

```bash
gh api "repos/$ORG/$REPO" --jq '{size, default_branch, fork}'
```

**CHECKPOINT.** `fork` must be `false`. If `size` is non-zero, stop and report.

---

## Step 2 — Mirror-clone upstream

```bash
cd "$WORKDIR"
git clone --mirror "$UPSTREAM" dynamitey-mirror.git
cd dynamitey-mirror.git
```

---

## Step 3 — Inspect before pushing

```bash
git rev-list --count master
git for-each-ref --format='%(refname)' | wc -l
git for-each-ref --format='%(refname)' | grep -c '^refs/pull/' || true
git tag | head -20
```

**CHECKPOINT.** Expected: 229 commits on `master`.

- If the commit count differs from 229, report the actual number and stop. Upstream may have moved, which is itself significant news.
- If the `refs/pull/` count is greater than 0, note it. GitHub rejects writes to hidden refs, so you must use the fallback push in Step 4.

---

## Step 4 — Push to the new repo

Primary:

```bash
git push --mirror "https://github.com/$ORG/$REPO.git"
```

If that fails with a hidden-ref error such as `deny updating a hidden ref`, use this instead. It pushes branches and tags only:

```bash
git push "https://github.com/$ORG/$REPO.git" \
  'refs/heads/*:refs/heads/*' 'refs/tags/*:refs/tags/*'
```

If both fail, stop and report the full error. Do not force-push.

---

## Step 5 — Create the working clone

```bash
cd "$WORKDIR"
rm -rf dynamitey-mirror.git
git clone "https://github.com/$ORG/$REPO.git"
cd "$REPO"
git remote add upstream "$UPSTREAM"
git remote set-url --push upstream DISABLED
git fetch upstream
git remote -v
```

**CHECKPOINT.** The `git remote -v` output must show `DISABLED` as the upstream push URL. If it does not, fix it before doing anything else.

---

## Step 6 — Verify parity and tag the baseline

```bash
git rev-list --count HEAD
git log --oneline -3
git tag upstream-baseline
git push origin upstream-baseline
```

**CHECKPOINT.** The commit count must match what Step 3 reported.

The `upstream-baseline` tag marks the last commit that is purely upstream's work. Everything after it is attributable to this project, which is what the Apache-2.0 "state your changes" requirement points at. Do not move or delete this tag.

---

## Step 7 — Survey the build

Do not modify anything. Report findings only.

```bash
grep -rn "TargetFramework" --include='*.csproj' --include='*.props' .
cat Directory.Build.props
cat Version.props
ls .github/workflows/
```

**CHECKPOINT.** Report:

1. Every TFM found, and which project declares it.
2. The TFMs of the test project specifically.
3. The workflow files present and what each targets.

The user needs the test project's TFMs before deciding how tests get run. Expect to find `net40` and `netstandard2.0` on the library, and something older on the tests.

---

## Step 8 — Baseline build, netstandard2.0 only

```bash
dotnet restore
dotnet build Dynamitey/Dynamitey.csproj -f netstandard2.0 -c Release
```

**The `net40` leg is expected to fail on WSL.** Compiling .NET Framework targets on Linux requires `Microsoft.NETFramework.ReferenceAssemblies`, and running net4x tests requires Mono. **Do not fix this and do not install Mono.** The `net40` target is being dropped in later work, so the problem resolves itself. Build the `netstandard2.0` leg only.

If the `netstandard2.0` build fails, report the full error output and stop. That is a real problem worth understanding before any modernization work starts.

Then attempt the tests, using the TFM identified in Step 7:

```bash
dotnet test Tests/Tests.csproj -f <tfm-from-step-7> -c Release
```

Report pass and fail counts. Upstream's committed test results show 182 passing tests, so that is the rough number to expect. **Do not fix failing tests.** They are the behavior-parity baseline and the user needs to see them as-is.

---

## Step 9 — Default branch (ask first)

Upstream uses `master`. There are no open PRs on the new repo, so renaming is free. **Ask the user before doing this.** If confirmed:

```bash
gh repo edit "$ORG/$REPO" --default-branch main
git branch -m master main
git push origin main
git push origin --delete master
```

---

## Done means

- `dynamitey-community/dynamitey` exists, is public, `fork: false`, and has the full upstream history.
- `upstream-baseline` tag is pushed.
- Local clone has `origin` writable and `upstream` fetch-only with push `DISABLED`.
- The `netstandard2.0` build succeeds.
- Test results reported, unmodified.
- A written summary of Step 7's findings.

Nothing has been renamed, retargeted, published, or otherwise changed from upstream's code.

---

## What comes next (not in scope)

For context only, so you understand why the baseline matters. Do not start any of it.

1. Drop `net40`, move to `netstandard2.0;net10.0`. .NET 8 and .NET 9 both reach end of support on 2026-11-10; .NET 10 is LTS through November 2028.
2. Rename package ID, assembly, and root namespace so the fork can coexist with the original. Upstream has 53 dependent packages, so assembly identity collisions are a real risk.
3. Migrate the deprecated NUnit asserts.
4. Add trim and AOT analyzer annotations. This library is DLR-based and will never be trim-safe or AOT-safe, so the public surface should carry `[RequiresUnreferencedCode]` and `[RequiresDynamicCode]` to give consumers build-time warnings instead of runtime failures.
5. README, NOTICE, SECURITY.md, CI, NuGet Trusted Publishing via OIDC.

Publishing is gated on a pending reply from the upstream maintainer. Do not publish anything.
