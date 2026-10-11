using System;

namespace ManagedCode.TimePeriodLibrary;

/// <summary>Finite UTC elapsed-duration slots in caller-owned buffers.</summary>
public static class FixedIntervalSlots
{
    /// <summary>Calculates the count without enumerating or allocating slots.</summary>
    /// <param name="interval">Finite source interval.</param>
    /// <param name="duration">Strictly positive elapsed slot duration.</param>
    /// <param name="includePartial">Whether to retain a shorter final slot.</param>
    /// <returns>The slot count, which can exceed a span's maximum length.</returns>
    public static long GetCount(UtcInterval interval, TimeSpan duration, bool includePartial = false)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(duration.Ticks, 0);
        long ticks = interval.Duration.Ticks;
        long count = ticks / duration.Ticks;
        return includePartial && ticks % duration.Ticks != 0 ? count + 1 : count;
    }

    /// <summary>Writes consecutive nonoverlapping slots beginning at the interval's inclusive start.</summary>
    /// <param name="interval">Finite source interval.</param>
    /// <param name="duration">Strictly positive elapsed slot duration.</param>
    /// <param name="destination">Caller-owned output; capacity is checked before any write.</param>
    /// <param name="includePartial">Whether to retain a shorter final slot.</param>
    /// <returns>The number of written slots.</returns>
    /// <exception cref="ArgumentException">The destination cannot hold every requested slot.</exception>
    public static int Write(UtcInterval interval, TimeSpan duration, Span<UtcInterval> destination, bool includePartial = false)
    {
        long count = GetCount(interval, duration, includePartial);
        if (count > destination.Length)
        {
            throw new ArgumentException("Destination is too small.", nameof(destination));
        }

        DateTimeOffset cursor = interval.Start;
        for (int i = 0; i < (int)count; i++)
        {
            long remaining = (interval.End - cursor).Ticks;
            DateTimeOffset end = cursor.AddTicks(Math.Min(duration.Ticks, remaining));
            destination[i] = new UtcInterval(cursor, end);
            cursor = end;
        }

        return (int)count;
    }
}
