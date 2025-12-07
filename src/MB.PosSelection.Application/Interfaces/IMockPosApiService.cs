using MB.PosSelection.Application.Dtos.External;

namespace MB.PosSelection.Application.Interfaces
{
    public interface IMockPosApiService
    {
        Task<List<PosRatioExternalDto>> GetRatesAsync(CancellationToken cancellationToken);
    }
}
