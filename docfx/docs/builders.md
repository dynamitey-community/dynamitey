# Builders and expandos

@Dynamitey.Builder constructs object graphs inline, in one expression, rather than by creating an object and assigning members one statement at a time.

## Expandos

The common case is building an `ExpandoObject`:

```csharp
using System.Dynamic;
using Dynamitey;

dynamic result = Expando.New(
    Test:  "test1",
    Test2: "Test 2nd");

var value = result.Test;    // "test1"
```

The member names come from the named arguments. There is no prototype type to declare and no sequence of assignments.

## The general form

`Expando.New` is a convenience over the general builder, which works for any type:

```csharp
var builder = Builder.New<ExpandoObject>();

dynamic result = builder.Object(
    Test:  "test1",
    Test2: "Test 2nd");
```

These two produce the same object — same values, same runtime type.

## Naming the thing you are building

Calling any member other than `Object` names the constructed object, which reads better for nested graphs:

```csharp
dynamic builder = new DynamicObjects.Builder<ExpandoObject>();

var robot = builder.Robot(
    LeftArm:  "Rise",
    RightArm: "Fall");
```

`Robot` is not a member that exists anywhere. The builder treats the name as a label, which is what makes a nested graph readable — each level says what it is rather than repeating `Object`.

## Why `Object` and `Single` are named that way

`IBuilder.Object` collides with a C# keyword, and so does `ILinq.Single`. Both are kept because they mirror the surface they stand in for — `Object` is the neutral name for "the thing being built", and `Single` mirrors `Enumerable.Single`. Renaming either to satisfy a naming analyzer would break the mirroring for no benefit.
