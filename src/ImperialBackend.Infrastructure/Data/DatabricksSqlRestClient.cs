using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ImperialBackend.Infrastructure.Data;

public class DatabricksSqlRestClient
{
	private readonly HttpClient _httpClient;
	private readonly ILogger<DatabricksSqlRestClient> _logger;
	private readonly string _warehouseId;
	private readonly string? _catalog;
	private readonly string? _schema;

	public DatabricksSqlRestClient(HttpClient httpClient, IConfiguration configuration, ILogger<DatabricksSqlRestClient> logger)
	{
		_httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));

		var section = configuration.GetSection("Databricks");
		var host = section["ServerHostname"] ?? throw new InvalidOperationException("Databricks:ServerHostname is required");
		var token = section["AccessToken"] ?? throw new InvalidOperationException("Databricks:AccessToken is required");
		var warehouseId = section["WarehouseId"];
		var httpPath = section["HTTPPath"]; // fallback to parse warehouse id
		_catalog = section["Catalog"];
		_schema = section["Schema"];

		_httpClient.BaseAddress = new Uri($"https://{host}/");
		_httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
		_httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

		_warehouseId = !string.IsNullOrWhiteSpace(warehouseId)
			? warehouseId
			: ParseWarehouseIdFromHttpPath(httpPath) ?? throw new InvalidOperationException("Databricks:WarehouseId not set and could not parse from HTTPPath");
	}

	private static string? ParseWarehouseIdFromHttpPath(string? httpPath)
	{
		if (string.IsNullOrWhiteSpace(httpPath)) return null;
		// Expected: /sql/1.0/warehouses/{warehouseId}
		var idx = httpPath.LastIndexOf("/warehouses/", StringComparison.OrdinalIgnoreCase);
		if (idx < 0) return null;
		var id = httpPath[(idx + "/warehouses/".Length)..].Trim('/');
		return string.IsNullOrWhiteSpace(id) ? null : id;
	}

	public async Task<JsonDocument> ExecuteStatementAsync(string sql, CancellationToken cancellationToken = default)
	{
		var payload = new
		{
			statement = sql,
			warehouse_id = _warehouseId,
			catalog = _catalog,
			schema = _schema,
			disposition = "INLINE"
		};

		var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
		using var postResponse = await _httpClient.PostAsync("api/2.0/sql/statements", content, cancellationToken);
		postResponse.EnsureSuccessStatusCode();
		await using var postStream = await postResponse.Content.ReadAsStreamAsync(cancellationToken);
		using var firstDoc = await JsonDocument.ParseAsync(postStream, cancellationToken: cancellationToken);

		var root = firstDoc.RootElement;
		string? state = root.GetProperty("status").GetProperty("state").GetString();
		if (string.Equals(state, "SUCCEEDED", StringComparison.OrdinalIgnoreCase))
		{
			return CloneJsonDocument(firstDoc);
		}

		var statementId = root.TryGetProperty("statement_id", out var idEl) ? idEl.GetString() : null;
		if (string.IsNullOrWhiteSpace(statementId))
		{
			throw new InvalidOperationException("Databricks SQL response missing statement_id");
		}

		// Poll until SUCCEEDED or FAILED
		for (;;)
		{
			await Task.Delay(TimeSpan.FromMilliseconds(300), cancellationToken);
			using var pollResponse = await _httpClient.GetAsync($"api/2.0/sql/statements/{statementId}", cancellationToken);
			pollResponse.EnsureSuccessStatusCode();
			await using var pollStream = await pollResponse.Content.ReadAsStreamAsync(cancellationToken);
			using var pollDoc = await JsonDocument.ParseAsync(pollStream, cancellationToken: cancellationToken);
			var s = pollDoc.RootElement.GetProperty("status").GetProperty("state").GetString();
			if (string.Equals(s, "SUCCEEDED", StringComparison.OrdinalIgnoreCase))
			{
				return CloneJsonDocument(pollDoc);
			}
			if (string.Equals(s, "FAILED", StringComparison.OrdinalIgnoreCase) || string.Equals(s, "CANCELED", StringComparison.OrdinalIgnoreCase))
			{
				throw new InvalidOperationException($"Databricks SQL statement ended with state {s}");
			}
		}
	}

	private static JsonDocument CloneJsonDocument(JsonDocument source)
	{
		using var ms = new MemoryStream();
		using (var writer = new Utf8JsonWriter(ms))
		{
			source.WriteTo(writer);
		}
		ms.Position = 0;
		return JsonDocument.Parse(ms);
	}
}