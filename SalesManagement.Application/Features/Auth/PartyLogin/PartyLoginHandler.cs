using Microsoft.Extensions.Logging;
using SalesManagement.Application.Common.Interfaces;
using SalesManagement.Domain.Common.Exceptions;
using SalesManagement.Domain.Features.Auth.PartyLogin;

namespace SalesManagement.Application.Features.Auth.PartyLogin
{
    public class PartyLoginHandler
    {
        private readonly IPartyRepository _partyRepository;
        private readonly ITokenGenerator _tokenGenerator;
        private readonly ILogger<PartyLoginHandler> _logger;

        public PartyLoginHandler(
            IPartyRepository partyRepository,
            ITokenGenerator tokenGenerator,
            ILogger<PartyLoginHandler> logger)
        {
            _partyRepository = partyRepository;
            _tokenGenerator = tokenGenerator;
            _logger = logger;
        }

        public async Task<PartyLoginResult> Handle(
            PartyLoginCommand command, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(command.Phone) || string.IsNullOrWhiteSpace(command.NationalCode))
                throw new DomainException("اطلاعات ورود ناقص است");

            var party = await _partyRepository.GetByPhoneAndNationalCodeAsync(
                command.Phone.Trim(), command.NationalCode.Trim(), cancellationToken);

            if (party is null)
            {
                _logger.LogWarning("تلاش ورود ناموفق برای شماره: {Phone}", command.Phone);
                throw new DomainException("شماره تماس یا کد ملی اشتباه است");
            }

            party.EnsureCanLogin();

            var accessToken = _tokenGenerator.GenerateAccessToken(party.PartyId, command.Phone);
            var refreshToken = _tokenGenerator.GenerateRefreshToken();

            _logger.LogInformation("ورود موفق Party: {PartyId}", party.PartyId);

            return new PartyLoginResult(
                accessToken, refreshToken, ExpiresInSeconds: 3600,
                party.PartyId, party.FullName);
        }
    }
}