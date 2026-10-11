using System;

namespace ManagedCode.TimePeriodLibrary;

/// <summary>Adapts an instance-scoped native clock to the legacy clock contract.</summary>
public sealed class TimeProviderClock : IClock
{
    /// <summary>Creates a clock using the supplied provider and its local time zone.</summary>
    /// <param name="timeProvider">The native current-time source.</param>
    public TimeProviderClock(TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        TimeProvider = timeProvider;
    }

    /// <summary>Gets the native current-time source.</summary>
    public TimeProvider TimeProvider { get; }

    /// <summary>Gets the provider's local wall-clock time, with unspecified kind.</summary>
    /// <remarks>A provider's local zone need not equal the machine's local zone.</remarks>
    public DateTime Now => TimeProvider.GetLocalNow().DateTime;

    /// <summary>Gets the current instant in UTC.</summary>
    public DateTimeOffset UtcNow => TimeProvider.GetUtcNow();
}
