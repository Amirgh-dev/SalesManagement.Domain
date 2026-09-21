using Microsoft.Extensions.Logging;
using SalesManagement.Domain.Common.Exceptions;
using SalesManagement.Domain.Features.Auth.PartyLogin;

namespace SalesManagement.Application.Features.Auth.Logout
{
    public record LogoutCommand(string? Jti, DateTimeOffset? ExpiresAt);

    public class LogoutHandler(IRevokedTokenStore revokedTokenStore, ILogger<LogoutHandler> logger)
    {
        public async Task Handle(LogoutCommand command, CancellationToken cancellationToken = default)
        {
            if (!Guid.TryParse(command.Jti, out var jti) || command.ExpiresAt is null)
                throw new AuthenticationException("توکن نامعتبر است");

            await revokedTokenStore.RevokeAsync(jti, command.ExpiresAt.Value, cancellationToken);

            logger.LogInformation("خروج کاربر؛ توکن {Jti} باطل شد", jti);
        }
    }
}
