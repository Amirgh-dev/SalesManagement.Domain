using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesManagement.API.Features.Auth.PartyLogin;
using SalesManagement.Application.Features.Auth.PartyLogin;

namespace SalesManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly PartyLoginHandler _partyLoginHandler;

        public AuthController(PartyLoginHandler partyLoginHandler)
        {
            _partyLoginHandler = partyLoginHandler;
        }

        [HttpPost("tokens")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(PartyLoginResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<PartyLoginResponse>> PartyLogin(
            [FromBody] PartyLoginRequest request, CancellationToken cancellationToken)
        {
            var command = new PartyLoginCommand(request.Phone, request.NationalCode);
            var result = await _partyLoginHandler.Handle(command, cancellationToken);

            return Ok(new PartyLoginResponse(
                result.AccessToken, result.ExpiresInSeconds,
                result.PartyId, result.FullName));
        }
    }
}