// -- FILE ------------------------------------------------------------------
// Maintained fork regression coverage. Original library copyright is retained.
// --------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using ManagedCode.TimePeriodLibrary;
using Xunit;

namespace ManagedCode.TimePeriodTests;

public sealed class ManagedWeekBoundaryTests
{
    public static IEnumerable<object[]> CalendarCases()
    {
        foreach (string name in new[] { "en-US", "fr-FR", "uk-UA" })
            foreach (CalendarWeekRule rule in Enum.GetValues<CalendarWeekRule>())
                foreach (DayOfWeek firstDay in Enum.GetValues<DayOfWeek>())
                    foreach (YearWeekType type in Enum.GetValues<YearWeekType>())
                        yield return new object[] { name, rule, firstDay, type };
    }

    [Theory]
    [MemberData(nameof(CalendarCases))]
    public void NewYearWeeksContainTheirDateAndMoveExactlySevenDays(string name, CalendarWeekRule rule, DayOfWeek firstDay, YearWeekType type)
    {
        CultureInfo culture = ConfiguredCulture(name, rule, firstDay);
        var calendar = new TimeCalendar(new TimeCalendarConfig { Culture = culture, YearWeekType = type, EndOffset = TimeSpan.Zero });
        for (int year = 1990; year <= 2035; year++)
        {
            DateTime first = new(year, 12, 20);
            for (int day = 0; day < 24; day++)
            {
                DateTime moment = first.AddDays(day).AddHours(13);
                var week = new Week(moment, calendar);
                Assert.Equal(firstDay, week.Start.DayOfWeek);
                Assert.True(week.Start <= moment && moment < week.End, $"{moment:O}: {week.Start:O}..{week.End:O}");
                Assert.Equal(TimeSpan.FromDays(7), week.Duration);
                Assert.Equal(week.Start.AddDays(-7), week.GetPreviousWeek().Start);
                Assert.Equal(week.Start.AddDays(7), week.GetNextWeek().Start);
                Assert.Equal(week.Start, week.GetPreviousWeek().GetNextWeek().Start);
                Assert.Equal(week.Start, week.GetNextWeek().GetPreviousWeek().Start);
                Assert.Equal(week.Start, new Week(week.Year, week.WeekOfYear, calendar).Start);
                Assert.Equal(week.Start, week.GetStartOfWeek(0));
                var days = week.GetDays();
                Assert.Equal(7, days.Count);
                Assert.Equal(week.Start, days[0].Start);
            }
        }
    }

    [Theory]
    [MemberData(nameof(CalendarCases))]
    public void CalendarNumberingRetainsBclSemantics(string name, CalendarWeekRule rule, DayOfWeek firstDay, YearWeekType type)
    {
        CultureInfo culture = ConfiguredCulture(name, rule, firstDay);
        for (int year = 1990; year <= 2035; year++)
        {
            DateTime start = new(year, 12, 20);
            for (int day = 0; day < 24; day++)
            {
                DateTime date = start.AddDays(day);
                TimeTool.GetWeekOfYear(date, culture, rule, firstDay, type, out int actualYear, out int actualWeek);
                if (type == YearWeekType.Iso8601 && rule == CalendarWeekRule.FirstFourDayWeek)
                {
                    if (firstDay == DayOfWeek.Monday)
                    {
                        Assert.Equal(ISOWeek.GetYear(date), actualYear);
                        Assert.Equal(ISOWeek.GetWeekOfYear(date), actualWeek);
                    }
                }
                else
                {
                    int expectedWeek = culture.Calendar.GetWeekOfYear(date, rule, firstDay);
                    Assert.Equal(expectedWeek, actualWeek);
                    Assert.Equal(expectedWeek >= 52 && date.Month < 12 ? yearOf(date) - 1 : yearOf(date), actualYear);
                }
            }
        }
        static int yearOf(DateTime date) => date.Year;
    }

    [Theory]
    [InlineData(2025, 1, 1, 2024, 12, 29)]
    [InlineData(2024, 1, 1, 2023, 12, 31)]
    [InlineData(2023, 1, 1, 2023, 1, 1)]
    public void SundayFirstWeekStartsAtTheActualWeekBoundary(int year, int month, int day, int expectedYear, int expectedMonth, int expectedDay)
    {
        CultureInfo culture = ConfiguredCulture("en-US", CalendarWeekRule.FirstDay, DayOfWeek.Sunday);
        Assert.Equal(new DateTime(expectedYear, expectedMonth, expectedDay), new Week(new DateTime(year, month, day), new TimeCalendar(new TimeCalendarConfig { Culture = culture })).Start);
    }

