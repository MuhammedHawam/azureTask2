using ImperialBackend.Domain.Entities;
using ImperialBackend.Domain.Interfaces;
using ImperialBackend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ImperialBackend.Infrastructure.Repositories;

/// <summary>
/// EF Core repository for OutletDetail with filtering, sorting, and pagination
/// </summary>
public class OutletDetailRepository : IOutletDetailRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<OutletDetailRepository> _logger;

    public OutletDetailRepository(ApplicationDbContext context, ILogger<OutletDetailRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IEnumerable<OutletDetail>> GetAllAsync(
        string? internalCode = null,
        string? aamsSkuCode = null,
        int? year = null,
        int? week = null,
        string? productGroupName = null,
        string? reportingProductGroupName = null,
        string? searchTerm = null,
        int pageNumber = 1,
        int pageSize = 10,
        string sortBy = "Year",
        string sortDirection = "desc",
        CancellationToken cancellationToken = default)
    {
        var query = _context.OutletDetails.AsNoTracking().AsQueryable();

        query = ApplyFilters(query, internalCode, aamsSkuCode, year, week, productGroupName, reportingProductGroupName, searchTerm);

        var ordered = ApplySorting(query, sortBy, sortDirection);

        var skip = (pageNumber - 1) * pageSize;
        return await ordered.Skip(skip).Take(pageSize).ToListAsync(cancellationToken);
    }

    public async Task<int> GetCountAsync(
        string? internalCode = null,
        string? aamsSkuCode = null,
        int? year = null,
        int? week = null,
        string? productGroupName = null,
        string? reportingProductGroupName = null,
        string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.OutletDetails.AsNoTracking().AsQueryable();
        query = ApplyFilters(query, internalCode, aamsSkuCode, year, week, productGroupName, reportingProductGroupName, searchTerm);
        return await query.CountAsync(cancellationToken);
    }

    private static IQueryable<OutletDetail> ApplyFilters(
        IQueryable<OutletDetail> query,
        string? internalCode,
        string? aamsSkuCode,
        int? year,
        int? week,
        string? productGroupName,
        string? reportingProductGroupName,
        string? searchTerm)
    {
        if (!string.IsNullOrWhiteSpace(internalCode))
        {
            query = query.Where(o => o.InternalCode == internalCode);
        }
        if (!string.IsNullOrWhiteSpace(aamsSkuCode))
        {
            query = query.Where(o => o.AamsSkuCode == aamsSkuCode);
        }
        if (year.HasValue)
        {
            query = query.Where(o => o.Year == year.Value);
        }
        if (week.HasValue)
        {
            query = query.Where(o => o.Week == week.Value);
        }
        if (!string.IsNullOrWhiteSpace(productGroupName))
        {
            query = query.Where(o => o.ProductGroupName == productGroupName);
        }
        if (!string.IsNullOrWhiteSpace(reportingProductGroupName))
        {
            query = query.Where(o => o.ReportingProductGroupName == reportingProductGroupName);
        }
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(o =>
                o.OutletName.Contains(searchTerm) ||
                o.InternalCode.Contains(searchTerm) ||
                o.AamsSkuCode.Contains(searchTerm) ||
                o.AamsSkuName.Contains(searchTerm) ||
                o.City.Contains(searchTerm) ||
                o.County.Contains(searchTerm) ||
                o.ProductGroupName.Contains(searchTerm) ||
                o.ReportingProductGroupName.Contains(searchTerm));
        }
        return query;
    }

    private static IOrderedQueryable<OutletDetail> ApplySorting(IQueryable<OutletDetail> query, string sortBy, string sortDirection)
    {
        var desc = sortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase);
        return sortBy.ToLowerInvariant() switch
        {
            "year" => desc ? query.OrderByDescending(o => o.Year) : query.OrderBy(o => o.Year),
            "week" => desc ? query.OrderByDescending(o => o.Week) : query.OrderBy(o => o.Week),
            "totalsales6w" => desc ? query.OrderByDescending(o => o.TotalSales6w) : query.OrderBy(o => o.TotalSales6w),
            "totalquantity" => desc ? query.OrderByDescending(o => o.TotalQuantity) : query.OrderBy(o => o.TotalQuantity),
            "countquantity" => desc ? query.OrderByDescending(o => o.CountQuantity) : query.OrderBy(o => o.CountQuantity),
            "orderscount6w" => desc ? query.OrderByDescending(o => o.OrdersCount6w) : query.OrderBy(o => o.OrdersCount6w),
            "price" => desc ? query.OrderByDescending(o => o.Price) : query.OrderBy(o => o.Price),
            _ => desc ? query.OrderByDescending(o => o.OutletName) : query.OrderBy(o => o.OutletName)
        };
    }
}

