using MB.PosSelection.Application.Interfaces;
using System.Diagnostics.Metrics;

namespace MB.PosSelection.Infrastructure.Services.Metrics
{
    public class PosMetricsService : IPosMetricsService
    {
        // OpenTelemetry
        public const string MeterName = "MB.PosSelection.Metrics";

        private readonly Meter _meter;
        private readonly Counter<long> _totalRequestsCounter;
        private readonly Counter<long> _cacheHitCounter;
        private readonly Counter<long> _cacheMissCounter;
        private readonly Histogram<double> _externalApiDurationHistogram;

        public PosMetricsService()
        {
            _meter = new Meter(MeterName, "1.0.0");

            _totalRequestsCounter = _meter.CreateCounter<long>(
                "pos_selection_requests_total",
                unit: "request",
                description: "Toplam işlenen POS seçim talebi sayısı");

            _cacheHitCounter = _meter.CreateCounter<long>(
                "pos_rates_cache_hits",
                unit: "hit",
                description: "L1/L2 Cache isabet sayısı");

            _cacheMissCounter = _meter.CreateCounter<long>(
                "pos_rates_cache_misses",
                unit: "miss",
                description: "Cache miss sonucu DB erişim sayısı");

            _externalApiDurationHistogram = _meter.CreateHistogram<double>(
                "external_api_duration_ms",
                unit: "ms",
                description: "Dış Mock API'den veri çekme süresi dağılımı");
        }

        public void IncrementTotalRequests() => _totalRequestsCounter.Add(1);
        public void IncrementCacheHit() => _cacheHitCounter.Add(1);
        public void IncrementCacheMiss() => _cacheMissCounter.Add(1);
        public void RecordExternalApiDuration(double milliseconds) => _externalApiDurationHistogram.Record(milliseconds);
    }
}
