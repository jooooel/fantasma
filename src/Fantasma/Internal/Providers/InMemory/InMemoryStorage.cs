namespace Fantasma.Internal;

internal sealed class InMemoryStorage : IJobStorage
{
    private readonly TimeProvider _time;
    private readonly ILogger<InMemoryStorage> _logger;
    private readonly List<Job> _jobs;

    public InMemoryStorage(TimeProvider time, ILogger<InMemoryStorage> logger)
    {
        _time = time ?? throw new ArgumentNullException(nameof(time));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _jobs = new List<Job>();
    }

    public void Dispose()
    {
    }

    public Task Add(Job job)
    {
        // Already added?
        if (_jobs.Any(x => x.Id == job.Id))
        {
            return Task.CompletedTask;
        }

        _jobs.Add(job);
        return Task.CompletedTask;
    }

    public Task Remove(Job job)
    {
        _jobs.Remove(job);

        return Task.CompletedTask;
    }

    public Task Update(Job job)
    {
        return Task.CompletedTask;
    }

    public Task<Job?> GetNextJob()
    {
        var now = _time.GetUtcNow().UtcDateTime;
        var job = _jobs.Where(x => x.ScheduledAt < now).MinBy(x => x.ScheduledAt);
        if (job == null)
        {
            return Task.FromResult<Job?>(null);
        }

        _jobs.Remove(job);

        return Task.FromResult<Job?>(job);
    }

    public Task Release(CompletedJob job)
    {
        if (job.Cron != null)
        {
            var rescheduled = job.Reschedule(_time);
            if (rescheduled != null)
            {
                _jobs.Add(rescheduled);
            }
            else
            {
                _logger.LogWarning(
                    "Fantasma: Recurring job '{Name}' could not be rescheduled. " +
                    "Cron expression '{Cron}' did not produce a next occurrence. " +
                    "This job will not run again.",
                    job.Name,
                    job.Cron);
            }
        }

        return Task.CompletedTask;
    }
}