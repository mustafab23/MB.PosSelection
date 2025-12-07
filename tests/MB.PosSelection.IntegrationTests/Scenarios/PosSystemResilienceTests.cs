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
    public class PosSystemResilienceTests : IClassFixture<IntegrationTestWebAppFactory>
    {
        private readonly IntegrationTestWebAppFactory _factory;
        private readonly Mock<IMockPosApiService> _mockApi;

        public PosSystemResilienceTests(IntegrationTestWebAppFactory factory)
        {
            _factory = factory;
            _mockApi = new Mock<IMockPosApiService>();
        }

        [Fact]
        public async Task Should_Return_Response_From_Database_When_Redis_Crashes()
        {
            // 1. SETUP
            using var scope = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddScoped(_ => _mockApi.Object);
                });
            }).Services.CreateScope();

            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            // Mock API: Veri dönsün (QNB Finansbank)
            _mockApi.Setup(x => x.GetRatesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<PosRatioExternalDto>
                {
                    new() {
                        PosName = "QNB",
                        Currency = "TRY",
                        Installment = 1,
                        CommissionRate = 0.015m,
                        CardType = "Credit",
                        CardBrand = "CardFinans",
                        Priority = 1
                    }
                });

            // 2. VERİ YÜKLE (SYNC)
            // Bu aşamada hem Veritabanına hem Redis'e veri yazılır.
            await mediator.Send(new SyncPosRatesCommand());

            // Kontrol: Veri sistemde var mı? (Normal şartlarda çalışıyor mu?)
            var initialQuery = await mediator.Send(new SelectBestPosQuery(
                new PosSelectionRequestDto { Amount = 100, Currency = "TRY", CardBrand = "CardFinans" }
            ));
            initialQuery.Data.Should().NotBeNull();
            initialQuery.Data.PosName.Should().Be("QNB");

            // -----------------------------------------------------------
            // KAOS ANI: REDIS'I ÖLDÜR (KILL REDIS)
            // -----------------------------------------------------------
            await _factory.StopRedisAsync();

            // Bekleme (Opsiyonel): Bağlantının tamamen koptuğundan emin olmak için çok kısa beklenebilir
            // Ama genelde StopAsync yeterlidir.
            await Task.Delay(500);

            // 3. KRİTİK SORGUYU AT (ASSERT)
            // Redis yok! Sistem veritabanına (Fallback) gidip cevabı dönebilmeli.
            // Eğer kodumuzda hata yönetimi (try-catch veya FusionCache FailSafe) yoksa bu satır patlar.

            var resilientQuery = await mediator.Send(new SelectBestPosQuery(
                new PosSelectionRequestDto { Amount = 100, Currency = "TRY", CardBrand = "CardFinans" }
            ));

            // 4. SONUÇLARI DOĞRULA
            resilientQuery.Should().NotBeNull();
            resilientQuery.Data.PosName.Should().Be("QNB");
            resilientQuery.Data.Price.Should().Be(1.50m); // 100 * 0.015 = 1.50

            // Not: Eğer FusionCache veya Repository katmanında Redis hatasını yutup 
            // DB'ye gitme mantığı (Fallback) kurulmamışsa bu test kırmızı yanar.
        }
    }
}
