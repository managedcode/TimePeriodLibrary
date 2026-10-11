using System;
using System.Collections.Generic;
using System.Linq;
using ManagedCode.TimePeriodLibrary;
using Xunit;

namespace ManagedCode.TimePeriodTests;

public sealed class ManagedSubtractionInvariantTests
{
    private static readonly DateTime Origin = new(2024, 1, 1);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SubtractionPreservesBothCallerCollectionsAndTheirBoundaries(bool combine)
    {
        var sources = new TimePeriodCollection { Range(0, 10), Range(5, 15) };
        var exclusions = new TimePeriodCollection { Range(3, 8), Range(12, 20) };
        var sourceSnapshot = sources.Select(p => (p, p.Start, p.End)).ToArray();
        var exclusionSnapshot = exclusions.Select(p => (p, p.Start, p.End)).ToArray();
        var subtractor = new TimePeriodSubtractor<TimeRange>();
        var first = subtractor.SubtractPeriods(sources, exclusions, combine);
        var second = subtractor.SubtractPeriods(sources, exclusions, combine);
        Assert.Equal(sourceSnapshot, sources.Select(p => (p, p.Start, p.End)).ToArray());
        Assert.Equal(exclusionSnapshot, exclusions.Select(p => (p, p.Start, p.End)).ToArray());
        Assert.Equal(Bounds(first), Bounds(second));
        Assert.All(first, period => Assert.DoesNotContain(exclusions, excluded => period.OverlapsWith(excluded)));
    }

    [Fact]
    public void OverlappingSourcesDoNotReintroduceExcludedRegionsWhenNotCombined()
    {
        var sources = new TimePeriodCollection { Range(0, 10), Range(5, 15) };
        var exclusions = new TimePeriodCollection { Range(6, 9) };
        var result = new TimePeriodSubtractor<TimeRange>().SubtractPeriods(sources, exclusions, false);
        Assert.Equal(new[] { (0L, 6L), (5L, 6L), (9L, 10L), (9L, 15L) }, Bounds(result));
    }

    [Fact]
    public void SourceAndExclusionsMayBeTheSameCollection()
    {
        var periods = new TimePeriodCollection { Range(0, 10), Range(5, 15) };
        Assert.Empty(new TimePeriodSubtractor<TimeRange>().SubtractPeriods(periods, periods, false));
        Assert.Equal(2, periods.Count);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DeterministicRandomizedSubtractionMatchesIndependentIntegerCellOracle(bool combine)
    {
        var random = new Random(701);
        for (var iteration = 0; iteration < 200; iteration++)
        {
            var sourcePairs = RandomPairs(random);
            var exclusionPairs = RandomPairs(random);
            var sources = new TimePeriodCollection(sourcePairs.Select(p => Range(p.start, p.end)));
            var exclusions = new TimePeriodCollection(exclusionPairs.Select(p => Range(p.start, p.end)));
            var result = new TimePeriodSubtractor<TimeRange>().SubtractPeriods(sources, exclusions, combine);
            for (var cell = 0; cell < 30; cell++)
            {
                var expected = sourcePairs.Count(p => p.start <= cell && cell < p.end);
                if (exclusionPairs.Any(p => p.start <= cell && cell < p.end)) expected = 0;
                if (combine && expected > 0) expected = 1;
                var midpoint = Origin.AddTicks(cell * 2L + 1);
                Assert.Equal(expected, result.Count(p => p.Start < midpoint && midpoint < p.End));
            }
            Assert.All(result, p => Assert.True(p.Start < p.End));
            Assert.Equal(sourcePairs.Length, sources.Count);
            Assert.Equal(exclusionPairs.Length, exclusions.Count);
        }
    }

    private static (int start, int end)[] RandomPairs(Random random) =>
        Enumerable.Range(0, random.Next(1, 7)).Select(_ =>
        {
            var start = random.Next(0, 29);
            return (start, random.Next(start + 1, 31));
        }).ToArray();

    private static TimeRange Range(int start, int end) => new(Origin.AddTicks(start * 2L), Origin.AddTicks(end * 2L));

    private static (long, long)[] Bounds(IEnumerable<ITimePeriod> periods) => periods
        .Select(p => ((p.Start - Origin).Ticks / 2, (p.End - Origin).Ticks / 2))
        .OrderBy(p => p.Item1).ThenBy(p => p.Item2).ToArray();
}
