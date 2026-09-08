# Migrating from the original Dynamitey

The original `Dynamitey` package on nuget.org is upstream's, at 3.0.3. This fork publishes as `Dynamitey.Community`. Moving between them is a one-line change.

## The change

```diff
- <PackageReference Include="Dynamitey" Version="3.0.3" />
+ <PackageReference Include="Dynamitey.Community" Version="4.0.0" />
```

No `using` directive changes. No source edits.

## Why the package changed name but the namespace did not

The package ID and assembly name are both `Dynamitey.Community`. The **root namespace is still `Dynamitey`**, and is pinned explicitly so it cannot drift.

That split solves a specific problem. Two assemblies claiming one identity resolve to a coin flip that surfaces as a runtime `MissingMethodException` — a failure that appears at load time, in production, with no compile-time warning. Renaming the assembly makes the collision impossible.

Keeping the namespace means your source does not move. The public API is unchanged by the rename: it is frozen by `PublicAPI.Shipped.txt` and `PublicAPI.Unshipped.txt`, and those files were **byte-identical** before and after. That is how the claim was checked rather than assumed.

## If you end up with both packages

The most likely route is **ImpromptuInterface**, which carries a compile-time reference to the original `Dynamitey`. A project referencing both gets a compile-time error rather than a silent runtime failure:

```
error CS0433: The type 'Invocation' exists in both
  'Dynamitey.Community, Version=4.0.0.0, ...' and 'Dynamitey, Version=3.0.3.0, ...'
```

Resolve it by aliasing the one you are not using directly:

```xml
<PackageReference Include="Dynamitey" Version="3.0.3" Aliases="upstream" />
```

`Aliases` is honored on a direct `PackageReference` and ignored on a transitive one, so the package has to be declared explicitly for this to take effect.

## What actually changed in 4.0.0

The major version moved because `net40` was dropped, which removes support for any consumer on .NET Framework 4.0. That is a breaking change, so 4.0.0 rather than 3.0.4.

| | |
| --- | --- |
| Target frameworks | `netstandard2.0;net10.0` — `net40` dropped |
| Public API | Unchanged apart from additions |
| Trimming and AOT | The public surface now carries `[RequiresUnreferencedCode]` and `[RequiresDynamicCode]`, so a trimmed or AOT project gets build-time warnings instead of runtime failures |
| Bug fixes | Including `InvokeConstructor` above 14 arguments, static-field `InvokeGet`, and `Dynamic.Linq` enumeration |

If you are on .NET Framework 4.0, stay on 3.0.3. `netstandard2.0` reaches .NET Framework 4.6.1 and later.
