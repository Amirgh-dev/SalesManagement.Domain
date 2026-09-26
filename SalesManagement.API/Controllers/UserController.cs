using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesManagement.API.Common;
using SalesManagement.Application.Features.Users.QueryUser;

namespace SalesManagement.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/user")]
    public class UserController : ControllerBase
    {
        private readonly QueryUserHandler _queryUserHandler;

        public UserController(QueryUserHandler queryUserHandler)
        {
            _queryUserHandler = queryUserHandler;
        }

        [HttpPost("query")]
        [ProducesResponseType(typeof(QueryUserResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<QueryUserResult>> Query(CancellationToken cancellationToken)
        {
            var result = await _queryUserHandler.Handle(
                new QueryUserCommand(User.GetPartyId()), cancellationToken);

            return Ok(result);
        }
    }
}
