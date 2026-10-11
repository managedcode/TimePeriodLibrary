// -- FILE ------------------------------------------------------------------
// name       : TimePeriodChain.cs
// project    : Itenso Time Period
// created    : Jani Giannoudis - 2011.02.18
// language   : C# 4.0
// environment: .NET 2.0
// copyright  : (c) 2011-2012 by Itenso GmbH, Switzerland
// --------------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;

namespace ManagedCode.TimePeriodLibrary;


// ------------------------------------------------------------------------
public class TimePeriodChain : ITimePeriodChain
{

    // ----------------------------------------------------------------------
    public TimePeriodChain()
    {
    } // TimePeriodChain

    // ----------------------------------------------------------------------
    public TimePeriodChain(IEnumerable<ITimePeriod> timePeriods)
    {
        if (timePeriods == null)
        {
            throw new ArgumentNullException("timePeriods");
        }
        AddAll(timePeriods);
    } // TimePeriodChain

    // ----------------------------------------------------------------------
    public bool IsReadOnly
    {
        get { return false; }
    } // IsReadOnly

    // ----------------------------------------------------------------------
    public ITimePeriod First
    {
        get { Reconcile(); return periods.Count > 0 ? periods[0] : null; }
    } // First

    // ----------------------------------------------------------------------
    public ITimePeriod Last
    {
        get { Reconcile(); return periods.Count > 0 ? periods[periods.Count - 1] : null; }
    } // Last

    // ----------------------------------------------------------------------
    public int Count
    {
        get { return periods.Count; }
    } // Count

    // ----------------------------------------------------------------------
    public ITimePeriod this[int index]
    {
        get { Reconcile(); return periods[index]; }
        set
        {
            ITimePeriod previous = periods[index];
            ValidateItem(value, "value", index);
            Reconcile();
            if (ReferenceEquals(previous, value)) return;
            DateTime position = previous.Start;
            DateTime end = position.Add(value.Duration);
            for (int i = index + 1; i < periods.Count; i++)
            {
                end = end.Add(periods[i].Duration);
            }
            value.Setup(position, position.Add(value.Duration));
            periods[index] = value;
            Reconcile();
        }
    } // this[]

    // ----------------------------------------------------------------------
    public bool IsAnytime
    {
        get { return !HasStart && !HasEnd; }
    } // IsAnytime

    // ----------------------------------------------------------------------
    public bool IsMoment
    {
        get { return Count != 0 && First.Start.Equals(Last.End); }
    } // IsMoment

    // ----------------------------------------------------------------------
    public bool HasStart
    {
        get { return Start != TimeSpec.MinPeriodDate; }
    } // HasStart

    // ----------------------------------------------------------------------
    public DateTime Start
    {
        get { return Count > 0 ? First.Start : TimeSpec.MinPeriodDate; }
        set
        {
            if (Count == 0)
            {
                return;
            }
            Move(value - Start);
        }
    } // Start

    // ----------------------------------------------------------------------
    public bool HasEnd
    {
        get { return End != TimeSpec.MaxPeriodDate; }
    } // HasEnd

    // ----------------------------------------------------------------------
    public DateTime End
    {
        get { return Count > 0 ? Last.End : TimeSpec.MaxPeriodDate; }
        set
        {
            if (Count == 0)
            {
                return;
            }
            Move(value - End);
        }
    } // End

    // ----------------------------------------------------------------------
    public TimeSpan Duration
    {
        get { return End - Start; }
    } // Duration

    // ----------------------------------------------------------------------
    public string DurationDescription
    {
        get { return TimeFormatter.Instance.GetDuration(Duration, DurationFormatType.Detailed); }
    } // DurationDescription

    // ----------------------------------------------------------------------
    public virtual TimeSpan GetDuration(IDurationProvider provider)
    {
        if (provider == null)
        {
            throw new ArgumentNullException("provider");
        }
        return provider.GetDuration(Start, End);
    } // GetDuration

    // ----------------------------------------------------------------------
    public virtual void Setup(DateTime newStart, DateTime newEnd)
    {
        throw new InvalidOperationException();
    } // Setup

