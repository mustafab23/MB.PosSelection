using MB.PosSelection.Domain.Entities;

namespace MB.PosSelection.Application.Models
{
    /// <summary>
    /// POS oranlarını O(1) hızında bulmak için optimize edilmiş In-Memory Index yapısı.
    /// </summary>
    public class PosRateLookupIndex
    {
        private readonly Dictionary<string, List<PosRatio>> _index;

        public PosRateLookupIndex(IEnumerable<PosRatio> rates)
        {
            _index = new Dictionary<string, List<PosRatio>>(StringComparer.OrdinalIgnoreCase);
            BuildIndex(rates);
        }

        public PosRateLookupIndex()
        {
            _index = new Dictionary<string, List<PosRatio>>(StringComparer.OrdinalIgnoreCase);
        }

        private void BuildIndex(IEnumerable<PosRatio> rates)
        {
            foreach (var rate in rates)
            {
                // Sadece zorunlu alanlardan anahtar oluşturuyoruz.
                // Bu sayede "TRY_6" isteyen biri, Bonus/Axess/World hepsine erişebilir.
                var key = GenerateKey(rate.Currency.ToString(), rate.Installment);

                if (!_index.ContainsKey(key))
                {
                    _index[key] = new List<PosRatio>();
                }
                _index[key].Add(rate);
            }
        }

        public List<PosRatio> FindRates(string currency, int installment, string? cardType, string? cardBrand)
        {
            // Geniş Havuzu Bul (O(1))
            var key = GenerateKey(currency, installment);

            if (!_index.TryGetValue(key, out var candidates))
            {
                return new List<PosRatio>();
            }

            // Bellek İçi Filtreleme
            var filtered = candidates.AsEnumerable();

            // Kart Tipi Filtresi
            if (!string.IsNullOrWhiteSpace(cardType))
            {
                filtered = filtered.Where(r => r.CardType.ToString().Equals(cardType, StringComparison.OrdinalIgnoreCase));
            }

            // Kart Markası Filtresi
            if (!string.IsNullOrWhiteSpace(cardBrand))
            {
                filtered = filtered.Where(r =>
                    string.Equals(r.CardBrand, cardBrand, StringComparison.OrdinalIgnoreCase)
                );
            }

            return filtered.ToList();
        }

        private static string GenerateKey(string currency, int installment)
        {
            // Key Formatı: TRY_6
            return $"{currency.ToUpperInvariant()}_{installment}";
        }
    }
}
