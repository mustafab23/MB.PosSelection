namespace MB.PosSelection.Infrastructure.Options
{
    public class BusinessRuleOptions
    {
        public const string SectionName = "BusinessRules";

        public Dictionary<string, decimal> CurrencyMultipliers { get; set; } = new();

        public decimal GetMultiplier(string currencyCode)
        {
            // Eğer currency tanımlı değilse varsayılan olarak 1.00 dön
            return CurrencyMultipliers.TryGetValue(currencyCode, out var multiplier)
                ? multiplier
                : 1.00m;
        }
    }
}
