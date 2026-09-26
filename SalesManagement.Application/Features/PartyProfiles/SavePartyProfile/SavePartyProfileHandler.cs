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
                cmd.Id, cmd.PartyId, cmd.UserName, cmd.AboutMe, cmd.Address, cmd.Geolocation);

            if (cmd.ProfilePhoto is { } mainCheck)
                ProfilePhotoRules.Validate(mainCheck.Extension, mainCheck.Content.Length);
            foreach (var (content, extension) in cmd.Photos)
                ProfilePhotoRules.Validate(extension, content.Length);

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
                var (created, deletedFileNames) =
                    await partyProfileRepository.UpsertAsync(profile, cancellationToken);

                foreach (var fileName in deletedFileNames)
                    await fileStorage.DeleteAsync(ProfileFolder, fileName, CancellationToken.None);

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
