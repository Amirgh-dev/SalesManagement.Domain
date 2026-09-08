using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SalesManagement.Application.Common.Interfaces;
using SalesManagement.Domain.Features.Auth.PartyLogin;
using SalesManagement.Domain.Features.Items;
using SalesManagement.Persistence.ConnectionFactories;
using SalesManagement.Persistence.Features.Auth.PartyLogin;
using SalesManagement.Persistence.Features.Items.GetCategories;
using SalesManagement.Persistence.Features.Items.GetItems;
using SalesManagement.Domain.Features.Quotations;
using SalesManagement.Persistence.Features.Quotations.GetVendorQuotations;
using SalesManagement.Domain.Features.Customers;
using SalesManagement.Persistence.Features.Customers.CreateCustomer;
using SalesManagement.Domain.Features.Locations;
using SalesManagement.Persistence.Features.Locations.GetLocations;
using SalesManagement.Domain.Features.Invoices;
using SalesManagement.Persistence.Features.Invoices.GetVendorInvoices;

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
            services.AddScoped<IItemRepository, ItemRepository>();
            services.AddScoped<ICategoryRepository, CategoryRepository>();
            services.AddScoped<IQuotationRepository, QuotationRepository>();
            services.AddScoped<ICustomerRepository, CustomerRepository>();
            services.AddScoped<ILocationRepository, LocationRepository>();
            services.AddScoped<IInvoiceRepository, InvoiceRepository>();


            return services;
        }
    }
}