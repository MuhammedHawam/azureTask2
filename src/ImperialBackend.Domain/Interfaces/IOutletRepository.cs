using ImperialBackend.Domain.Entities;
using ImperialBackend.Domain.Enums;

namespace ImperialBackend.Domain.Interfaces;

/// <summary>
/// Repository interface for Outlet entity operations with optimized queries
/// </summary>
public interface IOutletRepository
{
    Task<Outlet?> GetByIdAsync(string outletIdentifier, CancellationToken cancellationToken = default);

    Task<IEnumerable<Outlet>> GetAllAsync(
        int? year = null,
        int? week = null,
        string? healthStatus = null,
        string? searchTerm = null,
        int pageNumber = 1,
        int pageSize = 10,
        string sortBy = "StoreRank",
        string sortDirection = "asc",
        CancellationToken cancellationToken = default);

    Task<int> GetCountAsync(
        int? year = null,
        int? week = null,
        string? healthStatus = null,
        string? searchTerm = null,
        CancellationToken cancellationToken = default);

    Task<Outlet> AddAsync(Outlet outlet, CancellationToken cancellationToken = default);
    Task<Outlet> UpdateAsync(Outlet outlet, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string outletIdentifier, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(string outletIdentifier, CancellationToken cancellationToken = default);
}