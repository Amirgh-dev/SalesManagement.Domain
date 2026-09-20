using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesManagement.API.Features.Professors;
using SalesManagement.Application.Features.Professors.CreateProfessor;

namespace SalesManagement.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class ProfessorsController : ControllerBase
    {
        private readonly CreateProfessorHandler _createProfessorHandler;

        public ProfessorsController(CreateProfessorHandler createProfessorHandler)
        {
            _createProfessorHandler = createProfessorHandler;
        }

        [HttpPost]
        [ProducesResponseType(typeof(CreateProfessorResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<CreateProfessorResult>> Create(
            [FromBody] CreateProfessorRequest request, CancellationToken cancellationToken)
        {
            var result = await _createProfessorHandler.Handle(
                new CreateProfessorCommand(request.Id, request.Name, request.LastName, request.Specialization, request.IsOk),
                cancellationToken);

            return Ok(result);
        }
    }
}
