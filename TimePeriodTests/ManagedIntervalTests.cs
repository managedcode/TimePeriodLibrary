using System;
using ManagedCode.TimePeriodLibrary;
using Xunit;

namespace ManagedCode.TimePeriodTests;

public sealed class ManagedIntervalTests
{
    [Fact]
    public void RealOffsetsNormalizeToTheSameInstants()
    {
        var first = new UtcInterval(new DateTimeOffset(2024, 3, 31, 1, 30, 0, TimeSpan.FromHours(1)),
            new DateTimeOffset(2024, 3, 31, 3, 30, 0, TimeSpan.FromHours(2)));
        var equivalent = new UtcInterval(new DateTimeOffset(2024, 3, 31, 0, 30, 0, TimeSpan.Zero),
            new DateTimeOffset(2024, 3, 31, 1, 30, 0, TimeSpan.Zero));
        Assert.Equal(TimeSpan.FromHours(1), first.Duration);
        Assert.Equal(TimeSpan.Zero, first.Start.Offset);
        Assert.Equal(TimeSpan.Zero, first.End.Offset);
        Assert.Equal(equivalent, first);
        Assert.Equal(equivalent.GetHashCode(), first.GetHashCode());
        Assert.True(first == equivalent);
        Assert.False(first != equivalent);
        Assert.True(first.Equals((object)equivalent));
        Assert.False(first.Equals("interval"));
    }

    [Theory]
    [InlineData(0, 10, 0, true)]
    [InlineData(0, 10, 9, true)]
    [InlineData(0, 10, 10, false)]
    [InlineData(0, 10, -1, false)]
    [InlineData(0, 0, 0, false)]
    public void HalfOpenContainment(int start, int end, int point, bool expected)
    {
        Assert.Equal(expected, Interval(start, end).Contains(Instant(point)));
    }

    [Theory]
    [InlineData(0, 10, 10, 20, false)]
    [InlineData(0, 10, 9, 20, true)]
    [InlineData(0, 10, 0, 10, true)]
    [InlineData(0, 10, 5, 5, false)]
    [InlineData(5, 5, 0, 10, false)]
    [InlineData(0, 10, -5, 0, false)]
    [InlineData(0, 10, 2, 8, true)]
    public void IntersectionMatchesOverlap(int start, int end, int otherStart, int otherEnd, bool expected)
    {
        UtcInterval first = Interval(start, end);
        UtcInterval other = Interval(otherStart, otherEnd);
        Assert.Equal(expected, first.Overlaps(other));
        Assert.Equal(expected, other.Overlaps(first));
        Assert.Equal(expected, first.TryIntersect(other, out UtcInterval intersection));
        if (expected)
        {
            Assert.Equal(Interval(Math.Max(start, otherStart), Math.Min(end, otherEnd)), intersection);
        }
        else
        {
            Assert.True(intersection.IsEmpty);
        }
    }

    [Fact]
    public void EmptyDefaultAndExtremeBoundsAreSafe()
    {
        Assert.True(default(UtcInterval).IsEmpty);
        Assert.Equal(TimeSpan.Zero, default(UtcInterval).Duration);
        var entire = new UtcInterval(DateTimeOffset.MinValue, DateTimeOffset.MaxValue);
        Assert.Equal(DateTimeOffset.MaxValue - DateTimeOffset.MinValue, entire.Duration);
        Assert.True(entire.Contains(DateTimeOffset.MinValue));
        Assert.False(entire.Contains(DateTimeOffset.MaxValue));
        Assert.Throws<ArgumentOutOfRangeException>(() => new UtcInterval(DateTimeOffset.MaxValue, DateTimeOffset.MinValue));
    }

    [Fact]
    public void RandomIntersectionsMatchIndependentIntegerModel()
    {
        var random = new Random(7813);
        for (int i = 0; i < 4000; i++)
        {
            int start = random.Next(-100, 100);
            int end = start + random.Next(30);
            int otherStart = random.Next(-100, 100);
            int otherEnd = otherStart + random.Next(30);
            bool expected = Math.Max(start, otherStart) < Math.Min(end, otherEnd);
            Assert.Equal(expected, Interval(start, end).TryIntersect(Interval(otherStart, otherEnd), out UtcInterval result));
            if (expected)
            {
                Assert.Equal(Math.Min(end, otherEnd) - Math.Max(start, otherStart), result.Duration.Ticks);
            }
        }
    }

    [Fact]
    public void CompareUsesStartThenEnd()
    {
        Assert.True(Interval(0, 4).CompareTo(Interval(1, 2)) < 0);
        Assert.True(Interval(0, 4).CompareTo(Interval(0, 5)) < 0);
        Assert.Equal(0, Interval(0, 4).CompareTo(Interval(0, 4)));
    }

    internal static DateTimeOffset Instant(int ticks) => new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero).AddTicks(ticks);

    internal static UtcInterval Interval(int start, int end) => new UtcInterval(Instant(start), Instant(end));
}
