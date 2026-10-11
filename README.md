# ManagedCode.TimePeriodLibrary

[![.NET](https://github.com/managedcode/TimePeriodLibrary/actions/workflows/dotnet.yml/badge.svg)](https://github.com/managedcode/TimePeriodLibrary/actions/workflows/dotnet.yml)
[![Release](https://github.com/managedcode/TimePeriodLibrary/actions/workflows/release.yml/badge.svg)](https://github.com/managedcode/TimePeriodLibrary/actions/workflows/release.yml)
[![CodeQL](https://github.com/managedcode/TimePeriodLibrary/actions/workflows/codeql-analysis.yml/badge.svg)](https://github.com/managedcode/TimePeriodLibrary/actions/workflows/codeql-analysis.yml)
[![Performance](https://github.com/managedcode/TimePeriodLibrary/actions/workflows/performance.yml/badge.svg)](https://github.com/managedcode/TimePeriodLibrary/actions/workflows/performance.yml)
[![NuGet](https://img.shields.io/nuget/v/ManagedCode.TimePeriodLibrary.svg)](https://www.nuget.org/packages/ManagedCode.TimePeriodLibrary)
[![Downloads](https://img.shields.io/nuget/dt/ManagedCode.TimePeriodLibrary.svg)](https://www.nuget.org/packages/ManagedCode.TimePeriodLibrary)
[![License](https://img.shields.io/github/license/managedcode/TimePeriodLibrary.svg)](LICENSE.md)

Time ranges, calendar arithmetic and scheduling primitives for **.NET 10**. ManagedCode maintains the library with instance-based `TimeProvider` integration, UTC interval value types and caller-owned buffers alongside the established calendar and period APIs.

Use it to generate booking slots, subtract unavailable intervals, compare periods, work with business hours, or calculate calendar, fiscal, accounting and broadcast periods.

## Installation

```sh
dotnet add package ManagedCode.TimePeriodLibrary
```

The package, assembly and namespace are `ManagedCode.TimePeriodLibrary`. The library targets `net10.0` and is built with C# 14. Consumers need .NET 10; older target frameworks are not supported.

```csharp
using ManagedCode.TimePeriodLibrary;
```

## Choose the right time model

| Requirement | API | Boundary model |
| --- | --- | --- |
| Bookings and elapsed-time slots | `UtcInterval`, `FixedIntervalSlots` | UTC instants; inclusive start, exclusive end |
| Merge or subtract booking intervals | `IntervalOperations` | UTC instants and caller-owned spans |
| Split dates at calendar boundaries | `CalendarIntervalSplitter` | Gregorian `DateTimeKind.Unspecified` wall-clock dates |
| Current calendar periods | `TimeProviderCalendar` | Instance clock and calendar configuration |
| Mutable ranges and explicit open/closed edges | `TimeRange`, `TimeBlock`, `TimeInterval` | Established `DateTime` period model |
| Fiscal, accounting, broadcast and business calendars | Calendar classes and calculation tools | Explicit calendar rules and period mapping |

Keep wall-clock schedules and actual instants separate. A calendar day can have a different elapsed duration across a daylight-saving transition. Resolve invalid or ambiguous local times using your application's time-zone policy before constructing UTC booking intervals. Calendar arithmetic does not choose a policy for a missing or repeated local time.

## Generate booking slots

`UtcInterval` is a readonly value type. Its constructor normalizes `DateTimeOffset` boundaries to UTC and represents `[Start, End)`: an interval ending at 10:00 does not overlap one starting at 10:00. Empty intervals are permitted; reversed boundaries are rejected.

```csharp
using System;
using ManagedCode.TimePeriodLibrary;

var openingHours = new UtcInterval(
    new DateTimeOffset(2026, 10, 12, 9, 0, 0, TimeSpan.Zero),
    new DateTimeOffset(2026, 10, 12, 10, 0, 0, TimeSpan.Zero));
var slotDuration = TimeSpan.FromMinutes(15);

int required = checked((int)FixedIntervalSlots.GetCount(openingHours, slotDuration));
Span<UtcInterval> slots = stackalloc UtcInterval[required];
int written = FixedIntervalSlots.Write(openingHours, slotDuration, slots);

foreach (UtcInterval slot in slots[..written])
{
    Console.WriteLine($"{slot.Start:O} — {slot.End:O}");
}
// Four consecutive slots from 09:00 UTC to 10:00 UTC.
```

These slots measure elapsed time. By default, only full slots are emitted; a partial final slot requires the explicit inclusion option. Use a positive duration and obtain the required buffer size before writing. `stackalloc` is appropriate only for small, bounded inputs; use an array or a reused buffer for larger schedules. The returned count identifies the populated output slice.

## Normalize availability exclusions

`IntervalOperations.Normalize` sorts intervals in place and merges overlapping or touching ranges. It returns the number of normalized intervals; only that prefix is part of the result.

```csharp
using System;
using ManagedCode.TimePeriodLibrary;

var origin = new DateTimeOffset(2026, 10, 12, 9, 0, 0, TimeSpan.Zero);
Span<UtcInterval> unavailable = stackalloc UtcInterval[]
{
    new(origin.AddMinutes(45), origin.AddMinutes(60)),
    new(origin.AddMinutes(15), origin.AddMinutes(30)),
    new(origin.AddMinutes(30), origin.AddMinutes(45)),
};

int count = IntervalOperations.Normalize(unavailable);
foreach (UtcInterval interval in unavailable[..count])
{
    Console.WriteLine($"{interval.Start:O} — {interval.End:O}");
}
// One interval from 09:15 UTC to 10:00 UTC.
```

To remove unavailable time from a booking window, pass normalized exclusions to `IntervalOperations.GetSubtractionCount` and `IntervalOperations.Subtract`. Allocate or reuse an output buffer of the required size and keep it separate from the exclusions. Normalization mutates its input; copy it first when the original ordering and values must be preserved. A count operation lets callers validate capacity before writing results.

## Current time without a global clock

```csharp
using System;
using ManagedCode.TimePeriodLibrary;

var calendar = new TimeProviderCalendar(TimeProvider.System);
Day today = calendar.Day();

Console.WriteLine($"Today: {today.Start:yyyy-MM-dd}");
```

Supply an application-owned `TimeProvider` for deterministic tests or a configured local time zone. `TimeProviderClock` adapts a provider to the library's clock abstraction; `TimeProviderCalendar` creates current calendar periods from an instance provider. Prefer these instance APIs for new current-time code. Explicit-date constructors remain available when the caller already knows the date.

## Calendar periods and business rules

The established calendar API covers `Year`, `Halfyear`, `Quarter`, `Month`, `Week`, `Day`, `Hour` and `Minute`, plus their plural forms. Use `TimeCalendar` and `TimeCalendarConfig` to choose culture, year base month, week numbering and period-boundary offsets.

```csharp
using System;
using ManagedCode.TimePeriodLibrary;

var calendar = new TimeCalendar(new TimeCalendarConfig
{
    YearBaseMonth = YearMonth.October,
    YearWeekType = YearWeekType.Iso8601,
    YearType = YearType.FiscalYear,
});
var date = new DateTime(2026, 10, 12);
var fiscalYear = new Year(date, calendar);
var fiscalQuarter = new Quarter(date, calendar);

Console.WriteLine(fiscalYear.YearName);
Console.WriteLine(fiscalQuarter.QuarterOfYearName);
```

Calendar periods use the configured `ITimePeriodMapper`. The default `TimeCalendar` start offset is zero and end offset is minus one tick, so a day's mapped end precedes the next day. Preserve this distinction when moving between calendar periods and half-open `UtcInterval` values.

`CalendarIntervalSplitter.GetCount` and `CalendarIntervalSplitter.Write` split an explicit wall-clock range into Gregorian calendar segments using caller-owned buffers. Inputs use `DateTimeKind.Unspecified`; splitting does not perform time-zone or daylight-saving conversion. Use the count operation to size the destination and the write result to slice the populated entries.

| Task | Established APIs |
| --- | --- |
| Compare bounds, containment and overlap | `ITimePeriod`, `PeriodRelation`, `TimeCompare` |
| Define ranges by bounds or duration | `TimeRange`, `TimeBlock` |
| Control open and closed boundaries | `TimeInterval`, `IntervalEdge` |
| Hold or sequence periods | `TimePeriodCollection`, `TimePeriodChain` |
| Combine, intersect or subtract collections | `TimePeriodCombiner`, `TimePeriodIntersector`, `TimePeriodSubtractor` |
| Find unoccupied periods | `TimeGapCalculator` |
| Add time within allowed periods and working hours | `DateAdd`, `CalendarDateAdd` |
| Find matching calendar periods or working days | `CalendarPeriodCollector`, `DaySeeker` |
| Calculate accounting and broadcast calendars | `TimeCalendarConfig`, `FiscalCalendarTool`, `BroadcastYear`, `BroadcastMonth`, `BroadcastWeek` |

`TimePeriodChain` retains its child objects. The first child anchors the chain; reading bounds, indexed children or enumeration reconciles subsequent starts with each predecessor's end while retaining durations. Duplicate child identities and containment cycles are rejected. Account for this behavior when retaining mutable child references.

## Build and test

Run these commands from the repository root with the .NET 10 SDK:

```sh
dotnet restore ManagedCode.TimePeriodLibrary.slnx
dotnet format whitespace ManagedCode.TimePeriodLibrary.slnx --verify-no-changes --no-restore
dotnet build ManagedCode.TimePeriodLibrary.slnx --configuration Release --no-restore
dotnet test TimePeriodTests/TimePeriodTests.Core.csproj --configuration Release --no-build
```

The test suite uses xUnit and covers calendar boundaries, period relations, mutable containers and scheduling primitives. The [.NET workflow](https://github.com/managedcode/TimePeriodLibrary/actions/workflows/dotnet.yml) runs qualification on Linux and Windows; [CodeQL](https://github.com/managedcode/TimePeriodLibrary/actions/workflows/codeql-analysis.yml) provides security analysis. The [release workflow](https://github.com/managedcode/TimePeriodLibrary/actions/workflows/release.yml) qualifies and publishes the package.

## Performance

Buffered scheduling APIs let callers reuse storage and avoid materializing a period object for every result. They do not imply a universal throughput improvement: input size, normalization, buffer allocation and calendar rules all affect cost.

The [performance workflow](https://github.com/managedcode/TimePeriodLibrary/actions/workflows/performance.yml) retains benchmark artifacts. See [performance qualification](docs/performance.md) for workloads, commands and measurement limits. Compare results with the same runtime, hardware and buffer-allocation assumptions; retain allocated bytes and measurement uncertainty alongside timings.

## Manuals and resources

- [English manual (PDF)](docs/TimePeriod_en.pdf)
- [German manual (PDF)](docs/TimePeriod_de.pdf)
- [ManagedCode.TimePeriodLibrary on NuGet](https://www.nuget.org/packages/ManagedCode.TimePeriodLibrary)

The manuals document the established period and calendar APIs. Use the .NET 10 installation and namespace instructions above for this edition.

## Contributing

Report reproducible issues or propose focused changes in [ManagedCode.TimePeriodLibrary](https://github.com/managedcode/TimePeriodLibrary). Include explicit inputs, expected boundaries, calendar or time-zone configuration and the installed package version. Follow the repository's [maintenance and release plan](docs/maintenance-plan.md) when changing library behavior.

## Original authors and license

Released under the [MIT license](LICENSE.md). The original Time Period Library was created by **Jani Giannoudis / Itenso**: [Giannoudis/TimePeriodLibrary](https://github.com/Giannoudis/TimePeriodLibrary). Original copyright headers and the original license are retained. ManagedCode maintains this .NET 10 edition.
