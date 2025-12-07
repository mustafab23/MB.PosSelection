using MB.PosSelection.Application.Features.Pos.Commands.SyncPosRates;
using Medallion.Threading;
using MediatR;
using Microsoft.Extensions.Logging;
using Quartz;

namespace MB.PosSelection.Infrastructure.BackgroundJobs
{
    [DisallowConcurrentExecution]
    public class PosRateSyncJob : IJob
    {
        private readonly IMediator _mediator;
        private readonly ILogger<PosRateSyncJob> _logger;
        private readonly IDistributedLockProvider _lockProvider;

        public PosRateSyncJob(
            IMediator mediator,
            ILogger<PosRateSyncJob> logger,
            IDistributedLockProvider lockProvider)
        {
            _mediator = mediator;
            _logger = logger;
            _lockProvider = lockProvider;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var jobName = context.JobDetail.Key.Name;
            _logger.LogInformation("Job Triggered: {JobName} at {Time}", jobName, DateTimeOffset.Now);

            // DISTRIBUTED LOCK (REDIS)
            // Eğer 10 pod aynı anda ayağa kalkarsa, sadece 1 tanesi bu bloğa girer..
            await using (var handle = await _lockProvider.TryAcquireLockAsync("Lock:PosRateSyncJob"))
            {
                if (handle != null)
                {
                    _logger.LogInformation("Lock acquired for {JobName}. Starting synchronization...", jobName);

                    try
                    {
                        // Business Logic (Application Layer'a delege ediyoruz)
                        await _mediator.Send(new SyncPosRatesCommand(), context.CancellationToken);

                        _logger.LogInformation("{JobName} completed successfully.", jobName);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "{JobName} failed unexpectedly.", jobName);
                        throw new JobExecutionException(ex) { RefireImmediately = true };
                    }
                }
                else
                {
                    // Kilitli! başka bir instance şu an bu işi yapıyor.
                    _logger.LogInformation("Could not acquire lock for {JobName}. Another instance is running logic. Skipping.", jobName);
                }
            }
        }
    }
}
