using MB.PosSelection.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using Xunit;

namespace MB.PosSelection.IntegrationTests.Infrastructure
{
    public class IntegrationTestWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
    {
        // PostgreSQL Container
        private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
            .WithImage("postgres:15-alpine")
            .WithDatabase("pos_db_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        // 2. Redis Container
        private readonly RedisContainer _redisContainer = new RedisBuilder()
            .WithImage("redis:7-alpine")
            .Build();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // Container'ların dinamik portlu connection stringlerini al
            var dbConnString = _dbContainer.GetConnectionString();

            // ",abortConnect=false" ekleyerek uygulamanın Redis hazır olana kadar beklemesini sağlıyoruz.
            var redisConnString = _redisContainer.GetConnectionString() + ",abortConnect=false";

            // Override
            // appsettings.json yerine bu değerler kullanılacak
            builder.UseSetting("ConnectionStrings:DefaultConnection", dbConnString);
            builder.UseSetting("ConnectionStrings:Redis", redisConnString);

            builder.ConfigureTestServices(services =>
            {
                // DbContext'i Test Container'a Yönlendir
                services.RemoveAll(typeof(DbContextOptions<AppDbContext>));
                services.AddDbContext<AppDbContext>(options =>
                    options.UseNpgsql(dbConnString));

                // Test sırasında karışıklığı önlemek için
                // Quartz Job'larını Testte devre dışı bırak
                var quartzService = services.FirstOrDefault(d =>
                    d.ImplementationType?.FullName == "Quartz.Hosting.QuartzHostedService");

                if (quartzService != null)
                {
                    services.Remove(quartzService);
                }
            });
        }

        public async Task InitializeAsync()
        {
            // Containerları Paralel Başlat (Hız optimizasyonu)
            await Task.WhenAll(_dbContainer.StartAsync(), _redisContainer.StartAsync());

            // Migration'ları Uygula (Veritabanı Tablolarını Oluştur)
            using var scope = Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await dbContext.Database.MigrateAsync();
        }

        public new async Task DisposeAsync()
        {
            // Temizlik
            await Task.WhenAll(
                _dbContainer.DisposeAsync().AsTask(),
                _redisContainer.DisposeAsync().AsTask()
            );
        }

        // RESILIENCE TESTİ İÇİN
        public async Task StopRedisAsync()
        {
            // Redis Container'ını durduruyoruz.
            // Bu işlem, uygulamanın Redis bağlantısını koparacak ve Timeout/ConnectionError almasına neden olacaktır.
            await _redisContainer.StopAsync();
        }
    }
}
