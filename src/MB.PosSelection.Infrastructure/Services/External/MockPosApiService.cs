using MB.PosSelection.Application.Dtos.External;
using MB.PosSelection.Application.Interfaces;
using System.Net.Http.Json;
using System.Text.Json;
using System.Diagnostics;

namespace MB.PosSelection.Infrastructure.Services.External
{
    public class MockPosApiService : IMockPosApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IPosMetricsService _metrics;

        public MockPosApiService(HttpClient httpClient, IPosMetricsService metrics)
        {
            _httpClient = httpClient;
            _metrics = metrics;
        }

        public async Task<List<PosRatioExternalDto>> GetRatesAsync(CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            const string requestPath = "ratios";

            try
            {
                var response = await _httpClient.GetFromJsonAsync<List<PosRatioExternalDto>>(requestPath, options, cancellationToken);
                return response ?? new List<PosRatioExternalDto>();
            }
            catch (Exception ex)
            {
                throw new Exception($"Mock API ({requestPath}) çağrısı başarısız: {ex.Message}", ex);
            }
            finally
            {
                stopwatch.Stop();
                _metrics.RecordExternalApiDuration(stopwatch.Elapsed.TotalMilliseconds);
            }
        }
    }
}
