using Microsoft.EntityFrameworkCore;

namespace Fantasma;

[PublicAPI]
public sealed class FantasmaConfiguration
{
    internal List<Assembly> Assemblies { get; } = new();
    internal List<RecurringJob> RecurringJobs { get; } = new();
    internal Type? DatabaseContext { get; set; }
    internal bool NoCluster { get; set; }
    internal TimeSpan? SleepPreference { get; set; }

    public FantasmaConfiguration RegisterHandlersInAssembly(Assembly assembly)
    {
        Assemblies.Add(assembly);
        return this;
    }

    public FantasmaConfiguration RegisterHandlersInAssemblyContaining<T>()
    {
        Assemblies.Add(typeof(T).Assembly);
        return this;
    }

    public FantasmaConfiguration UseEntityFramework<T>()
        where T : DbContext, IFantasmaDatabase
    {
        DatabaseContext = typeof(T);
        return this;
    }

    public FantasmaConfiguration NoClustering()
    {
        NoCluster = true;
        return this;
    }

    public FantasmaConfiguration SetSleepPreference(TimeSpan time)
    {
        SleepPreference = time;
        return this;
    }

    public FantasmaConfiguration AddRecurringJob<T>(string name, JobId id, Cron cron, T data)
        where T : IJobData
    {
        // Validate the cron expression eagerly at registration time
        // so that invalid expressions fail at startup, not silently at runtime
        CronExpression parsedCron;
        try
        {
            parsedCron = CronExpression.Parse(cron.Expression, CronFormat.IncludeSeconds);
        }
        catch (Exception ex)
        {
            throw new ArgumentException(
                $"Invalid cron expression '{cron.Expression}' for job '{name}': {ex.Message}", nameof(cron), ex);
        }

        var nextOccurrence = parsedCron.GetNextOccurrence(DateTime.UtcNow);
        if (nextOccurrence == null)
        {
            throw new ArgumentException(
                $"Cron expression '{cron.Expression}' for job '{name}' does not produce a next occurrence. " +
                "This may indicate an incompatible Cronos version.", nameof(cron));
        }

        RecurringJobs.Add(new RecurringJob(id.Id, name, cron.Expression, data));
        return this;
    }
}