    // ----------------------------------------------------------------------
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    } // IEnumerable.GetEnumerator

    // ----------------------------------------------------------------------
    public IEnumerator<ITimePeriod> GetEnumerator()
    {
        Reconcile();
        return periods.GetEnumerator();
    } // GetEnumerator

    // ----------------------------------------------------------------------
    public virtual void Move(TimeSpan delta)
    {
        Reconcile();
        if (delta == TimeSpan.Zero) return;
        // Validate every destination before moving any referenced child.
        foreach (ITimePeriod period in periods)
        {
            CheckReadOnlyItem(period);
            _ = period.Start.Add(delta);
            _ = period.End.Add(delta);
        }
        foreach (ITimePeriod period in periods)
        {
            DateTime start = period.Start.Add(delta);
            period.Setup(start, start.Add(period.Duration));
        }
    } // Move

    // ----------------------------------------------------------------------
    public virtual void Add(ITimePeriod item)
    {
        ValidateItem(item, "item");
        Reconcile();
        if (periods.Count > 0)
        {
            DateTime end = periods[periods.Count - 1].End;
            CheckSpaceAfter(end, item.Duration);
            item.Setup(end, end.Add(item.Duration));
        }
        periods.Add(item);
    } // Add

    // ----------------------------------------------------------------------
    public bool ContainsPeriod(ITimePeriod test)
    {
        if (test == null)
        {
            throw new ArgumentNullException("test");
        }

        Reconcile();
        foreach (ITimePeriod period in periods)
        {
            if (period.IsSamePeriod(test))
            {
                return true;
            }
        }
        return false;
    } // ContainsPeriod

    // ----------------------------------------------------------------------
    public void AddAll(IEnumerable<ITimePeriod> timePeriods)
    {
        if (timePeriods == null)
        {
            throw new ArgumentNullException("timePeriods");
        }

        foreach (ITimePeriod period in timePeriods)
        {
            Add(period);
        }
    } // AddAll

    // ----------------------------------------------------------------------
    public virtual void Insert(int index, ITimePeriod period)
    {
        if (index < 0 || index > periods.Count) throw new ArgumentOutOfRangeException("index");
        ValidateItem(period, "period");
        Reconcile();
        if (periods.Count == 0)
        {
            periods.Add(period);
            return;
        }
        DateTime position;
        if (index == 0)
        {
            CheckSpaceBefore(periods[0].Start, period.Duration);
            position = periods[0].Start.Subtract(period.Duration);
        }
        else
        {
            CheckSpaceAfter(periods[periods.Count - 1].End, period.Duration);
            position = periods[index - 1].End;
        }
        period.Setup(position, position.Add(period.Duration));
        periods.Insert(index, period);
        Reconcile();
    } // Insert

    // ----------------------------------------------------------------------
    public virtual bool Contains(ITimePeriod period)
    {
        if (period == null)
        {
            throw new ArgumentNullException("period");
        }
        Reconcile();
        return periods.Contains(period);
    } // Contains

    // ----------------------------------------------------------------------
    public virtual int IndexOf(ITimePeriod period)
    {
        if (period == null)
        {
            throw new ArgumentNullException("period");
        }
        Reconcile();
        return periods.IndexOf(period);
    } // IndexOf

    // ----------------------------------------------------------------------
    public virtual void CopyTo(ITimePeriod[] array, int arrayIndex)
    {
        if (array == null)
        {
            throw new ArgumentNullException("array");
        }
        Reconcile();
        periods.CopyTo(array, arrayIndex);
    } // CopyTo

    // ----------------------------------------------------------------------
    public virtual void Clear()
    {
        periods.Clear();
    } // Clear

    // ----------------------------------------------------------------------
    public virtual bool Remove(ITimePeriod period)
    {
        if (period == null) throw new ArgumentNullException("period");
        Reconcile();
        int index = periods.IndexOf(period);
        if (index < 0) return false;
        periods.RemoveAt(index);
        Reconcile();
        return true;
    } // Remove

    // ----------------------------------------------------------------------
    public virtual void RemoveAt(int index)
    {
        Remove(this[index]);
    } // RemoveAt

    // ----------------------------------------------------------------------
    public virtual bool IsSamePeriod(ITimePeriod test)
    {
        if (test == null)
        {
            throw new ArgumentNullException("test");
        }
        return Start == test.Start && End == test.End;
    } // IsSamePeriod

    // ----------------------------------------------------------------------
    public virtual bool HasInside(DateTime test)
    {
        return TimePeriodCalc.HasInside(this, test);
    } // HasInside

    // ----------------------------------------------------------------------
    public virtual bool HasInside(ITimePeriod test)
    {
        if (test == null)
        {
            throw new ArgumentNullException("test");
        }
        return TimePeriodCalc.HasInside(this, test);
    } // HasInside

    // ----------------------------------------------------------------------
    public virtual bool IntersectsWith(ITimePeriod test)
    {
        if (test == null)
        {
            throw new ArgumentNullException("test");
        }
        return TimePeriodCalc.IntersectsWith(this, test);
    } // IntersectsWith

    // ----------------------------------------------------------------------
    public virtual bool OverlapsWith(ITimePeriod test)
    {
        if (test == null)
        {
            throw new ArgumentNullException("test");
        }
        return TimePeriodCalc.OverlapsWith(this, test);
    } // OverlapsWith

    // ----------------------------------------------------------------------
    public virtual PeriodRelation GetRelation(ITimePeriod test)
    {
        if (test == null)
        {
            throw new ArgumentNullException("test");
        }
        return TimePeriodCalc.GetRelation(this, test);
    } // GetRelation

    // ----------------------------------------------------------------------
    public virtual int CompareTo(ITimePeriod other, ITimePeriodComparer comparer)
    {
        if (other == null)
        {
            throw new ArgumentNullException("other");
        }
        if (comparer == null)
        {
            throw new ArgumentNullException("comparer");
        }
        return comparer.Compare(this, other);
    } // CompareTo

    // ----------------------------------------------------------------------
    public string GetDescription(ITimeFormatter formatter = null)
    {
        return Format(formatter ?? TimeFormatter.Instance);
    } // GetDescription

    // ----------------------------------------------------------------------
    protected virtual string Format(ITimeFormatter formatter)
    {
        return formatter.GetCollectionPeriod(Count, Start, End, Duration);
    } // Format

    // ----------------------------------------------------------------------
    public override string ToString()
    {
        return GetDescription();
    } // ToString

    // ----------------------------------------------------------------------
    public sealed override bool Equals(object obj)
    {
        if (obj == this)
        {
            return true;
        }
        if (obj == null || GetType() != obj.GetType())
        {
            return false;
        }
        return IsEqual(obj);
    } // Equals

    // ----------------------------------------------------------------------
    protected virtual bool IsEqual(object obj)
    {
        return HasSameData(obj as TimePeriodChain);
    } // IsEqual

    // ----------------------------------------------------------------------
    private bool HasSameData(IList<ITimePeriod> comp)
    {
        if (Count != comp.Count)
        {
            return false;
        }

        for (int i = 0; i < Count; i++)
        {
            if (!this[i].Equals(comp[i]))
            {
                return false;
            }
        }

        return true;
    } // HasSameData

    // ----------------------------------------------------------------------
    public sealed override int GetHashCode()
    {
        return HashTool.AddHashCode(GetType().GetHashCode(), ComputeHashCode());
    } // GetHashCode

    // ----------------------------------------------------------------------
    protected virtual int ComputeHashCode()
    {
        Reconcile();
        return HashTool.ComputeHashCode(periods);
    } // ComputeHashCode

    // ----------------------------------------------------------------------
    protected void CheckSpaceBefore(DateTime moment, TimeSpan duration)
    {
        bool hasSpace = duration >= TimeSpan.Zero;
        if (hasSpace)
        {
            TimeSpan remaining = moment - TimeSpec.MinPeriodDate;
            hasSpace = duration <= remaining;
        }

        if (!hasSpace)
        {
            throw new InvalidOperationException("duration " + duration + " out of range ");
        }
    } // CheckSpaceBefore

    // ----------------------------------------------------------------------
    protected void CheckSpaceAfter(DateTime moment, TimeSpan duration)
    {
        bool hasSpace = duration >= TimeSpan.Zero;
        if (hasSpace)
        {
            TimeSpan remaining = TimeSpec.MaxPeriodDate - moment;
            hasSpace = duration <= remaining;
        }

        if (!hasSpace)
        {
            throw new InvalidOperationException("duration " + duration + " out of range");
        }
    } // CheckSpaceAfter

    // ----------------------------------------------------------------------
    protected void CheckReadOnlyItem(ITimePeriod timePeriod)
    {
        if (timePeriod.IsReadOnly)
        {
            throw new NotSupportedException("TimePeriod is read-only");
        }
    } // CheckReadOnlyItem

    // ----------------------------------------------------------------------
    // Mutable children keep their current durations. The first child anchors the chain;
    // every later child is moved to its predecessor's end when the chain is observed.
    // Validation precedes movement, so an unrepresentable tail cannot partly shift it.
    private void Reconcile()
    {
        if (periods.Count > 0 && periods[0].Duration < TimeSpan.Zero)
            throw new InvalidOperationException("A chain requires non-negative child durations.");
        if (periods.Count < 2) return;
        DateTime end = periods[0].End;
        for (int i = 1; i < periods.Count; i++)
        {
            ITimePeriod period = periods[i];
            if (period.Start != end) CheckReadOnlyItem(period);
            TimeSpan duration = period.Duration;
            if (duration < TimeSpan.Zero) throw new InvalidOperationException("A chain requires non-negative child durations.");
            end = end.Add(duration);
        }
        end = periods[0].End;
        for (int i = 1; i < periods.Count; i++)
        {
            ITimePeriod period = periods[i];
            if (period.Start != end) period.Setup(end, end.Add(period.Duration));
            end = period.End;
        }
    }

    private void ValidateItem(ITimePeriod item, string parameterName, int replacingIndex = -1)
    {
        TimePeriodCollection.ValidateAddition(this, item, parameterName);
        CheckReadOnlyItem(item);
        // Built-in containers cannot relocate through ITimePeriod.Setup.
        // Reject before insertion, even when their current bounds happen to align.
        if (item is TimePeriodChain || item is TimePeriodCollection)
            throw new NotSupportedException("Nested time-period containers do not support chain relocation through Setup.");
        if (item.Duration < TimeSpan.Zero) throw new ArgumentException("A chain requires a non-negative duration.", parameterName);
        for (int i = 0; i < periods.Count; i++)
        {
            if (i != replacingIndex && ReferenceEquals(periods[i], item))
                throw new ArgumentException("A chain cannot contain the same mutable period twice.", parameterName);
        }
    }

    // members
    private readonly List<ITimePeriod> periods = new List<ITimePeriod>();

} // class TimePeriodChain

// namespace Itenso.TimePeriod
// -- EOF -------------------------------------------------------------------
