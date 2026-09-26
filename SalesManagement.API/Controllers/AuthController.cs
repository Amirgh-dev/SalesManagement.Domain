using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SalesManagement.API.Common;
using SalesManagement.API.Features.Auth.PartyLogin;
using SalesManagement.Application.Features.Auth.PartyLogin;
using SalesManagement.Application.Features.Auth.Logout;
using SalesManagement.Application.Features.Auth.RefreshToken;

namespace SalesManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly PartyLoginHandler _partyLoginHandler;

        private readonly RefreshTokenHandler _refreshTokenHandler;

        private readonly LogoutHandler _logoutHandler;

        public AuthController(
            PartyLoginHandler partyLoginHandler, RefreshTokenHandler refreshTokenHandler, LogoutHandler logoutHandler)
        {
            _partyLoginHandler = partyLoginHandler;
            _refreshTokenHandler = refreshTokenHandler;
            _logoutHandler = logoutHandler;
        }

        [HttpPost("logout")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Logout(CancellationToken cancellationToken)
        {
            var jti = User.FindFirst("jti")?.Value;
            DateTimeOffset? expiresAt = long.TryParse(User.FindFirst("exp")?.Value, out var exp)
                ? DateTimeOffset.FromUnixTimeSeconds(exp)
                : null;

            await _logoutHandler.Handle(new LogoutCommand(jti, expiresAt), cancellationToken);

            return NoContent();
        }

        [HttpPost("refresh")]
        [Authorize]
        [ProducesResponseType(typeof(PartyLoginResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<PartyLoginResponse>> Refresh(CancellationToken cancellationToken)
        {
            var phone = User.FindFirst("phone")?.Value ?? "";
            var result = await _refreshTokenHandler.Handle(
                new RefreshTokenCommand(User.GetPartyId(), phone), cancellationToken);

            return Ok(new PartyLoginResponse(
                result.AccessToken, result.ExpiresInSeconds, result.PartyId, result.FullName, result.ProfilePhotoUrl));
        }

        [HttpPost("tokens")]
        [AllowAnonymous]
        [EnableRateLimiting(ApiServiceCollectionExtensions.AuthRateLimiterPolicy)]
        [ProducesResponseType(typeof(PartyLoginResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<ActionResult<PartyLoginResponse>> PartyLogin(
            [FromBody] PartyLoginRequest request, CancellationToken cancellationToken)
        {
            var command = new PartyLoginCommand(request.Phone, request.NationalCode);
            var result = await _partyLoginHandler.Handle(command, cancellationToken);

            return Ok(new PartyLoginResponse(
                result.AccessToken, result.ExpiresInSeconds,
                result.PartyId, result.FullName, result.ProfilePhotoUrl));
        }
    }
}
