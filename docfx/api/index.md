# API reference

Every public type and member in `Dynamitey.Community`, generated from the XML documentation comments in the source.

The `Dynamitey.Internal` namespace is deliberately excluded. Its types are public for reasons of DLR plumbing — generated call sites and cached binders must reach them across assembly boundaries — but they are not a supported surface and are not documented here. They remain tracked in `PublicAPI.Unshipped.txt`, so they cannot change silently; they are simply not part of the contract offered to consumers.

## Where to look first

| Type | What it does |
| --- | --- |
| @Dynamitey.Dynamic | The main entry point: `InvokeMember`, `InvokeGet`, `InvokeSet`, `InvokeConstructor`, `Curry`, `Linq`, and the conversion helpers |
| @Dynamitey.InvokeContext | Chooses the context a call binds in — static versus instance, and whose accessibility applies |
| @Dynamitey.InvokeArg | Passes a named argument to a dynamic call |
| @Dynamitey.CacheableInvocation | A reusable, pre-resolved invocation. See [Caching](../docs/caching.md) |
| @Dynamitey.Builder | Inline object-graph construction. See [Builders](../docs/builders.md) |
| @Dynamitey.Tupler | Runtime tuple creation and manipulation. See [Tuples](../docs/tuples.md) |
| @Dynamitey.DynamicObjects.LateType | A type resolved by name at runtime. See [Late types](../docs/late-types.md) |
