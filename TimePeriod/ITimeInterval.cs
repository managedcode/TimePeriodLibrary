// -- FILE ------------------------------------------------------------------
// name       : ITimeInterval.cs
// project    : Itenso Time Period
// created    : Jani Giannoudis - 2011.05.06
// language   : C# 4.0
// environment: .NET 2.0
// copyright  : (c) 2011-2012 by Itenso GmbH, Switzerland
// --------------------------------------------------------------------------
using System;

namespace ManagedCode.TimePeriodLibrary;


// ------------------------------------------------------------------------
/// <summary>A period with open or closed endpoints. Enabled open edges adjust effective bounds by one tick.</summary>
public interface ITimeInterval : ITimePeriod
{

    // ----------------------------------------------------------------------
    /// <summary>Gets whether start is open.</summary>
    bool IsStartOpen { get; }

    // ----------------------------------------------------------------------
    /// <summary>Gets whether end is open.</summary>
    bool IsEndOpen { get; }

    // ----------------------------------------------------------------------
    /// <summary>Gets whether both edges are open.</summary>
    bool IsOpen { get; }

    // ----------------------------------------------------------------------
    /// <summary>Gets whether start is closed.</summary>
    bool IsStartClosed { get; }

    // ----------------------------------------------------------------------
    /// <summary>Gets whether end is closed.</summary>
    bool IsEndClosed { get; }

    // ----------------------------------------------------------------------
    /// <summary>Gets whether both edges are closed.</summary>
    bool IsClosed { get; }

    // ----------------------------------------------------------------------
    /// <summary>Gets whether this is a moment without two closed edges.</summary>
    bool IsEmpty { get; }

    // ----------------------------------------------------------------------
    /// <summary>Gets whether this is a moment with both edges closed.</summary>
    bool IsDegenerate { get; }

    // ----------------------------------------------------------------------
    /// <summary>Gets whether open edges adjust the effective bounds.</summary>
    bool IsIntervalEnabled { get; }

    // ----------------------------------------------------------------------
    /// <summary>Gets or sets raw start before open-edge tick adjustment.</summary>
    DateTime StartInterval { get; set; }

    // ----------------------------------------------------------------------
    /// <summary>Gets or sets raw end before open-edge tick adjustment.</summary>
    DateTime EndInterval { get; set; }

    // ----------------------------------------------------------------------
    /// <summary>Gets or sets the start edge kind.</summary>
    IntervalEdge StartEdge { get; set; }

    // ----------------------------------------------------------------------
    /// <summary>Gets or sets the end edge kind.</summary>
    IntervalEdge EndEdge { get; set; }

    // ----------------------------------------------------------------------
    /// <summary>Shifts both raw endpoints by offset.</summary>
    void Move(TimeSpan offset);

    // ----------------------------------------------------------------------
    /// <summary>Moves raw start earlier only when the moment precedes it.</summary>
    void ExpandStartTo(DateTime moment);

    // ----------------------------------------------------------------------
    /// <summary>Moves raw end later only when the moment follows it.</summary>
    void ExpandEndTo(DateTime moment);

    // ----------------------------------------------------------------------
    /// <summary>Expands raw bounds to contain a moment.</summary>
    void ExpandTo(DateTime moment);

    // ----------------------------------------------------------------------
    /// <summary>Expands raw bounds to contain another period.</summary>
    void ExpandTo(ITimePeriod period);

    // ----------------------------------------------------------------------
    /// <summary>Moves raw start later when the moment lies within the effective interval.</summary>
    void ShrinkStartTo(DateTime moment);

    // ----------------------------------------------------------------------
    /// <summary>Moves raw end earlier when the moment lies within the effective interval.</summary>
    void ShrinkEndTo(DateTime moment);

    // ----------------------------------------------------------------------
    /// <summary>Shrinks raw endpoints to an intersecting period.</summary>
    void ShrinkTo(ITimePeriod period);

    // ----------------------------------------------------------------------
    /// <summary>Creates a shifted copy preserving edge kinds and edge enablement.</summary>
    ITimeInterval Copy(TimeSpan offset);

} // interface ITimeInterval

// namespace Itenso.TimePeriod
// -- EOF -------------------------------------------------------------------
