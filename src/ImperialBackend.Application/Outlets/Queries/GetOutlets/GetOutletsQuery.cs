using ImperialBackend.Application.Common.Models;
using ImperialBackend.Application.DTOs;
using ImperialBackend.Domain.Enums;
using MediatR;
using System.ComponentModel;

namespace ImperialBackend.Application.Outlets.Queries.GetOutlets;

/// <summary>
/// Query to get outlets with filtering, sorting, and pagination
/// </summary>
public record GetOutletsQuery : IRequest<Result<PagedResult<OutletDto>>>
{
    public int? Year { get; init; }
    public int? Week { get; init; }
    public string? HealthStatus { get; init; }
    public string? SearchTerm { get; init; }

    [DefaultValue(1)]
    public int PageNumber { get; init; } = 1;

    [DefaultValue(10)]
    public int PageSize { get; init; } = 10;

    [DefaultValue("StoreRank")]
    public string SortBy { get; init; } = "StoreRank";

    [DefaultValue("asc")]
    public string SortDirection { get; init; } = "asc";
}