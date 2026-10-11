# ManagedCode.TimePeriodLibrary

Maintained fork of Giannoudis/TimePeriodLibrary. Target only .NET 10 / C# 14.
Keep original copyright headers, MIT license and README attribution.

- `dotnet build ManagedCode.TimePeriodLibrary.slnx --configuration Release`
- `dotnet test TimePeriodTests/TimePeriodTests.Core.csproj --configuration Release --no-build`
- `dotnet format whitespace ManagedCode.TimePeriodLibrary.slnx --verify-no-changes --no-restore`
- `dotnet tool restore`; `dotnet tool run roslynk -- stdio`

Time calculations remain library-owned. Test every defect with a focused regression and
maintain independent invariants for boundaries, cultures, time zones and mutable inputs.
Use instance TimeProvider for current time; preserve legacy explicit-date APIs. Never
change global ClockProxy in concurrent tests or claim spans are faster without measurements.
Use spans/value types when measurements justify them. Retain benchmark source, runtime,
hardware, workloads and allocation receipts. Native clocks and caller dates are distinct.

Central package/version policy lives in Directory.Packages.props and Directory.Build.props.
Required release pipeline builds/tests the exact commit, packs the exact PackageId/version,
publishes to NuGet and then creates the GitHub release. Check the public feed before a
consumer update. Do not use temporary local packages. Never stash or discard concurrent work.

## Installed ManagedCode skills and semantic tooling

Project skills are installed with `dotnet skills install ... --agent codex --target .codex/skills`
from https://skills.managed-code.com/ (catalog-v2026.10.11.0). Use the narrowest installed skill:
dotnet/modern-csharp, format, code-analysis/analyzer-config, analyzer packages, xunit/run-tests,
coverlet, quality-ci and codeql. The library retains xUnit2/VSTest; Prostir's TUnit rule does not apply here.
Read the repository overlay for run-tests when present. Keep diagnostic output bounded and retain full artifacts.

Roslynk starts through `.codex/config.toml` and the checked-in tool manifest. Open the absolute
ManagedCode.TimePeriodLibrary.slnx and wait for Ready. Use semantic navigation/rename and code fixes for compiled C#.
The public namespace and assembly are ManagedCode.TimePeriodLibrary; preserve original copyright attribution.

## Quality and delivery

Apply whitespace formatting, then build with .NET10 analyzers, run focused regressions and the full suite,
collect coverage and inspect it. Retain legacy warning counts explicitly; never blanket suppress warnings
or describe a warning-bearing build as analyzer-clean. Security findings are errors. Benchmark representative
workloads with BenchmarkDotNet, retaining allocations, runtime, hardware and uncertainty.
CodeQL security-extended scans the exact library on pushes/PRs and weekly; review actionable results.
Release qualification includes Linux and Windows. Publish only the exact qualified net10 package;
verify NuGet's public payload (accounting for its signature) before creating the GitHub release or consuming it.
Review the final diff and tests for independent expectations, boundary behavior and compatibility.

## Feature layout

Keep each calculation feature and its interfaces/helpers together: Periods, PeriodCollections, PeriodAlgorithms/Timeline, Calendars (Gregorian, Fiscal, Broadcast), BusinessTime, Clocks, Formatting, Primitives and Scheduling. Tests mirror these features; shared fixtures belong in TestSupport. Preserve the public ManagedCode.TimePeriodLibrary namespace across feature folders. Physical organization must not create a second assembly or duplicate compiled types. Resource files remain at the project root to preserve their manifest names. The desktop-only timeline implementation remains excluded through its exact new path.
