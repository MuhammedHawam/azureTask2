namespace ImperialBackend.Application.DTOs;

/// <summary>
/// DTO for outlet_detail rows
/// </summary>
public class OutletDetailDto
{
    public string InternalCode { get; set; } = string.Empty;
    public string AamsSkuCode { get; set; } = string.Empty;
    public int Year { get; set; }
    public int Week { get; set; }
    public decimal TotalQuantity { get; set; }
    public int CountQuantity { get; set; }
    public decimal TotalSales6w { get; set; }
    public int OrdersCount6w { get; set; }
    public string OutletName { get; set; } = string.Empty;
    public string County { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string AamsSkuName { get; set; } = string.Empty;
    public string ProductGroupName { get; set; } = string.Empty;
    public string ReportingProductGroupName { get; set; } = string.Empty;
    public decimal AamsSkuWhiteStickConversionFactor { get; set; }
    public decimal Price { get; set; }
}

