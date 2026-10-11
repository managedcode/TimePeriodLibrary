using System;

namespace ManagedCode.TimePeriodLibrary;

/// <summary>Gregorian wall-clock boundaries for calendar splitting.</summary>
public enum CalendarIntervalUnit
{
    /// <summary>January 1 boundaries.</summary>
    Year,
    /// <summary>January, April, July and October boundaries.</summary>
    Quarter,
    /// <summary>First-of-month boundaries.</summary>
    Month,
    /// <summary>Monday midnight boundaries.</summary>
    Week,
    /// <summary>Midnight boundaries.</summary>
    Day,
    /// <summary>Whole-hour boundaries.</summary>
    Hour,
    /// <summary>Whole-minute boundaries.</summary>
    Minute,
    /// <summary>Whole-second boundaries.</summary>
    Second,
}

/// <summary>An immutable half-open Gregorian wall-clock interval with unspecified date kind.</summary>
/// <remarks>This value has no time-zone or instant semantics and makes no daylight-saving assumptions.</remarks>
public readonly struct WallClockInterval : IEquatable<WallClockInterval>
{
    /// <summary>Creates a wall-clock interval, allowing equal endpoints.</summary>
    /// <param name="start">Inclusive unspecified-kind Gregorian start.</param>
    /// <param name="end">Exclusive unspecified-kind Gregorian end, at or after start.</param>
    public WallClockInterval(DateTime start, DateTime end)
    {
        if (start.Kind != DateTimeKind.Unspecified || end.Kind != DateTimeKind.Unspecified)
        {
            throw new ArgumentException("Calendar splitting requires unspecified-kind wall-clock dates.");
        }

        if (end < start)
        {
            throw new ArgumentOutOfRangeException(nameof(end));
        }

        Start = start;
        End = end;
    }

    /// <summary>Gets the inclusive wall-clock start.</summary>
    public DateTime Start { get; }

    /// <summary>Gets the exclusive wall-clock end.</summary>
    public DateTime End { get; }

    /// <summary>Gets whether the interval is empty.</summary>
    public bool IsEmpty => Start == End;

    /// <summary>Compares both wall-clock endpoints.</summary>
    /// <param name="other">The other interval.</param>
    /// <returns>True when both endpoints match.</returns>
    public bool Equals(WallClockInterval other) => Start == other.Start && End == other.End;

    /// <summary>Compares this interval with an object.</summary>
    /// <param name="obj">The object to compare.</param>
    /// <returns>True when it is an equal wall-clock interval.</returns>
    public override bool Equals(object obj) => obj is WallClockInterval other && Equals(other);

    /// <summary>Gets the endpoint hash.</summary>
    /// <returns>The endpoint hash.</returns>
    public override int GetHashCode() => HashCode.Combine(Start, End);

    /// <summary>Tests endpoint equality.</summary>
    /// <param name="left">First interval.</param>
    /// <param name="right">Second interval.</param>
    /// <returns>True for equal endpoints.</returns>
    public static bool operator ==(WallClockInterval left, WallClockInterval right) => left.Equals(right);

    /// <summary>Tests endpoint inequality.</summary>
    /// <param name="left">First interval.</param>
    /// <param name="right">Second interval.</param>
    /// <returns>True for different endpoints.</returns>
    public static bool operator !=(WallClockInterval left, WallClockInterval right) => !left.Equals(right);
}

/// <summary>Splits Gregorian wall-clock intervals at aligned calendar boundaries without time-zone conversion.</summary>
/// <remarks>Uses Gregorian years/months and Monday weeks. Partial first/last segments are retained.
/// For elapsed UTC durations, use <see cref="FixedIntervalSlots"/> instead.</remarks>
public static class CalendarIntervalSplitter
{
    /// <summary>Calculates the required capacity in constant time, without enumerating segments.</summary>
    /// <param name="interval">Gregorian wall-clock interval.</param>
    /// <param name="unit">The aligned boundary unit.</param>
    /// <returns>The number of nonempty segments, potentially larger than a span.</returns>
    public static long GetCount(WallClockInterval interval, CalendarIntervalUnit unit)
    {
        ValidateUnit(unit);
        if (interval.IsEmpty)
        {
            return 0;
        }

        // Last included tick makes an exact end boundary belong only to the preceding segment.
        DateTime last = interval.End.AddTicks(-1);
        return GetBucket(last, unit) - GetBucket(interval.Start, unit) + 1;
    }

    /// <summary>Writes nonempty contiguous segments covering the interval exactly.</summary>
    /// <param name="interval">Gregorian wall-clock interval.</param>
    /// <param name="unit">The aligned calendar boundary unit.</param>
    /// <param name="destination">Output buffer; validated before writing.</param>
    /// <returns>The number of written segments.</returns>
    /// <exception cref="ArgumentException">Output capacity is insufficient.</exception>
    public static int Write(WallClockInterval interval, CalendarIntervalUnit unit, Span<WallClockInterval> destination)
    {
        long count = GetCount(interval, unit);
        if (count > destination.Length)
        {
            throw new ArgumentException("Destination is too small.", nameof(destination));
        }

        DateTime cursor = interval.Start;
        for (int i = 0; i < (int)count; i++)
        {
            // No boundary addition is needed for the last bucket, including DateTime.MaxValue.
            DateTime end = i == (int)count - 1 ? interval.End : NextBoundary(cursor, unit);
            destination[i] = new WallClockInterval(cursor, end);
            cursor = end;
        }

        return (int)count;
    }

    private static void ValidateUnit(CalendarIntervalUnit unit)
    {
        if (unit < CalendarIntervalUnit.Year || unit > CalendarIntervalUnit.Second)
        {
            throw new ArgumentOutOfRangeException(nameof(unit));
        }
    }

    private static long GetBucket(DateTime value, CalendarIntervalUnit unit) => unit switch
    {
        CalendarIntervalUnit.Year => value.Year - 1L,
        CalendarIntervalUnit.Quarter => (value.Year - 1L) * 4 + (value.Month - 1) / 3,
        CalendarIntervalUnit.Month => (value.Year - 1L) * 12 + value.Month - 1,
        _ => value.Ticks / GetUnitTicks(unit),
    };

    private static DateTime NextBoundary(DateTime value, CalendarIntervalUnit unit) => unit switch
    {
        CalendarIntervalUnit.Year => new DateTime(value.Year + 1, 1, 1),
        CalendarIntervalUnit.Quarter => new DateTime(value.Year, ((value.Month - 1) / 3) * 3 + 1, 1).AddMonths(3),
        CalendarIntervalUnit.Month => new DateTime(value.Year, value.Month, 1).AddMonths(1),
        _ => new DateTime((value.Ticks / GetUnitTicks(unit) + 1) * GetUnitTicks(unit)),
    };

    private static long GetUnitTicks(CalendarIntervalUnit unit) => unit switch
    {
        CalendarIntervalUnit.Week => TimeSpan.TicksPerDay * 7,
        CalendarIntervalUnit.Day => TimeSpan.TicksPerDay,
        CalendarIntervalUnit.Hour => TimeSpan.TicksPerHour,
        CalendarIntervalUnit.Minute => TimeSpan.TicksPerMinute,
        CalendarIntervalUnit.Second => TimeSpan.TicksPerSecond,
        _ => throw new ArgumentOutOfRangeException(nameof(unit)),
    };
}
