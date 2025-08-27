using ImperialBackend.Domain.Entities;

namespace ImperialBackend.Domain.Interfaces;

/// <summary>
/// Repository interface for OutletDetail rows with filtering and pagination
/// </summary>
public interface IOutletDetailRepository
{
    Task<IEnumerable<OutletDetail>> GetAllAsync(
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
        CancellationToken cancellationToken = default);

    Task<int> GetCountAsync(
        string? internalCode = null,
        string? aamsSkuCode = null,
        int? year = null,
        int? week = null,
        string? productGroupName = null,
        string? reportingProductGroupName = null,
        string? searchTerm = null,
        CancellationToken cancellationToken = default);
}

