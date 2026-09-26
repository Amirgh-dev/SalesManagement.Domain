using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesManagement.Application.Features.SaleTypeDiscounts.GetSaleTypeDiscounts;

namespace SalesManagement.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/sale-type-discounts")]
    public class SaleTypeDiscountsController : ControllerBase
    {
        private readonly GetSaleTypeDiscountsHandler _getHandler;

        public SaleTypeDiscountsController(GetSaleTypeDiscountsHandler getHandler)
        {
            _getHandler = getHandler;
        }

        [HttpPost("list")]
        [ProducesResponseType(typeof(GetSaleTypeDiscountsResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<GetSaleTypeDiscountsResult>> List(CancellationToken cancellationToken)
        {
            return Ok(await _getHandler.Handle(cancellationToken));
        }
    }
}
