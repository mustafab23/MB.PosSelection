using MB.PosSelection.Application.Features.Pos.Commands.SyncPosRates;
using MB.PosSelection.Infrastructure.BackgroundJobs;
using Medallion.Threading;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Quartz;
using Xunit;

namespace MB.PosSelection.UnitTests.Infrastructure.BackgroundJobs
{
    public class PosRateSyncJobTests
    {
        private readonly Mock<IMediator> _mockMediator;
        private readonly Mock<ILogger<PosRateSyncJob>> _mockLogger;
        private readonly Mock<IDistributedLockProvider> _mockLockProvider;
        private readonly Mock<IDistributedLock> _mockLock;
        private readonly Mock<IDistributedSynchronizationHandle> _mockLockHandle;

        private readonly PosRateSyncJob _job;

        public PosRateSyncJobTests()
        {
            _mockMediator = new Mock<IMediator>();
            _mockLogger = new Mock<ILogger<PosRateSyncJob>>();
            _mockLockProvider = new Mock<IDistributedLockProvider>();
            _mockLock = new Mock<IDistributedLock>();
            _mockLockHandle = new Mock<IDistributedSynchronizationHandle>();

            // ZİNCİRLEME MOCK SETUP
            // 1. Provider'dan "CreateLock" çağrıldığında bizim Mock Lock dönsün.
            _mockLockProvider
                .Setup(x => x.CreateLock(It.IsAny<string>()))
                .Returns(_mockLock.Object);

            _job = new PosRateSyncJob(
                _mockMediator.Object,
                _mockLogger.Object,
                _mockLockProvider.Object
            );
        }

        [Fact]
        public async Task Execute_Should_Send_SyncCommand_When_Lock_Is_Acquired()
        {
            // Arrange
            var context = new Mock<IJobExecutionContext>();
            var jobDetail = new Mock<IJobDetail>();
            jobDetail.Setup(x => x.Key).Returns(new JobKey("TestJob"));
            context.Setup(x => x.JobDetail).Returns(jobDetail.Object);

            // 2. Lock nesnesinden "TryAcquireAsync" çağrıldığında Handle dönsün (Kilit Başarılı)
            _mockLock
                .Setup(x => x.TryAcquireAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .Returns(new ValueTask<IDistributedSynchronizationHandle?>(_mockLockHandle.Object));

            _mockMediator.Setup(x => x.Send(It.IsAny<SyncPosRatesCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            await _job.Execute(context.Object);

            // Assert
            _mockMediator.Verify(x => x.Send(It.IsAny<SyncPosRatesCommand>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockLockHandle.Verify(x => x.DisposeAsync(), Times.Once);
        }

        [Fact]
        public async Task Execute_Should_Skip_Execution_When_Lock_Is_NOT_Acquired()
        {
            // Arrange
            var context = new Mock<IJobExecutionContext>();
            var jobDetail = new Mock<IJobDetail>();
            jobDetail.Setup(x => x.Key).Returns(new JobKey("TestJob"));
            context.Setup(x => x.JobDetail).Returns(jobDetail.Object);

            // 2. Lock nesnesinden "TryAcquireAsync" çağrıldığında NULL dönsün (Kilit Başarısız)
            _mockLock
                .Setup(x => x.TryAcquireAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .Returns(new ValueTask<IDistributedSynchronizationHandle?>((IDistributedSynchronizationHandle?)null));

            // Act
            await _job.Execute(context.Object);

            // Assert
            _mockMediator.Verify(x => x.Send(It.IsAny<SyncPosRatesCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Execute_Should_Throw_JobExecutionException_When_Command_Fails()
        {
            // Arrange
            var context = new Mock<IJobExecutionContext>();
            var jobDetail = new Mock<IJobDetail>();
            jobDetail.Setup(x => x.Key).Returns(new JobKey("TestJob"));
            context.Setup(x => x.JobDetail).Returns(jobDetail.Object);

            _mockLock
                .Setup(x => x.TryAcquireAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .Returns(new ValueTask<IDistributedSynchronizationHandle?>(_mockLockHandle.Object));

            var exception = new Exception("Critical Database Error");
            _mockMediator.Setup(x => x.Send(It.IsAny<SyncPosRatesCommand>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);

            // Act & Assert
            await Assert.ThrowsAsync<JobExecutionException>(() => _job.Execute(context.Object));
            _mockLockHandle.Verify(x => x.DisposeAsync(), Times.Once);
        }
    }
}
