using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SalesManagement.Application.Common.Interfaces;
using SalesManagement.Infrastructure.Configuration;
using SalesManagement.Infrastructure.Payments;
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

            services.Configure<MediaSettings>(configuration.GetSection(MediaSettings.SectionName));
            services.AddSingleton<SalesManagement.Domain.Common.IMediaUrlBuilder, MediaUrlBuilder>();

            services.Configure<ZarinPalSettings>(configuration.GetSection(ZarinPalSettings.SectionName));
            services.AddScoped<IPaymentCallbackUrlProvider, PaymentCallbackUrlProvider>();
            services.AddHttpClient<IPaymentGateway, ZarinPalGateway>();

            return services;
        }
    }
}
