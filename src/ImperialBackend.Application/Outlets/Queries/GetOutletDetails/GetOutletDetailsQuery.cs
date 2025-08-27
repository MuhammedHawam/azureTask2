using ImperialBackend.Application.Common.Models;
using ImperialBackend.Application.DTOs;
using MediatR;
using System.ComponentModel;

namespace ImperialBackend.Application.Outlets.Queries.GetOutletDetails;

public class GetOutletDetailsQuery : IRequest<Result<PagedResult<OutletDetailDto>>>
{
    public string? InternalCode { get; init; }
    public string? AamsSkuCode { get; init; }
    public int? Year { get; init; }
    public int? Week { get; init; }
    public string? ProductGroupName { get; init; }
    public string? ReportingProductGroupName { get; init; }
    public string? SearchTerm { get; init; }

    [DefaultValue(1)]
    public int PageNumber { get; init; } = 1;

    [DefaultValue(10)]
    public int PageSize { get; init; } = 10;

    [DefaultValue("Year")]
    public string SortBy { get; init; } = "Year";

    [DefaultValue("desc")]
    public string SortDirection { get; init; } = "desc";
}

