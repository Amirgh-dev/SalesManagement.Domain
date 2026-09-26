using Microsoft.Extensions.Logging;
using SalesManagement.Application.Common.Interfaces;
using SalesManagement.Domain.Common;
using SalesManagement.Domain.Features.PartyProfiles;

namespace SalesManagement.Application.Features.PartyProfiles.SaveProfilePhoto
{
    public record SaveProfilePhotoCommand(int PartyId, Stream Content, string? Extension, long Length);

    public record SaveProfilePhotoResult(Guid Id, string FileName, string? Url);

    public class SaveProfilePhotoHandler(
        IProfilePhotoRepository photoRepository,
        IFileStorage fileStorage,
        IMediaUrlBuilder mediaUrlBuilder,
        ILogger<SaveProfilePhotoHandler> logger)
    {
        private const string ProfileFolder = "Profile";

        public async Task<SaveProfilePhotoResult> Handle(
            SaveProfilePhotoCommand cmd, CancellationToken cancellationToken = default)
        {
            ProfilePhotoRules.Validate(cmd.Extension, cmd.Length);

            var ext = ProfilePhotoRules.NormalizeExtension(cmd.Extension);
            var savedFileName = await fileStorage.SaveAsync(cmd.Content, ProfileFolder, ext, cancellationToken);

            try
            {
                var replaced = await photoRepository.ReplaceMainPhotoAsync(
                    cmd.PartyId, savedFileName, cancellationToken);

                if (replaced.OldFileName is not null)
                    await fileStorage.DeleteAsync(ProfileFolder, replaced.OldFileName, CancellationToken.None);

                logger.LogInformation("عکس پروفایلِ کاربر {PartyRef} تغییر کرد", cmd.PartyId);

                return new SaveProfilePhotoResult(
                    replaced.NewPhotoId,
                    savedFileName, mediaUrlBuilder.Build($"/media/images/{ProfileFolder}/{savedFileName}"));
            }
            catch
            {
                await fileStorage.DeleteAsync(ProfileFolder, savedFileName, CancellationToken.None);
                throw;
            }
        }
    }
}
