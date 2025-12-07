using FluentAssertions;
using MB.PosSelection.Application.Common.Exceptions;
using MB.PosSelection.Application.Dtos;
using MB.PosSelection.Application.Features.Pos.Queries.SelectBestPos;
using MB.PosSelection.Application.Interfaces;
using MB.PosSelection.Application.Models;
using MB.PosSelection.Application.Rules;
using MB.PosSelection.Domain.Entities;
using MB.PosSelection.Domain.Enums;
using MB.PosSelection.Domain.Interfaces;
using MB.PosSelection.Infrastructure.Options;
using MB.PosSelection.Infrastructure.Services.Calculators;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace MB.PosSelection.UnitTests.Application.Handlers
{
    public class SelectBestPosQueryHandlerTests
    {
        private readonly Mock<IPosRateRepository> _mockRepo;
        private readonly Mock<IPosMetricsService> _mockMetrics;

        private readonly List<ICostCalculationStrategy> _strategies;
        private readonly List<IPosSelectionRule> _rules;

        public SelectBestPosQueryHandlerTests()
        {
            _mockRepo = new Mock<IPosRateRepository>();
            _mockMetrics = new Mock<IPosMetricsService>();

            var optionsData = new BusinessRuleOptions();
            optionsData.CurrencyMultipliers.Add("TRY", 1.00m);
            optionsData.CurrencyMultipliers.Add("USD", 1.01m);
            var options = Options.Create(optionsData);

            _strategies = new List<ICostCalculationStrategy>
        {
            new TryCostCalculator(options),
            new UsdCostCalculator(options)
        };

            _rules = new List<IPosSelectionRule>
        {
            new LowestCostRule(),
            new HighestPriorityRule(),
            new PosNameRule()
        };
        }

        [Fact]
        public async Task Handle_Should_Throw_NotFoundException_When_No_Matching_Rates_Found()
        {
            // Arrange
            // Repository boş bir Index dönsün
            _mockRepo.Setup(x => x.GetRatesIndexAsync())
                .ReturnsAsync(new PosRateLookupIndex(new List<PosRatio>()));

            var handler = new SelectBestPosQueryHandler(
                _mockRepo.Object,
                _strategies,
                _rules,
                _mockMetrics.Object
            );

            var requestDto = new PosSelectionRequestDto
            {
                Currency = "TRY",
                Amount = 100,
                Installment = 1,
                CardType = "Credit",
                CardBrand = "Bonus"
            };

            var query = new SelectBestPosQuery(requestDto);

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(query, CancellationToken.None));

            // Metriklerin çağrıldığını doğrula
            _mockMetrics.Verify(m => m.IncrementTotalRequests(), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_Return_Winner_When_Rates_Exist()
        {
            // Arrange
            // 100 TL işlem, %1 komisyon = 1 TL maliyet. Toplam 101 TL olmalı.
            var ratio = new PosRatio("batch1", "TestBank", CardType.Credit, "bonus", 1, Currency.TRY, 0.01m, 0, 1);
            var index = new PosRateLookupIndex(new List<PosRatio> { ratio });

            _mockRepo.Setup(x => x.GetRatesIndexAsync())
                .ReturnsAsync(index);

            var handler = new SelectBestPosQueryHandler(
                _mockRepo.Object,
                _strategies,
                _rules,
                _mockMetrics.Object
            );

            var requestDto = new PosSelectionRequestDto
            {
                Currency = "TRY",
                Amount = 100,
                Installment = 1,
                CardType = "Credit",
                CardBrand = "bonus"
            };

            var query = new SelectBestPosQuery(requestDto);

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Data.PosName.Should().Be("TestBank");
            result.Data.PayableTotal.Should().Be(101.00m);
        }

        [Fact]
        public async Task Handle_Should_Return_USD_Calculator_Result_When_Currency_Is_USD()
        {
            // Arrange
            // USD için %1.01 çarpanı uygulanmalı: 
            // Amount: 100
            // Rate: 0.02
            // Hesap: (100 * 0.02) * 1.01 = 2.02 Maliyet
            // Total: 102.02
            // Mock veride CardBrand = "bonus"
            var ratio = new PosRatio("b1", "UsdBank", CardType.Credit, "bonus", 1, Currency.USD, 0.02m, 0, 1);
            var index = new PosRateLookupIndex(new[] { ratio });

            _mockRepo.Setup(x => x.GetRatesIndexAsync()).ReturnsAsync(index);

            var handler = new SelectBestPosQueryHandler(_mockRepo.Object, _strategies, _rules, _mockMetrics.Object);

            var query = new SelectBestPosQuery(new PosSelectionRequestDto
            {
                Amount = 100,
                Currency = "USD",
                CardType = "Credit",
                Installment = 1,
                CardBrand = "bonus"
            });

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Data.Price.Should().Be(2.02m);
            result.Data.PosName.Should().Be("UsdBank");
        }

        [Fact]
        public async Task Handle_Should_Filter_Out_Mismatched_Installments()
        {
            // Arrange
            // Repository'de sadece 6 taksit seçeneği var
            var ratio = new PosRatio("b1", "Bank6", CardType.Credit, "bonus", 6, Currency.TRY, 0.02m, 0, 1);
            var index = new PosRateLookupIndex(new[] { ratio });

            _mockRepo.Setup(x => x.GetRatesIndexAsync()).ReturnsAsync(index);

            var handler = new SelectBestPosQueryHandler(_mockRepo.Object, _strategies, _rules, _mockMetrics.Object);

            // Biz 3 taksit istiyoruz -> Eşleşme yok -> Hata fırlatmalı
            var query = new SelectBestPosQuery(new PosSelectionRequestDto
            {
                Amount = 100,
                Currency = "TRY",
                CardType = "Credit",
                CardBrand = "bonus",
                Installment = 3
            });

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(query, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_Should_Select_Winner_Reason_When_Multiple_Options_Exist()
        {
            // Arrange
            // BankA: Maliyet 2.00, Priority 5
            // BankB: Maliyet 2.00, Priority 10 (Maliyetler eşit olduğu için Priority'ye bakılır, Kazanan bu olmalı)
            // Mock veride CardBrand = "axess"
            var bankA = new PosRatio("b1", "BankA", CardType.Credit, "axess", 1, Currency.TRY, 0.02m, 0, 5);
            var bankB = new PosRatio("b1", "BankB", CardType.Credit, "axess", 1, Currency.TRY, 0.02m, 0, 10);

            var index = new PosRateLookupIndex(new[] { bankA, bankB });
            _mockRepo.Setup(x => x.GetRatesIndexAsync()).ReturnsAsync(index);

            var handler = new SelectBestPosQueryHandler(_mockRepo.Object, _strategies, _rules, _mockMetrics.Object);

            var query = new SelectBestPosQuery(new PosSelectionRequestDto
            {
                Amount = 100,
                Currency = "TRY",
                Installment = 1,
                CardType = "Credit",
                CardBrand = "axess"
            });

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Data.PosName.Should().Be("BankB");

            // Metadata kontrolü: Kazanma sebebinin "HighestPriority" kuralı olduğunu doğrula
            result.Meta.SelectionReason.Should().Contain("HighestPriority");
        }
    }
}
