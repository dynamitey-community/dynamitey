# Late types

@Dynamitey.DynamicObjects.LateType wraps a type resolved by name at runtime, so you can use a type from an assembly you have no reference to as though you did.

## Basic use

```csharp
using Dynamitey.DynamicObjects;

dynamic type = new LateType("System.Text.RegularExpressions.Regex, System.Text.RegularExpressions");

dynamic instance = type.@new(@"\d+");
```

The string is an assembly-qualified type name. Constructing an instance goes through `@new`, escaped because `new` is a keyword.

## When the type is not there

This is the part worth understanding before you rely on it. A `LateType` for a type that cannot be found does **not** throw at construction. It throws when you use it:

```csharp
dynamic late = new LateType("Nonexistent.Type, Nonexistent.Assembly");

// Constructing the LateType succeeded. This is where it fails:
var value = late.AnyMember;   // throws LateType.MissingTypeException
```

Check first when the type is genuinely optional:

```csharp
var late = new LateType("Optional.Type, Optional.Assembly");

if (late.IsAvailable)
{
    // safe to use
}
```

That two-stage behavior is deliberate. It lets you construct a `LateType` unconditionally at startup and decide later whether the feature it represents is present, rather than wrapping construction in a `try`.

## Resolving a type directly

`LateType.FindType` is the underlying lookup, and returns `null` rather than throwing:

```csharp
Type? resolved = LateType.FindType("Some.Type, Some.Assembly");
```

Every resolution failure is reported as `null`, not only the "not found" case — a malformed name, a missing or unloadable assembly, and a bad image all yield `null`. If you need to know *why* resolution failed, this is not the API for it.
