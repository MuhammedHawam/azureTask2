using ImperialBackend.Domain.Common;
using ImperialBackend.Domain.Enums;
using ImperialBackend.Domain.ValueObjects;

namespace ImperialBackend.Domain.Entities;

/// <summary>
/// Represents an outlet entity with business rules and invariants
/// </summary>
public class Outlet : BaseEntity
{
    public Outlet(
        int year,
        int week,
        int totalOuterQuantity,
        int countOuterQuantity,
        decimal totalSales6w,
        decimal mean,
        decimal lowerLimit,
        decimal upperLimit,
        string healthStatus,
        int storeRank,
        string outletName,
        string outletIdentifier,
        string addressLine1,
        string state,
        string county)
    {
        Year = year;
        Week = week;
        TotalOuterQuantity = totalOuterQuantity;
        CountOuterQuantity = countOuterQuantity;
        TotalSales6w = totalSales6w;
        Mean = mean;
        LowerLimit = lowerLimit;
        UpperLimit = upperLimit;
        HealthStatus = healthStatus ?? string.Empty;
        StoreRank = storeRank;
        OutletName = outletName ?? string.Empty;
        OutletIdentifier = outletIdentifier ?? string.Empty;
        AddressLine1 = addressLine1 ?? string.Empty;
        State = state ?? string.Empty;
        County = county ?? string.Empty;
    }

    private Outlet() { }

    public int Year { get; private set; }
    public int Week { get; private set; }
    public int TotalOuterQuantity { get; private set; }
    public int CountOuterQuantity { get; private set; }
    public decimal TotalSales6w { get; private set; }
    public decimal Mean { get; private set; }
    public decimal LowerLimit { get; private set; }
    public decimal UpperLimit { get; private set; }
    public string HealthStatus { get; private set; } = string.Empty;
    public int StoreRank { get; private set; }
    public string OutletName { get; private set; } = string.Empty;
    public string OutletIdentifier { get; private set; } = string.Empty;
    public string AddressLine1 { get; private set; } = string.Empty;
    public string State { get; private set; } = string.Empty;
    public string County { get; private set; } = string.Empty;
}