using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesManagement.Application.Features.Items.GetCategories;
using SalesManagement.Application.Features.Items.GetItemById;
using SalesManagement.Application.Features.Items.GetItems;
using SalesManagement.Application.Features.Items.GetItemsByCategory;

namespace SalesManagement.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    [SuppressMessage("Major Code Smell", "S6960:Controllers should not have mixed responsibilities",
        Justification = "All actions operate on the same Items resource (list, get-by-id, categories, items-by-category).")]
    public class ItemsController : ControllerBase
    {
        private readonly GetItemsHandler _getItemsHandler;
        private readonly GetItemByIdHandler _getItemByIdHandler;
        private readonly GetCategoriesHandler _getCategoriesHandler;
        private readonly GetItemsByCategoryHandler _getItemsByCategoryHandler;

        public ItemsController(
            GetItemsHandler getItemsHandler,
            GetItemByIdHandler getItemByIdHandler,
            GetCategoriesHandler getCategoriesHandler,
            GetItemsByCategoryHandler getItemsByCategoryHandler)
        {
            _getItemsHandler = getItemsHandler;
            _getItemByIdHandler = getItemByIdHandler;
            _getCategoriesHandler = getCategoriesHandler;
            _getItemsByCategoryHandler = getItemsByCategoryHandler;
        }

        [HttpPost("[action]")]
        [ProducesResponseType(typeof(GetItemsResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<GetItemsResult>> GetItems(CancellationToken cancellationToken)
        {
            var result = await _getItemsHandler.Handle(new GetItemsQuery(), cancellationToken);
            return Ok(result);
        }

        [HttpPost("{id:int}")]
        [ProducesResponseType(typeof(GetItemByIdResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<GetItemByIdResult>> GetItemById(int id, CancellationToken cancellationToken)
        {
            var result = await _getItemByIdHandler.Handle(new GetItemByIdQuery(id), cancellationToken);
            return Ok(result);
        }

        [HttpPost("categories")]
        [ProducesResponseType(typeof(GetCategoriesResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<GetCategoriesResult>> GetCategories(CancellationToken cancellationToken)
        {
            var result = await _getCategoriesHandler.Handle(new GetCategoriesQuery(), cancellationToken);
            return Ok(result);
        }

        [HttpPost("by-category/{code}")]
        [ProducesResponseType(typeof(GetItemsResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<GetItemsResult>> GetItemsByCategory(
            string code, CancellationToken cancellationToken)
        {
            var result = await _getItemsByCategoryHandler.Handle(
                new GetItemsByCategoryQuery(code), cancellationToken);
            return Ok(result);
        }
    }
}
