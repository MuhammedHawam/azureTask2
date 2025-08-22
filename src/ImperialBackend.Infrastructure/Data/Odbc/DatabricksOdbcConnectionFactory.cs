using System.Data.Odbc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ImperialBackend.Infrastructure.Data.Odbc;

public class DatabricksOdbcConnectionFactory
{
	private readonly IConfiguration _configuration;
	private readonly ILogger<DatabricksOdbcConnectionFactory> _logger;

	public DatabricksOdbcConnectionFactory(
		IConfiguration configuration,
		ILogger<DatabricksOdbcConnectionFactory> logger)
	{
		_configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public async Task<OdbcConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default)
	{
		var section = _configuration.GetSection("Databricks");
		var host = section["ServerHostname"] ?? string.Empty;
		var httpPath = section["HTTPPath"] ?? string.Empty;
		var accessToken = section["AccessToken"] ?? string.Empty;
		var catalog = section["Catalog"];
		var schema = section["Schema"];
		var driverName = section["OdbcDriver"] ?? "Simba Spark ODBC Driver";

		if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(httpPath) || string.IsNullOrWhiteSpace(accessToken))
		{
			throw new InvalidOperationException("Databricks ODBC configuration is missing required values: ServerHostname, HTTPPath, or AccessToken.");
		}

		// Compose a DSN-less ODBC connection string for Databricks SQL Warehouse
		// Note: Driver name may vary depending on environment (e.g., "Databricks ODBC Driver"). Allow override via Databricks:OdbcDriver
		var connectionString =
			$"Driver={{{driverName}}};" +
			$"Host={host};" +
			"Port=443;" +
			$"HTTPPath={httpPath};" +
			"ThriftTransport=2;" +
			"SSL=1;" +
			"AuthMech=3;" +
			"UID=token;" +
			$"PWD={accessToken};" +
			"SparkServerType=3;" +
			(catalog is not null ? $"Catalog={catalog};" : string.Empty) +
			(schema is not null ? $"Schema={schema};" : string.Empty);

		_logger.LogInformation("Opening ODBC connection to Databricks host {Host}", host);

		var connection = new OdbcConnection(connectionString);

		// Open is synchronous; run on thread pool to avoid blocking
		await Task.Run(() => connection.Open(), cancellationToken);
		return connection;
	}
}