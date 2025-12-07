using MB.PosSelection.Domain.Enums;
using MB.PosSelection.Domain.Interfaces;
using MB.PosSelection.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace MB.PosSelection.Infrastructure.Services.Calculators
{
    public class TryCostCalculator : ICostCalculationStrategy
    {
        public Currency SupportedCurrency => Currency.TRY;
        private readonly BusinessRuleOptions _options;

        public TryCostCalculator(IOptions<BusinessRuleOptions> options)
        {
            _options = options.Value;
        }

        public decimal CalculateCost(decimal amount, decimal commissionRate, decimal minFee)
        {
            // Config'den TRY çarpanını (1.00) çekiyoruz
            var multiplier = _options.GetMultiplier("TRY");

            // Maliyeti Hesapla
            var rawCost = amount * commissionRate * multiplier;

            // 2 Hane Yuvarlama (Half-Up)
            var roundedCost = Math.Round(rawCost, 2, MidpointRounding.AwayFromZero);

            // Min Fee ile Kıyasla
            return Math.Max(roundedCost, minFee);
        }
    }
}
