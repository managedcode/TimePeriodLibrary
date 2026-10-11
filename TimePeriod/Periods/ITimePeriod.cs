// -- FILE ------------------------------------------------------------------
// name       : ITimePeriod.cs
// project    : Itenso Time Period
// created    : Jani Giannoudis - 2011.02.18
// language   : C# 4.0
// environment: .NET 2.0
// copyright  : (c) 2011-2012 by Itenso GmbH, Switzerland
// --------------------------------------------------------------------------
using System;

namespace ManagedCode.TimePeriodLibrary;


// ------------------------------------------------------------------------
/// <summary>Describes effective time bounds, duration and temporal relations.</summary>
public interface ITimePeriod
{

    // ----------------------------------------------------------------------
    /// <summary>Gets whether the lower bound is not the unbounded minimum sentinel.</summary>
    bool HasStart { get; }

    // ----------------------------------------------------------------------
    /// <summary>Gets the effective lower bound.</summary>
    DateTime Start { get; }

    // ----------------------------------------------------------------------
    /// <summary>Gets whether the upper bound is not the unbounded maximum sentinel.</summary>
    bool HasEnd { get; }

    // ----------------------------------------------------------------------
    /// <summary>Gets the effective upper bound.</summary>
    DateTime End { get; }

    // ----------------------------------------------------------------------
    /// <summary>Gets the duration between the effective bounds.</summary>
    TimeSpan Duration { get; }

    // ----------------------------------------------------------------------
    /// <summary>Gets duration formatted by the default formatter.</summary>
    string DurationDescription { get; }

    // ----------------------------------------------------------------------
    /// <summary>Gets whether duration is zero.</summary>
    bool IsMoment { get; }

    // ----------------------------------------------------------------------
    /// <summary>Gets whether both bounds are unbounded.</summary>
    bool IsAnytime { get; }

    // ----------------------------------------------------------------------
    /// <summary>Gets whether mutations are rejected.</summary>
    bool IsReadOnly { get; }

    // ----------------------------------------------------------------------
    /// <summary>Calculates duration using the supplied provider.</summary>
    TimeSpan GetDuration(IDurationProvider provider);

    // ----------------------------------------------------------------------
    /// <summary>Sets both bounds according to the implementation's ordering and mutation rules.</summary>
    void Setup(DateTime newStart, DateTime newEnd);

    // ----------------------------------------------------------------------
    /// <summary>Tests equality of effective bounds rather than reference identity.</summary>
    bool IsSamePeriod(ITimePeriod test);

    // ----------------------------------------------------------------------
    /// <summary>Tests whether a moment lies inside the effective bounds, including endpoints.</summary>
    bool HasInside(DateTime test);

    // ----------------------------------------------------------------------
    /// <summary>Tests whether another period is fully contained.</summary>
    bool HasInside(ITimePeriod test);

    // ----------------------------------------------------------------------
    /// <summary>Tests whether any point is shared, including a touching endpoint.</summary>
    bool IntersectsWith(ITimePeriod test);

    // ----------------------------------------------------------------------
    /// <summary>Tests whether positive-duration time is shared; touching endpoints do not overlap.</summary>
    bool OverlapsWith(ITimePeriod test);

    // ----------------------------------------------------------------------
    /// <summary>Classifies another period's position relative to this period.</summary>
    PeriodRelation GetRelation(ITimePeriod test);

    // ----------------------------------------------------------------------
    /// <summary>Compares periods using the supplied comparer.</summary>
    int CompareTo(ITimePeriod other, ITimePeriodComparer comparer);

    // ----------------------------------------------------------------------
    /// <summary>Formats this period using the supplied formatter.</summary>
    string GetDescription(ITimeFormatter formatter);

} // interface ITimePeriod

// namespace Itenso.TimePeriod
// -- EOF -------------------------------------------------------------------
