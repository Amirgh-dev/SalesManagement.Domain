using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SalesManagement.Application.Common.Interfaces;
using SalesManagement.Infrastructure.Configuration;
using SalesManagement.Infrastructure.Security;
using SalesManagement.Infrastructure.Storage;

namespace SalesManagement.Infrastructure
{
    public static class InfrastructureServiceCollectionExtensions
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
            services.AddScoped<ITokenGenerator, JwtTokenGenerator>();

            services.AddSingleton<ISepidarConfigurationProvider, SepidarConfigurationProvider>();

            services.AddSingleton<IFileStorage, LocalFileStorage>();

            return services;
        }
    }
}
