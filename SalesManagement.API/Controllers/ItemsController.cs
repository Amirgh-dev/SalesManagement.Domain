using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesManagement.API.Features.Items;
using SalesManagement.Application.Features.Items.QueryItems;

namespace SalesManagement.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class ItemsController : ControllerBase
    {
        private readonly QueryItemsHandler _queryItemsHandler;

        public ItemsController(QueryItemsHandler queryItemsHandler)
        {
            _queryItemsHandler = queryItemsHandler;
        }

        [HttpPost("query")]
        [ProducesResponseType(typeof(QueryItemsResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<QueryItemsResult>> Query(
            [FromBody] QueryItemsRequest request, CancellationToken cancellationToken)
        {
            var result = await _queryItemsHandler.Handle(
                new QueryItemsCommand(
                    request.Id, request.CategoryCode, request.Scope, request.Fields,
                    request.Search?.Term, request.Search?.Fields,
                    request.OrderBy, request.Descending,
                    request.Skip, request.Take, request.Page, request.PageSize),
                cancellationToken);

            return Ok(result);
        }
    }
}
