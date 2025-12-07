using Dapper;
using MB.PosSelection.Application.Interfaces;
using MB.PosSelection.Domain.Enums;
using MB.PosSelection.Domain.Interfaces;
using MB.PosSelection.Infrastructure.BackgroundJobs;
using MB.PosSelection.Infrastructure.Options;
using MB.PosSelection.Infrastructure.Persistence;
using MB.PosSelection.Infrastructure.Persistence.Repositories;
using MB.PosSelection.Infrastructure.Persistence.TypeHandlers;
using MB.PosSelection.Infrastructure.Services;
using MB.PosSelection.Infrastructure.Services.External;
using MB.PosSelection.Infrastructure.Services.Metrics;
using Medallion.Threading;
using Medallion.Threading.Redis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;
using Quartz;
using StackExchange.Redis;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Backplane.StackExchangeRedis;
using ZiggyCreatures.Caching.Fusion.Serialization.SystemTextJson;

namespace MB.PosSelection.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            // 1. CONFIGURATION & TYPE HANDLERS
            // ---------------------------------------------------------
            services.Configure<BusinessRuleOptions>(configuration.GetSection(BusinessRuleOptions.SectionName));

            // Dapper Type Handlers (Static configuration)
            SqlMapper.AddTypeHandler(new StringToEnumHandler<Currency>());
            SqlMapper.AddTypeHandler(new StringToEnumHandler<CardType>());

            // 2. METRICS & OBSERVABILITY (Low Level Dependencies)
            services.AddSingleton<PosMetricsService>();
            services.AddSingleton<IPosMetricsService>(sp => sp.GetRequiredService<PosMetricsService>());

            // 3. DATABASE (PERSISTENCE)
            // EF Core (PostgreSQL) - Write Side
            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

            // Dapper Support - Read Side
            // IApplicationDbContext üzerinden EF Context'e erişim sağlar
            services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<AppDbContext>());

            // 4. REDIS & CACHING INFRASTRUCTURE
            // ---------------------------------------------------------
            var redisConnectionString = configuration.GetConnectionString("Redis");

            // A) Redis Connection Multiplexer (Singleton)
            services.AddSingleton<IConnectionMultiplexer>(sp =>
            {
                var options = ConfigurationOptions.Parse(redisConnectionString);
                options.AbortOnConnectFail = false; // Redis kapalıysa bile uygulama açılsın
                options.AllowAdmin = true; // Gerekirse admin komutları için
                return ConnectionMultiplexer.Connect(options);
            });

            // B) Standart Redis Cache (IDistributedCache implementation)
            // CachedPosRateRepository içindeki pointer yönetimi için gerekli.
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnectionString;
                options.InstanceName = "PosSelection:";
            });

            // C) FusionCache (L1 + L2 + Backplane)
            services.AddFusionCache()
                .WithDefaultEntryOptions(options =>
                {
                    options.Duration = TimeSpan.FromMinutes(30);
                    options.FailSafeMaxDuration = TimeSpan.FromHours(2);
                })
                .WithSerializer(new FusionCacheSystemTextJsonSerializer())
                .WithDistributedCache(new RedisCache(new RedisCacheOptions
                {
                    Configuration = redisConnectionString
                }))
                // Podlar arasındaki eski bilgiler silinip, senkronizasyon sağlanması
                .WithBackplane(new RedisBackplane(new RedisBackplaneOptions
                {
                    Configuration = redisConnectionString
                }));

            // D) Distributed Lock Provider (Redis)
            services.AddSingleton<IDistributedLockProvider>(sp =>
            {
                var connection = sp.GetRequiredService<IConnectionMultiplexer>();
                return new RedisDistributedSynchronizationProvider(connection.GetDatabase());
            });

            // 5. REPOSITORIES (DECORATOR PATTERN)
            // ---------------------------------------------------------

            // Core Repository (Decoration yapılacak ham sınıf)
            services.AddScoped<PosRateRepository>();

            // Decorator Implementation
            services.AddScoped<IPosRateRepository>(provider =>
            {
                var coreRepo = provider.GetRequiredService<PosRateRepository>();
                var fusionCache = provider.GetRequiredService<IFusionCache>();
                var distributedCache = provider.GetRequiredService<IDistributedCache>();
                var metrics = provider.GetRequiredService<IPosMetricsService>();

                return new CachedPosRateRepository(coreRepo, fusionCache, distributedCache, metrics);
            });

            // 6. DOMAIN SERVICES & STRATEGIES
            // ---------------------------------------------------------

            // Rules
            services.AddTransient<IPosSelectionRule, Application.Rules.LowestCostRule>();
            services.AddTransient<IPosSelectionRule, Application.Rules.HighestPriorityRule>();
            services.AddTransient<IPosSelectionRule, Application.Rules.LowestCommissionRule>();
            services.AddTransient<IPosSelectionRule, Application.Rules.PosNameRule>();

            // Calculators
            services.AddSingleton<ICostCalculationStrategy, Services.Calculators.TryCostCalculator>();
            services.AddSingleton<ICostCalculationStrategy, Services.Calculators.UsdCostCalculator>();

            // Strategy Factory
            services.AddSingleton<CostCalculationStrategyFactory>();

            // 7. EXTERNAL SERVICES (HTTP Clients)
            // ---------------------------------------------------------
            services.AddHttpClient<IMockPosApiService, MockPosApiService>((sp, client) =>
            {
                var config = sp.GetRequiredService<IConfiguration>();
                var baseUrl = config["ExternalServices:PayTrPosApi:BaseUrl"];

                if (string.IsNullOrEmpty(baseUrl))
                    throw new InvalidOperationException("ExternalServices:PayTrPosApi:BaseUrl konfigürasyonu eksik!");

                client.BaseAddress = new Uri(baseUrl);
                client.Timeout = TimeSpan.FromSeconds(10);
            })
            .AddPolicyHandler(GetRetryPolicy())
            .AddPolicyHandler(GetCircuitBreakerPolicy());

            // 8. BACKGROUND JOBS (QUARTZ)
            // ---------------------------------------------------------
            services.AddQuartz(q =>
            {
                var jobKey = new JobKey("PosRateSyncJob");
                q.AddJob<PosRateSyncJob>(opts => opts.WithIdentity(jobKey));

                // TimeZone Handling
                TimeZoneInfo turkeyTimeZone = GetTurkeyTimeZone();

                q.AddTrigger(opts => opts
                    .ForJob(jobKey)
                    .WithIdentity("PosRateSyncJob-Trigger")
                    .WithCronSchedule("0 59 23 * * ?", x => x.InTimeZone(turkeyTimeZone))); // Her gece 23:59
            });

            services.AddQuartzHostedService(options =>
            {
                options.WaitForJobsToComplete = true;
            });

            return services;
        }

        // --- HELPER METHODS ---

        // TimeZone Ayarı
        // Hedef: Türkiye Saati
        // Linux/Docker: "Europe/Istanbul", Windows: "Turkey Standard Time"
        private static TimeZoneInfo GetTurkeyTimeZone()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");
            }
            catch
            {
                try
                {
                    return TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");
                }
                catch
                {
                    return TimeZoneInfo.Utc;
                }
            }
        }

        // Retry Politikası: Geçici ağ hatalarında 3 kere tekrar dener
        private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound)
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
        }

        // Circuit Breaker Politikası: Servis tamamen çöktüyse sistemi yorma
        // Ardışık 5 hatadan sonra 30 saniye boyunca istek atma (Devreyi aç)
        private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));
        }
    }
}