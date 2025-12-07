using MB.PosSelection.Application.Interfaces;
using MB.PosSelection.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MB.PosSelection.Infrastructure.Persistence
{
    public class AppDbContext : DbContext, IApplicationDbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<PosRatio> PosRatios { get; set; }
        public DbSet<PosRatioHistory> PosRatioHistories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PosRatio>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.BatchId);
                entity.Property(e => e.BatchId).IsRequired().HasMaxLength(50);
                entity.Property(e => e.PosName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.CardBrand).HasMaxLength(50);
                entity.Property(e => e.Currency).HasConversion<string>();
                entity.Property(e => e.CardType).HasConversion<string>();

                entity.Property(e => e.CommissionRate).HasColumnType("decimal(18,4)");
                entity.Property(e => e.MinFee).HasColumnType("decimal(18,2)");
            });

            modelBuilder.Entity<PosRatioHistory>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.BatchId);
                entity.HasIndex(e => e.OriginalPosRatioId);
                entity.Property(e => e.BatchId).IsRequired().HasMaxLength(50);
                entity.Property(e => e.CommissionRate).HasColumnType("decimal(18,4)");
                entity.Property(e => e.MinFee).HasColumnType("decimal(18,2)");
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}
