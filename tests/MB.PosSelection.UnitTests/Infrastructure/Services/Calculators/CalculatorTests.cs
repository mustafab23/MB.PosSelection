using FluentAssertions;
using MB.PosSelection.Infrastructure.Options;
using MB.PosSelection.Infrastructure.Services.Calculators;
using Microsoft.Extensions.Options;
using Xunit;

namespace MB.PosSelection.UnitTests.Infrastructure.Services.Calculators
{
    public class CalculatorTests
    {
        private IOptions<BusinessRuleOptions> CreateOptions()
        {
            var rules = new BusinessRuleOptions();
            rules.CurrencyMultipliers.Add("TRY", 1.00m);
            rules.CurrencyMultipliers.Add("USD", 1.01m);

            return Options.Create(rules);
        }

        [Theory]
        [InlineData(100, 0.02, 0, 2.00)] // 100 * 0.02 = 2.00
        [InlineData(100, 0.01, 5, 5.00)] // 1.00 < MinFee(5) -> 5.00
        [InlineData(100.12, 0.015, 0, 1.50)] // 1.5018 -> Yuvarlama -> 1.50
        [InlineData(100, 0.01996, 2.00, 2.00)] // Raw: 1.996 -> Round: 2.00 -> Max(2.00, 2.00) = 2.00
        public void TryCostCalculator_Should_Calculate_Correctly(decimal amount, decimal rate, decimal minFee, decimal expectedCost)
        {
            // Arrange
            var calculator = new TryCostCalculator(CreateOptions());

            // Act
            var result = calculator.CalculateCost(amount, rate, minFee);

            // Assert
            result.Should().Be(expectedCost);
        }

        [Theory]
        [InlineData(100, 0.02, 0, 2.02)] // USD: (100 * 0.02) * 1.01 = 2.02
        [InlineData(100, 0.01, 5, 5.00)] // MinFee: (100 * 0.01 * 1.01) = 1.01 < 5 => 5
        public void UsdCostCalculator_Should_Apply_Extra_Multiplier(decimal amount, decimal rate, decimal minFee, decimal expectedCost)
        {
            // Arrange
            var calculator = new UsdCostCalculator(CreateOptions());

            // Act
            var result = calculator.CalculateCost(amount, rate, minFee);

            // Assert
            result.Should().Be(expectedCost);
        }
    }
}
