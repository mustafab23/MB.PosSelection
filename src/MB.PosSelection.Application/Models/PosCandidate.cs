using MB.PosSelection.Domain.Entities;

namespace MB.PosSelection.Application.Models
{
    public record PosCandidate(
    PosRatio Ratio,
    decimal Price,
    decimal PayableTotal
);
}
