namespace SalesManagement.Application.Common.Interfaces
{
    public interface IFileStorage
    {
        Task<string> SaveAsync(Stream content, string folder, string extension, CancellationToken ct = default);

        Task DeleteAsync(string folder, string fileName, CancellationToken ct = default);
    }
}