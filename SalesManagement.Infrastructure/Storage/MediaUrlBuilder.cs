using Microsoft.Extensions.Options;
using SalesManagement.Domain.Common;

namespace SalesManagement.Infrastructure.Storage
{
    public class MediaSettings
    {
        public const string SectionName = "Media";
        public string BaseUrl { get; set; } = string.Empty;
    }

    public class MediaUrlBuilder(IOptions<MediaSettings> settings) : IMediaUrlBuilder
    {
        public string? Build(string? path) => MediaUrl.Combine(settings.Value.BaseUrl, path);
    }
}
