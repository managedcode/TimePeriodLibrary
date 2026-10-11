using System;
using System.Collections.Generic;
using System.Linq;
using ManagedCode.TimePeriodLibrary;
using Xunit;

namespace ManagedCode.TimePeriodTests;

public sealed class ManagedContainerSafetyTests
{
    [Fact]
    public void EmptyCollectionCannotContainItself()
    {
        var periods = new TimePeriodCollection();
        var error = Assert.Throws<ArgumentException>(() => periods.Add(periods));
        Assert.Equal("item", error.ParamName);
        Assert.Contains("directly or indirectly", error.Message);
        Assert.Empty(periods);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void IndirectCycleIsRejectedByInsertOrReplacement(bool replace)
    {
        var leaf = Range();
        var parent = new TimePeriodCollection();
        var child = new TimePeriodCollection { leaf };
        parent.Add(child);
        Assert.Throws<ArgumentException>(() =>
        {
            if (replace) child[0] = parent;
            else child.Insert(0, parent);
        });
        Assert.Single(child);
        Assert.Same(leaf, child[0]);
        Assert.Equal(leaf.Start, parent.Start);
    }

    [Fact]
    public void AddAllRejectsWholeBatchBeforeAnyInsertion()
    {
        var existing = Range();
        var periods = new TimePeriodCollection { existing };
        Assert.Throws<ArgumentException>(() => periods.AddAll(new ITimePeriod[] { Range(), periods }));
        Assert.Single(periods);
        Assert.Same(existing, periods[0]);
        Assert.Throws<ArgumentNullException>(() => periods.AddAll(new ITimePeriod[] { Range(), null }));
        Assert.Single(periods);
    }

    [Fact]
    public void FailingEnumeratorLeavesCollectionUnchanged()
    {
        var existing = Range();
        var periods = new TimePeriodCollection { existing };
        Assert.Throws<InvalidOperationException>(() => periods.AddAll(Failing()));
        Assert.Single(periods);
        Assert.Same(existing, periods[0]);
    }

    [Fact]
    public void SelfEnumerationDuplicatesExistingChildrenSafely()
    {
        var leaf = Range();
        var periods = new TimePeriodCollection { leaf };
        periods.AddAll(periods);
        Assert.Equal(2, periods.Count);
        Assert.All(periods, period => Assert.Same(leaf, period));
    }

    [Fact]
    public void SharedNestedChildrenAreValidAndUseIdentityRatherThanValueEquality()
    {
        var child = new TimePeriodCollection { Range() };
        var equalChild = new TimePeriodCollection { Range() };
        var periods = new TimePeriodCollection { child, equalChild, child };
        Assert.Equal(3, periods.Count);
        Assert.Equal(Range().Start, periods.Start);
        periods.AddAll(Enumerable.Repeat<ITimePeriod>(child, 500));
        Assert.Equal(503, periods.Count);
    }

    [Fact]
    public void CycleValidationDoesNotRecursivelyEvaluateDeepContainerBounds()
    {
        var root = new TimePeriodCollection();
        var cursor = root;
        for (var i = 0; i < 10000; i++)
        {
            var child = new TimePeriodCollection();
            cursor.Add(child);
            cursor = child;
        }
        Assert.Throws<ArgumentException>(() => cursor.Add(root));
        Assert.Empty(cursor);
    }

    [Fact]
    public void ChainAndCollectionCannotFormAnIndirectCycle()
    {
        var chain = new TimePeriodChain();
        var collection = new TimePeriodCollection { chain };
        Assert.Throws<ArgumentException>(() => chain.Add(collection));
        Assert.Empty(chain);
        Assert.Single(collection);
    }

    [Fact]
    public void AddAllPreservesDerivedAddHooksAfterWholeBatchValidation()
    {
        var periods = new RecordingCollection();
        periods.AddAll(new[] { Range(), Range() });
        Assert.Equal(2, periods.AddCalls);
        Assert.All(periods, period => Assert.Equal(Range().Start.AddDays(1), period.Start));
        Assert.Throws<ArgumentException>(() => periods.AddAll(new ITimePeriod[] { Range(), periods }));
        Assert.Equal(2, periods.AddCalls);
        Assert.Equal(2, periods.Count);
    }

    private sealed class RecordingCollection : TimePeriodCollection
    {
        public int AddCalls { get; private set; }

        public override void Add(ITimePeriod item)
        {
            AddCalls++;
            base.Add(item is ITimeRange range ? range.Copy(TimeSpan.FromDays(1)) : item);
        }
    }

    private static TimeRange Range() => new(new DateTime(2024, 1, 1), new DateTime(2024, 1, 2));

    private static IEnumerable<ITimePeriod> Failing()
    {
        yield return Range();
        throw new InvalidOperationException("Enumeration failed.");
    }
}
