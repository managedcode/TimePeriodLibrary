using System;
using System.Collections.Generic;
using ManagedCode.TimePeriodLibrary;
using Xunit;

namespace ManagedCode.TimePeriodTests;

public sealed class ManagedIntervalSplitterTests
{
    [Theory]
    [InlineData(CalendarIntervalUnit.Year)]
    [InlineData(CalendarIntervalUnit.Quarter)]
    [InlineData(CalendarIntervalUnit.Month)]
    [InlineData(CalendarIntervalUnit.Week)]
    [InlineData(CalendarIntervalUnit.Day)]
    [InlineData(CalendarIntervalUnit.Hour)]
    [InlineData(CalendarIntervalUnit.Minute)]
    [InlineData(CalendarIntervalUnit.Second)]
    public void AllUnitsRetainPartialEndpointsAndExactlyCoverSource(CalendarIntervalUnit unit)
    {
        DateTime start = new DateTime(2024, 2, 28, 23, 59, 58).AddTicks(3);
        DateTime end = new DateTime(2024, 3, 1, 0, 0, 1).AddTicks(5);
        var source = new WallClockInterval(start, end);
        var expected = ReferenceSplit(start, end, unit);
        var output = new WallClockInterval[expected.Count];
        Assert.Equal(expected.Count, CalendarIntervalSplitter.GetCount(source, unit));
        Assert.Equal(expected.Count, CalendarIntervalSplitter.Write(source, unit, output));
        for (int i = 0; i < output.Length; i++)
        {
            Assert.Equal(expected[i].Start, output[i].Start);
            Assert.Equal(expected[i].End, output[i].End);
            Assert.False(output[i].IsEmpty);
            Assert.Equal(DateTimeKind.Unspecified, output[i].Start.Kind);
        }

        Assert.Equal(source.Start, output[0].Start);
        Assert.Equal(source.End, output[^1].End);
    }

    [Theory]
    [InlineData(CalendarIntervalUnit.Year)]
    [InlineData(CalendarIntervalUnit.Quarter)]
    [InlineData(CalendarIntervalUnit.Month)]
    [InlineData(CalendarIntervalUnit.Week)]
    [InlineData(CalendarIntervalUnit.Day)]
    [InlineData(CalendarIntervalUnit.Hour)]
    [InlineData(CalendarIntervalUnit.Minute)]
    [InlineData(CalendarIntervalUnit.Second)]
    public void EmptyAndMaximumBoundaryAreSafe(CalendarIntervalUnit unit)
    {
        Assert.Equal(0, CalendarIntervalSplitter.GetCount(default, unit));
        Assert.Equal(0, CalendarIntervalSplitter.Write(default, unit, Span<WallClockInterval>.Empty));
        var atMax = new WallClockInterval(DateTime.MaxValue.AddTicks(-1), DateTime.MaxValue);
        var output = new WallClockInterval[1];
        Assert.Equal(1, CalendarIntervalSplitter.Write(atMax, unit, output));
        Assert.Equal(atMax.Start, output[0].Start);
        Assert.Equal(atMax.End, output[0].End);
        var atMin = new WallClockInterval(DateTime.MinValue, DateTime.MinValue.AddTicks(1));
        Assert.Equal(1, CalendarIntervalSplitter.Write(atMin, unit, output));
        Assert.Equal(atMin.Start, output[0].Start);
    }

    [Theory]
    [InlineData(2024, 1, 1, CalendarIntervalUnit.Year)]
    [InlineData(2024, 4, 1, CalendarIntervalUnit.Quarter)]
    [InlineData(2024, 2, 1, CalendarIntervalUnit.Month)]
    [InlineData(2024, 2, 26, CalendarIntervalUnit.Week)]
    [InlineData(2024, 2, 29, CalendarIntervalUnit.Day)]
    public void ExactEndBoundaryDoesNotCreateEmptyTrailingSegment(int year, int month, int day, CalendarIntervalUnit unit)
    {
        DateTime boundary = new DateTime(year, month, day);
        var interval = new WallClockInterval(boundary.AddTicks(-1), boundary);
        var output = new WallClockInterval[1];
        Assert.Equal(1, CalendarIntervalSplitter.Write(interval, unit, output));
        Assert.Equal(boundary, output[0].End);
    }

    [Fact]
    public void GregorianLeapMonthAndMondayWeekAreExplicit()
    {
        var february = new WallClockInterval(new DateTime(2024, 2, 1), new DateTime(2024, 3, 1));
        var months = new WallClockInterval[1];
        Assert.Equal(1, CalendarIntervalSplitter.Write(february, CalendarIntervalUnit.Month, months));
        Assert.Equal(29, (months[0].End - months[0].Start).TotalDays);
        var week = new WallClockInterval(new DateTime(2024, 3, 3), new DateTime(2024, 3, 5));
        var weeks = new WallClockInterval[2];
        Assert.Equal(2, CalendarIntervalSplitter.Write(week, CalendarIntervalUnit.Week, weeks));
        Assert.Equal(new DateTime(2024, 3, 4), weeks[0].End);
    }

