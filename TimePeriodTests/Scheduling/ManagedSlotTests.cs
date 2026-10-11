using System;
using System.Linq;
using ManagedCode.TimePeriodLibrary;
using Xunit;
using static ManagedCode.TimePeriodTests.ManagedIntervalTests;

namespace ManagedCode.TimePeriodTests;

public sealed class ManagedSlotTests
{
    [Theory]
    [InlineData(0, 1, false, 0)]
    [InlineData(0, 1, true, 0)]
    [InlineData(10, 2, false, 5)]
    [InlineData(10, 2, true, 5)]
    [InlineData(10, 3, false, 3)]
    [InlineData(10, 3, true, 4)]
    [InlineData(10, 20, false, 0)]
    [InlineData(10, 20, true, 1)]
    [InlineData(1, 1, false, 1)]
    public void CountsAndCoverageHonorPartialPolicy(int length, int duration, bool partial, int expected)
    {
        UtcInterval source = Interval(0, length);
        var output = new UtcInterval[expected];
        Assert.Equal(expected, FixedIntervalSlots.GetCount(source, TimeSpan.FromTicks(duration), partial));
        Assert.Equal(expected, FixedIntervalSlots.Write(source, TimeSpan.FromTicks(duration), output, partial));
        for (int i = 0; i < output.Length; i++)
        {
            Assert.False(output[i].IsEmpty);
            Assert.Equal(Instant(i * duration), output[i].Start);
            Assert.True(output[i].Duration.Ticks <= duration);
            if (i > 0)
            {
                Assert.Equal(output[i - 1].End, output[i].Start);
            }
        }

        long covered = output.Sum(slot => slot.Duration.Ticks);
        Assert.Equal(partial ? length : (length / duration) * duration, covered);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(long.MinValue)]
    public void RejectsNonpositiveDuration(long ticks)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => FixedIntervalSlots.GetCount(Interval(0, 10), TimeSpan.FromTicks(ticks)));
        Assert.Throws<ArgumentOutOfRangeException>(() => FixedIntervalSlots.Write(Interval(0, 10), TimeSpan.FromTicks(ticks), new UtcInterval[10]));
    }

    [Fact]
    public void BufferValidationIsAtomicAndUnusedTailIsPreserved()
    {
        UtcInterval sentinel = Interval(30, 40);
        var shortOutput = new[] { sentinel };
        Assert.Throws<ArgumentException>(() => FixedIntervalSlots.Write(Interval(0, 10), TimeSpan.FromTicks(3), shortOutput));
        Assert.Equal(sentinel, shortOutput[0]);
        var output = new[] { sentinel, sentinel, sentinel, sentinel, sentinel };
        Assert.Equal(3, FixedIntervalSlots.Write(Interval(0, 10), TimeSpan.FromTicks(3), output));
        Assert.Equal(sentinel, output[3]);
        Assert.Equal(sentinel, output[4]);
    }

    [Fact]
    public void ExtremeBoundsHugeDurationsAndHugeCountsDoNotOverflow()
    {
        var all = new UtcInterval(DateTimeOffset.MinValue, DateTimeOffset.MaxValue);
        Assert.Equal(all.Duration.Ticks, FixedIntervalSlots.GetCount(all, TimeSpan.FromTicks(1)));
        Assert.Throws<ArgumentException>(() => FixedIntervalSlots.Write(all, TimeSpan.FromTicks(1), new UtcInterval[1]));
        Assert.Equal(0, FixedIntervalSlots.GetCount(all, TimeSpan.MaxValue));
        var output = new UtcInterval[1];
        Assert.Equal(1, FixedIntervalSlots.Write(all, TimeSpan.MaxValue, output, true));
        Assert.Equal(all, output[0]);
        UtcInterval nearMax = new UtcInterval(DateTimeOffset.MaxValue.AddTicks(-5), DateTimeOffset.MaxValue);
        Assert.Equal(1, FixedIntervalSlots.Write(nearMax, TimeSpan.MaxValue, output, true));
        Assert.Equal(nearMax, output[0]);
    }

    [Fact]
    public void OffsetTransitionStillUsesElapsedUtcDuration()
    {
        var spring = new UtcInterval(new DateTimeOffset(2024, 3, 31, 1, 0, 0, TimeSpan.FromHours(1)),
            new DateTimeOffset(2024, 3, 31, 4, 0, 0, TimeSpan.FromHours(2)));
        var output = new UtcInterval[2];
        Assert.Equal(2, FixedIntervalSlots.Write(spring, TimeSpan.FromHours(1), output));
        Assert.Equal(spring.Start, output[0].Start);
        Assert.Equal(spring.End, output[1].End);
        Assert.All(output, slot => Assert.Equal(TimeSpan.FromHours(1), slot.Duration));
    }

    [Fact]
    public void RandomSlotsMatchIndependentIntegerDivision()
    {
        var random = new Random(11029);
        for (int i = 0; i < 2000; i++)
        {
            int length = random.Next(200);
            int duration = random.Next(1, 50);
            bool partial = random.Next(2) == 0;
            int count = length / duration + (partial && length % duration > 0 ? 1 : 0);
            var output = new UtcInterval[count];
            Assert.Equal(count, FixedIntervalSlots.Write(Interval(0, length), TimeSpan.FromTicks(duration), output, partial));
            for (int j = 0; j < count; j++)
            {
                Assert.Equal(Interval(j * duration, Math.Min((j + 1) * duration, length)), output[j]);
            }
        }
    }
}
