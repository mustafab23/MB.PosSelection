using FluentAssertions;
using MB.PosSelection.Application.Dtos;
using MB.PosSelection.Application.Dtos.External;
using MB.PosSelection.Application.Features.Pos.Commands.SyncPosRates;
using MB.PosSelection.Application.Features.Pos.Queries.SelectBestPos;
using MB.PosSelection.Application.Interfaces;
using MB.PosSelection.IntegrationTests.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace MB.PosSelection.IntegrationTests.Scenarios
{
    public class PosSystemConcurrencyTests : IClassFixture<IntegrationTestWebAppFactory>
    {
        private readonly IntegrationTestWebAppFactory _factory;
        private readonly Mock<IMockPosApiService> _mockApi;

        public PosSystemConcurrencyTests(IntegrationTestWebAppFactory factory)
        {
            _factory = factory;
            _mockApi = new Mock<IMockPosApiService>();
        }

        [Fact]
        public async Task Should_Handle_High_Traffic_And_Updates_Simultaneously_Without_Error()
        {
            // "Service Scoped" olduğu için her request kendi scope'unda mock'a erişecek.
            // Bu yüzden global bir mock setup yapıyoruz.
            _mockApi.Setup(x => x.GetRatesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<PosRatioExternalDto>
                {
                    new() { PosName = "YapiKredi", Currency = "TRY", Installment = 1, CommissionRate = 0.019m, CardType = "Credit", CardBrand = "World", Priority = 1 }
                });

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddScoped(_ => _mockApi.Object);
                });
            });

            // Önce veritabanına bir veri basalım (Day 1)
            using (var scope = client.Services.CreateScope())
            {
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                await mediator.Send(new SyncPosRatesCommand()); // İlk veri (0.019 oranı) yüklendi
            }

            // ----------------------------------------------------------------
            // KAOS ANI: CONCURRENCY TEST BAŞLIYOR
            // Senaryo: Bir yandan UPDATE yapılırken, aynı anda 50 kullanıcı SORGULAMA yapıyor.
            // ----------------------------------------------------------------

            // GÖREV 1: YAZMA İŞLEMİ (UPDATE)
            // Mock API oranını değiştiriyoruz (Simülasyon: Ertesi gün oldu, oran %1.50'ye düştü)
            _mockApi.Setup(x => x.GetRatesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<PosRatioExternalDto>
                {
                    new() { PosName = "YapiKredi", Currency = "TRY", Installment = 1, CommissionRate = 0.015m, CardType = "Credit", CardBrand = "World", Priority = 1 }
                });

            var updateTask = Task.Run(async () =>
            {
                // Her Thread/Task kendi Scope'unu yaratmalı (Simulating Real HTTP Requests)
                using var scope = client.Services.CreateScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                // Biraz gecikme ekleyerek okuma işlemleriyle çakışmasını garantileyelim
                await Task.Delay(50);
                await mediator.Send(new SyncPosRatesCommand());
            });

            // GÖREV 2: OKUMA İŞLEMLERİ (READ - HIGH TRAFFIC)
            // 50 Adet eşzamanlı istek
            var readTasks = Enumerable.Range(0, 50).Select(async i =>
            {
                // Her request kendi Scope'unda çalışır! (EF Core Context Thread-Safe değildir)
                using var scope = client.Services.CreateScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                // Rastgele küçük gecikmelerle gerçekçi trafik simülasyonu
                await Task.Delay(Random.Shared.Next(0, 100));

                return await mediator.Send(new SelectBestPosQuery(
                    new PosSelectionRequestDto { Amount = 100, Currency = "TRY", CardBrand = "World" }
                ));
            }).ToList();

            // 3. AKSİYON: Hepsini aynı anda bekliyoruz
            var allTasks = new List<Task> { updateTask };
            allTasks.AddRange(readTasks);

            await Task.WhenAll(allTasks);

            // ----------------------------------------------------------------
            // 4. ASSERTIONS (DOĞRULAMA)
            // ----------------------------------------------------------------

            // A. Hiçbir okuma işlemi hata almamalı (Exception fırlatılmamalı)
            // Race Condition varsa burada "Collection was modified" veya "DbContext disposed" hataları alırdık.
            var results = await Task.WhenAll(readTasks);
            results.Should().NotContainNulls();

            // B. Tutarlılık Kontrolü:
            // Okunan değer ya ESKİ (%1.9) ya da YENİ (%1.5) olmalı.
            // ASLA "0", "null" veya saçma bir değer olmamalı.
            foreach (var result in results)
            {
                result.Data.PosName.Should().Be("YapiKredi");
                // Oran ya 0.019 (1.90 TL) ya da 0.015 (1.50 TL) olmalı
                result.Data.Price.Should().Match(p => p == 1.90m || p == 1.50m);
            }

            // C. Son Durum Kontrolü (Eventual Consistency):
            // Her şey bittiğinde veritabanında son sürüm (%1.50) olmalı.
            using (var scope = client.Services.CreateScope())
            {
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                var finalResult = await mediator.Send(new SelectBestPosQuery(
                     new PosSelectionRequestDto { Amount = 100, Currency = "TRY", CardBrand = "World" }
                ));

                finalResult.Data.Price.Should().Be(1.50m);
            }
        }
    }
}
