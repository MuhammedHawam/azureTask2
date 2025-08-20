using AutoMapper;
using ImperialBackend.Application.Common.Models;
using ImperialBackend.Application.DTOs;
using ImperialBackend.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ImperialBackend.Application.Outlets.Queries.GetOutlets;

/// <summary>
/// Handler for GetOutletsQuery with optimized performance and comprehensive filtering
/// </summary>
public class GetOutletsQueryHandler : IRequestHandler<GetOutletsQuery, Result<PagedResult<OutletDto>>>
{
    private readonly IOutletRepository _outletRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetOutletsQueryHandler> _logger;

    public GetOutletsQueryHandler(
        IOutletRepository outletRepository,
        IMapper mapper,
        ILogger<GetOutletsQueryHandler> logger)
    {
        _outletRepository = outletRepository ?? throw new ArgumentNullException(nameof(outletRepository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<PagedResult<OutletDto>>> Handle(GetOutletsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting outlets - Page: {PageNumber}, PageSize: {PageSize}, Filters: {@Filters}",
                request.PageNumber, request.PageSize, new
                {
                    request.Year,
                    request.Week,
                    request.HealthStatus,
                    request.SearchTerm,
                    request.SortBy,
                    request.SortDirection
                });

            if (request.PageNumber < 1)
            {
                return Result<PagedResult<OutletDto>>.Failure("Page number must be greater than 0");
            }

            if (request.PageSize < 1 || request.PageSize > 100)
            {
                return Result<PagedResult<OutletDto>>.Failure("Page size must be between 1 and 100");
            }

            var outlets = await _outletRepository.GetAllAsync(
                year: request.Year,
                week: request.Week,
                healthStatus: request.HealthStatus,
                searchTerm: request.SearchTerm,
                pageNumber: request.PageNumber,
                pageSize: request.PageSize,
                sortBy: request.SortBy,
                sortDirection: request.SortDirection,
                cancellationToken: cancellationToken);

            var totalCount = await _outletRepository.GetCountAsync(
                year: request.Year,
                week: request.Week,
                healthStatus: request.HealthStatus,
                searchTerm: request.SearchTerm,
                cancellationToken: cancellationToken);

            var outletDtos = _mapper.Map<IEnumerable<OutletDto>>(outlets);

            var pagedResult = new PagedResult<OutletDto>(
                outletDtos,
                totalCount,
                request.PageNumber,
                request.PageSize);

            _logger.LogInformation("Successfully retrieved {Count} outlets out of {TotalCount} total (Page {PageNumber}/{TotalPages})",
                pagedResult.Count, pagedResult.TotalCount, request.PageNumber, pagedResult.TotalPages);

            return Result<PagedResult<OutletDto>>.Success(pagedResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while handling GetOutletsQuery");
            return Result<PagedResult<OutletDto>>.Failure("An error occurred while retrieving outlets");
        }
    }
}