using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesManagement.API.Common;
using SalesManagement.API.Features.PartyProfiles;
using SalesManagement.Application.Features.PartyProfiles.QueryProfiles;
using SalesManagement.Application.Features.PartyProfiles.SavePartyProfile;
using SalesManagement.Application.Features.PartyProfiles.SaveProfilePhoto;

namespace SalesManagement.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/party-profiles")]
    [SuppressMessage("Major Code Smell", "S6960:Controllers should not have mixed responsibilities",
        Justification = "All actions operate on the same party-profile resource (save, query).")]
    public class PartyProfilesController : ControllerBase
    {
        private readonly SavePartyProfileHandler _saveHandler;
        private readonly QueryProfilesHandler _queryHandler;

        private readonly SaveProfilePhotoHandler _photoHandler;

        public PartyProfilesController(
            SavePartyProfileHandler saveHandler,
            QueryProfilesHandler queryHandler,
            SaveProfilePhotoHandler photoHandler)
        {
            _saveHandler = saveHandler;
            _queryHandler = queryHandler;
            _photoHandler = photoHandler;
        }

        [HttpPost("photo")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(SaveProfilePhotoResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<SaveProfilePhotoResult>> SavePhoto(
            [FromForm] SaveProfilePhotoRequest request, CancellationToken cancellationToken)
        {
            var photo = request.Photo!;
            await using var stream = photo.OpenReadStream();

            var result = await _photoHandler.Handle(
                new SaveProfilePhotoCommand(User.GetPartyId(), stream, Path.GetExtension(photo.FileName), photo.Length),
                cancellationToken);

            return Ok(result);
        }

        [HttpPost("[action]")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(SavePartyProfileResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<SavePartyProfileResult>> Save(
            [FromForm] SavePartyProfileRequest request, CancellationToken cancellationToken)
        {
            var partyId = User.GetPartyId();

            var photos = request.Photos ?? [];
            var mainStream = request.ProfilePhoto?.OpenReadStream();
            var galleryStreams = photos.Select(f => f.OpenReadStream()).ToList();

            try
            {
                (Stream Content, string? Extension)? mainPhoto = mainStream is null
                    ? null
                    : (mainStream, Path.GetExtension(request.ProfilePhoto!.FileName));

                var galleryInputs = photos
                    .Zip(galleryStreams, (file, stream) => (stream, (string?)Path.GetExtension(file.FileName)))
                    .ToList();

                var result = await _saveHandler.Handle(
                    new SavePartyProfileCommand(
                        partyId, request.UserName, request.AboutMe, request.Address, request.Geolocation,
                        mainPhoto, galleryInputs),
                    cancellationToken);

                return Ok(result);
            }
            finally
            {
                if (mainStream is not null)
                    await mainStream.DisposeAsync();
                foreach (var stream in galleryStreams)
                    await stream.DisposeAsync();
            }
        }

        [HttpPost("query")]
        [ProducesResponseType(typeof(QueryProfilesResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<QueryProfilesResult>> Query(
            [FromBody] QueryProfilesRequest request, CancellationToken cancellationToken)
        {
            Guid? id = Guid.TryParse(request.Id, out var parsedId) ? parsedId : null;

            var result = await _queryHandler.Handle(
                new QueryProfilesCommand(
                    id,
                    request.Scope, request.Fields,
                    request.Search?.Term, request.Search?.Fields,
                    request.OrderBy, request.Descending,
                    request.Skip, request.Take, request.Page, request.PageSize,
                    request.ProvinceRef, request.CountyRef),
                cancellationToken);

            return Ok(result);
        }
    }
}
