// -- FILE ------------------------------------------------------------------
// name       : ITimeRange.cs
// project    : Itenso Time Period
// created    : Jani Giannoudis - 2011.02.18
// language   : C# 4.0
// environment: .NET 2.0
// copyright  : (c) 2011-2012 by Itenso GmbH, Switzerland
// --------------------------------------------------------------------------
using System;

namespace ManagedCode.TimePeriodLibrary;


// ------------------------------------------------------------------------
/// <summary>A period with independent start and end bounds; moving one bound changes duration.</summary>
public interface ITimeRange : ITimePeriod
{

    // ----------------------------------------------------------------------
    /// <summary>Gets or sets start while keeping end fixed.</summary>
    new DateTime Start { get; set; }

    // ----------------------------------------------------------------------
    /// <summary>Gets or sets end while keeping start fixed.</summary>
    new DateTime End { get; set; }

    // ----------------------------------------------------------------------
    /// <summary>Gets or sets duration from the current start, adjusting end.</summary>
    new TimeSpan Duration { get; set; }

    // ----------------------------------------------------------------------
    /// <summary>Shifts both bounds without changing duration.</summary>
    void Move(TimeSpan offset);

    // ----------------------------------------------------------------------
    /// <summary>Moves start earlier only when the moment precedes it.</summary>
    void ExpandStartTo(DateTime moment);

    // ----------------------------------------------------------------------
    /// <summary>Moves end later only when the moment follows it.</summary>
    void ExpandEndTo(DateTime moment);

    // ----------------------------------------------------------------------
    /// <summary>Expands bounds to contain a moment.</summary>
    void ExpandTo(DateTime moment);

    // ----------------------------------------------------------------------
    /// <summary>Expands bounds to contain another period.</summary>
    void ExpandTo(ITimePeriod period);

    // ----------------------------------------------------------------------
    /// <summary>Moves start later only when the moment lies inside the range.</summary>
    void ShrinkStartTo(DateTime moment);

    // ----------------------------------------------------------------------
    /// <summary>Moves end earlier only when the moment lies inside the range.</summary>
    void ShrinkEndTo(DateTime moment);

    // ----------------------------------------------------------------------
    /// <summary>Shrinks bounds to an intersecting period.</summary>
    void ShrinkTo(ITimePeriod period);

    // ----------------------------------------------------------------------
    /// <summary>Creates a copy shifted by the specified offset.</summary>
    ITimeRange Copy(TimeSpan offset);

    // ----------------------------------------------------------------------
    /// <summary>Returns an intersection, or null when no point is shared.</summary>
    ITimeRange GetIntersection(ITimePeriod period);

} // interface ITimeRange

// namespace Itenso.TimePeriod
// -- EOF -------------------------------------------------------------------
