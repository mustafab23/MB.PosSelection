using MB.PosSelection.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MB.PosSelection.Application.Interfaces
{
    public interface IApplicationDbContext
    {
        DbSet<PosRatio> PosRatios { get; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    }
}
