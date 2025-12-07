using MB.PosSelection.Application.Interfaces;
using MB.PosSelection.Domain.Entities;
using MB.PosSelection.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MB.PosSelection.Application.Features.Pos.Commands.SyncPosRates
{
    public class SyncPosRatesCommandHandler : IRequestHandler<SyncPosRatesCommand, bool>
    {
        private readonly IMockPosApiService _apiService;
        private readonly IApplicationDbContext _context; // EF Core DbContext arayüzü
        private readonly IPosRateRepository _repository; // Dapper + Cache (Decorator)
        private readonly ILogger<SyncPosRatesCommandHandler> _logger;

        public SyncPosRatesCommandHandler(
            IMockPosApiService apiService,
            IApplicationDbContext context,
            IPosRateRepository repository,
            ILogger<SyncPosRatesCommandHandler> logger)
        {
            _apiService = apiService;
            _context = context;
            _repository = repository;
            _logger = logger;
        }

        public async Task<bool> Handle(SyncPosRatesCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Dış servisten POS oranları çekiliyor...");
            var externalRates = await _apiService.GetRatesAsync(cancellationToken);

            if (!externalRates.Any())
            {
                _logger.LogWarning("Dış servisten veri gelmedi, işlem iptal edildi.");
                return false;
            }

            var batchId = DateTime.UtcNow.ToString("yyyyMMdd_HHmmssfff");
            _logger.LogInformation($"Yeni veri paketi hazırlanıyor. BatchId: {batchId}");

            var newEntities = new List<PosRatio>();
            var ignoredCount = 0;

            foreach (var dto in externalRates)
            {
                if (!Enum.TryParse<Currency>(dto.Currency, true, out var currencyEnum))
                {
                    _logger.LogWarning("Bilinmeyen Para Birimi! POS: {PosName}, Gelen Değer: {Value}. Bu kayıt atlandı.",
                        dto.PosName, dto.Currency);
                    ignoredCount++;
                    continue;
                }

                if (!Enum.TryParse<CardType>(dto.CardType, true, out var cardTypeEnum))
                {
                    _logger.LogWarning("Bilinmeyen Kart Tipi! POS: {PosName}, Gelen Değer: {Value}. Bu kayıt atlandı.",
                        dto.PosName, dto.CardType);
                    ignoredCount++;
                    continue;
                }

                var entity = new PosRatio(
                    batchId,
                    dto.PosName,
                    cardTypeEnum,
                    dto.CardBrand,
                    dto.Installment,
                    currencyEnum,
                    dto.CommissionRate,
                    dto.MinFee,
                    dto.Priority
                );

                newEntities.Add(entity);
            }

            if (!newEntities.Any())
            {
                _logger.LogError("Gelen verilerin hiçbiri geçerli formatta değil! Güncelleme iptal edildi. Lütfen dış servisi kontrol edin.");
                return false;
            }

            if (ignoredCount > 0)
            {
                _logger.LogWarning("{Total} kayıttan {Ignored} tanesi veri hatası nedeniyle atlandı.", externalRates.Count, ignoredCount);
            }

            await _context.PosRatios.AddRangeAsync(newEntities, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation($"{newEntities.Count} adet yeni oran veritabanına kaydedildi.");

            try
            {
                await _repository.RefreshCacheAsync(newEntities, batchId);
                _logger.LogInformation($"Cache başarıyla güncellendi (Batch: {batchId}).");

                await _repository.CleanOldBatchesAsync(batchId);
                _logger.LogInformation("Eski versiyon veriler DB'den temizlendi.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cache güncelleme veya temizlik sırasında hata oluştu. Ancak DB güncel.");
                throw;
            }

            return true;
        }
    }
}
