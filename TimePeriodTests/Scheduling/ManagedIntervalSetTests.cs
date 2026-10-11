using System;
using System.Linq;
using ManagedCode.TimePeriodLibrary;
using Xunit;
using static ManagedCode.TimePeriodTests.ManagedIntervalTests;

namespace ManagedCode.TimePeriodTests;

public sealed class ManagedIntervalSetTests
{
    [Fact]
    public void NormalizeMergesTouchingNestedAndDuplicateAndDropsEmpty()
    {
        UtcInterval[] input = { Interval(9, 12), Interval(3, 7), Interval(0, 3), Interval(1, 2), Interval(4, 4), Interval(3, 7), Interval(15, 17) };
        int count = IntervalOperations.Normalize(input);
        Assert.Equal(new[] { Interval(0, 7), Interval(9, 12), Interval(15, 17) }, input[..count]);
        Assert.Equal(count, IntervalOperations.Normalize(input.AsSpan(0, count)));
        Assert.Equal(0, IntervalOperations.Normalize(Span<UtcInterval>.Empty));
        Assert.Equal(0, IntervalOperations.Normalize(new[] { default(UtcInterval), Interval(4, 4) }));
    }

    [Fact]
    public void SubtractClipsOutsideExclusionsAndPreservesInputs()
    {
        UtcInterval[] exclusions = { Interval(-5, 2), Interval(4, 6), Interval(8, 12), Interval(20, 30) };
        UtcInterval[] original = exclusions.ToArray();
        var output = new UtcInterval[5];
        int count = IntervalOperations.Subtract(Interval(0, 10), exclusions, output);
        Assert.Equal(new[] { Interval(2, 4), Interval(6, 8) }, output[..count]);
        Assert.Equal(count, IntervalOperations.GetSubtractionCount(Interval(0, 10), exclusions));
        Assert.Equal(original, exclusions);
    }

    [Theory]
    [InlineData(-5, 0, 1)]
    [InlineData(10, 15, 1)]
    [InlineData(0, 10, 0)]
    [InlineData(-5, 15, 0)]
    [InlineData(0, 5, 1)]
    [InlineData(5, 10, 1)]
    [InlineData(3, 7, 2)]
    public void SubtractionBoundaryCases(int start, int end, int expected)
    {
        var output = new UtcInterval[2];
        int count = IntervalOperations.Subtract(Interval(0, 10), new[] { Interval(start, end) }, output);
        Assert.Equal(expected, count);
        Assert.Equal(10 - Math.Max(0, Math.Min(10, end) - Math.Max(0, start)), output[..count].Sum(value => value.Duration.Ticks));
    }

    [Fact]
    public void EmptySourceAndTouchingExclusionsAreWellDefined()
    {
        Assert.Equal(0, IntervalOperations.Subtract(Interval(5, 5), Array.Empty<UtcInterval>(), Span<UtcInterval>.Empty));
        Assert.Equal(0, IntervalOperations.Subtract(Interval(0, 10), new[] { Interval(0, 5), Interval(5, 10) }, Span<UtcInterval>.Empty));
        var output = new UtcInterval[1];
        Assert.Equal(1, IntervalOperations.Subtract(Interval(0, 10), Array.Empty<UtcInterval>(), output));
        Assert.Equal(Interval(0, 10), output[0]);
    }

    [Fact]
    public void InvalidInputAliasAndShortBuffersFailBeforeWriting()
    {
        UtcInterval sentinel = Interval(30, 40);
        var output = new[] { sentinel };
        Assert.Throws<ArgumentException>(() => IntervalOperations.Subtract(Interval(0, 10), new[] { Interval(3, 7) }, output));
        Assert.Equal(sentinel, output[0]);
        Assert.Throws<ArgumentException>(() => IntervalOperations.Subtract(Interval(0, 10), new[] { Interval(5, 8), Interval(2, 3) }, output));
        Assert.Throws<ArgumentException>(() => IntervalOperations.Subtract(Interval(0, 10), new[] { Interval(2, 8), Interval(4, 9) }, output));
        Assert.Throws<ArgumentException>(() => IntervalOperations.Subtract(Interval(0, 10), new[] { Interval(4, 4) }, output));
        UtcInterval[] alias = { Interval(2, 3), Interval(5, 6), sentinel, sentinel };
        UtcInterval[] copy = alias.ToArray();
        Assert.Throws<ArgumentException>(() => IntervalOperations.Subtract(Interval(0, 10), alias.AsSpan(0, 2), alias.AsSpan(1, 3)));
        Assert.Equal(copy, alias);
        // Disjoint slices of one buffer are permitted.
        Assert.Equal(1, IntervalOperations.Subtract(Interval(0, 2), alias.AsSpan(0, 2), alias.AsSpan(2, 2)));
    }

    [Fact]
    public void RandomNormalizationAndSubtractionMatchDiscreteCoverageModel()
    {
        var random = new Random(21987);
        for (int trial = 0; trial < 500; trial++)
        {
            var input = new UtcInterval[random.Next(30)];
            var covered = new bool[64];
            for (int i = 0; i < input.Length; i++)
            {
                int start = random.Next(65);
                int end = random.Next(start, 65);
                input[i] = Interval(start, end);
                for (int point = start; point < end; point++)
                {
                    covered[point] = true;
                }
            }

            int count = IntervalOperations.Normalize(input);
            for (int i = 0; i < count; i++)
            {
                Assert.False(input[i].IsEmpty);
                if (i > 0)
                {
                    Assert.True(input[i - 1].End < input[i].Start);
                }
            }

            var remainder = new UtcInterval[count + 1];
            int remainderCount = IntervalOperations.Subtract(Interval(0, 64), input.AsSpan(0, count), remainder);
            for (int point = 0; point < 64; point++)
            {
                bool normalizedCoverage = input.Take(count).Any(value => value.Contains(Instant(point)));
                bool remainingCoverage = remainder.Take(remainderCount).Any(value => value.Contains(Instant(point)));
                Assert.Equal(covered[point], normalizedCoverage);
                Assert.Equal(!covered[point], remainingCoverage);
            }

            Assert.Equal(64, input.Take(count).Sum(value => value.Duration.Ticks) + remainder.Take(remainderCount).Sum(value => value.Duration.Ticks));
        }
    }
}
