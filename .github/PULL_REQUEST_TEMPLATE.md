<!--
Thanks for contributing.

Keep one concern per pull request. If this is a dependency bump that also needs
source changes to compile, that is one concern and belongs together — but a bump
that additionally rewrites unrelated code is two.
-->

## What this changes

<!-- What and why. If you rejected an alternative approach, say which and why. -->

## Related issue

<!-- Closes #N, or "none" for something small and self-contained. -->

## Verification

<!--
What you actually ran, and what it said. Not "tests pass" — the numbers.

  dotnet build -c Release -warnaserror
  dotnet test Tests/Tests.csproj -c Release
-->

- [ ] `dotnet build -c Release -warnaserror` — 0 warnings, 0 errors
- [ ] `dotnet test Tests/Tests.csproj -c Release` — **186 passed, 0 failed, 0 skipped**, with no `--filter`
- [ ] New behaviour has a test, or this changes no behaviour

## Checks worth a second look

- [ ] Nothing here targets, links to, or pushes to the upstream repository. Links to upstream issues and pull requests are in backticks, not clickable — a clickable one posts a cross-reference onto their timeline.
- [ ] If a generated file changed (`InvokeHelper.cs`, `InlineLambdas.cs`, `ThisFunctions.cs`), the `.tt` template changed too and the output was regenerated rather than hand-edited.
- [ ] `netstandard2.0` still builds. It is kept deliberately and is not a simplification opportunity.
- [ ] No new assertion uses `ClassicAssert`. New tests use `Assert.That(actual, Is.EqualTo(expected))`.
