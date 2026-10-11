# Performance qualification

Run `dotnet run --project benchmarks/TimePeriod.Benchmarks --configuration Release -- --filter '*BookingIntervalBenchmarks*' --job short --exporters json markdown --artifacts artifacts/performance/benchmarks`.
Keep the BenchmarkDotNet environment, source commit, runtime, confidence/error values and allocated bytes with every claim.

Workloads compare 128 and 4096 elapsed slots and normalized exclusions. Legacy methods allocate materialized object collections; buffered methods reuse caller-owned output. Setup/allocation is intentionally excluded from the buffered measurement. Both paths return the same number of intervals; independent regression tests establish endpoint correctness.

Do not compare a machine's timings to another machine as an optimization ratio. Short runs are qualification evidence with visible uncertainty, not a release promise. Future work should profile large exclusion sets, calendar splitting and adversarial alias graphs before changing algorithms.

## Local qualification, 2026-10-11

[Full report](benchmarks/2026-10-11-macos-arm64.md), [raw results](benchmarks/2026-10-11-macos-arm64.json) and [exact source hashes](benchmarks/2026-10-11-source.json) retain the measured precommit working tree. Eight ShortRun workloads completed on .NET 10.0.12 / SDK 10.0.401, Apple M2 Pro / macOS arm64. Buffered slots and subtraction reported zero managed allocation per measured operation after caller buffers were allocated during setup. The materialized 4096-slot path allocated 196664 B; general-purpose subtraction allocated 1634260 B for that workload.

Timing confidence intervals were wide on this shared development machine, and one minimum-iteration warning was reported. Treat these measurements as allocation and execution qualification, not a stable speedup ratio. The scheduled/manual performance workflow records independently qualified CI receipts for its exact commit.
