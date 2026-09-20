using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesManagement.API.Features.Posts;
using SalesManagement.Application.Features.Posts.LikePost;

namespace SalesManagement.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class PostLikesController : ControllerBase
    {
        private readonly LikePostHandler _likePostHandler;

        public PostLikesController(LikePostHandler likePostHandler)
        {
            _likePostHandler = likePostHandler;
        }

        [HttpPost]
        [ProducesResponseType(typeof(LikePostResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<LikePostResult>> Like(
            [FromBody] LikePostRequest request, CancellationToken cancellationToken)
        {
            var result = await _likePostHandler.Handle(
                new LikePostCommand(request.PostId.GetValueOrDefault()), cancellationToken);

            return Ok(result);
        }
    }
}
