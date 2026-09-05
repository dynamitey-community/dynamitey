# Dynamitey benchmarks

These measure Dynamitey's dispatch cost against the reflection it replaces.

They were previously NUnit tests in `Tests/SpeedTest.cs`, which asserted on
wall-clock durations — `Assert.Less(dynamiteyElapsed, reflectionElapsed)` after
a million iterations of each. That made them fail on any loaded machine and
skip when the two came within 1.4x of each other, so CI had to exclude the
whole category to stay green. They are benchmarks, and they now live in a
benchmark harness. See issue #9.

Nothing here asserts. These do not run in CI and cannot fail a build; the CI
workflow only compiles this project so it cannot rot.

## Running

Always in Release. BenchmarkDotNet refuses to run an unoptimised build.

```bash
dotnet run -c Release --project Benchmarks -- --list flat     # what exists
dotnet run -c Release --project Benchmarks                     # everything, slow
dotnet run -c Release --project Benchmarks -- --filter '*Tuple*'
```

In every class the reflection or `Activator` variant is the `Baseline`, so the
`Ratio` column reads directly as "how many times the cost of doing it the
normal way." Below 1.00 means Dynamitey is faster.

## What is measured

| Class | Compares |
| --- | --- |
| `PropertyGetAnonymous` | `Dynamic.InvokeGet` on an anonymous type vs `PropertyInfo.GetValue` |
| `PropertyGetPoco` | `CacheableInvocation` get vs `PropertyInfo.GetValue` |
| `PropertySet` | `Dynamic.InvokeSet` and cacheable set vs `PropertyInfo.SetValue` |
| `PropertySetNull` | the same, assigning null |
| `ConstructorOneArg` | `Tuple<string>` via Dynamitey vs `Activator.CreateInstance` |
| `ConstructorNoArg` | `List<string>`, including the generic `Activator` overload |
| `ConstructorValueType` | `DateTime`, a value type with three arguments |
| `MethodNoArgs` | `int.ToString()` |
| `MethodNullArg` | overload resolution when the argument is null |
| `MethodOverloadDoubleCall` | two different overloads through one call site |
| `MethodFourArgs` | `string.IndexOf` with four arguments |
| `MethodVoid` | a void method, `Dictionary.Clear` |
| `StaticPropertyGet` | `DateTime.Today` through a static context |
| `StaticMethodInvoke` | `DateTime.Parse` through a static context |
| `DelegateInvokeFunc` | `FastDynamicInvoke` vs `Delegate.DynamicInvoke` |
| `DelegateInvokeAction` | the same for an `Action` |
| `TupleIsTuple` | `Tupler.IsTuple` vs `FSharpType.IsTuple` |
| `TupleIndex` | `Tupler.Index` vs `FSharpValue.GetTupleField` |
| `TupleToList` | `Tupler.ToList` vs `FSharpValue.GetTupleFields` |
| `ListToTuple` | `Tupler.ToTuple` vs `FSharpType.MakeTupleType` + `MakeTuple` |

Each class also carries `[MemoryDiagnoser]`, so allocations per operation are
reported alongside time. The original tests measured only elapsed time.
