using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using ManagedCode.TimePeriodLibrary;
using Xunit;

namespace ManagedCode.TimePeriodTests;

public sealed class ManagedDocumentationTests
{
    [Fact]
    public void DateDiffKeepsCallerOrderAndSignedDuration()
    {
        var start = new DateTime(2024, 1, 1);
        var end = start.AddDays(1).AddHours(2).AddMinutes(3).AddSeconds(4);
        var forward = new DateDiff(start, end);
        var reverse = new DateDiff(end, start);
        Assert.Equal(start, reverse.Date2);
        Assert.Equal(end, reverse.Date1);
        Assert.Equal(-forward.Difference, reverse.Difference);
        Assert.Equal(-26, reverse.Hours);
        Assert.Equal(-1563, reverse.Minutes);
        Assert.Equal(-93784, reverse.Seconds);
    }

    [Fact]
    public void GeneratedXmlDocumentsDateOrderingAndFundamentalInterfaceMembers()
    {
        var assembly = typeof(DateDiff).Assembly.Location;
        var xmlPath = Path.ChangeExtension(assembly, ".xml");
        Assert.True(File.Exists(xmlPath), $"Missing packaged IntelliSense documentation: {xmlPath}");
        var members = XDocument.Load(xmlPath).Descendants("member").ToDictionary(e => (string)e.Attribute("name"));
        var constructor = members[$"M:{typeof(DateDiff).FullName}.#ctor(System.DateTime,System.DateTime)"];
        Assert.Contains("negative", constructor.Elements("param").Single(e => (string)e.Attribute("name") == "date2").Value);
        foreach (var type in new[] { typeof(ITimePeriod), typeof(ITimeRange), typeof(ITimeBlock), typeof(ITimeInterval) })
        {
            Assert.Contains(members.Keys, key => key == $"T:{type.FullName}");
            foreach (var property in type.GetProperties())
            {
                Assert.True(members.TryGetValue($"P:{type.FullName}.{property.Name}", out var entry));
                Assert.False(string.IsNullOrWhiteSpace(entry.Element("summary")?.Value));
            }
        }
    }
}
