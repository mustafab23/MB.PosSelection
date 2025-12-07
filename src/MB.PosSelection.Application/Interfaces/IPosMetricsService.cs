namespace MB.PosSelection.Application.Interfaces
{
    /// <summary>
    /// Uygulama genelindeki iş metriklerini (Business Metrics) yöneten servis sözleşmesi.
    /// Infrastructure katmanındaki gerçek sayaçlara (Meter) soyutlama sağlar.
    /// </summary>
    public interface IPosMetricsService
    {
        /// <summary>
        /// Başarılı bir POS seçim işlemini sayar.
        /// </summary>
        void IncrementTotalRequests();

        /// <summary>
        /// Cache'den veri başarıyla döndüğünde sayar.
        /// </summary>
        void IncrementCacheHit();

        /// <summary>
        /// Cache'de veri bulunamayıp DB'ye gidildiğinde sayar.
        /// </summary>
        void IncrementCacheMiss();

        /// <summary>
        /// Dış API çağrı süresini kaydeder.
        /// </summary>
        /// <param name="milliseconds">Geçen süre (ms)</param>
        void RecordExternalApiDuration(double milliseconds);
    }
}
