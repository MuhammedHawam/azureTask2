using ImperialBackend.Domain.Common;

namespace ImperialBackend.Domain.Entities;

/// <summary>
/// Represents detailed outlet product records from dev_gold.outlet_detail
/// </summary>
public class OutletDetail : BaseEntity
{
    public OutletDetail(
        string internalCode,
        string aamsSkuCode,
        int year,
        int week,
        decimal totalQuantity,
        int countQuantity,
        decimal totalSales6w,
        int ordersCount6w,
        string outletName,
        string county,
        string city,
        string postalCode,
        string address,
        string aamsSkuName,
        string productGroupName,
        string reportingProductGroupName,
        decimal aamsSkuWhiteStickConversionFactor,
        decimal price)
    {
        InternalCode = internalCode ?? string.Empty;
        AamsSkuCode = aamsSkuCode ?? string.Empty;
        Year = year;
        Week = week;
        TotalQuantity = totalQuantity;
        CountQuantity = countQuantity;
        TotalSales6w = totalSales6w;
        OrdersCount6w = ordersCount6w;
        OutletName = outletName ?? string.Empty;
        County = county ?? string.Empty;
        City = city ?? string.Empty;
        PostalCode = postalCode ?? string.Empty;
        Address = address ?? string.Empty;
        AamsSkuName = aamsSkuName ?? string.Empty;
        ProductGroupName = productGroupName ?? string.Empty;
        ReportingProductGroupName = reportingProductGroupName ?? string.Empty;
        AamsSkuWhiteStickConversionFactor = aamsSkuWhiteStickConversionFactor;
        Price = price;
    }

    private OutletDetail() { }

    public string InternalCode { get; private set; } = string.Empty;
    public string AamsSkuCode { get; private set; } = string.Empty;
    public int Year { get; private set; }
    public int Week { get; private set; }
    public decimal TotalQuantity { get; private set; }
    public int CountQuantity { get; private set; }
    public decimal TotalSales6w { get; private set; }
    public int OrdersCount6w { get; private set; }
    public string OutletName { get; private set; } = string.Empty;
    public string County { get; private set; } = string.Empty;
    public string City { get; private set; } = string.Empty;
    public string PostalCode { get; private set; } = string.Empty;
    public string Address { get; private set; } = string.Empty;
    public string AamsSkuName { get; private set; } = string.Empty;
    public string ProductGroupName { get; private set; } = string.Empty;
    public string ReportingProductGroupName { get; private set; } = string.Empty;
    public decimal AamsSkuWhiteStickConversionFactor { get; private set; }
    public decimal Price { get; private set; }
}

