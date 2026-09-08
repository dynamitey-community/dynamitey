# Currying and partial application

@Dynamitey.Dynamic.Curry turns a function of several arguments into a chain of functions of one argument, applied as you supply them.

## Currying a delegate

```csharp
using Dynamitey;

Func<int, int, int> add = (x, y) => x + y;

var addFour = Dynamic.Curry(add)(4);
var result  = addFour(6);        // 10
```

`Dynamic.Curry(add)` gives back something you call with the first argument, which gives back something you call with the second.

## By name

Arguments can be supplied by parameter name rather than position, which lets you fix any argument rather than only the leftmost:

```csharp
Func<int, int, int> subtract = (x, y) => x - y;

var subtractSeven = Dynamic.Curry(subtract)(arg2: 7);
var result        = subtractSeven(arg1: 10);   // 3
```

Note the parameter names: for a `Func<,,>` they are `arg1` and `arg2`, the names on the delegate type itself, not names from your lambda. A lambda's parameter names exist only in your source; the delegate's are what the runtime sees.

## Converting back to a typed delegate

A curried function is dynamic, but it converts to a matching delegate type, so it can cross back into statically-typed code:

```csharp
Func<string, string, string> concat = (x, y) => x + y;

var curried  = Dynamic.Curry(concat)("4");
var typed    = (Func<string, string>)curried;
var result   = typed("10");        // "410"
```

Assignment works as well as a cast, including when the return type is a value type:

```csharp
Func<string, string, int> addStrings = (x, y) => int.Parse(x) + int.Parse(y);

var curried        = Dynamic.Curry(addStrings)("4");
Func<string, int> typed = curried;
var result         = typed("10");  // 14
```

## Partial application

@Dynamitey.PartialApply is the related operation: fix some arguments now, supply the rest later, without decomposing into single-argument steps.

Use currying when you want a chain of one-argument functions. Use partial application when you want to fix two of five arguments and keep a three-argument function.
