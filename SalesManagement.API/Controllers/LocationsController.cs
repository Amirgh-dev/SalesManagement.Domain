using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesManagement.Application.Features.Locations.GetLocations;

namespace SalesManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LocationsController : ControllerBase
    {
        private readonly GetLocationsHandler _getLocationsHandler;

        public LocationsController(GetLocationsHandler getLocationsHandler)
        {
            _getLocationsHandler = getLocationsHandler;
        }

        [HttpPost("provinces")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(GetLocationsResult), StatusCodes.Status200OK)]
        public async Task<ActionResult<GetLocationsResult>> Provinces(CancellationToken cancellationToken)
        {
            var result = await _getLocationsHandler.GetProvincesAsync(cancellationToken);
            return Ok(result);
        }

        [HttpPost("provinces/{provinceId:int}/counties")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(GetLocationsResult), StatusCodes.Status200OK)]
        public async Task<ActionResult<GetLocationsResult>> Counties(
            int provinceId, CancellationToken cancellationToken)
        {
            var result = await _getLocationsHandler.GetCountiesAsync(provinceId, cancellationToken);
            return Ok(result);
        }
    }
}
