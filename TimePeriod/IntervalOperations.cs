using System;

namespace ManagedCode.TimePeriodLibrary;

/// <summary>Allocation-conscious interval set operations over caller-owned spans.</summary>
public static class IntervalOperations
{
    /// <summary>Sorts and merges overlaps and touching endpoints in place, discarding empty intervals.</summary>
    /// <param name="intervals">Mutable input; the normalized set occupies its returned-length prefix.</param>
    /// <returns>The number of normalized intervals. The remaining buffer contents are unspecified.</returns>
    public static int Normalize(Span<UtcInterval> intervals)
    {
        intervals.Sort();
        int count = 0;
        foreach (UtcInterval interval in intervals)
        {
            if (interval.IsEmpty)
            {
                continue;
            }

            if (count > 0 && interval.Start <= intervals[count - 1].End)
            {
                UtcInterval previous = intervals[count - 1];
                if (interval.End > previous.End)
                {
                    intervals[count - 1] = new UtcInterval(previous.Start, interval.End);
                }
            }
            else
            {
                intervals[count++] = interval;
            }
        }

        return count;
    }

    /// <summary>Subtracts an ordered nonoverlapping exclusion set, writing remaining positive-duration intervals.</summary>
    /// <param name="source">The interval to subtract from.</param>
    /// <param name="exclusions">Sorted nonempty exclusions; touching endpoints are accepted.</param>
    /// <param name="destination">Output buffer, which must not overlap exclusions.</param>
    /// <returns>The number of written intervals.</returns>
    /// <exception cref="ArgumentException">Input is not normalized, spans overlap, or the output is too small.</exception>
    /// <remarks>Validation and capacity checks precede writing. Neither source nor exclusions are modified.</remarks>
    public static int Subtract(UtcInterval source, ReadOnlySpan<UtcInterval> exclusions, Span<UtcInterval> destination)
    {
        Validate(exclusions);
        if (exclusions.Overlaps(destination))
        {
            throw new ArgumentException("Exclusion and destination spans must not overlap.", nameof(destination));
        }

        int required = SubtractCore(source, exclusions, default, false);
        if (required > destination.Length)
        {
            throw new ArgumentException("Destination is too small.", nameof(destination));
        }

        return SubtractCore(source, exclusions, destination, true);
    }

    /// <summary>Calculates the output capacity needed to subtract normalized exclusions.</summary>
    /// <param name="source">The source interval.</param>
    /// <param name="exclusions">Sorted nonempty nonoverlapping exclusions.</param>
    /// <returns>The required number of output elements.</returns>
    /// <exception cref="ArgumentException">Exclusions are not sorted, overlap, or contain empty intervals.</exception>
    public static int GetSubtractionCount(UtcInterval source, ReadOnlySpan<UtcInterval> exclusions)
    {
        Validate(exclusions);
        return SubtractCore(source, exclusions, default, false);
    }

    private static void Validate(ReadOnlySpan<UtcInterval> intervals)
    {
        for (int i = 0; i < intervals.Length; i++)
        {
            if (intervals[i].IsEmpty || (i > 0 && intervals[i].Start < intervals[i - 1].End))
            {
                throw new ArgumentException("Exclusions must be sorted, nonempty, and nonoverlapping.", nameof(intervals));
            }
        }
    }

    private static int SubtractCore(UtcInterval source, ReadOnlySpan<UtcInterval> exclusions, Span<UtcInterval> destination, bool write)
    {
        DateTimeOffset cursor = source.Start;
        int count = 0;
        foreach (UtcInterval exclusion in exclusions)
        {
            if (exclusion.End <= cursor)
            {
                continue;
            }

            if (exclusion.Start >= source.End)
            {
                break;
            }

            if (exclusion.Start > cursor)
            {
                if (write)
                {
                    destination[count] = new UtcInterval(cursor, exclusion.Start);
                }

                count = checked(count + 1);
            }

            cursor = exclusion.End < source.End ? exclusion.End : source.End;
            if (cursor == source.End)
            {
                break;
            }
        }

        if (cursor < source.End)
        {
            if (write)
            {
                destination[count] = new UtcInterval(cursor, source.End);
            }

            count = checked(count + 1);
        }

        return count;
    }
}
