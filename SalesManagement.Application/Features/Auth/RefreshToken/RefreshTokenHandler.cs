using Microsoft.Extensions.Logging;
using SalesManagement.Application.Common.Interfaces;
using SalesManagement.Application.Features.Auth.PartyLogin;
using SalesManagement.Domain.Common.Exceptions;
using SalesManagement.Domain.Features.Auth.PartyLogin;

namespace SalesManagement.Application.Features.Auth.RefreshToken
{
    public record RefreshTokenCommand(int PartyId, string Phone);

    public class RefreshTokenHandler(
        IPartyRepository partyRepository,
        ITokenGenerator tokenGenerator,
        ProfilePhotoUrlResolver photoUrlResolver,
        ILogger<RefreshTokenHandler> logger)
    {
        public async Task<PartyLoginResult> Handle(
            RefreshTokenCommand command, CancellationToken cancellationToken = default)
        {
            var party = await partyRepository.GetByIdAsync(command.PartyId, cancellationToken)
                        ?? throw new AuthenticationException("کاربر پیدا نشد");

            party.EnsureCanLogin();

            var accessToken = tokenGenerator.GenerateAccessToken(party.PartyId, command.Phone);

            logger.LogInformation("تمدید توکن Party: {PartyId}", party.PartyId);

            return new PartyLoginResult(
                accessToken, tokenGenerator.AccessTokenExpirationSeconds, party.PartyId, party.FullName,
                await photoUrlResolver.GetAsync(party.PartyId, cancellationToken));
        }
    }
}
