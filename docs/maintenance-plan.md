# .NET 10 maintenance and release

Creator-authorized scope, 2026-10-11: only .NET 10, package ManagedCode.TimePeriodLibrary
10.x, native TimeProvider, useful modern scheduling/value/span operations, meaningful
upstream defects, many focused/invariant tests, analyzers, Roslynk, benchmarks, badges,
GitHub/NuGet delivery and retained upstream attribution.

1. Record upstream baseline and reproduce issues with tests.
2. Modernize central build/test/tool configuration, using ManagedCode namespaces while retaining original attribution and license.
3. Repair applicable year/week, mutable-chain and recursive-container behavior.
4. Add instance-clock calendar operations and bounded allocation-conscious interval/slot APIs.
5. Qualify XML documentation, deterministic/randomized invariants, benchmarks and Roslynk.
6. Run final build, format and full tests; retain measured benchmark receipts.
7. Commit scoped changes, push, follow GitHub qualification/publication and verify NuGet bytes.
8. Update Prostir only during its coordinated source window; apply the published library to
   booking intervals/slots and let the CRM owner make its own approved planner adapter.

Baseline: managedcode/master and Giannoudis/master both 2a591ad218c8a61b6c2d2a9aa72ecee2c6b4f498.
Upstream #17 quarter fix is already present. #18/#6 are year-boundary week arithmetic;
#9 concerns external child mutation; #5 reports a self-containing collection; #19 requests
splitting; #2 requests usable XML documentation; #4 concerns maintenance rather than a bug.
No build/test/performance/publication claim is established by this plan.

## Qualification state

The final local .NET 10 solution build has zero errors; inherited analyzer warnings remain visible (11434 in the full rebuild). Roslynk also reports zero errors, with analyzer warning/information backlog retained. Formatting, 1057 regression/invariant cases and coverage are release gates. Eight measured precommit benchmark workloads and exact measured source hashes are retained under `docs/benchmarks`; the final CI performance workflow records its own exact-commit results. Linux and Windows qualification, CodeQL, the source-bound package, GitHub release and public NuGet payload are checked by the automatic release pipeline. Local evidence alone is not publication.
