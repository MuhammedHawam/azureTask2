using ImperialBackend.Application.Common.Models;
using ImperialBackend.Application.DTOs;
using ImperialBackend.Application.Outlets.Queries.GetOutletDetails;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ImperialBackend.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class OutletDetailsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<OutletDetailsController> _logger;

    public OutletDetailsController(IMediator mediator, ILogger<OutletDetailsController> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<OutletDetailDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<OutletDetailDto>>> Get([FromQuery] GetOutletDetailsQuery query)
    {
        var result = await _mediator.Send(query);
        if (!result.IsSuccess)
        {
            return BadRequest(result.Error);
        }
        return Ok(result.Value);
    }
}

