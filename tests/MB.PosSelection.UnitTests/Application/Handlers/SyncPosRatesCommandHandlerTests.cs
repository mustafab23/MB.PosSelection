using FluentAssertions;
using MB.PosSelection.Application.Dtos.External;
using MB.PosSelection.Application.Features.Pos.Commands.SyncPosRates;
using MB.PosSelection.Application.Interfaces;
using MB.PosSelection.Domain.Entities;
using MB.PosSelection.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MB.PosSelection.UnitTests.Application.Handlers
{
    public class SyncPosRatesCommandHandlerTests
    {
        private readonly Mock<IMockPosApiService> _mockApiService;
        private readonly Mock<IApplicationDbContext> _mockDbContext;
        private readonly Mock<IPosRateRepository> _mockRepository;
        private readonly Mock<ILogger<SyncPosRatesCommandHandler>> _mockLogger;
        private readonly Mock<DbSet<PosRatio>> _mockDbSet;

        private readonly SyncPosRatesCommandHandler _handler;

        public SyncPosRatesCommandHandlerTests()
        {
            _mockApiService = new Mock<IMockPosApiService>();
            _mockDbContext = new Mock<IApplicationDbContext>();
            _mockRepository = new Mock<IPosRateRepository>();
            _mockLogger = new Mock<ILogger<SyncPosRatesCommandHandler>>();

            _mockDbSet = new Mock<DbSet<PosRatio>>();
            _mockDbContext.Setup(x => x.PosRatios).Returns(_mockDbSet.Object);

            _handler = new SyncPosRatesCommandHandler(
                _mockApiService.Object,
                _mockDbContext.Object,
                _mockRepository.Object,
                _mockLogger.Object
            );
        }

        [Fact]
        public async Task Handle_Should_Return_False_And_Do_Nothing_When_Api_Returns_Empty()
        {
            // Arrange
            _mockApiService.Setup(x => x.GetRatesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<PosRatioExternalDto>());

            // Act
            var result = await _handler.Handle(new SyncPosRatesCommand(), CancellationToken.None);

            // Assert
            result.Should().BeFalse();
            _mockDbSet.Verify(x => x.AddRangeAsync(It.IsAny<IEnumerable<PosRatio>>(), It.IsAny<CancellationToken>()), Times.Never);
            _mockDbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
            _mockRepository.Verify(x => x.RefreshCacheAsync(It.IsAny<IEnumerable<PosRatio>>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_Save_To_Db_And_Update_Cache_On_Success()
        {
            // Arrange
            var apiData = new List<PosRatioExternalDto>
        {
            new() { PosName = "BankA", Currency = "TRY", CardType = "Credit", Installment = 1, CommissionRate = 0.01m, CardBrand = "bonus" },
            new() { PosName = "BankB", Currency = "USD", CardType = "Debit", Installment = 1, CommissionRate = 0.02m, CardBrand = "axess" }
        };

            _mockApiService.Setup(x => x.GetRatesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(apiData);

            // Act
            var result = await _handler.Handle(new SyncPosRatesCommand(), CancellationToken.None);

            // Assert
            result.Should().BeTrue();

            _mockDbSet.Verify(x => x.AddRangeAsync(
                It.Is<IEnumerable<PosRatio>>(r => r.Count() == 2),
                It.IsAny<CancellationToken>()), Times.Once);

            _mockDbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            _mockRepository.Verify(x => x.RefreshCacheAsync(
                It.Is<IEnumerable<PosRatio>>(r => r.Count() == 2),
                It.IsAny<string>()), Times.Once);

            _mockRepository.Verify(x => x.CleanOldBatchesAsync(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_Map_Dto_To_Entity_Correctly()
        {
            // Arrange
            var apiData = new List<PosRatioExternalDto>
        {
            new()
            {
                PosName = "TestBank",
                Currency = "TRY",
                CardType = "Credit",
                CardBrand = "Bonus", 
                Installment = 3,
                CommissionRate = 0.015m,
                MinFee = 0,
                Priority = 1
            }
        };

            _mockApiService.Setup(x => x.GetRatesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(apiData);

            List<PosRatio> capturedEntities = new();

            _mockDbSet
                .Setup(x => x.AddRangeAsync(It.IsAny<IEnumerable<PosRatio>>(), It.IsAny<CancellationToken>()))
                .Callback<IEnumerable<PosRatio>, CancellationToken>((entities, ct) => capturedEntities.AddRange(entities))
                .Returns(Task.CompletedTask);

            // Act
            await _handler.Handle(new SyncPosRatesCommand(), CancellationToken.None);

            // Assert
            capturedEntities.Should().ContainSingle();
            var entity = capturedEntities.First();

            entity.PosName.Should().Be("TestBank");
            entity.Currency.Should().Be(Currency.TRY);
            entity.CardType.Should().Be(CardType.Credit);
            entity.CardBrand.Should().Be("bonus");
            entity.Installment.Should().Be(3);
            entity.BatchId.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public async Task Handle_Should_Not_Update_Cache_If_Database_Save_Fails()
        {
            // Arrange
            // Hatanın veritabanı aşamasında (SaveChangesAsync) çıkmasını istiyoruz,
            // Entity oluşturma aşamasında değil.
            _mockApiService.Setup(x => x.GetRatesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<PosRatioExternalDto>
                {
                new() { PosName = "BankA", CardBrand = "bonus", Installment = 1, Currency = "TRY", CardType = "Credit" }
                });

            _mockDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new DbUpdateException("DB Connection Failed"));

            // Act
            Func<Task> act = async () => await _handler.Handle(new SyncPosRatesCommand(), CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<DbUpdateException>();

            // Cache güncellenmemeli
            _mockRepository.Verify(x => x.RefreshCacheAsync(It.IsAny<IEnumerable<PosRatio>>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_Throw_Exception_If_Cache_Refresh_Fails()
        {
            // Arrange
            _mockApiService.Setup(x => x.GetRatesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<PosRatioExternalDto>
                {
                new() { PosName = "BankA", CardBrand = "bonus", Installment = 1, Currency = "TRY", CardType = "Credit" }
                });

            _mockRepository.Setup(x => x.RefreshCacheAsync(It.IsAny<IEnumerable<PosRatio>>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("Redis Connection Failed"));

            // Act
            Func<Task> act = async () => await _handler.Handle(new SyncPosRatesCommand(), CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<Exception>().WithMessage("Redis Connection Failed");

            // DB'ye yazılmış olmalı (Rollback olmadığı sürece)
            _mockDbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
