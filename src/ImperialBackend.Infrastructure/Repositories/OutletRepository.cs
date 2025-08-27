using ImperialBackend.Domain.Entities;
using ImperialBackend.Domain.Enums;
using ImperialBackend.Domain.Interfaces;
using ImperialBackend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ImperialBackend.Infrastructure.Repositories;

/// <summary>
/// Entity Framework Core implementation of IOutletRepository optimized for Databricks
/// </summary>
public class OutletRepository : IOutletRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<OutletRepository> _logger;

    public OutletRepository(ApplicationDbContext context, ILogger<OutletRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Outlet?> GetByIdAsync(string internalCode, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting outlet by internalCode: {InternalCode}", internalCode);
        return await _context.Outlets
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.InternalCode == internalCode, cancellationToken);
    }

    //public async Task<IEnumerable<Outlet>> GetAllAsync(
    //    int? year = null,
    //    int? week = null,
    //    string? healthStatus = null,
    //    string? searchTerm = null,
    //    int pageNumber = 1,
    //    int pageSize = 10,
    //    string sortBy = "StoreRank",
    //    string sortDirection = "asc",
    //    CancellationToken cancellationToken = default)
    //{
    //    _logger.LogDebug("Getting outlets: Page {PageNumber}, Size {PageSize}, Sort {SortBy} {SortDirection}", pageNumber, pageSize, sortBy, sortDirection);

    //    var query = _context.Outlets.AsNoTracking().AsQueryable();

    //    query = ApplyFilters(query, year, week, healthStatus, searchTerm);
    //    query = ApplySorting(query, sortBy, sortDirection);

    //    var skip = (pageNumber - 1) * pageSize;
    //    query = query.Skip(skip).Take(pageSize);

    //    return await query.ToListAsync(cancellationToken);
    //}

    public async Task<IEnumerable<Outlet>> GetAllAsync(
    int? year = null,
        int? week = null,
        string? healthStatus = null,
        string? searchTerm = null,
        int pageNumber = 1,
        int pageSize = 10,
        string sortBy = "StoreRank",
        string sortDirection = "asc",
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting outlets with filters - Page: {PageNumber}, PageSize: {PageSize}, SortBy: {SortBy}",
            pageNumber, pageSize, sortBy);

        // Provider-friendly approach: pick latest per outlet using two-step MAX(Year) then MAX(Week)
        var baseQuery = _context.Outlets.AsNoTracking();

        // Step 1: latest year per outlet
        var latestYearPerOutlet = baseQuery
            .GroupBy(o => o.InternalCode)
            .Select(g => new
            {
                InternalCode = g.Key,
                MaxYear = g.Max(o => o.Year)
            });

        // Step 2: within that year, latest week per outlet
        var latestWeekPerOutlet = baseQuery
            .Join(
                latestYearPerOutlet,
                o => new { o.InternalCode, o.Year },
                y => new { y.InternalCode, Year = y.MaxYear },
                (o, y) => o)
            .GroupBy(o => new { o.InternalCode, o.Year })
            .Select(g => new
            {
                g.Key.InternalCode,
                g.Key.Year,
                MaxWeek = g.Max(o => o.Week)
            });

        // Final: join back to get the full outlet rows (latest per outlet)
        var latestPerOutletQuery = baseQuery
            .Join(
                latestWeekPerOutlet,
                o => new { o.InternalCode, o.Year, o.Week },
                lw => new { lw.InternalCode, lw.Year, Week = lw.MaxWeek },
                (o, lw) => o);

        // Apply filters at database level
        var query = ApplyFilters(latestPerOutletQuery, year, week, healthStatus, searchTerm);

        // Apply sorting at database level
        var ordered = ApplySorting(query, sortBy, sortDirection);

        // Apply pagination
        var skip = (pageNumber - 1) * pageSize;
        return await ordered.Skip(skip).Take(pageSize).ToListAsync(cancellationToken);
    }
    public async Task<int> GetCountAsync(
        int? year = null,
        int? week = null,
        string? healthStatus = null,
        string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting outlet count with filters");

        // Ensure count reflects the same latest-per-outlet selection as GetAllAsync
        var baseQuery = _context.Outlets.AsNoTracking();

        var latestYearPerOutlet = baseQuery
            .GroupBy(o => o.InternalCode)
            .Select(g => new
            {
                InternalCode = g.Key,
                MaxYear = g.Max(o => o.Year)
            });

        var latestWeekPerOutlet = baseQuery
            .Join(
                latestYearPerOutlet,
                o => new { o.InternalCode, o.Year },
                y => new { y.InternalCode, Year = y.MaxYear },
                (o, y) => o)
            .GroupBy(o => new { o.InternalCode, o.Year })
            .Select(g => new
            {
                g.Key.InternalCode,
                g.Key.Year,
                MaxWeek = g.Max(o => o.Week)
            });

        var latestPerOutletQuery = baseQuery
            .Join(
                latestWeekPerOutlet,
                o => new { o.InternalCode, o.Year, o.Week },
                lw => new { lw.InternalCode, lw.Year, Week = lw.MaxWeek },
                (o, lw) => o);

        var filtered = ApplyFilters(latestPerOutletQuery, year, week, healthStatus, searchTerm);

        return await filtered.CountAsync(cancellationToken);
    }

    public async Task<Outlet> AddAsync(Outlet outlet, CancellationToken cancellationToken = default)
    {
        if (outlet == null)
            throw new ArgumentNullException(nameof(outlet));

        _logger.LogDebug("Adding new outlet: {OutletName}", outlet.OutletName);
        _context.Outlets.Add(outlet);
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Successfully added outlet with InternalCode: {InternalCode}", outlet.InternalCode);
        return outlet;
    }

    public async Task<Outlet> UpdateAsync(Outlet outlet, CancellationToken cancellationToken = default)
    {
        if (outlet == null)
            throw new ArgumentNullException(nameof(outlet));

        _logger.LogDebug("Updating outlet: {InternalCode}", outlet.InternalCode);
        _context.Outlets.Update(outlet);
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Successfully updated outlet: {InternalCode}", outlet.InternalCode);
        return outlet;
    }

    public async Task<bool> DeleteAsync(string internalCode, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Deleting outlet: {InternalCode}", internalCode);

        var outlet = await _context.Outlets.FirstOrDefaultAsync(o => o.InternalCode == internalCode, cancellationToken);
        if (outlet == null)
        {
            _logger.LogWarning("Outlet not found for deletion: {InternalCode}", internalCode);
            return false;
        }

        _context.Outlets.Remove(outlet);
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Successfully deleted outlet: {InternalCode}", internalCode);
        return true;
    }

    public async Task<bool> ExistsAsync(string internalCode, CancellationToken cancellationToken = default)
    {
        return await _context.Outlets.AnyAsync(o => o.InternalCode == internalCode, cancellationToken);
    }

    private static IQueryable<Outlet> ApplyFilters(
        IQueryable<Outlet> query,
        int? year,
        int? week,
        string? healthStatus,
        string? searchTerm)
    {
        if (year.HasValue)
        {
            query = query.Where(o => o.Year == year.Value);
        }

        if (week.HasValue)
        {
            query = query.Where(o => o.Week == week.Value);
        }

        if (!string.IsNullOrWhiteSpace(healthStatus))
        {
            query = query.Where(o => o.HealthStatus == healthStatus);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(o =>
                o.OutletName.Contains(searchTerm) ||
                o.InternalCode.Contains(searchTerm) ||
                o.AddressLine1.Contains(searchTerm) ||
                o.State.Contains(searchTerm) ||
                o.County.Contains(searchTerm));
        }

        return query;
    }

    private static IOrderedQueryable<Outlet> ApplySorting(IQueryable<Outlet> query, string sortBy, string sortDirection)
    {
        var desc = sortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase);
        return sortBy.ToLowerInvariant() switch
        {
            "year" => desc ? query.OrderByDescending(o => o.Year) : query.OrderBy(o => o.Year),
            "week" => desc ? query.OrderByDescending(o => o.Week) : query.OrderBy(o => o.Week),
            "totalouterquantity" => desc ? query.OrderByDescending(o => o.TotalOuterQuantity) : query.OrderBy(o => o.TotalOuterQuantity),
            "countouterquantity" => desc ? query.OrderByDescending(o => o.CountOuterQuantity) : query.OrderBy(o => o.CountOuterQuantity),
            "totalsales6w" => desc ? query.OrderByDescending(o => o.TotalSales6w) : query.OrderBy(o => o.TotalSales6w),
            "mean" => desc ? query.OrderByDescending(o => o.Mean) : query.OrderBy(o => o.Mean),
            "lowerlimit" => desc ? query.OrderByDescending(o => o.LowerLimit) : query.OrderBy(o => o.LowerLimit),
            "upperlimit" => desc ? query.OrderByDescending(o => o.UpperLimit) : query.OrderBy(o => o.UpperLimit),
            "healthstatus" => desc ? query.OrderByDescending(o => o.HealthStatus) : query.OrderBy(o => o.HealthStatus),
            "outletname" => desc ? query.OrderByDescending(o => o.OutletName) : query.OrderBy(o => o.OutletName),
            "state" => desc ? query.OrderByDescending(o => o.State) : query.OrderBy(o => o.State),
            "county" => desc ? query.OrderByDescending(o => o.County) : query.OrderBy(o => o.County),
            _ => desc ? query.OrderByDescending(o => o.StoreRank) : query.OrderBy(o => o.StoreRank)
        };
    }
}