    [Fact]
    public void InvalidKindsUnitReversedAndShortCapacityFailBeforeWriting()
    {
        Assert.Throws<ArgumentException>(() => new WallClockInterval(DateTime.UtcNow, DateTime.UtcNow));
        Assert.Throws<ArgumentException>(() => new WallClockInterval(DateTime.Now, DateTime.Now));
        Assert.Throws<ArgumentOutOfRangeException>(() => new WallClockInterval(new DateTime(2024, 3, 1), new DateTime(2024, 2, 1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => CalendarIntervalSplitter.GetCount(default, (CalendarIntervalUnit)(-1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => CalendarIntervalSplitter.GetCount(default, (CalendarIntervalUnit)99));
        var sentinel = new WallClockInterval(new DateTime(2020, 1, 1), new DateTime(2020, 1, 2));
        var output = new[] { sentinel };
        Assert.Throws<ArgumentException>(() => CalendarIntervalSplitter.Write(new WallClockInterval(new DateTime(2024, 1, 1), new DateTime(2024, 1, 3)), CalendarIntervalUnit.Day, output));
        Assert.Equal(sentinel.Start, output[0].Start);
        Assert.Equal(sentinel.End, output[0].End);
    }

    [Fact]
    public void CountsAcrossTheEntireDateDomainDoNotEnumerateOrOverflow()
    {
        var domain = new WallClockInterval(DateTime.MinValue, DateTime.MaxValue);
        Assert.Equal(9999, CalendarIntervalSplitter.GetCount(domain, CalendarIntervalUnit.Year));
        Assert.Equal(9999 * 12, CalendarIntervalSplitter.GetCount(domain, CalendarIntervalUnit.Month));
        Assert.Equal((DateTime.MaxValue.Ticks - 1) / TimeSpan.TicksPerSecond + 1,
            CalendarIntervalSplitter.GetCount(domain, CalendarIntervalUnit.Second));
        Assert.Throws<ArgumentException>(() => CalendarIntervalSplitter.Write(domain, CalendarIntervalUnit.Second, new WallClockInterval[1]));
    }

    [Fact]
    public void RandomCalendarSplitsMatchIndependentDateArithmetic()
    {
        var random = new Random(97171);
        for (int trial = 0; trial < 500; trial++)
        {
            DateTime start = new DateTime(random.Next(1900, 2100), random.Next(1, 13), random.Next(1, 28),
                random.Next(24), random.Next(60), random.Next(60)).AddTicks(random.Next(10));
            CalendarIntervalUnit unit = (CalendarIntervalUnit)random.Next(8);
            DateTime end = unit >= CalendarIntervalUnit.Hour
                ? start.AddSeconds(random.Next(1, 500))
                : start.AddDays(random.Next(1, 100));
            var expected = ReferenceSplit(start, end, unit);
            var output = new WallClockInterval[expected.Count + 1];
            Assert.Equal(expected.Count, CalendarIntervalSplitter.Write(new WallClockInterval(start, end), unit, output));
            for (int i = 0; i < expected.Count; i++)
            {
                Assert.Equal(expected[i].Start, output[i].Start);
                Assert.Equal(expected[i].End, output[i].End);
            }

            Assert.True(output[^1].IsEmpty);
        }
    }

    private static List<WallClockInterval> ReferenceSplit(DateTime start, DateTime end, CalendarIntervalUnit unit)
    {
        DateTime boundary = new DateTime(start.Year, start.Month, start.Day, start.Hour, start.Minute, start.Second);
        if (unit <= CalendarIntervalUnit.Day)
        {
            boundary = start.Date;
        }

        if (unit == CalendarIntervalUnit.Year)
        {
            boundary = new DateTime(start.Year, 1, 1);
        }
        else if (unit == CalendarIntervalUnit.Quarter)
        {
            boundary = new DateTime(start.Year, ((start.Month - 1) / 3) * 3 + 1, 1);
        }
        else if (unit == CalendarIntervalUnit.Month)
        {
            boundary = new DateTime(start.Year, start.Month, 1);
        }
        else if (unit == CalendarIntervalUnit.Week)
        {
            while (boundary.DayOfWeek != DayOfWeek.Monday)
            {
                boundary = boundary.AddDays(-1);
            }
        }
        else if (unit == CalendarIntervalUnit.Hour)
        {
            boundary = new DateTime(start.Year, start.Month, start.Day, start.Hour, 0, 0);
        }
        else if (unit == CalendarIntervalUnit.Minute)
        {
            boundary = new DateTime(start.Year, start.Month, start.Day, start.Hour, start.Minute, 0);
        }

        var result = new List<WallClockInterval>();
        DateTime cursor = start;
        while (cursor < end)
        {
            boundary = unit switch
            {
                CalendarIntervalUnit.Year => boundary.AddYears(1),
                CalendarIntervalUnit.Quarter => boundary.AddMonths(3),
                CalendarIntervalUnit.Month => boundary.AddMonths(1),
                CalendarIntervalUnit.Week => boundary.AddDays(7),
                CalendarIntervalUnit.Day => boundary.AddDays(1),
                CalendarIntervalUnit.Hour => boundary.AddHours(1),
                CalendarIntervalUnit.Minute => boundary.AddMinutes(1),
                _ => boundary.AddSeconds(1),
            };
            DateTime next = boundary < end ? boundary : end;
            result.Add(new WallClockInterval(cursor, next));
            cursor = next;
        }

        return result;
    }
}
