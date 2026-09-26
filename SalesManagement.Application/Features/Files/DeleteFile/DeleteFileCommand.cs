namespace SalesManagement.Application.Features.Files.DeleteFile
{
    public record DeleteFileCommand(Guid? FileId, int PartyId);
}
