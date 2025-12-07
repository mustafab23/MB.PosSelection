using FluentAssertions;
using MB.PosSelection.Application.Interfaces;
using MB.PosSelection.Application.Models;
using MB.PosSelection.Domain.Entities;
using MB.PosSelection.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.Caching.Distributed;
using Moq;
using System.Text;
using Xunit;
using ZiggyCreatures.Caching.Fusion;

namespace MB.PosSelection.UnitTests.Infrastructure.Repositories
{
    public class CachedPosRateRepositoryTests
    {
        private readonly Mock<IPosRateRepository> _mockDecoratedRepo;
        private readonly Mock<IPosMetricsService> _mockMetrics;
        private readonly Mock<IDistributedCache> _mockDistCache;
        private readonly IFusionCache _realFusionCache;
        private readonly CachedPosRateRepository _repository;

        public CachedPosRateRepositoryTests()
        {
            _mockDecoratedRepo = new Mock<IPosRateRepository>();
            _mockMetrics = new Mock<IPosMetricsService>();
            _mockDistCache = new Mock<IDistributedCache>();

            // FusionCache'i "Memory" modunda başlatıyoruz
            _realFusionCache = new FusionCache(new FusionCacheOptions());

            _repository = new CachedPosRateRepository(
                _mockDecoratedRepo.Object,
                _realFusionCache,
                _mockDistCache.Object,
                _mockMetrics.Object
            );
        }

        [Fact]
        public async Task GetRatesIndexAsync_Should_Call_Decorated_Repo_When_Cache_And_Pointer_Are_Missing()
        {
            // Arrange
            // 1. Pointer (Redis) boş dönsün (GetAsync mockluyoruz çünkü GetStringAsync extension)
            _mockDistCache
                .Setup(x => x.GetAsync(It.Is<string>(k => k.Contains("pointer")), It.IsAny<CancellationToken>()))
                .ReturnsAsync((byte[])null); // Null dönerse "v_init" varsayılır

            // 2. DB'den dönecek veri
            var dbIndex = new PosRateLookupIndex(new List<PosRatio>());
            _mockDecoratedRepo.Setup(x => x.GetRatesIndexAsync()).ReturnsAsync(dbIndex);

            // Act
            var result = await _repository.GetRatesIndexAsync();

            // Assert
            result.Should().BeEquivalentTo(dbIndex);

            // Metrik artmalı
            _mockMetrics.Verify(m => m.IncrementTotalRequests(), Times.Once);

            // Veritabanına gidilmeli
            _mockDecoratedRepo.Verify(x => x.GetRatesIndexAsync(), Times.Once);
        }

        [Fact]
        public async Task RefreshCacheAsync_Should_Update_FusionCache_And_DistributedCache_Pointer()
        {
            // Arrange
            var newRates = new List<PosRatio>();
            var batchId = "batch_new";

            // Act
            await _repository.RefreshCacheAsync(newRates, batchId);

            // Assert

            // 1. FusionCache (L1) güncellendi mi? (Data Key)
            var dataKey = $"pos_rates:index:{batchId}";
            var cachedData = await _realFusionCache.GetOrDefaultAsync<PosRateLookupIndex>(dataKey);
            cachedData.Should().NotBeNull();

            // 2. Redis Pointer güncellendi mi?
            // SetStringAsync arka planda SetAsync çağırır. Onu doğruluyoruz.
            _mockDistCache.Verify(x => x.SetAsync(
                It.Is<string>(k => k.Contains("pointer")),
                It.Is<byte[]>(b => Encoding.UTF8.GetString(b) == batchId), // Değer kontrolü
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()
            ), Times.Once);

            // 3. Pointer Local Cache'den silindi mi? 
            // (FusionCache remove işlemi bellekten siler, bunu dolaylı yoldan pointer'ı tekrar okuyarak test edebiliriz
            // ama RefreshCacheAsync metodunda RemoveAsync çağrıldığı için Memory'de olmamalı veya null olmalı)
            // Ancak bu test ortamında FusionCache gerçek olduğu için RemoveAsync çalışmıştır.
        }
    }
}
