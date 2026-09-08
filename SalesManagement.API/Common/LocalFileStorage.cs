using SalesManagement.Application.Common.Interfaces;

namespace SalesManagement.API.Common
{
    public class LocalFileStorage(IWebHostEnvironment env) : IFileStorage
    {
        private string Root => env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");

        public async Task<string> SaveAsync(
            Stream content, string folder, string extension, CancellationToken ct = default)
        {
            var ext = extension.StartsWith('.') ? extension.ToLowerInvariant() : "." + extension.ToLowerInvariant();
            var fileName = $"{Guid.NewGuid():N}{ext}";              

            var dir = Path.Combine(Root, "media", "images", folder);
            Directory.CreateDirectory(dir);

            var fullPath = Path.Combine(dir, fileName);
            await using var fs = new FileStream(fullPath, FileMode.Create);
            await content.CopyToAsync(fs, ct);

            return fileName;
        }

        public Task DeleteAsync(string folder, string fileName, CancellationToken ct = default)
        {
            var fullPath = Path.Combine(Root, "media", "images", folder, fileName);
            if (File.Exists(fullPath)) File.Delete(fullPath);
            return Task.CompletedTask;
        }
    }
}