using System;

namespace Fantasma.Tests;

public sealed class TestJobData : IJobData
{
}

public class FantasmaConfigurationTests
{
    [Fact]
    public void AddRecurringJob_WithValidCron_Succeeds()
    {
        var config = new FantasmaConfiguration();

        config.AddRecurringJob(
            "Test job",
            new JobId("test-1"),
            new Cron("*/10 * * * * *"),
            new TestJobData());
    }

    [Fact]
    public void AddRecurringJob_WithInvalidCron_ThrowsArgumentException()
    {
        var config = new FantasmaConfiguration();

        var ex = Assert.Throws<ArgumentException>(() =>
            config.AddRecurringJob(
                "Bad job",
                new JobId("test-2"),
                new Cron("not-a-cron"),
                new TestJobData()));

        Assert.Contains("Invalid cron expression", ex.Message);
        Assert.Contains("Bad job", ex.Message);
    }

    [Fact]
    public void AddRecurringJob_WithFiveFieldCron_ThrowsArgumentException()
    {
        var config = new FantasmaConfiguration();

        var ex = Assert.Throws<ArgumentException>(() =>
            config.AddRecurringJob(
                "Five field job",
                new JobId("test-3"),
                new Cron("*/5 * * * *"),
                new TestJobData()));

        Assert.Contains("Invalid cron expression", ex.Message);
    }

    [Fact]
    public void AddRecurringJob_WithNamedCron_Succeeds()
    {
        var config = new FantasmaConfiguration();

        config.AddRecurringJob(
            "Hourly job",
            new JobId("test-4"),
            new Cron("@hourly"),
            new TestJobData());
    }
}
