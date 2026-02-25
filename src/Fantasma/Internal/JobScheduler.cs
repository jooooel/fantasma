namespace Fantasma.Internal;

internal sealed class JobScheduler : IJobScheduler
{
    private readonly IJobProvider _provider;
    private readonly TimeProvider _time;
    private readonly ILogger<JobScheduler> _logger;

    public JobScheduler(IJobProvider provider, TimeProvider time, ILogger<JobScheduler> logger)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _time = time ?? throw new ArgumentNullException(nameof(time));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> Schedule(string name, IJobData data, Trigger trigger)
    {
        var scheduledAt = GetNextExecutionTime(trigger, _time);
        if (scheduledAt == null)
        {
            _logger.LogWarning(
                "Fantasma: Failed to calculate next execution time for job '{Name}'. " +
                "The cron expression may be invalid or incompatible with the current Cronos version. " +
                "Cron: {Cron}",
                name,
                trigger.IsRecurring ? trigger.Cron : "N/A");
            return false;
        }

        _logger.LogDebug("Fantasma: Scheduled job '{Name}' for {ScheduledAt:O}", name, scheduledAt.Value);

        var job = new Job
        {
            Id = trigger.Id,
            ScheduledAt = scheduledAt.Value,
            Status = JobStatus.Scheduled,
            Data = data,
            Kind = trigger.GetJobKind(),
            Cron = trigger.IsRecurring ? trigger.Cron : null,
            Name = name,
        };

        using (var storage = _provider.GetStorage())
        {
            await storage.Add(job);
        }

        return true;
    }

    private static DateTimeOffset? GetNextExecutionTime(Trigger trigger, TimeProvider time)
    {
        if (trigger.IsDelayed)
        {
            return trigger.Time;
        }

        if (trigger.IsRecurring)
        {
            var expr = CronExpression.Parse(trigger.Cron, CronFormat.IncludeSeconds);
            var now = time.GetUtcNow().UtcDateTime;
            return expr.GetNextOccurrence(now);
        }

        return time.GetUtcNow();
    }
}