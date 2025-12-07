using MB.PosSelection.Domain.Enums;
using MB.PosSelection.Domain.Interfaces;

namespace MB.PosSelection.Infrastructure.Services
{
    public class CostCalculationStrategyFactory
    {
        private readonly IEnumerable<ICostCalculationStrategy> _strategies;

        public CostCalculationStrategyFactory(IEnumerable<ICostCalculationStrategy> strategies)
        {
            _strategies = strategies;
        }

        public ICostCalculationStrategy GetStrategy(Currency currency)
        {
            var strategy = _strategies.FirstOrDefault(s => s.SupportedCurrency == currency);

            if (strategy == null)
            {
                // Eğer o para birimi için özel bir hesaplayıcı yoksa
                // varsayılan olarak TRY gibi davran
                return _strategies.First(s => s.SupportedCurrency == Currency.TRY);
            }

            return strategy;
        }
    }
}
