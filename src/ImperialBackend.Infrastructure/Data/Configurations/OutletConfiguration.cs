using ImperialBackend.Domain.Entities;
using ImperialBackend.Domain.Enums;
using ImperialBackend.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ImperialBackend.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for Outlet entity
/// </summary>
public class OutletConfiguration : IEntityTypeConfiguration<Outlet>
{
    public void Configure(EntityTypeBuilder<Outlet> builder)
    {
        builder.ToTable("outlet_summary", "dev_gold");

        // Ignore BaseEntity (audit) + old GUID Id so EF doesn't project non-existent columns
        builder.Ignore(o => o.Id);
        builder.Ignore(o => o.CreatedAt);
        builder.Ignore(o => o.UpdatedAt);
        builder.Ignore(o => o.CreatedBy);
        builder.Ignore(o => o.UpdatedBy);

        // Key is internalCode
        builder.HasKey(o => o.InternalCode);
        builder.Property(o => o.InternalCode)
            .HasColumnName("internalCode")
            .HasMaxLength(100);

        // Column mappings for the new minimal shape
        builder.Property(o => o.Year).HasColumnName("year");
        builder.Property(o => o.Week).HasColumnName("week");
        builder.Property(o => o.TotalOuterQuantity).HasColumnName("TotalQuantity");
        builder.Property(o => o.CountOuterQuantity).HasColumnName("CountQuantity");
        builder.Property(o => o.TotalSales6w).HasColumnName("total_sales_6w");
        builder.Property(o => o.Mean).HasColumnName("mean");
        builder.Property(o => o.LowerLimit).HasColumnName("lowerlimit");
        builder.Property(o => o.UpperLimit).HasColumnName("upperlimit");
        builder.Property(o => o.HealthStatus).HasColumnName("health_status").HasMaxLength(50);
        builder.Property(o => o.StoreRank).HasColumnName("store_rank");
        builder.Property(o => o.OutletName).HasColumnName("OutletName").HasMaxLength(200);
        builder.Property(o => o.AddressLine1).HasColumnName("Address").HasMaxLength(200);
        builder.Property(o => o.State).HasColumnName("City").HasMaxLength(50);
        builder.Property(o => o.County).HasColumnName("county").HasMaxLength(100);
    }
}
