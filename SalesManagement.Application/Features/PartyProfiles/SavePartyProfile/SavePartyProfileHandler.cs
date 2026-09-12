using Microsoft.Extensions.Logging;
using SalesManagement.Application.Common.Interfaces;
using SalesManagement.Domain.Features.PartyProfiles;

namespace SalesManagement.Application.Features.PartyProfiles.SavePartyProfile
{
    public class SavePartyProfileHandler(
        IPartyProfileRepository partyProfileRepository,
        IFileStorage fileStorage,
        ILogger<SavePartyProfileHandler> logger)
    {
        private const string ProfileFolder = "Profile";

        public async Task<SavePartyProfileResult> Handle(
            SavePartyProfileCommand cmd, CancellationToken cancellationToken = default)
        {
            var profile = PartyProfile.Create(
                cmd.PartyId, cmd.UserName, cmd.AboutMe, cmd.Address, cmd.Geolocation);

            var savedFileNames = new List<string>();

            if (cmd.ProfilePhoto is { } main)
            {
                var savedMain = await fileStorage.SaveAsync(
                    main.Content, ProfileFolder, main.Extension ?? ".jpg", cancellationToken);
                savedFileNames.Add(savedMain);
                profile = profile.WithProfilePhoto(savedMain);
            }

            var savedGalleryNames = new List<string>();
            foreach (var (content, extension) in cmd.Photos)
            {
                var savedFileName = await fileStorage.SaveAsync(
                    content, ProfileFolder, extension ?? ".jpg", cancellationToken);
                savedFileNames.Add(savedFileName);
                savedGalleryNames.Add(savedFileName);
            }
            profile = profile.WithPhotos(savedGalleryNames);

            try
            {
                var (created, oldMainPhotoFileName) =
                    await partyProfileRepository.UpsertAsync(profile, cancellationToken);

                if (oldMainPhotoFileName is not null)
                    await fileStorage.DeleteAsync(ProfileFolder, oldMainPhotoFileName, CancellationToken.None);

                logger.LogInformation(
                    "پروفایلِ کاربر {PartyRef} ذخیره شد (Created={Created})", profile.PartyRef, created);

                return new SavePartyProfileResult(created);
            }
            catch
            {
                foreach (var fileName in savedFileNames)
                    await fileStorage.DeleteAsync(ProfileFolder, fileName, CancellationToken.None);
                throw;
            }
        }
    }
}
