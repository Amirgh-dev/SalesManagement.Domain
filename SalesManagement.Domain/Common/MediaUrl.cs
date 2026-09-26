namespace SalesManagement.Domain.Common
{
    public interface IMediaUrlBuilder
    {
        string? Build(string? path);
    }

    public static class MediaUrl
    {
        public static string? Combine(string? baseUrl, string? path)
        {
            if (string.IsNullOrWhiteSpace(path)) return null;

            var relative = "/" + path.Trim().TrimStart('/');
            return string.IsNullOrWhiteSpace(baseUrl) ? relative : baseUrl.Trim().TrimEnd('/') + relative;
        }
    }
}
