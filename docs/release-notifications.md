# Notifications sent for the ported upstream issues

Six issues were carried over from `ekonbenefits/dynamitey` when this fork was
created. Each carries the **`notify-on-close`** label. The original reporter
was owed one message when there was a package to install. Those messages have
been sent. The text below is the record of what went out.

**Sent on 2026-09-26**, after `Dynamitey.Community` 4.0.0 was on nuget.org.
The comments are on #11, #12, #13, #14, #15, and #16. Do not send them again.

## When these went out

They waited until `Dynamitey.Community` 4.0.0 was on nuget.org. The package id
in every message is `Dynamitey.Community`. The original `Dynamitey` package is
upstream's and is not what these fixes ship in.

The reason is deliberate. These people did not ask for this fork, and each is
worth interrupting exactly once. A message saying "fixed on `main`, but there
is nothing you can install" spends that one interruption on news nobody can act
on. A message pointing at a package they can actually use does not.

The two "could not reproduce" messages ask the reporter for a failing case.
They went out with the others, on the same day.

The names below are written without `@`. That is deliberate. This file must
not notify anyone. The posted comments added the `@`. #11 went to both
`jdh28` and `jbtule`. Every message names `Dynamitey.Community`, not the
original `Dynamitey` package.

---

## Fixed — four messages

### #11 → `jdh28` and `jbtule` *(two people)*

> Your report from 2014 about `Dynamic.InvokeConstructor` throwing
> `InvalidCastException` above 14 arguments is fixed, in the community
> continuation at `dynamitey-community/dynamitey`.
>
> The cause was not a hard arity limit; it was a return-type mismatch. `InvokeHelper.tt`
> generates a case per argument count up to 14; anything above that fell to a
> hand-written branch that built the call site with `typeof(TTarget)` where it
> should have used `typeof(TReturn)`. For a constructor `TTarget` is
> `System.Type`, so the call site expected a `Type` back and got the constructed
> object instead — exactly the exception you saw.
>
> `jdh28` — your follow-up was also covered. You said the suggested workaround
> did not handle `MyClass(string firstArg, params string[] otherArgs)`, and it
> did not. There is now a test for that shape specifically, alongside 14, 15, 16
> and 20 argument cases.
>
> Released as `Dynamitey.Community` 4.0.0. The original `Dynamitey` package is unchanged.

### #12 → `PiotrZierhoffer`

> Your 2014 report — `Dynamic.InvokeGet` reaching an instance field but not a
> static one — is fixed in the community continuation at
> `dynamitey-community/dynamitey`.
>
> You were right that it was a real asymmetry rather than a usage error. Neither
> DLR binder shape could reach a static field at all: `InvokeMember("get_" +
> name)` only ever finds a property accessor method, and a field has none. That
> held even for a public static field on a public type.
>
> The fix falls back to reflection on the failure path only, so the fast path is
> untouched, and it is gated on the caller's context so it cannot read members a
> restricted context should not see.
>
> Released as `Dynamitey.Community` 4.0.0. The original `Dynamitey` package is unchanged.

### #13 → `tpluscode`

> Your 2018 report about `InvokeGet` with a static context failing on nested
> private classes is fixed in the community continuation at
> `dynamitey-community/dynamitey`.
>
> Your own diagnosis was the key to it. You noticed the behaviour depended on
> execution order — that calling `WithStaticContext` first changed what happened
> afterwards. That was correct, and it was the actual bug: `Binder.GetMember`
> with `IsStaticType` cannot bind a static member on a cold type, and only
> appeared to work once some other call had warmed the shared binder cache. Your
> "set first, then get" observation was that warming, not a fix.
>
> Your unmerged pull request upstream was read while working on this. It was on
> the right track and its `[INCOMPLETE!]` tag was fair — the approach it took
> also relied on the warming.
>
> A related bug you ran into from the other direction is fixed too: getting and
> then setting the same static property in one process used to fail with
> "cannot explicitly call operator or accessor".
>
> Released as `Dynamitey.Community` 4.0.0. The original `Dynamitey` package is unchanged.

### #16 → `fmichellonet`

> Your 2022 report about `Dynamic.InvokeMember` throwing on an async method of
> an internal class is fixed in the community continuation at
> `dynamitey-community/dynamitey`. **Your original code needs no changes.**
>
> It took reproducing your exact setup — `Azure.Data.Tables` 12.5.0 against the
> storage emulator — to find it, and it was worth doing, because the cause was
> not where it appeared to be. The invocation was never the problem. It succeeds
> and hands back a perfectly good `Task<T>`. The **`await`** was failing:
> awaiting a `dynamic` compiles into dynamic `GetAwaiter`/`IsCompleted`/
> `GetResult` calls that the C# runtime binder resolves in *your* assembly's
> accessibility context, and `ResponseWithHeaders<…>` is internal to
> `Azure.Core`. The binder cannot produce a value of a type you cannot see, so
> `GetResult` bound to a void-returning form.
>
> `InvokeMember` now detects that case and returns a wrapper whose await pattern
> is entirely public, so your line works unchanged. A `Task<T>` with a visible
> `T` is returned exactly as before.
>
> Released as `Dynamitey.Community` 4.0.0. The original `Dynamitey` package is unchanged.

---

## Could not reproduce — two messages

These ask for a failing case. Both say the issue can be reopened.

### #14 → `jjxtra`

> Your 2020 report about a generic method call failing was carried into the
> community continuation at `dynamitey-community/dynamitey`, and we could not
> reproduce it on current code.
>
> Rather than close it on a shrug, the natural code path for your description
> was covered from scratch. Nothing in the project tested explicit generic
> arguments — `InvokeMemberName(name, typeof(T))` — which is what you reach for
> when inference cannot resolve the type argument, exactly the case the
> maintainer's reply carved out. There are now 16 tests across: return-type-only
> generics, arguments giving inference nothing to work with, multiple type
> parameters where only some are inferable, `class`/`new()`/base-type
> constraints, generic methods on generic types, value versus reference type
> arguments, `params`, static methods, non-public types, `InvokeMemberAction`
> and `CacheableInvocation` — each checked against `MakeGenericMethod` directly,
> which is the workaround you used.
>
> All of them pass. If you still have the case that failed, please reopen with
> it — the coverage means a genuine regression would now be caught immediately.

### #15 → `StefH`

> Your 2021 report — `'System.ValueType' does not contain a definition for
> 'GetAwaiter'` when awaiting a `ValueTask<T>`-returning method — was carried
> into the community continuation at `dynamitey-community/dynamitey`, and we
> could not reproduce it.
>
> What was tried: genuinely-async versus precompleted `ValueTask<T>`, `Task<T>`
> for comparison, awaiting directly versus assigning to a typed local first —
> against both current code and a from-scratch build of the original 3.0.3-era
> source, loaded into `net8.0` and `net10.0` hosts. Every combination worked.
>
> The error implies the *static* type was `System.ValueType`, and current
> binding does not produce that: the boxed result keeps its `ValueTask<T>`
> runtime type, which is what `await` resolves `GetAwaiter` from.
>
> Given the report predates several .NET and Roslyn releases, the most likely
> explanation is that it was fixed outside this library. But that is inference,
> not proof. **If you can still produce a failing case, please reopen** — a
> closely related bug on the same theme (#16, awaiting a result whose type is
> inaccessible to the caller) turned out to be entirely real once the reporter's
> exact dependency was stood up, so this one is not being dismissed lightly.
