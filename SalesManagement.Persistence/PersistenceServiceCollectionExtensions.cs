using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SalesManagement.Application.Common.Interfaces;
using SalesManagement.Domain.Features.Auth.PartyLogin;
using SalesManagement.Persistence.ConnectionFactories;
using SalesManagement.Persistence.Features.Auth.PartyLogin;

namespace SalesManagement.Persistence
{
    public static class PersistenceServiceCollectionExtensions
    {
        public static IServiceCollection AddPersistence(
            this IServiceCollection services, IConfiguration configuration)
        {
            // Register connection factories
            services.AddSingleton<ISepidarConnectionFactory, SepidarConnectionFactory>();
            services.AddSingleton<ISepidyarhesabConnectionFactory, SepidyarhesabConnectionFactory>();

            // Register repositories
            services.AddScoped<IPartyRepository, PartyRepository>();

            return services;
        }
    }
}