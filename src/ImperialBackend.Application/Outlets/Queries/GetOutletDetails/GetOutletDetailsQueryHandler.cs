using AutoMapper;
using ImperialBackend.Application.Common.Models;
using ImperialBackend.Application.DTOs;
using ImperialBackend.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ImperialBackend.Application.Outlets.Queries.GetOutletDetails;

public class GetOutletDetailsQueryHandler : IRequestHandler<GetOutletDetailsQuery, Result<PagedResult<OutletDetailDto>>>
{
    private readonly IOutletDetailRepository _repository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetOutletDetailsQueryHandler> _logger;

    public GetOutletDetailsQueryHandler(IOutletDetailRepository repository, IMapper mapper, ILogger<GetOutletDetailsQueryHandler> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<PagedResult<OutletDetailDto>>> Handle(GetOutletDetailsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var rows = await _repository.GetAllAsync(
                internalCode: request.InternalCode,
                aamsSkuCode: request.AamsSkuCode,
                year: request.Year,
                week: request.Week,
                productGroupName: request.ProductGroupName,
                reportingProductGroupName: request.ReportingProductGroupName,
                searchTerm: request.SearchTerm,
                pageNumber: request.PageNumber,
                pageSize: request.PageSize,
                sortBy: request.SortBy,
                sortDirection: request.SortDirection,
                cancellationToken: cancellationToken);

            var totalCount = await _repository.GetCountAsync(
                internalCode: request.InternalCode,
                aamsSkuCode: request.AamsSkuCode,
                year: request.Year,
                week: request.Week,
                productGroupName: request.ProductGroupName,
                reportingProductGroupName: request.ReportingProductGroupName,
                searchTerm: request.SearchTerm,
                cancellationToken: cancellationToken);

            var dtos = _mapper.Map<IEnumerable<OutletDetailDto>>(rows);
            var paged = new PagedResult<OutletDetailDto>(dtos, totalCount, request.PageNumber, request.PageSize);
            return Result<PagedResult<OutletDetailDto>>.Success(paged);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling GetOutletDetailsQuery");
            return Result<PagedResult<OutletDetailDto>>.Failure("Failed to get outlet details");
        }
    }
}

