# Tuples

@Dynamitey.Tupler creates and inspects `System.Tuple` instances at runtime, without the arity being known at compile time.

## Creating

```csharp
using Dynamitey;

object tuple = Tupler.Create(1, "2", "3", 4);
```

That produces a genuine `Tuple<int, string, string, int>` — the same type `Tuple.Create(1, "2", "3", 4)` would give you, and equal to it. The element types come from the runtime types of the arguments.

## Beyond seven elements

`System.Tuple` tops out at seven elements plus a nested `TRest`. `Tupler.Create` handles the nesting for you:

```csharp
object tuple = Tupler.Create(1, "2", "3", 4, 5, 6, 7, "8", "9", 10, "11", 12);
```

which is exactly:

```csharp
new Tuple<int, string, string, int, int, int, int, Tuple<string, string, int, string, int>>(
    1, "2", "3", 4, 5, 6, 7, Tuple.Create("8", "9", 10, "11", 12));
```

Writing that by hand is where the arity limit stops being a curiosity and starts being a problem. `Tupler.Create` is the reason you do not have to.

## Inspecting

Because nesting is an implementation detail of `System.Tuple` rather than something callers care about, the accessors see through it:

| Member | Returns |
| --- | --- |
| `Tupler.Size(tuple)` | The logical element count, counting through nesting |
| `Tupler.Index(tuple, i)` | The element at a logical index |
| `Tupler.First(tuple)` | The first element |
| `Tupler.Second(tuple)` | The second element |
| `Tupler.Last(tuple)` | The last element, however deeply nested |
| `Tupler.ToList(tuple)` | Every element, flattened, as `IList<dynamic>` |
| `Tupler.IsTuple(target)` | Whether the target is a tuple at all |

So a twelve-element tuple reports `Size` of 12, not 8-with-a-nested-thing.

## From a sequence

```csharp
dynamic tuple = new object[] { 1, "two", 3.0 }.ToTuple();
```

`ToTuple` is an extension method on `IEnumerable`, so any sequence can become a tuple whose arity is decided by its length at runtime.
