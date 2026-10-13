using CronTaskRunner.Core.Scheduling;

namespace CronTaskRunner.Core.Tests.Scheduling;

public class CronScheduleTests
{
    [Fact]
    public void GetNextOccurrence_ReturnsEarliestAcrossAllExpressions()
    {
        var schedule = CronSchedule.Parse(["0 12 * * *", "30 8 * * *"]);
        var from = new DateTimeOffset(2026, 10, 8, 7, 0, 0, TimeSpan.Zero);

        var next = schedule.GetNextOccurrence(from, TimeZoneInfo.Utc);

        Assert.Equal(new DateTimeOffset(2026, 10, 8, 8, 30, 0, TimeSpan.Zero), next);
    }

    [Fact]
    public void GetNextOccurrence_IsStrictlyAfterFrom()
    {
        var schedule = CronSchedule.Parse(["30 8 * * *"]);
        var from = new DateTimeOffset(2026, 10, 8, 8, 30, 0, TimeSpan.Zero);

        var next = schedule.GetNextOccurrence(from, TimeZoneInfo.Utc);

        Assert.Equal(from.AddDays(1), next);
    }

    [Fact]
    public void Parse_InvalidExpression_ThrowsWithExpressionText()
    {
        var ex = Assert.Throws<FormatException>(() => CronSchedule.Parse(["0 12 * * *", "61 * * * *"]));

        Assert.Contains("'61 * * * *'", ex.Message);
    }

    [Fact]
    public void Parse_NoExpressions_Throws()
    {
        Assert.Throws<FormatException>(() => CronSchedule.Parse([]));
    }
}
