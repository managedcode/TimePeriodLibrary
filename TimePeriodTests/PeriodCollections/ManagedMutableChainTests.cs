// -- FILE ------------------------------------------------------------------
// Maintained fork regression coverage. Original library copyright is retained.
// --------------------------------------------------------------------------

using System;
using System.Linq;
using ManagedCode.TimePeriodLibrary;
using Xunit;

namespace ManagedCode.TimePeriodTests;

public sealed class ManagedMutableChainTests
{
    private static readonly DateTime Origin = new(2025, 1, 1);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CollectionSearchReconcilesBeforeAnyOtherObservation(bool useIndexOf)
    {
        var first = new TimeRange(Origin, TimeSpan.FromHours(1));
        var second = new TimeRange(Origin, TimeSpan.FromHours(1));
        var chain = new TimePeriodChain { first, second };
        first.End = Origin.AddHours(2);
        var oldTail = new TimeRange(Origin.AddHours(1), Origin.AddHours(2));
        var expectedTail = new TimeRange(Origin.AddHours(2), Origin.AddHours(3));

        if (useIndexOf)
        {
            Assert.Equal(-1, chain.IndexOf(oldTail));
            Assert.Equal(1, chain.IndexOf(expectedTail));
        }
        else
        {
            Assert.False(chain.Contains(oldTail));
            Assert.True(chain.Contains(expectedTail));
        }

        Assert.Same(second, chain.Last);
        Assert.Equal(Origin.AddHours(3), chain.End);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ValueObservationsReconcileBeforeAnIncidentalBoundsRead(bool hashFirst)
    {
        var first = new TimeRange(Origin, TimeSpan.FromHours(1));
        var second = new TimeRange(Origin, TimeSpan.FromHours(1));
        var chain = new TimePeriodChain { first, second };
        first.End = Origin.AddHours(2);

        if (hashFirst)
        {
            int hash = chain.GetHashCode();
            Assert.Equal(Origin.AddHours(3), chain.End);
            Assert.Equal(hash, chain.GetHashCode());
        }
        else
        {
            var oldTail = new TimeRange(Origin.AddHours(1), Origin.AddHours(2));
            var expectedTail = new TimeRange(Origin.AddHours(2), Origin.AddHours(3));
            Assert.False(chain.ContainsPeriod(oldTail));
            Assert.True(chain.ContainsPeriod(expectedTail));
            Assert.Equal(Origin.AddHours(3), chain.End);
            Assert.False(chain.ContainsPeriod(oldTail));
            Assert.True(chain.ContainsPeriod(expectedTail));
        }
    }

    [Fact]
    public void ExpandingFirstReferencedRangeMovesTheTailAndUpdatesBounds()
    {
        var first = new TimeRange(Origin, TimeSpan.FromHours(1));
        var second = new TimeRange(Origin, TimeSpan.FromHours(2));
        var chain = new TimePeriodChain { first, second };
        first.ExpandEndTo(Origin.AddHours(3));
        Assert.Equal(Origin.AddHours(5), chain.End);
        Assert.Same(first, chain.First);
        Assert.Same(second, chain.Last);
        Assert.Equal(first.End, second.Start);
        Assert.Equal(TimeSpan.FromHours(2), second.Duration);
        Assert.Equal(TimeSpan.FromHours(5), chain.Duration);
    }

    [Theory]
    [InlineData(-120)]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(120)]
    public void ExternalFirstMoveAnchorsTheWholeChain(int minutes)
    {
        var chain = MakeChain(5);
        var first = chain[0];
        var tail = chain.Skip(1).ToArray();
        ((TimeRange)first).Move(TimeSpan.FromMinutes(minutes));
        Assert.Equal(Origin.AddMinutes(minutes), chain.Start);
        Assert.Equal(Origin.AddMinutes(minutes + 15), chain.End);
        AssertContiguous(chain);
        for (int i = 0; i < tail.Length; i++) Assert.Same(tail[i], chain[i + 1]);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void MutableDurationChangesPropagateThroughEveryObservation(int observation)
    {
        var chain = MakeChain(4);
        var middle = (TimeRange)chain[1];
        middle.Duration = TimeSpan.FromMinutes(20);
        switch (observation)
        {
            case 0: _ = chain.First; break;
            case 1: _ = chain.Last; break;
            case 2: _ = chain[3]; break;
            default: _ = chain.ToArray(); break;
        }
        Assert.Equal(TimeSpan.FromMinutes(28), chain.Duration);
        Assert.Equal(TimeSpan.FromMinutes(20), middle.Duration);
        AssertContiguous(chain);
    }

    [Fact]
    public void MovingATailExternallyRetainsItsDurationAndRestoresItsChainPosition()
    {
        var chain = MakeChain(3);
        var last = chain.Last;
        ((TimeRange)last).Move(TimeSpan.FromDays(2));
        Assert.Equal(Origin.AddMinutes(6), chain.End);
        Assert.Equal(Origin.AddMinutes(3), last.Start);
        Assert.Equal(TimeSpan.FromMinutes(3), last.Duration);
    }

    [Fact]
    public void InsertingBeforeSingleChildPreservesTheExistingChildPosition()
    {
        var original = new TimeRange(Origin, TimeSpan.FromMinutes(3));
        var chain = new TimePeriodChain { original };
        var prepend = new TimeRange(Origin.AddDays(1), TimeSpan.FromMinutes(2));
        chain.Insert(0, prepend);
        Assert.Equal(Origin.AddMinutes(-2), chain.Start);
        Assert.Equal(Origin, original.Start);
        Assert.Equal(prepend.End, original.Start);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void InsertAtEveryPositionRetainsDurationsAndContiguity(int index)
    {
        var chain = MakeChain(3);
        var inserted = new TimeRange(Origin.AddDays(1), TimeSpan.FromMinutes(10));
        chain.Insert(index, inserted);
        Assert.Same(inserted, chain[index]);
        Assert.Equal(TimeSpan.FromMinutes(16), chain.Duration);
        AssertContiguous(chain);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void RemoveAtEveryPositionClosesTheGap(int index)
    {
        var chain = MakeChain(3);
        var removed = chain[index];
        DateTime expectedStart = index == 0 ? chain[1].Start : Origin;
        chain.RemoveAt(index);
        Assert.Equal(2, chain.Count);
        Assert.Equal(expectedStart, chain.Start);
        Assert.Equal(TimeSpan.FromMinutes(6) - removed.Duration, chain.Duration);
        AssertContiguous(chain);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void ReplacementPreservesTheReplacedPositionAndMovesOnlyItsTail(int index)
    {
        var chain = MakeChain(3);
        DateTime position = chain[index].Start;
        var replacement = new TimeRange(Origin.AddDays(1), TimeSpan.FromMinutes(10));
        chain[index] = replacement;
        Assert.Same(replacement, chain[index]);
        Assert.Equal(position, replacement.Start);
        Assert.Equal(Origin, chain.Start);
        Assert.Equal(TimeSpan.FromMinutes(15 - index), chain.Duration);
        AssertContiguous(chain);
    }

    [Fact]
    public void InvalidReplacementDoesNotRemoveTheExistingChild()
    {
        var chain = MakeChain(3);
        var old = chain[1];
        Assert.Throws<ArgumentNullException>(() => chain[1] = null!);
        Assert.Throws<NotSupportedException>(() => chain[1] = new TimeRange(Origin, TimeSpan.FromHours(1), true));
        Assert.Same(old, chain[1]);
        Assert.Equal(3, chain.Count);
        AssertContiguous(chain);
    }

    [Fact]
    public void DuplicateMutableIdentityIsRejectedWithoutChangingTheChain()
    {
        var chain = MakeChain(3);
        var first = chain.First;
        Assert.Throws<ArgumentException>(() => chain.Add(first));
        Assert.Throws<ArgumentException>(() => chain.Insert(1, first));
        Assert.Throws<ArgumentException>(() => chain[2] = first);
        chain[0] = first;
        Assert.Equal(3, chain.Count);
        AssertContiguous(chain);
    }

    [Fact]
    public void DirectAndIndirectCyclesAreRejectedBeforeReadingTheirBounds()
    {
        var chain = MakeChain(2);
        Assert.Throws<ArgumentException>(() => chain.Add(chain));
        Assert.Throws<ArgumentException>(() => chain.Insert(0, chain));
        Assert.Throws<ArgumentException>(() => chain[0] = chain);
        var collection = new TimePeriodCollection { chain };
        Assert.Throws<ArgumentException>(() => chain.Add(collection));
        Assert.Equal(2, chain.Count);
        AssertContiguous(chain);
    }

    [Fact]
    public void OverflowingTailReconciliationLeavesEveryTailUnmoved()
    {
        var first = new TimeRange(DateTime.MaxValue.AddMinutes(-10), TimeSpan.FromMinutes(1));
        var second = new TimeRange(Origin, TimeSpan.FromMinutes(2));
        var third = new TimeRange(Origin, TimeSpan.FromMinutes(3));
        var chain = new TimePeriodChain { first, second, third };
        DateTime secondStart = second.Start;
        DateTime thirdStart = third.Start;
        first.End = DateTime.MaxValue.AddMinutes(-4);
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = chain.End);
        Assert.Equal(secondStart, second.Start);
        Assert.Equal(thirdStart, third.Start);
        first.End = DateTime.MaxValue.AddMinutes(-6);
        Assert.Equal(DateTime.MaxValue.AddMinutes(-1), chain.End);
        AssertContiguous(chain);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    public void OverflowingMoveDoesNotPartiallyMoveReferencedChildren(int direction)
    {
        DateTime start = direction < 0 ? DateTime.MinValue : DateTime.MaxValue.AddMinutes(-3);
        var first = new TimeRange(start, TimeSpan.FromMinutes(1));
        var second = new TimeRange(start, TimeSpan.FromMinutes(2));
        var chain = new TimePeriodChain { first, second };
        DateTime firstStart = first.Start;
        DateTime secondStart = second.Start;
        Assert.Throws<ArgumentOutOfRangeException>(() => chain.Move(TimeSpan.FromTicks(direction)));
        Assert.Equal(firstStart, first.Start);
        Assert.Equal(secondStart, second.Start);
    }

    [Fact]
    public void EmptyAndMomentChildrenKeepLegacyBoundsAndContiguity()
    {
        var chain = new TimePeriodChain();
        Assert.Equal(DateTime.MinValue, chain.Start);
        Assert.Equal(DateTime.MaxValue, chain.End);
        Assert.Null(chain.First);
        Assert.Null(chain.Last);
        chain.Move(TimeSpan.FromDays(1));
        chain.Add(new TimeRange(Origin));
        chain.Add(new TimeRange(Origin.AddDays(1)));
        Assert.True(chain.IsMoment);
        Assert.Equal(TimeSpan.Zero, chain.Duration);
        Assert.Equal(Origin, chain.End);
        AssertContiguous(chain);
        chain.Clear();
        Assert.Empty(chain);
    }

    [Fact]
    public void UnsupportedNestedRelocationIsRejectedBeforeInsertionOrReplacement()
    {
        var inner = MakeChain(2);
        var outer = MakeChain(3);
        var original = outer[1];
        DateTime innerStart = inner.Start;
        Assert.Throws<NotSupportedException>(() => outer.Add(inner));
        Assert.Throws<NotSupportedException>(() => outer.Insert(0, inner));
        Assert.Throws<NotSupportedException>(() => outer[1] = inner);
        Assert.Throws<NotSupportedException>(() => new TimePeriodChain().Add(inner));
        var collection = new TimePeriodCollection { new TimeRange(Origin, TimeSpan.FromMinutes(1)) };
        Assert.Throws<NotSupportedException>(() => outer.Add(collection));
        Assert.Equal(3, outer.Count);
        Assert.Same(original, outer[1]);
        Assert.Equal(innerStart, inner.Start);
        AssertContiguous(outer);
        AssertContiguous(inner);
    }

    [Fact]
    public void StableChainBoundsAllocateNoManagedMemory()
    {
        var chain = MakeChain(8);
        for (int i = 0; i < 100; i++) _ = chain.Duration;
        long before = GC.GetAllocatedBytesForCurrentThread();
        long ticks = 0;
        for (int i = 0; i < 1000; i++) ticks += chain.Duration.Ticks;
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.True(ticks > 0);
        Assert.Equal(0, allocated);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MomentChildrenCanBeAddedAtEitherDateTimeBoundary(bool maximum)
    {
        DateTime moment = maximum ? DateTime.MaxValue : DateTime.MinValue;
        var chain = new TimePeriodChain { new TimeRange(moment), new TimeRange(moment) };
        chain.Insert(0, new TimeRange(moment));
        Assert.Equal(moment, chain.Start);
        Assert.Equal(moment, chain.End);
        Assert.Equal(TimeSpan.Zero, chain.Duration);
        AssertContiguous(chain);
    }

    [Fact]
    public void NegativeMutableDurationsAreRejectedWithoutShiftingTheTail()
    {
        var chain = MakeChain(3);
        var first = (TimeRange)chain.First;
        var last = chain.Last;
        DateTime oldStart = last.Start;
        first.Duration = TimeSpan.FromMinutes(-1);
        Assert.Throws<InvalidOperationException>(() => _ = chain.End);
        Assert.Equal(oldStart, last.Start);
        var invalid = new TimeRange(Origin);
        invalid.Duration = TimeSpan.FromMinutes(-1);
        var empty = new TimePeriodChain();
        Assert.Throws<ArgumentException>(() => empty.Add(invalid));
        Assert.Equal(0, empty.Count);
    }

    private static TimePeriodChain MakeChain(int count)
    {
        var chain = new TimePeriodChain();
        for (int i = 1; i <= count; i++) chain.Add(new TimeRange(Origin, TimeSpan.FromMinutes(i)));
        return chain;
    }

    private static void AssertContiguous(TimePeriodChain chain)
    {
        var children = chain.ToArray();
        for (int i = 1; i < children.Length; i++) Assert.Equal(children[i - 1].End, children[i].Start);
        Assert.Equal(children.Aggregate(TimeSpan.Zero, (total, period) => total + period.Duration), chain.Duration);
    }
}
