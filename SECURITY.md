# Security policy

## Reporting a vulnerability

**Do not open a public issue for a security problem.**

Report it through GitHub's private vulnerability reporting:

> [**Report a vulnerability**](https://github.com/dynamitey-community/dynamitey/security/advisories/new)

That creates a private advisory visible only to you and the maintainers. If the
link does not work for you, open a normal issue saying only that you have a
security report and would like a private channel — no details — and a
maintainer will open the advisory.

Useful things to include, as far as you have them: the version or commit, the
target framework, what an attacker can do, and a reproduction.

## What to expect

This project is maintained by volunteers, so these are honest intentions rather
than a contractual commitment:

| | |
| --- | --- |
| Acknowledgement | within 7 days |
| Initial assessment | within 30 days |
| Fix or a decision not to fix | depends on severity and complexity |

You will be credited in the advisory unless you ask not to be.

## Supported versions

**No version of this fork has been released.** Nothing here has been published
to NuGet, so there is no released artefact to patch — see
[#8](https://github.com/dynamitey-community/dynamitey/issues/8).

The `Dynamitey` package on nuget.org is the original project's, published by its
original maintainers. This project cannot issue fixes for it. A vulnerability
affecting that published package should be reported to
[`ekonbenefits/dynamitey`](https://github.com/ekonbenefits/dynamitey), not here.
Reports about the code in this repository are welcome regardless, and once this
project does publish, that is what this policy will cover.

## Scope

This library performs dynamic dispatch through the DLR. Two consequences are
worth stating plainly, because they are properties of the design rather than
defects:

- **Invoking members named at runtime is what the library does.** If an
  application passes attacker-controlled strings to `Dynamic.InvokeMember`,
  `InvokeGet`, `InvokeSet`, or `InvokeConstructor`, that application has given
  the attacker the ability to call arbitrary members on the target. That is a
  vulnerability in the calling application, not in this library. Treat member
  names as you would treat SQL: never build them from untrusted input.
- **The library is not trim-safe or AOT-safe and never will be.** Trimming a
  consuming application can remove members this library resolves at runtime,
  turning a working call into a runtime failure. Tracked as
  [#4](https://github.com/dynamitey-community/dynamitey/issues/4).

Reports that this library will invoke whatever member it is asked to invoke are
not vulnerabilities. Reports that it can be made to invoke something it was
*not* asked to invoke very much are.

## What is scanned

Every pull request runs CodeQL against the shipped library with the
`security-and-quality` queries, a dependency review that blocks known-vulnerable
dependencies, and `dotnet list package --vulnerable --include-transitive`.
Dependabot is enabled for NuGet and GitHub Actions.
