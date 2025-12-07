using FluentAssertions;
using MB.PosSelection.Application.Dtos;
using MB.PosSelection.Application.Dtos.External;
using MB.PosSelection.Application.Features.Pos.Commands.SyncPosRates;
using MB.PosSelection.Application.Features.Pos.Queries.SelectBestPos;
using MB.PosSelection.Application.Interfaces;
using MB.PosSelection.Infrastructure.Persistence;
using MB.PosSelection.IntegrationTests.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace MB.PosSelection.IntegrationTests.Scenarios
{
    public class PosSystemEndToEndTests : IClassFixture<IntegrationTestWebAppFactory>
    {
        private readonly IntegrationTestWebAppFactory _factory;
        private readonly Mock<IMockPosApiService> _mockApi;

        public PosSystemEndToEndTests(IntegrationTestWebAppFactory factory)
        {
            _factory = factory;
            _mockApi = new Mock<IMockPosApiService>();
        }

        [Fact]
        public async Task AtomicSwitch_Should_UpdateRates_And_ArchiveOldOnes()
        {
            // ARRANGE: Web uygulamasını Mock API ile yapılandır
            using var scope = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    // Gerçek MockPosApiService yerine bizim kontrolümüzdeki Mock nesnesini ver
                    services.AddScoped(_ => _mockApi.Object);
                });
            }).Services.CreateScope();

            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // ----------------------------------------------------------------
            // 🔥 KRİTİK DÜZELTME: TEMİZLİK (CLEANUP)
            // ----------------------------------------------------------------
            // Seed datadan gelen verileri siliyoruz ki test steril olsun.
            dbContext.PosRatios.RemoveRange(dbContext.PosRatios);
            dbContext.PosRatioHistories.RemoveRange(dbContext.PosRatioHistories);
            await dbContext.SaveChangesAsync();

            // ----------------------------------------------------------------
            // SENARYO FAZ 1: İlk Veri Yükleme (Day 1)
            // ----------------------------------------------------------------

            // Mock API: Garanti %1.50 komisyon veriyor
            _mockApi.Setup(x => x.GetRatesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<PosRatioExternalDto>
                {
                    new() { PosName = "Garanti", Currency = "TRY", Installment = 1, CommissionRate = 0.015m, CardType = "Credit", CardBrand = "Bonus", Priority = 1 }
                });

            // ACT: Sync komutunu çalıştır
            await mediator.Send(new SyncPosRatesCommand());

            // ASSERT FAZ 1:
            var activeRatesDay1 = await dbContext.PosRatios.ToListAsync();
            activeRatesDay1.Should().HaveCount(1);
            activeRatesDay1.First().CommissionRate.Should().Be(0.015m);
            var batchIdDay1 = activeRatesDay1.First().BatchId; // Batch ID'yi sakla

            // ----------------------------------------------------------------
            // SENARYO FAZ 2: İkinci Veri Yükleme (Day 2 - Atomic Switch)
            // ----------------------------------------------------------------

            // Mock API: Ertesi gün Garanti indirim yaptı, oran %1.20 oldu
            _mockApi.Setup(x => x.GetRatesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<PosRatioExternalDto>
                {
                    new() { PosName = "Garanti", Currency = "TRY", Installment = 1, CommissionRate = 0.012m, CardType = "Credit", CardBrand = "Bonus", Priority = 1 }
                });

            // ACT: Sync komutunu TEKRAR çalıştır
            await mediator.Send(new SyncPosRatesCommand());

            // ----------------------------------------------------------------
            // ASSERT FAZ 3: Final Kontroller (Principal Developer Onayı)
            // ----------------------------------------------------------------

            // 1. Aktif tablo güncellendi mi?
            var activeRatesDay2 = await dbContext.PosRatios.ToListAsync();
            activeRatesDay2.Should().HaveCount(1);
            activeRatesDay2.First().CommissionRate.Should().Be(0.012m, "Çünkü yeni oran %1.20 olmalı");
            activeRatesDay2.First().BatchId.Should().NotBe(batchIdDay1, "Çünkü yeni bir BatchId atanmalı");

            // 2. Eski veri Arşive gitti mi?
            var archivedRates = await dbContext.PosRatioHistories.ToListAsync();
            archivedRates.Should().HaveCount(1);
            archivedRates.First().BatchId.Should().Be(batchIdDay1, "Çünkü eski batch arşive taşınmalı");
            archivedRates.First().CommissionRate.Should().Be(0.015m, "Çünkü arşivdeki veri eski oran olmalı");

            // 3. Sistem (Query) doğru çalışıyor mu?
            // Constructor (Parantez) yerine Object Initializer (Süslü Parantez) kullanıyoruz.
            var queryResult = await mediator.Send(new SelectBestPosQuery(
                new PosSelectionRequestDto
                {
                    Amount = 100,
                    Installment = 1,
                    Currency = "TRY",
                    CardType = "Credit",
                    CardBrand = "Bonus"
                }
            ));

            // Beklenen: Yeni oran (%1.20) üzerinden hesaplama
            // 100 * 0.012 = 1.20 TL Maliyet
            // 100 + 1.20 = 101.20 TL Toplam
            queryResult.Data.Price.Should().Be(1.20m);
            queryResult.Data.PayableTotal.Should().Be(101.20m);
        }
    }
}
