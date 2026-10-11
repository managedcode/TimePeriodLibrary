using System;

namespace ManagedCode.TimePeriodLibrary;

/// <summary>Creates legacy calendar periods from an instance-scoped current-time source.</summary>
/// <remarks>Explicit-date constructors remain independent of the clock. Each factory call samples once.</remarks>
public sealed class TimeProviderCalendar
{
    private readonly TimeProviderClock clock;

    /// <summary>Creates a factory using the provider's local time zone and the supplied calendar.</summary>
    /// <param name="timeProvider">The current-time source.</param>
    /// <param name="calendar">Calendar rules; null selects the existing default calendar.</param>
    public TimeProviderCalendar(TimeProvider timeProvider, ITimeCalendar calendar = null)
    {
        clock = new TimeProviderClock(timeProvider);
        Calendar = calendar ?? new TimeCalendar();
    }

    /// <summary>Gets the calendar used by every period factory.</summary>
    public ITimeCalendar Calendar { get; }

    /// <summary>Gets the current local wall-clock time with unspecified kind.</summary>
    public DateTime Now => clock.Now;

    /// <summary>Creates the year containing the provider's current local time.</summary>
    public Year Year() => new Year(Now, Calendar);

    /// <summary>Creates the halfyear containing the provider's current local time.</summary>
    public Halfyear Halfyear() => new Halfyear(Now, Calendar);

    /// <summary>Creates the quarter containing the provider's current local time.</summary>
    public Quarter Quarter() => new Quarter(Now, Calendar);

    /// <summary>Creates the month containing the provider's current local time.</summary>
    public Month Month() => new Month(Now, Calendar);

    /// <summary>Creates the week containing the provider's current local time.</summary>
    public Week Week() => new Week(Now, Calendar);

    /// <summary>Creates the day containing the provider's current local time.</summary>
    public Day Day() => new Day(Now, Calendar);

    /// <summary>Creates the hour containing the provider's current local time.</summary>
    public Hour Hour() => new Hour(Now, Calendar);

    /// <summary>Creates the minute containing the provider's current local time.</summary>
    public Minute Minute() => new Minute(Now, Calendar);
}
