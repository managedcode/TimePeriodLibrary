// -- FILE ------------------------------------------------------------------
// name       : ITimeBlock.cs
// project    : Itenso Time Period
// created    : Jani Giannoudis - 2011.02.18
// language   : C# 4.0
// environment: .NET 2.0
// copyright  : (c) 2011-2012 by Itenso GmbH, Switzerland
// --------------------------------------------------------------------------
using System;

namespace ManagedCode.TimePeriodLibrary;


// ------------------------------------------------------------------------
/// <summary>A period defined by start and duration; moving either bound preserves duration.</summary>
public interface ITimeBlock : ITimePeriod
{

    // ----------------------------------------------------------------------
    /// <summary>Gets or sets start, shifting end to preserve duration.</summary>
    new DateTime Start { get; set; }

    // ----------------------------------------------------------------------
    /// <summary>Gets or sets end, shifting start to preserve duration.</summary>
    new DateTime End { get; set; }

    // ----------------------------------------------------------------------
    /// <summary>Gets or sets duration from start, adjusting end.</summary>
    new TimeSpan Duration { get; set; }

    // ----------------------------------------------------------------------
    /// <summary>Sets start and duration together.</summary>
    void Setup(DateTime newStart, TimeSpan newDuration);

    // ----------------------------------------------------------------------
    /// <summary>Shifts both bounds without changing duration.</summary>
    void Move(TimeSpan delta);

    // ----------------------------------------------------------------------
    /// <summary>Changes duration while preserving start.</summary>
    void DurationFromStart(TimeSpan newDuration);

    // ----------------------------------------------------------------------
    /// <summary>Changes duration while preserving end.</summary>
    void DurationFromEnd(TimeSpan newDuration);

    // ----------------------------------------------------------------------
    /// <summary>Creates a copy shifted by the specified delta.</summary>
    ITimeBlock Copy(TimeSpan delta);

    // ----------------------------------------------------------------------
    /// <summary>Creates an equally long block ending at this block's start plus offset.</summary>
    ITimeBlock GetPreviousPeriod(TimeSpan offset);

    // ----------------------------------------------------------------------
    /// <summary>Creates an equally long block starting at this block's end plus offset.</summary>
    ITimeBlock GetNextPeriod(TimeSpan offset);

    // ----------------------------------------------------------------------
    /// <summary>Returns an intersection, or null when no point is shared.</summary>
    ITimeBlock GetIntersection(ITimePeriod period);

} // class ITimeBlock

// namespace Itenso.TimePeriod
// -- EOF -------------------------------------------------------------------