    [Theory]
    [InlineData("en-US", DayOfWeek.Sunday)]
    [InlineData("fr-FR", DayOfWeek.Monday)]
    [InlineData("uk-UA", DayOfWeek.Monday)]
    public void WorkingTimeSubtractionCrossesNewYearWithoutMovingForward(string name, DayOfWeek firstDay)
    {
        var calendar = new TimeCalendar(new TimeCalendarConfig { Culture = ConfiguredCulture(name, CalendarWeekRule.FirstDay, firstDay), EndOffset = TimeSpan.Zero });
        var add = new CalendarDateAdd(calendar);
        for (int day = 1; day <= 6; day++) add.WeekDays.Add((DayOfWeek)day);
        add.WorkingHours.Add(new HourRange(9, 16));
        add.ExcludePeriods.Add(new TimeRange(new DateTime(2025, 12, 24), new DateTime(2025, 12, 27)));
        DateTime source = new(2025, 1, 2, 16, 0, 0);
        DateTime filledBoundary = new(2024, 12, 31, 9, 0, 0);
        DateTime nextBoundary = new(2024, 12, 30, 16, 0, 0);
        TimeSpan offset = TimeSpan.FromHours(21);
        // Fill stops at the exhausted period's boundary. Next crosses the closed
        // working-hours gap in the seek direction, even when no time remains.
        Assert.Equal(filledBoundary, add.Subtract(source, offset, SeekBoundaryMode.Fill));
        Assert.Equal(filledBoundary, add.Add(source, -offset, SeekBoundaryMode.Fill));
        Assert.Equal(nextBoundary, add.Subtract(source, offset));
        Assert.Equal(nextBoundary, add.Subtract(source, offset, SeekBoundaryMode.Next));
        Assert.Equal(nextBoundary, add.Add(source, -offset));
        Assert.Equal(source, add.Add(filledBoundary, offset, SeekBoundaryMode.Fill));
        Assert.Equal(new DateTime(2025, 1, 3, 9, 0, 0), add.Add(filledBoundary, offset));
        foreach (SeekBoundaryMode mode in Enum.GetValues<SeekBoundaryMode>())
        {
            Assert.Equal(filledBoundary.AddTicks(1), add.Subtract(source, offset - TimeSpan.FromTicks(1), mode));
            Assert.Equal(nextBoundary.AddTicks(-1), add.Subtract(source, offset + TimeSpan.FromTicks(1), mode));
        }
    }

    [Fact]
    public void DaySpecificWorkingHoursAreUsedWithoutOtherFilters()
    {
        var add = new CalendarDateAdd(new TimeCalendar(new TimeCalendarConfig { Culture = CultureInfo.GetCultureInfo("fr-FR"), EndOffset = TimeSpan.Zero }));
        add.WorkingDayHours.Add(new DayHourRange(DayOfWeek.Monday, 9, 16));
        Assert.Equal(new DateTime(2025, 1, 13, 10, 0, 0), add.Add(new DateTime(2025, 1, 6, 15, 0, 0), TimeSpan.FromHours(2)));
        Assert.Equal(new DateTime(2025, 1, 6, 15, 0, 0), add.Subtract(new DateTime(2025, 1, 13, 10, 0, 0), TimeSpan.FromHours(2)));
    }

    [Fact]
    public void InvalidWeekArgumentsFailPromptly()
    {
        CultureInfo culture = CultureInfo.GetCultureInfo("en-US");
        Assert.Throws<ArgumentOutOfRangeException>(() => TimeTool.GetStartOfYearWeek(2025, 0, culture, YearWeekType.Calendar));
        Assert.Throws<ArgumentOutOfRangeException>(() => TimeTool.GetStartOfYearWeek(2025, -1, culture, YearWeekType.Calendar));
        Assert.Throws<ArgumentOutOfRangeException>(() => TimeTool.GetStartOfYearWeek(2025, int.MaxValue, culture, YearWeekType.Calendar));
        Assert.Throws<ArgumentOutOfRangeException>(() => TimeTool.GetStartOfYearWeek(2025, 1, culture, (CalendarWeekRule)42, DayOfWeek.Sunday, YearWeekType.Calendar));
        Assert.Throws<ArgumentOutOfRangeException>(() => TimeTool.GetStartOfYearWeek(2025, 1, culture, CalendarWeekRule.FirstDay, (DayOfWeek)42, YearWeekType.Calendar));
    }

    private static CultureInfo ConfiguredCulture(string name, CalendarWeekRule rule, DayOfWeek firstDay)
    {
        var culture = (CultureInfo)CultureInfo.GetCultureInfo(name).Clone();
        culture.DateTimeFormat.CalendarWeekRule = rule;
        culture.DateTimeFormat.FirstDayOfWeek = firstDay;
        return culture;
    }
}
