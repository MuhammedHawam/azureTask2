using ImperialBackend.Domain.Entities;
using ImperialBackend.Domain.Enums;
using ImperialBackend.Domain.Interfaces;
using ImperialBackend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ImperialBackend.Infrastructure.Repositories;

/// <summary>
/// Entity Framework Core implementation of IOutletRepository optimized for Databricks
/// </summary>
public class OutletRepository : IOutletRepository
{
	private readonly ApplicationDbContext _context;
	private readonly ILogger<OutletRepository> _logger;
	private readonly DatabricksSqlRestClient _sqlClient;

	public OutletRepository(ApplicationDbContext context, ILogger<OutletRepository> logger, DatabricksSqlRestClient sqlClient)
	{
		_context = context ?? throw new ArgumentNullException(nameof(context));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		_sqlClient = sqlClient ?? throw new ArgumentNullException(nameof(sqlClient));
	}

	public async Task<Outlet?> GetByIdAsync(string outletIdentifier, CancellationToken cancellationToken = default)
	{
		_logger.LogDebug("Getting outlet by identifier: {OutletIdentifier}", outletIdentifier);
		return await _context.Outlets
			.AsNoTracking()
			.FirstOrDefaultAsync(o => o.OutletIdentifier == outletIdentifier, cancellationToken);
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
		_logger.LogDebug("Getting outlets (REST) - Page: {PageNumber}, PageSize: {PageSize}, SortBy: {SortBy}",
			pageNumber, pageSize, sortBy);

		// Map sort column to SQL column name
		var sortColumn = sortBy?.ToLowerInvariant() switch
		{
			"outletname" => "OutletName",
			"healthstatus" => "health_status",
			"totalsales6w" => "total_sales_6w",
			"year" => "year",
			"week" => "week",
			_ => "store_rank"
		};
		var dir = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC";
		var skip = (pageNumber - 1) * pageSize;

		// Build SQL (latest per outlet via ROW_NUMBER)
		var filters = new List<string> { "rn = 1" };
		if (year.HasValue) filters.Add($"year = {year.Value}");
		if (week.HasValue) filters.Add($"week = {week.Value}");
		if (!string.IsNullOrWhiteSpace(healthStatus)) filters.Add($"health_status = '{healthStatus.Replace("'", "''")}'");
		if (!string.IsNullOrWhiteSpace(searchTerm))
		{
			var like = $"%{searchTerm.ToLowerInvariant().Replace("'", "''")}%";
			filters.Add($"(LOWER(OutletName) LIKE '{like}' OR LOWER(OutletIdentifier) LIKE '{like}' OR LOWER(AddressLine1) LIKE '{like}' OR LOWER(State) LIKE '{like}' OR LOWER(County) LIKE '{like}')");
		}

		var sql = $@"
WITH latest AS (
  SELECT
    year,
    week,
    TotalOuterQuantity,
    CountOuterQuantity,
    total_sales_6w,
    mean,
    lowerlimit,
    upperlimit,
    health_status,
    store_rank,
    OutletName,
    OutletIdentifier,
    AddressLine1,
    State,
    County,
    ROW_NUMBER() OVER (PARTITION BY OutletIdentifier ORDER BY year DESC, week DESC) AS rn
  FROM dev_gold.outlet_aggregated
)
SELECT
  year,
  week,
  TotalOuterQuantity,
  CountOuterQuantity,
  total_sales_6w,
  mean,
  lowerlimit,
  upperlimit,
  health_status,
  store_rank,
  OutletName,
  OutletIdentifier,
  AddressLine1,
  State,
  County
FROM latest
WHERE {string.Join(" AND ", filters)}
ORDER BY {sortColumn} {dir}
LIMIT {pageSize} OFFSET {skip};";

		using var doc = await _sqlClient.ExecuteStatementAsync(sql, cancellationToken);
		var results = new List<Outlet>();

		if (doc.RootElement.TryGetProperty("result", out var resultEl) &&
			resultEl.TryGetProperty("data_array", out var dataArray))
		{
			foreach (var row in dataArray.EnumerateArray())
			{
				// Row order must match SELECT list
				var outlet = new Outlet(
					year: row[0].GetInt32(),
					week: row[1].GetInt32(),
					totalOuterQuantity: row[2].GetInt32(),
					countOuterQuantity: row[3].GetInt32(),
					totalSales6w: row[4].GetDecimal(),
					mean: row[5].GetDecimal(),
					lowerLimit: row[6].GetDecimal(),
					upperLimit: row[7].GetDecimal(),
					healthStatus: row[8].GetString() ?? string.Empty,
					storeRank: row[9].GetInt32(),
					outletName: row[10].GetString() ?? string.Empty,
					outletIdentifier: row[11].GetString() ?? string.Empty,
					addressLine1: row[12].GetString() ?? string.Empty,
					state: row[13].GetString() ?? string.Empty,
					county: row[14].GetString() ?? string.Empty
				);
				results.Add(outlet);
			}
		}
		else
		{
			_logger.LogWarning("Databricks SQL response did not contain result.data_array");
		}

		return results;
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
			.GroupBy(o => o.OutletIdentifier)
			.Select(g => new
			{
				OutletIdentifier = g.Key,
				MaxYear = g.Max(o => o.Year)
			});

		var latestWeekPerOutlet = baseQuery
			.Join(
				latestYearPerOutlet,
				o => new { o.OutletIdentifier, o.Year },
				y => new { y.OutletIdentifier, Year = y.MaxYear },
				(o, y) => o)
			.GroupBy(o => new { o.OutletIdentifier, o.Year })
			.Select(g => new
			{
				g.Key.OutletIdentifier,
				g.Key.Year,
				MaxWeek = g.Max(o => o.Week)
			});

		var latestPerOutletQuery = baseQuery
			.Join(
				latestWeekPerOutlet,
				o => new { o.OutletIdentifier, o.Year, o.Week },
				lw => new { lw.OutletIdentifier, lw.Year, Week = lw.MaxWeek },
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
		_logger.LogInformation("Successfully added outlet with Identifier: {OutletIdentifier}", outlet.OutletIdentifier);
		return outlet;
	}

	public async Task<Outlet> UpdateAsync(Outlet outlet, CancellationToken cancellationToken = default)
	{
		if (outlet == null)
			throw new ArgumentNullException(nameof(outlet));

		_logger.LogDebug("Updating outlet: {OutletIdentifier}", outlet.OutletIdentifier);
		_context.Outlets.Update(outlet);
		await _context.SaveChangesAsync(cancellationToken);
		_logger.LogInformation("Successfully updated outlet: {OutletIdentifier}", outlet.OutletIdentifier);
		return outlet;
	}

	public async Task<bool> DeleteAsync(string outletIdentifier, CancellationToken cancellationToken = default)
	{
		_logger.LogDebug("Deleting outlet: {OutletIdentifier}", outletIdentifier);

		var outlet = await _context.Outlets.FirstOrDefaultAsync(o => o.OutletIdentifier == outletIdentifier, cancellationToken);
		if (outlet == null)
		{
			_logger.LogWarning("Outlet not found for deletion: {OutletIdentifier}", outletIdentifier);
			return false;
		}

		_context.Outlets.Remove(outlet);
		await _context.SaveChangesAsync(cancellationToken);
		_logger.LogInformation("Successfully deleted outlet: {OutletIdentifier}", outletIdentifier);
		return true;
	}

	public async Task<bool> ExistsAsync(string outletIdentifier, CancellationToken cancellationToken = default)
	{
		return await _context.Outlets.AnyAsync(o => o.OutletIdentifier == outletIdentifier, cancellationToken);
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
			var lowerSearchTerm = searchTerm.ToLower();
			query = query.Where(o =>
				(o.OutletName.ToLower().Contains(lowerSearchTerm)) ||
				(o.OutletIdentifier.ToLower().Contains(lowerSearchTerm)) ||
				(o.AddressLine1.ToLower().Contains(lowerSearchTerm)) ||
				(o.State.ToLower().Contains(lowerSearchTerm)) ||
				(o.County.ToLower().Contains(lowerSearchTerm))
			);
		}

		return query;
	}

	private static IOrderedQueryable<Outlet> ApplySorting(IQueryable<Outlet> query, string sortBy, string sortDirection)
	{
		var sort = sortBy?.ToLowerInvariant();
		var desc = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);

		switch (sort)
		{
			case "outletname":
				return desc ? query.OrderByDescending(o => o.OutletName) : query.OrderBy(o => o.OutletName);
			case "healthstatus":
				return desc ? query.OrderByDescending(o => o.HealthStatus) : query.OrderBy(o => o.HealthStatus);
			case "totalsales6w":
				return desc ? query.OrderByDescending(o => o.TotalSales6w) : query.OrderBy(o => o.TotalSales6w);
			case "year":
				return desc ? query.OrderByDescending(o => o.Year) : query.OrderBy(o => o.Year);
			case "week":
				return desc ? query.OrderByDescending(o => o.Week) : query.OrderBy(o => o.Week);
			default:
				return desc ? query.OrderByDescending(o => o.StoreRank) : query.OrderBy(o => o.StoreRank);
		}
	}
}