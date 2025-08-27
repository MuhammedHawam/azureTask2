using ImperialBackend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ImperialBackend.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for OutletDetail mapped to dev_gold.outlet_detail
/// </summary>
public class OutletDetailConfiguration : IEntityTypeConfiguration<OutletDetail>
{
    public void Configure(EntityTypeBuilder<OutletDetail> builder)
    {
        builder.ToTable("outlet_detail", "dev_gold");

        // Ignore BaseEntity audit fields
        builder.Ignore(o => o.Id);
        builder.Ignore(o => o.CreatedAt);
        builder.Ignore(o => o.UpdatedAt);
        builder.Ignore(o => o.CreatedBy);
        builder.Ignore(o => o.UpdatedBy);

        // Key could be composite: internalCode + AamsSKUCode + year + week
        builder.HasKey(o => new { o.InternalCode, o.AamsSkuCode, o.Year, o.Week });

        builder.Property(o => o.InternalCode).HasColumnName("internalCode").HasMaxLength(100);
        builder.Property(o => o.AamsSkuCode).HasColumnName("AamsSKUCode").HasMaxLength(100);
        builder.Property(o => o.Year).HasColumnName("year");
        builder.Property(o => o.Week).HasColumnName("week");
        builder.Property(o => o.TotalQuantity).HasColumnName("TotalQuantity");
        builder.Property(o => o.CountQuantity).HasColumnName("CountQuantity");
        builder.Property(o => o.TotalSales6w).HasColumnName("total_sales_6w");
        builder.Property(o => o.OrdersCount6w).HasColumnName("ordersCount_6w");
        builder.Property(o => o.OutletName).HasColumnName("OutletName").HasMaxLength(200);
        builder.Property(o => o.County).HasColumnName("county").HasMaxLength(100);
        builder.Property(o => o.City).HasColumnName("City").HasMaxLength(100);
        builder.Property(o => o.PostalCode).HasColumnName("Postal_Code").HasMaxLength(50);
        builder.Property(o => o.Address).HasColumnName("Address").HasMaxLength(200);
        builder.Property(o => o.AamsSkuName).HasColumnName("AamsSKUName").HasMaxLength(200);
        builder.Property(o => o.ProductGroupName).HasColumnName("ProductGroupName").HasMaxLength(200);
        builder.Property(o => o.ReportingProductGroupName).HasColumnName("ReportingProductGroupName").HasMaxLength(200);
        builder.Property(o => o.AamsSkuWhiteStickConversionFactor).HasColumnName("AamsSKUWhiteStickConversionFactor");
        builder.Property(o => o.Price).HasColumnName("price");
    }
}

