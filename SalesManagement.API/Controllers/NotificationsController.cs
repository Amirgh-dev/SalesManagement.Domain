using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesManagement.API.Common;
using SalesManagement.API.Features.Notifications;
using SalesManagement.Application.Features.Notifications.GetNotifications;

namespace SalesManagement.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class NotificationsController : ControllerBase
    {
        private readonly GetNotificationsHandler _handler;

        public NotificationsController(GetNotificationsHandler handler)
        {
            _handler = handler;
        }

        [HttpPost("[action]")]
        [ProducesResponseType(typeof(GetNotificationsResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<GetNotificationsResult>> Query(
            [FromBody] GetNotificationsRequest request, CancellationToken cancellationToken)
        {
            var partyId = User.GetPartyId();

            var result = await _handler.Handle(
                new GetNotificationsCommand(partyId, request.Skip, request.Take),
                cancellationToken);

            return Ok(result);
        }
    }
}
