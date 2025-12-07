using MB.PosSelection.Application.Interfaces;
using MB.PosSelection.Application.Models;
using MB.PosSelection.Domain.Entities;
using Microsoft.Extensions.Caching.Distributed;
using ZiggyCreatures.Caching.Fusion;

namespace MB.PosSelection.Infrastructure.Persistence.Repositories
{
    public class CachedPosRateRepository : IPosRateRepository
    {
        private readonly IPosRateRepository _decorated; // Dapper Repository (Veritabanı)
        private readonly IFusionCache _cache; // Hybrid Cache (RAM + Redis) L1 + L2 Cache
        private readonly IDistributedCache _distributedCache; // Pointer yönetimi için saf Redis erişimi
        private readonly IPosMetricsService _metrics;

        // Pointer Key: Bu anahtarın değeri, datanın nerede olduğunu söyler.
        // Örn Değer: "v_20251205_1400"
        private const string CurrentVersionKey = "pos_rates:pointer:current_version";

        public CachedPosRateRepository(
            IPosRateRepository decorated,
            IFusionCache cache,
            IDistributedCache distributedCache,
            IPosMetricsService metrics)
        {
            _decorated = decorated;
            _cache = cache;
            _distributedCache = distributedCache;
            _metrics = metrics;
        }

        public async Task<PosRateLookupIndex> GetRatesIndexAsync()
        {
            _metrics.IncrementTotalRequests();

            // Aktif Versiyonu (Pointer) Bul
            var activeVersion = await _cache.GetOrSetAsync<string>(
                CurrentVersionKey,
                async (ctx, ct) =>
                {
                    ctx.Options.Duration = TimeSpan.FromSeconds(5);
                    ctx.Options.FailSafeMaxDuration = TimeSpan.FromMinutes(10);

                    var version = await _distributedCache.GetStringAsync(CurrentVersionKey, ct);
                    return version ?? "v_init";
                }
            );

            // Redis tamamen boşsa veya pointer yoksa, DB'den en güncel Batch'i çek.
            if (activeVersion == "v_init")
            {
                return await _decorated.GetRatesIndexAsync();
            }            

            // 2. Lookup Index'i Çek (L1 + L2)
            var dataKey = $"pos_rates:index:{activeVersion}";

            var index = await _cache.GetOrSetAsync<PosRateLookupIndex>(
                dataKey,
                async (ctx, ct) =>
                {
                    _metrics.IncrementCacheMiss();

                    ctx.Options.Duration = TimeSpan.FromHours(24);
                    return await _decorated.GetRatesIndexAsync();
                }
            );

            return index ?? new PosRateLookupIndex(Enumerable.Empty<PosRatio>());
        }

        public async Task RefreshCacheAsync(IEnumerable<PosRatio> rates, string batchId)
        {
            // Listeyi O(1) Index formatına çevir
            var lookupIndex = new PosRateLookupIndex(rates);

            // Yeni Key ile Cache'e yaz (pos_rates:index:v_20251205_1530)
            var newDataKey = $"pos_rates:index:{batchId}";

            // FusionCache'e set ediyoruz. Backplane (Pub/Sub) sayesinde
            // bu işlem diğer pod'ları haberdar edebilir (eğer remove/expire olursa).
            // Ancak burada yeni bir key set ediyoruz, bu yüzden conflict yok.
            await _cache.SetAsync(
                newDataKey,
                lookupIndex,
                options => options.SetDuration(TimeSpan.FromHours(24))
            );

            // Pointer Güncellemesi
            await _distributedCache.SetStringAsync(CurrentVersionKey, batchId);

            // Pointer Cache'i Temizle
            // Backplane aktif olduğunda, bu komut TÜM POD'LARDAKİ
            // CurrentVersionKey'i L1 (Memory) cache'den siler.
            // Böylece tüm podlar 5 saniye beklemeden anında yeni versiyona geçer
            await _cache.RemoveAsync(CurrentVersionKey);
        }

        public async Task CleanOldBatchesAsync(string activeBatchId)
        {
            await _decorated.CleanOldBatchesAsync(activeBatchId);
        }
    }
}
