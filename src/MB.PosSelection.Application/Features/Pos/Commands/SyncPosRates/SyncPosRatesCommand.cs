using MediatR;

namespace MB.PosSelection.Application.Features.Pos.Commands.SyncPosRates
{
    public record SyncPosRatesCommand : IRequest<bool>;
}
