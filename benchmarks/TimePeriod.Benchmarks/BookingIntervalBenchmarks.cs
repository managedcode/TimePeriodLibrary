using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace ManagedCode.TimePeriodLibrary.Benchmarks;

/// <summary>Runs reproducible elapsed-time slot and exclusion benchmarks.</summary>
public static class BenchmarkProgram
{
    /// <summary>Runs the requested benchmark workloads.</summary>
    /// <param name="args">BenchmarkDotNet arguments.</param>
    public static void Main(string[] args) => BenchmarkSwitcher.FromAssembly(typeof(BenchmarkProgram).Assembly).Run(args);
}

/// <summary>Compares allocating legacy results with caller-buffer UTC interval operations.</summary>
[MemoryDiagnoser]
public class BookingIntervalBenchmarks
{
    private UtcInterval range;
    private UtcInterval[] excluded = [];
    private UtcInterval[] destination = [];
    private TimePeriodCollection legacySources = new();
    private TimePeriodCollection legacyExclusions = new();

    /// <summary>Gets or sets the number of expected fixed-duration slots.</summary>
    [Params(128, 4096)]
    public int SlotCount { get; set; }

    /// <summary>Creates semantically equivalent, nonoverlapping finite workloads outside measurements.</summary>
    [GlobalSetup]
    public void Setup()
    {
        var start = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        range = new UtcInterval(start, start.AddMinutes(SlotCount * 15));
        destination = new UtcInterval[SlotCount + 1];
        excluded = new UtcInterval[SlotCount / 4];
        legacySources = new TimePeriodCollection { new TimeRange(range.Start.UtcDateTime, range.End.UtcDateTime) };
        legacyExclusions = new TimePeriodCollection();
        for (int i = 0; i < excluded.Length; i++)
        {
            var first = start.AddMinutes((i * 4 + 1) * 15);
            excluded[i] = new UtcInterval(first, first.AddMinutes(15));
            legacyExclusions.Add(new TimeRange(excluded[i].Start.UtcDateTime, excluded[i].End.UtcDateTime));
        }
    }

    /// <summary>Allocates one legacy object per elapsed slot and a result list.</summary>
    /// <returns>The materialized slot count.</returns>
    [Benchmark]
    public int LegacySlots()
    {
        var result = new List<TimeRange>(SlotCount);
        DateTime cursor = range.Start.UtcDateTime;
        for (int i = 0; i < SlotCount; i++)
        {
            DateTime end = cursor.AddMinutes(15);
            result.Add(new TimeRange(cursor, end));
            cursor = end;
        }

        return result.Count;
    }

    /// <summary>Writes the same elapsed slots into a reusable caller buffer.</summary>
    /// <returns>The written slot count.</returns>
    [Benchmark]
    public int BufferedSlots() => FixedIntervalSlots.Write(range, TimeSpan.FromMinutes(15), destination);

    /// <summary>Uses the inherited general-purpose subtraction kernel.</summary>
    /// <returns>The number of remaining intervals.</returns>
    [Benchmark]
    public int LegacySubtract() => new TimePeriodSubtractor<TimeRange>().SubtractPeriods(legacySources, legacyExclusions).Count;

    /// <summary>Uses pre-normalized UTC exclusions and a reusable destination buffer.</summary>
    /// <returns>The number of remaining intervals.</returns>
    [Benchmark]
    public int BufferedSubtract() => IntervalOperations.Subtract(range, excluded, destination);
}
