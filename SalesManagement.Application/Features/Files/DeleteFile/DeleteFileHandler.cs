using Microsoft.Extensions.Logging;
using SalesManagement.Application.Common.Interfaces;
using SalesManagement.Domain.Common.Exceptions;
using SalesManagement.Domain.Features.Files;

namespace SalesManagement.Application.Features.Files.DeleteFile
{
    public class DeleteFileHandler(
        IEnumerable<IFileSource> sources,
        IFileStorage fileStorage,
        ILogger<DeleteFileHandler> logger)
    {
        public async Task Handle(DeleteFileCommand cmd, CancellationToken cancellationToken = default)
        {
            if (cmd.FileId is null || cmd.FileId == Guid.Empty)
                throw new DomainException("شناسه‌ی فایل نامعتبر است");

            foreach (var source in sources)
            {
                var file = await source.DeleteOwnedAsync(cmd.FileId.Value, cmd.PartyId, cancellationToken);
                if (file is null) continue;

                await RemoveFromDiskAsync(source.Name, cmd.FileId.Value, file);

                logger.LogInformation(
                    "فایل {FileId} از {Source} توسط کاربر {PartyId} حذف شد", cmd.FileId, source.Name, cmd.PartyId);
                return;
            }

            throw new NotFoundException("فایل پیدا نشد");
        }

        private async Task RemoveFromDiskAsync(string source, Guid fileId, StoredFile file)
        {
            if (!IsSafeSegment(file.Folder) || !IsSafeSegment(file.FileName))
            {
                logger.LogWarning(
                    "مسیر فایل {FileId} از {Source} نامعتبر بود و فایل فیزیکی حذف نشد", fileId, source);
                return;
            }

            try
            {
                await fileStorage.DeleteAsync(file.Folder, file.FileName, CancellationToken.None);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                logger.LogWarning(ex, "حذف فایل فیزیکی {FileId} از {Source} ناموفق بود", fileId, source);
            }
        }

        private static bool IsSafeSegment(string value) =>
            value.Length > 0 && value is not ("." or "..") && value == Path.GetFileName(value);
    }
}
