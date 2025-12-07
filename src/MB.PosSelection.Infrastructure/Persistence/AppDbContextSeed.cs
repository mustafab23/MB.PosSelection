using MB.PosSelection.Domain.Entities;
using MB.PosSelection.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace MB.PosSelection.Infrastructure.Persistence
{
    public static class AppDbContextSeed
    {
        public static async Task SeedAsync(AppDbContext context)
        {
            // Eğer veritabanında hiç veri yoksa ekle
            if (!await context.PosRatios.AnyAsync())
            {
                var rates = new List<PosRatio>
            {
                // PDF'teki Örnek Verilerden Bazıları
                
                // Garanti (TRY - 6 Taksit)
                new PosRatio("20251207_235942146", "Garanti", CardType.Credit, "bonus", 6, Currency.TRY, 0.027m, 0.00m, 0),
                
                // KuveytTurk (TRY - 6 Taksit - Daha ucuz oran)
                new PosRatio("20251207_235942146", "KuveytTurk", CardType.Credit, "saglam", 6, Currency.TRY, 0.0260m, 2.00m, 0),
                
                // Denizbank (USD - 3 Taksit)
                new PosRatio("20251207_235942146", "Denizbank", CardType.Credit, "bonus", 3, Currency.USD, 0.0310m, 0.00m, 0),
                
                // QNB (TRY - 3 Taksit)
                new PosRatio("20251207_235942146", "QNB", CardType.Credit, "cardfinans", 3, Currency.TRY, 0.0229m, 0.00m, 0),
                
                // YapiKredi (TRY - 12 Taksit - Priority testi için)
                new PosRatio("20251207_235942146", "YapiKredi", CardType.Credit, "world", 12, Currency.TRY, 0.0310m, 0.00m, 7),
                new PosRatio("20251207_235942146", "Akbank", CardType.Credit, "axess", 12, Currency.TRY, 0.0310m, 0.00m, 6) // Düşük priority
            };

                await context.PosRatios.AddRangeAsync(rates);
                await context.SaveChangesAsync();
            }
        }
    }
}
