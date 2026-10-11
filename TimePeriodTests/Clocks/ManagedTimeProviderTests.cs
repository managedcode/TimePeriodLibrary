using System;
using ManagedCode.TimePeriodLibrary;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace ManagedCode.TimePeriodTests;

public sealed class ManagedTimeProviderTests
{
    [Fact]
    public void InstanceClocksFreezeAdvanceAndRemainIndependent()
    {
        var firstProvider = new FakeTimeProvider(new DateTimeOffset(2024, 2, 29, 23, 59, 0, TimeSpan.Zero));
        var secondProvider = new FakeTimeProvider(new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var first = new TimeProviderClock(firstProvider);
        var second = new TimeProviderClock(secondProvider);
        Assert.Equal(first.Now, first.Now);
        firstProvider.Advance(TimeSpan.FromMinutes(2));
        Assert.Equal(new DateTime(2024, 3, 1, 0, 1, 0), first.Now);
        Assert.Equal(2030, second.Now.Year);
        Assert.Equal(firstProvider.GetUtcNow(), first.UtcNow);
    }

    [Theory]
    [InlineData(-12)]
    [InlineData(-5)]
    [InlineData(0)]
    [InlineData(5.5)]
    [InlineData(14)]
    public void ProviderZoneDeterminesCalendarDate(double hours)
    {
        var provider = new FakeTimeProvider(new DateTimeOffset(2024, 1, 1, 2, 37, 19, TimeSpan.Zero));
        var zone = TimeZoneInfo.CreateCustomTimeZone("ProviderZone", TimeSpan.FromHours(hours), "Provider zone", "Provider zone");
        provider.SetLocalTimeZone(zone);
        var factory = new TimeProviderCalendar(provider);
        DateTime expected = TimeZoneInfo.ConvertTime(provider.GetUtcNow(), zone).DateTime;
        Assert.Equal(expected, factory.Now);
        Assert.Equal(DateTimeKind.Unspecified, factory.Now.Kind);
        Assert.Equal(new Day(expected).Start, factory.Day().Start);
        Assert.Equal(new Hour(expected).Start, factory.Hour().Start);
        Assert.Equal(new Minute(expected).Start, factory.Minute().Start);
        Assert.Equal(new Year(expected).Start, factory.Year().Start);
        Assert.Equal(new Halfyear(expected).Start, factory.Halfyear().Start);
        Assert.Equal(new Quarter(expected).Start, factory.Quarter().Start);
        Assert.Equal(new Month(expected).Start, factory.Month().Start);
        Assert.Equal(new Week(expected).Start, factory.Week().Start);
    }

    [Fact]
    public void ExplicitDatesAndCustomCalendarRemainIndependentOfCurrentTime()
    {
        var provider = new FakeTimeProvider(new DateTimeOffset(2025, 1, 1, 12, 0, 0, TimeSpan.Zero));
        var calendar = new TimeCalendar(new TimeCalendarConfig { YearBaseMonth = YearMonth.April });
        var factory = new TimeProviderCalendar(provider, calendar);
        Assert.Same(calendar, factory.Calendar);
        Assert.Equal(new Year(factory.Now, calendar).Start, factory.Year().Start);
        DateTime explicitDate = new DateTime(1997, 9, 12);
        var explicitMonth = new Month(explicitDate, calendar);
        provider.Advance(TimeSpan.FromDays(90));
        Assert.Equal(new Month(explicitDate, calendar).Start, explicitMonth.Start);
        Assert.Equal(new Month(factory.Now, calendar).Start, factory.Month().Start);
    }

    [Fact]
    public void NativeZoneRulesFollowSpringForwardWithoutReinterpretingCallerDates()
    {
        var startTransition = TimeZoneInfo.TransitionTime.CreateFloatingDateRule(
            new DateTime(1, 1, 1, 2, 0, 0), 3, 5, DayOfWeek.Sunday);
        var endTransition = TimeZoneInfo.TransitionTime.CreateFloatingDateRule(
            new DateTime(1, 1, 1, 3, 0, 0), 10, 5, DayOfWeek.Sunday);
        var rule = TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(
            new DateTime(2020, 1, 1), new DateTime(2030, 12, 31),
            TimeSpan.FromHours(1), startTransition, endTransition);
        var zone = TimeZoneInfo.CreateCustomTimeZone("DST-provider", TimeSpan.FromHours(1),
            "DST provider", "Standard", "Summer", new[] { rule });
        var provider = new FakeTimeProvider(new DateTimeOffset(2024, 3, 31, 0, 59, 0, TimeSpan.Zero));
        provider.SetLocalTimeZone(zone);
        var factory = new TimeProviderCalendar(provider);
        Assert.Equal(new DateTime(2024, 3, 31, 1, 59, 0), factory.Now);
        provider.Advance(TimeSpan.FromMinutes(2));
        Assert.Equal(new DateTime(2024, 3, 31, 3, 1, 0), factory.Now);
        Assert.Equal(new DateTime(2024, 3, 31, 3, 0, 0), factory.Hour().Start);
        var callerDate = new DateTime(2024, 3, 31, 2, 30, 0);
        Assert.Equal(new DateTime(2024, 3, 31, 2, 0, 0), new Hour(callerDate).Start);
    }

    [Theory]
    [InlineData(2023, 12, 31, 2024, 1, 1)]
    [InlineData(2024, 2, 28, 2024, 2, 29)]
    [InlineData(2024, 2, 29, 2024, 3, 1)]
    [InlineData(2024, 3, 31, 2024, 4, 1)]
    [InlineData(2024, 6, 30, 2024, 7, 1)]
    public void AdvancingNativeProviderCrossesCalendarBoundaries(int year, int month, int day,
        int expectedYear, int expectedMonth, int expectedDay)
    {
        var provider = new FakeTimeProvider(new DateTimeOffset(year, month, day, 23, 59, 59, TimeSpan.Zero));
        var factory = new TimeProviderCalendar(provider);
        var oldDay = factory.Day();
        provider.Advance(TimeSpan.FromSeconds(1));
        DateTime expected = new DateTime(expectedYear, expectedMonth, expectedDay);
        Assert.Equal(expected, factory.Now);
        Assert.Equal(expected, factory.Day().Start);
        Assert.Equal(new DateTime(year, month, day), oldDay.Start);
        Assert.Equal(new Year(expected).Start, factory.Year().Start);
        Assert.Equal(new Halfyear(expected).Start, factory.Halfyear().Start);
        Assert.Equal(new Quarter(expected).Start, factory.Quarter().Start);
        Assert.Equal(new Month(expected).Start, factory.Month().Start);
        Assert.Equal(new Week(expected).Start, factory.Week().Start);
    }

    [Fact]
    public void ClocksRejectMissingProvider()
    {
        Assert.Throws<ArgumentNullException>(() => new TimeProviderClock(null));
        Assert.Throws<ArgumentNullException>(() => new TimeProviderCalendar(null));
    }
}
