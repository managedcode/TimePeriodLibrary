# ManagedCode.TimePeriodLibrary 10.0.0

The maintained package targets .NET 10 only and uses the ManagedCode.TimePeriodLibrary namespace and assembly.
Replace the older TimePeriodLibrary.NET package and update imports when migrating.

- Native instance TimeProvider clock and calendar factories.
- Half-open UTC value intervals, span-based normalization and subtraction, elapsed slots, and Gregorian wall-clock splitting.
- Year/week boundaries, excluded-day working hours, mutable chains, cyclic containers and subtraction correctness fixes.
- Mutable-chain value queries reconcile before searching or hashing after child mutation.
- Regression/invariant tests, XML IntelliSense, analyzer configuration, Roslynk, CodeQL and reproducible allocation/throughput benchmarks.
- Original Jani Giannoudis/Itenso credits and MIT license retained.

Calendar wall-clock splitting uses unspecified DateTime values and Monday-based weeks. It does not resolve time-zone gaps or folds.
Consumers own their time-zone/availability policy and pass resulting actual instants into UTC intervals.
