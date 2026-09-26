using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesManagement.API.Features.Posts;
using SalesManagement.Application.Features.Posts.CreateComment;
using SalesManagement.Application.Features.Posts.CreatePost;
using SalesManagement.Application.Features.Posts.DeleteComment;
using SalesManagement.Application.Features.Posts.DeletePost;

namespace SalesManagement.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    [SuppressMessage("Major Code Smell", "S6960:Controllers should not have mixed responsibilities",
        Justification = "All actions operate on the same Posts resource (create post, add comment).")]
    public class PostsController : ControllerBase
    {
        private readonly CreatePostHandler _createPostHandler;
        private readonly CreateCommentHandler _createCommentHandler;
        private readonly DeletePostHandler _deletePostHandler;
        private readonly DeleteCommentHandler _deleteCommentHandler;

        public PostsController(
            CreatePostHandler createPostHandler,
            CreateCommentHandler createCommentHandler,
            DeletePostHandler deletePostHandler,
            DeleteCommentHandler deleteCommentHandler)
        {
            _createPostHandler = createPostHandler;
            _createCommentHandler = createCommentHandler;
            _deletePostHandler = deletePostHandler;
            _deleteCommentHandler = deleteCommentHandler;
        }

        [HttpPost("[action]")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(CreatePostResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<CreatePostResult>> Create(
            [FromForm] CreatePostRequest request, CancellationToken cancellationToken)
        {
            var images = request.Images ?? [];
            var streams = images.Select(f => f.OpenReadStream()).ToList();

            try
            {
                var imageInputs = images
                    .Zip(streams, (file, stream) => (stream, (string?)Path.GetExtension(file.FileName)))
                    .ToList();

                var result = await _createPostHandler.Handle(
                    new CreatePostCommand(
                        request.Id, request.ProfileRef.GetValueOrDefault(), request.Title, request.Post,
                        request.Type, imageInputs),
                    cancellationToken);

                return Ok(result);
            }
            finally
            {
                foreach (var stream in streams)
                    await stream.DisposeAsync();
            }
        }

        [HttpPost("delete")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Delete(
            [FromBody] DeletePostRequest request, CancellationToken cancellationToken)
        {
            await _deletePostHandler.Handle(
                new DeletePostCommand(request.Id, request.ProfileRef), cancellationToken);

            return NoContent();
        }

        [HttpPost("comments")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> CreateComment(
            [FromBody] CreateCommentRequest request, CancellationToken cancellationToken)
        {
            await _createCommentHandler.Handle(
                new CreateCommentCommand(
                    request.Id,
                    request.PostRef.GetValueOrDefault(),
                    request.ProfileRef.GetValueOrDefault(),
                    request.Comment),
                cancellationToken);

            return NoContent();
        }

        [HttpPost("comments/delete")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> DeleteComment(
            [FromBody] DeleteCommentRequest request, CancellationToken cancellationToken)
        {
            await _deleteCommentHandler.Handle(
                new DeleteCommentCommand(request.Id, request.ProfileRef), cancellationToken);

            return NoContent();
        }
    }
}
