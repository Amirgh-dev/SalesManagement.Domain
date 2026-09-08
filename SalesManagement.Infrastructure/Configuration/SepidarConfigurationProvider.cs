using System.Text.Json;
using System.Text.Json.Serialization;
using SalesManagement.Application.Common.Configuration;
using SalesManagement.Application.Common.Interfaces;

namespace SalesManagement.Infrastructure.Configuration
{
    public class SepidarConfigurationProvider : ISepidarConfigurationProvider
    {
        private const string RelativePath = "wwwroot/configuration/SepidarConfiguration.json";

        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
            ReadCommentHandling = JsonCommentHandling.Skip
        };

        private readonly Lazy<SepidarConfiguration> _configuration;

        public SepidarConfigurationProvider()
        {
            _configuration = new Lazy<SepidarConfiguration>(Load);
        }

        public SepidarConfiguration Get() => _configuration.Value;

        private static SepidarConfiguration Load()
        {
            var path = Path.Combine(AppContext.BaseDirectory,
                RelativePath.Replace('/', Path.DirectorySeparatorChar));

            if (!File.Exists(path))
                throw new FileNotFoundException(
                    $"فایل پیکربندی سپیدار پیدا نشد: {path}", path);

            var json = File.ReadAllText(path);

            return JsonSerializer.Deserialize<SepidarConfiguration>(json, SerializerOptions)
                   ?? throw new InvalidOperationException(
                       $"محتوای فایل پیکربندی سپیدار نامعتبر است: {path}");
        }
    }
}
