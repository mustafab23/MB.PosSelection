using MB.PosSelection.Domain.Enums;
using MB.PosSelection.Domain.Interfaces;
using MB.PosSelection.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace MB.PosSelection.Infrastructure.Services.Calculators
{
    public class UsdCostCalculator : ICostCalculationStrategy
    {
        public Currency SupportedCurrency => Currency.USD;
        private readonly BusinessRuleOptions _options;

        public UsdCostCalculator(IOptions<BusinessRuleOptions> options)
        {
            _options = options.Value;
        }

        public decimal CalculateCost(decimal amount, decimal commissionRate, decimal minFee)
        {
            // Config'den USD çarpanını (1.01) çekiyoruz
            var multiplier = _options.GetMultiplier("USD");

            // Maliyeti Hesapla
            var rawCost = amount * commissionRate * multiplier;

            // 2 Hane Yuvarlama (Half-Up)
            var roundedCost = Math.Round(rawCost, 2, MidpointRounding.AwayFromZero);

            // Min Fee ile Kıyasla
            return Math.Max(roundedCost, minFee);
        }
    }
}
