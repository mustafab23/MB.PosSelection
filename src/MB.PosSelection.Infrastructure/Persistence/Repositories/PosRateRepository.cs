using Dapper;
using MB.PosSelection.Application.Interfaces;
using MB.PosSelection.Application.Models;
using MB.PosSelection.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System.Data;

namespace MB.PosSelection.Infrastructure.Persistence.Repositories
{
    public class PosRateRepository : IPosRateRepository
    {
        private readonly AppDbContext _dbContext; // Yazma işlemleri için EF Core Context
        private readonly IConfiguration _configuration;

        public PosRateRepository(IConfiguration configuration, AppDbContext dbContext)
        {
            _configuration = configuration;
            _dbContext = dbContext;
        }

        // Dapper bağlantısı (Sadece Okuma için)
        private IDbConnection CreateConnection()
        {
            return new NpgsqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        }
        
        public async Task<PosRateLookupIndex> GetRatesIndexAsync()
        {
            // OKUMA: Dapper (Fail-Safe mekanizması devreye girerse burası çalışır)
            using var connection = CreateConnection();

            // Şu an aktif olan BatchId'yi bul
            const string sql = @"
                WITH LatestBatch AS (
                    SELECT ""BatchId"" 
                    FROM ""PosRatios"" 
                    ORDER BY ""CreatedAt"" DESC
                    LIMIT 1
                )
                SELECT 
                    ""Id"", ""BatchId"", ""PosName"", ""CardType"", 
                    ""CardBrand"", ""Installment"", ""Currency"", 
                    ""CommissionRate"", ""MinFee"", ""Priority""
                FROM ""PosRatios""
                WHERE ""BatchId"" = (SELECT ""BatchId"" FROM LatestBatch)";

            var rates = await connection.QueryAsync<PosRatio>(sql);

            // Index Oluştur ve Dön
            return new PosRateLookupIndex(rates);
        }

        public Task RefreshCacheAsync(IEnumerable<PosRatio> rates, string batchId)
        {
            // Dapper repository'nin cache ile işi yoktur.
            // Bu metot Decorator pattern gereği buradadır ama boş döner.
            // Veritabanına yazma işi CommandHandler içinde "Bulk Insert" ile yapılıyor.
            return Task.CompletedTask;
        }

        public async Task CleanOldBatchesAsync(string activeBatchId)
        {
            // YAZMA: EF Core
            var strategy = _dbContext.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _dbContext.Database.BeginTransactionAsync();
                try
                {
                    // Arşivleme
                    const string archiveSql = @"
                        INSERT INTO ""PosRatioHistories"" (
                            ""Id"", ""OriginalPosRatioId"", ""ArchivedAt"", ""BatchId"", 
                            ""PosName"", ""CardType"", ""CardBrand"", 
                            ""Installment"", ""Currency"", ""CommissionRate"", 
                            ""MinFee"", ""Priority"", 
                            ""CreatedBy"", ""CreatedAt"", ""LastModifiedBy"", ""LastModifiedAt""
                        )
                        SELECT 
                            gen_random_uuid(), ""Id"", (NOW() AT TIME ZONE 'UTC'), ""BatchId"", 
                            ""PosName"", ""CardType""::text, ""CardBrand"", 
                            ""Installment"", ""Currency""::text, ""CommissionRate"", 
                            ""MinFee"", ""Priority"", 
                            ""CreatedBy"", ""CreatedAt"", ""LastModifiedBy"", ""LastModifiedAt""
                        FROM ""PosRatios""
                        WHERE ""BatchId"" != {0}; 
                    ";

                    await _dbContext.Database.ExecuteSqlRawAsync(archiveSql, activeBatchId);

                    // Silme (Eski Batch'leri ana tablodan uçur)
                    const string deleteSql = @"DELETE FROM ""PosRatios"" WHERE ""BatchId"" != {0}";
                    await _dbContext.Database.ExecuteSqlRawAsync(deleteSql, activeBatchId);

                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }
    }
}
