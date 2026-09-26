using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesManagement.API.Common;
using SalesManagement.API.Features.Files;
using SalesManagement.Application.Features.Files.DeleteFile;

namespace SalesManagement.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/files")]
    public class FilesController : ControllerBase
    {
        private readonly DeleteFileHandler _deleteFileHandler;

        public FilesController(DeleteFileHandler deleteFileHandler)
        {
            _deleteFileHandler = deleteFileHandler;
        }

        [HttpPost("delete")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(
            [FromBody] DeleteFileRequest request, CancellationToken cancellationToken)
        {
            await _deleteFileHandler.Handle(
                new DeleteFileCommand(request.Id, User.GetPartyId()), cancellationToken);

            return NoContent();
        }
    }
}
