using System.Data;
using System.Data.Odbc;
using ImperialBackend.Domain.Entities;
using ImperialBackend.Domain.Interfaces;
using ImperialBackend.Infrastructure.Data.Odbc;
using Microsoft.Extensions.Logging;

namespace ImperialBackend.Infrastructure.Repositories;

public class OdbcOutletRepository : IOutletRepository
{
	private readonly DatabricksOdbcConnectionFactory _connectionFactory;
	private readonly ILogger<OdbcOutletRepository> _logger;

	public OdbcOutletRepository(
		DatabricksOdbcConnectionFactory connectionFactory,
		ILogger<OdbcOutletRepository> logger)
	{
		_connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public async Task<Outlet?> GetByIdAsync(string outletIdentifier, CancellationToken cancellationToken = default)
	{
		const string sql = @"
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
SELECT year, week, TotalOuterQuantity, CountOuterQuantity, total_sales_6w, mean, lowerlimit, upperlimit, health_status, store_rank, OutletName, OutletIdentifier, AddressLine1, State, County
FROM latest
WHERE rn = 1 AND OutletIdentifier = ?";

		await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
		await using var command = new OdbcCommand(sql, connection);
		command.Parameters.AddWithValue("@p1", outletIdentifier);

		await using var reader = await Task.Run(() => command.ExecuteReader(CommandBehavior.SingleRow), cancellationToken);
		if (await Task.Run(() => reader.Read(), cancellationToken))
		{
			return MapOutlet(reader);
		}
		return null;
	}

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
		_logger.LogDebug("ODBC GetAllAsync: page {Page}, size {Size}, sortBy {SortBy} {SortDir}", pageNumber, pageSize, sortBy, sortDirection);

		var columnMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			{"StoreRank", "store_rank"},
			{"OutletName", "OutletName"},
			{"HealthStatus", "health_status"},
			{"TotalSales6w", "total_sales_6w"},
			{"Year", "year"},
			{"Week", "week"}
		};
		var orderColumn = columnMap.TryGetValue(sortBy, out var mapped) ? mapped : "store_rank";
		var orderDir = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC";

		var whereClauses = new List<string> { "rn = 1" };
		var parameters = new List<OdbcParameter>();

		if (year.HasValue)
		{
			whereClauses.Add("year = ?");
			parameters.Add(new OdbcParameter("@p_year", year.Value));
		}
		if (week.HasValue)
		{
			whereClauses.Add("week = ?");
			parameters.Add(new OdbcParameter("@p_week", week.Value));
		}
		if (!string.IsNullOrWhiteSpace(healthStatus))
		{
			whereClauses.Add("health_status = ?");
			parameters.Add(new OdbcParameter("@p_health", healthStatus));
		}
		if (!string.IsNullOrWhiteSpace(searchTerm))
		{
			whereClauses.Add("(LOWER(OutletName) LIKE ? OR LOWER(OutletIdentifier) LIKE ? OR LOWER(AddressLine1) LIKE ? OR LOWER(State) LIKE ? OR LOWER(County) LIKE ?)");
			var like = $"%{searchTerm!.ToLowerInvariant()}%";
			parameters.Add(new OdbcParameter("@p_s1", like));
			parameters.Add(new OdbcParameter("@p_s2", like));
			parameters.Add(new OdbcParameter("@p_s3", like));
			parameters.Add(new OdbcParameter("@p_s4", like));
			parameters.Add(new OdbcParameter("@p_s5", like));
		}

		var skip = (pageNumber - 1) * pageSize;

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
SELECT year, week, TotalOuterQuantity, CountOuterQuantity, total_sales_6w, mean, lowerlimit, upperlimit, health_status, store_rank, OutletName, OutletIdentifier, AddressLine1, State, County
FROM latest
WHERE {string.Join(" AND ", whereClauses)}
ORDER BY {orderColumn} {orderDir}
LIMIT ? OFFSET ?";

		await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
		await using var command = new OdbcCommand(sql, connection);
		foreach (var p in parameters)
		{
			command.Parameters.Add(p);
		}
		command.Parameters.Add(new OdbcParameter("@p_limit", pageSize));
		command.Parameters.Add(new OdbcParameter("@p_offset", skip));

