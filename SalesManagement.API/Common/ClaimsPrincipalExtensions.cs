using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using SalesManagement.Domain.Common.Exceptions;

namespace SalesManagement.API.Common
{
    public static class ClaimsPrincipalExtensions
    {
        public static int GetPartyId(this ClaimsPrincipal user)
        {
            var raw = user.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub);

            if (!int.TryParse(raw, out var partyId) || partyId <= 0)
                throw new AuthenticationException("شناسه کاربر در توکن نامعتبر است");

            return partyId;
        }
    }
}
