using Microsoft.Extensions.Logging;
using SalesManagement.Domain.Common;
using SalesManagement.Domain.Features.PartyProfiles;

namespace SalesManagement.Application.Features.Auth.PartyLogin
{
    public class ProfilePhotoUrlResolver(
        IProfilePhotoRepository photoRepository,
        IMediaUrlBuilder mediaUrlBuilder,
        ILogger<ProfilePhotoUrlResolver> logger)
    {
        public async Task<string?> GetAsync(int partyId, CancellationToken cancellationToken = default)
        {
            try
            {
                var path = await photoRepository.GetMainPhotoPathAsync(partyId, cancellationToken);
                return mediaUrlBuilder.Build(path);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "خواندن عکس پروفایلِ کاربر {PartyId} ناموفق بود", partyId);
                return null;
            }
        }
    }
}
