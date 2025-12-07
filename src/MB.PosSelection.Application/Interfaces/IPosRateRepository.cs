using MB.PosSelection.Application.Models;
using MB.PosSelection.Domain.Entities;

namespace MB.PosSelection.Application.Interfaces
{
    public interface IPosRateRepository
    {
        /// <summary>
        /// Geçerli (Aktif) POS oranlarını getirir.
        /// Önce Memory, sonra Redis, en son DB'ye bakar.
        /// </summary>
        Task<PosRateLookupIndex> GetRatesIndexAsync();

        /// <summary>
        /// Atomic Cache Update
        /// Verilen listeyi yeni bir versiyon olarak Redis'e yazar ve
        /// sistemi anında o versiyona geçirir.
        /// </summary>
        /// <param name="rates">Yeni oran listesi</param>
        /// <param name="batchId">Yeni versiyon numarası (örn: v_20231205_1500)</param>
        Task RefreshCacheAsync(IEnumerable<PosRatio> rates, string batchId);

        /// <summary>
        /// Veritabanındaki eski ve kullanılmayan batch kayıtlarını siler.
        /// </summary>
        Task CleanOldBatchesAsync(string activeBatchId);
    }
}
