using System;

namespace ManagedCode.TimePeriodLibrary;

/// <summary>An immutable half-open interval of UTC instants, including an explicitly allowed empty interval.</summary>
/// <remarks>Input offsets are normalized. Reversed endpoints are rejected; equal endpoints are empty.</remarks>
public readonly struct UtcInterval : IEquatable<UtcInterval>, IComparable<UtcInterval>
{
    /// <summary>Creates an interval, normalizing both endpoints to UTC.</summary>
    /// <param name="start">Inclusive first instant.</param>
    /// <param name="end">Exclusive last instant, at or after start.</param>
    public UtcInterval(DateTimeOffset start, DateTimeOffset end)
    {
        if (end < start)
        {
            throw new ArgumentOutOfRangeException(nameof(end), "End must not precede start.");
        }

        Start = start.ToUniversalTime();
        End = end.ToUniversalTime();
    }

    /// <summary>Gets the inclusive UTC start.</summary>
    public DateTimeOffset Start { get; }

    /// <summary>Gets the exclusive UTC end.</summary>
    public DateTimeOffset End { get; }

    /// <summary>Gets elapsed duration, independent of wall-clock transitions.</summary>
    public TimeSpan Duration => End - Start;

    /// <summary>Gets whether the interval contains no instants.</summary>
    public bool IsEmpty => Start == End;

    /// <summary>Determines whether an instant lies at or after start and strictly before end.</summary>
    /// <param name="instant">The instant to test, in any offset.</param>
    /// <returns>True if the instant is contained.</returns>
    public bool Contains(DateTimeOffset instant) => instant >= Start && instant < End;

    /// <summary>Determines whether another nonempty interval shares any instants.</summary>
    /// <param name="other">The other interval.</param>
    /// <returns>True for positive-duration intersection; touching endpoints do not overlap.</returns>
    public bool Overlaps(UtcInterval other) => !IsEmpty && !other.IsEmpty && Start < other.End && other.Start < End;

    /// <summary>Obtains the positive-duration intersection, or an empty result if there is none.</summary>
    /// <param name="other">The other interval.</param>
    /// <param name="intersection">The intersection; default when the intervals do not overlap.</param>
    /// <returns>True when the intersection is nonempty.</returns>
    public bool TryIntersect(UtcInterval other, out UtcInterval intersection)
    {
        if (!Overlaps(other))
        {
            intersection = default;
            return false;
        }

        intersection = new UtcInterval(Start > other.Start ? Start : other.Start, End < other.End ? End : other.End);
        return true;
    }

    /// <summary>Compares intervals lexicographically by start, then end.</summary>
    /// <param name="other">The other interval.</param>
    /// <returns>A negative, zero, or positive comparison result.</returns>
    public int CompareTo(UtcInterval other)
    {
        int startComparison = Start.CompareTo(other.Start);
        return startComparison != 0 ? startComparison : End.CompareTo(other.End);
    }

    /// <summary>Compares normalized endpoints for equality.</summary>
    /// <param name="other">The other interval.</param>
    /// <returns>True when both endpoints match.</returns>
    public bool Equals(UtcInterval other) => Start == other.Start && End == other.End;

    /// <summary>Compares this interval with an object.</summary>
    /// <param name="obj">The object to compare.</param>
    /// <returns>True if the object is an equal interval.</returns>
    public override bool Equals(object obj) => obj is UtcInterval other && Equals(other);

    /// <summary>Gets a hash derived from both normalized endpoints.</summary>
    /// <returns>The interval hash.</returns>
    public override int GetHashCode() => HashCode.Combine(Start, End);

    /// <summary>Tests endpoint equality.</summary>
    /// <param name="left">First interval.</param>
    /// <param name="right">Second interval.</param>
    /// <returns>True for equal endpoints.</returns>
    public static bool operator ==(UtcInterval left, UtcInterval right) => left.Equals(right);

    /// <summary>Tests endpoint inequality.</summary>
    /// <param name="left">First interval.</param>
    /// <param name="right">Second interval.</param>
    /// <returns>True for different endpoints.</returns>
    public static bool operator !=(UtcInterval left, UtcInterval right) => !left.Equals(right);
}
