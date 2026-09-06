using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesManagement.Application.Features.Items.GetItems;

namespace SalesManagement.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class ItemsController : ControllerBase
    {
        private readonly GetItemsHandler _getItemsHandler;

        public ItemsController(GetItemsHandler getItemsHandler)
        {
            _getItemsHandler = getItemsHandler;
        }

        [HttpPost]
        [ProducesResponseType(typeof(GetItemsResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<GetItemsResult>> GetItems(CancellationToken cancellationToken)
        {
            var result = await _getItemsHandler.Handle(new GetItemsQuery(), cancellationToken);
            return Ok(result);
        }
    }
}
