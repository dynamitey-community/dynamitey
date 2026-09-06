# AOT smoke test

Dynamitey is built on the DLR and **can never be trim-safe or AOT-safe**. This project
does not test that it works under NativeAOT. It pins **how it fails**.

That matters because two things in this repository make specific claims about those
failures — the `[RequiresUnreferencedCode]`/`[RequiresDynamicCode]` messages added in
issue #4, and the README section describing them. Documentation that quietly stops being
true is worse than no documentation.

## What it checks

1. **A NativeAOT publish produces IL warnings that name Dynamitey APIs.** Before the
   annotations, the same publish produced 382 warnings, all anonymous and inside
   Dynamitey internals, none naming a public API a consumer had called. Afterwards it
   produces around a dozen, at the consumer's own call sites. If that regresses, the
   annotations have stopped reaching consumers even though everything still compiles.

2. **The documented runtime failures still hold**, asserted by running the published
   native binary:

   | Case | Expected |
   | --- | --- |
   | `InvokeGet` on a public property | `RuntimeBinderException` claiming the member is absent |
   | `InvokeConstructor`, 5 args (generated 0–14 path) | `RuntimeBinderException` claiming no constructors |
   | `InvokeConstructor`, 20 args (Reflection.Emit path, #27) | `PlatformNotSupportedException`, message still mentioning `14` and `Reflection.Emit` |

   The first two are misleading on purpose — that is the point. `Simple` plainly declares
   `Name`. A consumer hitting that suspects their own code first, which is exactly why
   the build-time warnings matter. The third is the only honest one, and the test asserts
   its message stays actionable rather than merely present.

## Why a console app rather than an NUnit fixture

The thing under test is the **published native binary**. A test host would not itself be
AOT-compiled, so running these assertions under one would prove nothing. Exit code 0
means every case behaved as documented; non-zero names what changed.

## Why it is not in `Dynamitey.sln`

`PublishAot` pulls in the ILCompiler package, and nothing else in the repository should
pay that cost on an ordinary build. The `aot-smoke` CI job is the only thing that
compiles this project, which is also what stops it rotting.

## Running it locally

Needs a C toolchain (`clang`) for native linking.

```bash
dotnet publish AotSmokeTest -c Release -r linux-x64
./AotSmokeTest/bin/Release/net10.0/linux-x64/native/Dynamitey.AotSmokeTest
```

## What it does not cover

Only the surface it calls. An unannotated new API is caught by
`Tests/AotAnnotationTest.cs`, which inspects the public surface by reflection and so
catches it whether or not anything calls it — and runs everywhere without a NativeAOT
toolchain.