		var results = new List<Outlet>();
		await using var reader = await Task.Run(() => command.ExecuteReader(), cancellationToken);
		while (await Task.Run(() => reader.Read(), cancellationToken))
		{
			results.Add(MapOutlet(reader));
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
		var whereClauses = new List<string> { "rn = 1" };
		var parameters = new List<OdbcParameter>();

		if (year.HasValue)
		{
			whereClauses.Add("year = ?");
			parameters.Add(new OdbcParameter("@p_year", year.Value));
		}
		if (week.HasValue)
		{
			whereClauses.Add("week = ?");
			parameters.Add(new OdbcParameter("@p_week", week.Value));
		}
		if (!string.IsNullOrWhiteSpace(healthStatus))
		{
			whereClauses.Add("health_status = ?");
			parameters.Add(new OdbcParameter("@p_health", healthStatus));
		}
		if (!string.IsNullOrWhiteSpace(searchTerm))
		{
			whereClauses.Add("(LOWER(OutletName) LIKE ? OR LOWER(OutletIdentifier) LIKE ? OR LOWER(AddressLine1) LIKE ? OR LOWER(State) LIKE ? OR LOWER(County) LIKE ?)");
			var like = $"%{searchTerm!.ToLowerInvariant()}%";
			parameters.Add(new OdbcParameter("@p_s1", like));
			parameters.Add(new OdbcParameter("@p_s2", like));
			parameters.Add(new OdbcParameter("@p_s3", like));
			parameters.Add(new OdbcParameter("@p_s4", like));
			parameters.Add(new OdbcParameter("@p_s5", like));
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
SELECT COUNT(*)
FROM latest
WHERE {string.Join(" AND ", whereClauses)}";

		await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
		await using var command = new OdbcCommand(sql, connection);
		foreach (var p in parameters)
		{
			command.Parameters.Add(p);
		}
		var scalar = await Task.Run(() => command.ExecuteScalar(), cancellationToken);
		return Convert.ToInt32(scalar);
	}

	public Task<Outlet> AddAsync(Outlet outlet, CancellationToken cancellationToken = default)
	{
		throw new NotSupportedException("AddAsync is not supported via ODBC repository in this example.");
	}

	public Task<Outlet> UpdateAsync(Outlet outlet, CancellationToken cancellationToken = default)
	{
		throw new NotSupportedException("UpdateAsync is not supported via ODBC repository in this example.");
	}

	public Task<bool> DeleteAsync(string outletIdentifier, CancellationToken cancellationToken = default)
	{
		throw new NotSupportedException("DeleteAsync is not supported via ODBC repository in this example.");
	}

	public async Task<bool> ExistsAsync(string outletIdentifier, CancellationToken cancellationToken = default)
	{
		const string sql = @"
WITH latest AS (
  SELECT OutletIdentifier, ROW_NUMBER() OVER (PARTITION BY OutletIdentifier ORDER BY year DESC, week DESC) AS rn
  FROM dev_gold.outlet_aggregated
)
SELECT COUNT(*) FROM latest WHERE rn = 1 AND OutletIdentifier = ?";

		await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
		await using var command = new OdbcCommand(sql, connection);
		command.Parameters.AddWithValue("@p1", outletIdentifier);
		var scalar = await Task.Run(() => command.ExecuteScalar(), cancellationToken);
		return Convert.ToInt32(scalar) > 0;
	}

	private static Outlet MapOutlet(IDataRecord record)
	{
		var year = Convert.ToInt32(record["year"]);
		var week = Convert.ToInt32(record["week"]);
		var totalOuterQuantity = Convert.ToInt32(record["TotalOuterQuantity"]);
		var countOuterQuantity = Convert.ToInt32(record["CountOuterQuantity"]);
		var totalSales6w = Convert.ToDecimal(record["total_sales_6w"]);
		var mean = Convert.ToDecimal(record["mean"]);
		var lowerLimit = Convert.ToDecimal(record["lowerlimit"]);
		var upperLimit = Convert.ToDecimal(record["upperlimit"]);
		var healthStatus = Convert.ToString(record["health_status"]) ?? string.Empty;
		var storeRank = Convert.ToInt32(record["store_rank"]);
		var outletName = Convert.ToString(record["OutletName"]) ?? string.Empty;
		var outletIdentifier = Convert.ToString(record["OutletIdentifier"]) ?? string.Empty;
		var addressLine1 = Convert.ToString(record["AddressLine1"]) ?? string.Empty;
		var state = Convert.ToString(record["State"]) ?? string.Empty;
		var county = Convert.ToString(record["County"]) ?? string.Empty;

		return new Outlet(
			year,
			week,
			totalOuterQuantity,
			countOuterQuantity,
			totalSales6w,
			mean,
			lowerLimit,
			upperLimit,
			healthStatus,
			storeRank,
			outletName,
			outletIdentifier,
			addressLine1,
			state,
			county
		);
	}
}