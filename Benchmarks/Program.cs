using System.Reflection;
using BenchmarkDotNet.Running;

// Run everything:      dotnet run -c Release --project Benchmarks
// Run one class:       dotnet run -c Release --project Benchmarks -- --filter *PropertyGet*
// List what exists:    dotnet run -c Release --project Benchmarks -- --list flat
BenchmarkSwitcher.FromAssembly(Assembly.GetExecutingAssembly()).Run(args);
