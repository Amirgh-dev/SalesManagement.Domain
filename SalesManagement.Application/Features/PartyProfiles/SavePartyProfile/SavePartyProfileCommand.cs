namespace SalesManagement.Application.Features.PartyProfiles.SavePartyProfile
{
    public record SavePartyProfileCommand(
        int PartyId,
        Guid? Id,
        string? UserName,
        string? AboutMe,
        string? Address,
        string? Geolocation,
        (Stream Content, string? Extension)? ProfilePhoto,
        IReadOnlyList<(Stream Content, string? Extension)> Photos);
}
