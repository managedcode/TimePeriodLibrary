// -- FILE ------------------------------------------------------------------
// name       : TimePeriodSubtractor.cs
// project    : Itenso Time Period
// created    : Jani Giannoudis - 2011.09.30
// language   : C# 4.0
// environment: .NET 2.0
// copyright  : (c) 2011-2012 by Itenso GmbH, Switzerland
// --------------------------------------------------------------------------
using System;

namespace ManagedCode.TimePeriodLibrary;


// ------------------------------------------------------------------------
public class TimePeriodSubtractor<T> where T : ITimePeriod, new()
{

    // ----------------------------------------------------------------------
    public TimePeriodSubtractor() :
        this(null)
    {
    } // TimePeriodSubtractor

    // ----------------------------------------------------------------------
    public TimePeriodSubtractor(ITimePeriodMapper periodMapper)
    {
        this.periodMapper = periodMapper;
        timePeriodCombiner = new TimePeriodCombiner<T>(periodMapper);
        timeGapCalculator = new TimeGapCalculator<T>(periodMapper);
        timePeriodIntersector = new TimePeriodIntersector<T>(periodMapper);
    } // TimePeriodSubtractor

    // ----------------------------------------------------------------------
    public ITimePeriodMapper PeriodMapper
    {
        get { return periodMapper; }
    } // PeriodMapper

    // ----------------------------------------------------------------------
    /// <summary>Removes the subtracting periods from source time without modifying either caller's collection or bounds.</summary>
    /// <param name="sourcePeriods">Source periods whose remaining time is returned.</param>
    /// <param name="subtractingPeriods">Periods whose positive-duration time is excluded.</param>
    /// <param name="combinePeriods">True to combine source periods; false to preserve each source's remaining fragments, including overlap between sources.</param>
    /// <returns>The remaining time periods.</returns>
    public virtual ITimePeriodCollection SubtractPeriods(ITimePeriodContainer sourcePeriods, ITimePeriodCollection subtractingPeriods,
        bool combinePeriods = true)
    {
        if (sourcePeriods == null)
        {
            throw new ArgumentNullException("sourcePeriods");
        }
        if (subtractingPeriods == null)
        {
            throw new ArgumentNullException("subtractingPeriods");
        }

        if (sourcePeriods.Count == 0)
        {
            return new TimePeriodCollection();
        }

        if (subtractingPeriods.Count == 0 && !combinePeriods)
        {
            return new TimePeriodCollection(sourcePeriods);
        }

        // combined source periods
        if (combinePeriods)
        {
            sourcePeriods = timePeriodCombiner.CombinePeriods(sourcePeriods);
        }

        // combined subtracting periods
        if (subtractingPeriods.Count == 0)
        {
            return new TimePeriodCollection(sourcePeriods);
        }
        subtractingPeriods = timePeriodCombiner.CombinePeriods(subtractingPeriods);

        // Invert the exclusions without appending gaps to the caller's mutable container.
        ITimePeriodCollection gaps = timeGapCalculator.GetGaps(
            subtractingPeriods, new TimeRange(sourcePeriods.Start, sourcePeriods.End));
        if (combinePeriods)
        {
            TimePeriodCollection intersectionInput = new TimePeriodCollection(sourcePeriods);
            intersectionInput.AddAll(gaps);
            return timePeriodIntersector.IntersectPeriods(intersectionInput, true);
        }

        // Intersect each source separately: overlapping sources must not count as
        // an intersection inside an excluded region.
        TimePeriodCollection result = new TimePeriodCollection();
        foreach (ITimePeriod source in sourcePeriods)
        {
            TimePeriodCollection intersectionInput = new TimePeriodCollection { source };
            intersectionInput.AddAll(gaps);
            result.AddAll(timePeriodIntersector.IntersectPeriods(intersectionInput, false));
        }
        result.SortByStart();
        return result;
    } // SubtractPeriods

    // ----------------------------------------------------------------------
    // members
    private readonly ITimePeriodMapper periodMapper;
    private readonly TimePeriodCombiner<T> timePeriodCombiner;
    private readonly TimeGapCalculator<T> timeGapCalculator;
    private readonly TimePeriodIntersector<T> timePeriodIntersector;

} // class TimePeriodSubtractor

// namespace Itenso.TimePeriod
// -- EOF -------------------------------------------------------------------
