using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesManagement.API.Features.Posts;
using SalesManagement.Application.Features.Posts.CreateComment;
using SalesManagement.Application.Features.Posts.CreatePost;

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

        public PostsController(
            CreatePostHandler createPostHandler,
            CreateCommentHandler createCommentHandler)
        {
            _createPostHandler = createPostHandler;
            _createCommentHandler = createCommentHandler;
        }

        [HttpPost("[action]")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Create(
            [FromForm] CreatePostRequest request, CancellationToken cancellationToken)
        {
            var images = request.Images ?? [];
            var streams = images.Select(f => f.OpenReadStream()).ToList();

            try
            {
                var imageInputs = images
                    .Zip(streams, (file, stream) => (stream, (string?)Path.GetExtension(file.FileName)))
                    .ToList();

                await _createPostHandler.Handle(
                    new CreatePostCommand(
                        request.Id, request.ProfileRef.GetValueOrDefault(), request.Title, request.Post,
                        imageInputs, request.RemovePhotoIds),
                    cancellationToken);
            }
            finally
            {
                foreach (var stream in streams)
                    await stream.DisposeAsync();
            }

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
    }
}
