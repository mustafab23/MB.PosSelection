using MB.PosSelection.Application.Dtos;
using MB.PosSelection.Application.Models;
using MediatR;

namespace MB.PosSelection.Application.Features.Pos.Queries.SelectBestPos
{
    public record SelectBestPosQuery(PosSelectionRequestDto Request) : IRequest<PosSelectionResult>;
}
