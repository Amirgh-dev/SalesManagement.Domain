using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SalesManagement.Domain.Features.Auth.PartyLogin;
using SalesManagement.Domain.Features.Items;
using SalesManagement.Persistence.ConnectionFactories;
using SalesManagement.Persistence.Features.Auth.PartyLogin;
using SalesManagement.Persistence.Features.Items.GetCategories;
using SalesManagement.Persistence.Features.Items.GetItems;
using SalesManagement.Domain.Features.Quotations;
using SalesManagement.Persistence.Features.Quotations.QueryQuotations;
using SalesManagement.Domain.Features.Customers;
using SalesManagement.Persistence.Features.Customers.CreateCustomer;
using SalesManagement.Domain.Features.Locations;
using SalesManagement.Persistence.Features.Locations.GetLocations;
using SalesManagement.Domain.Features.Invoices;
using SalesManagement.Persistence.Features.Invoices.QueryInvoices;
using SalesManagement.Domain.Features.PartyProfiles;
using SalesManagement.Persistence.Features.PartyProfiles.SavePartyProfile;
using SalesManagement.Persistence.Features.PartyProfiles.QueryProfiles;
using SalesManagement.Domain.Features.Posts;
using SalesManagement.Persistence.Features.Posts.CreatePost;
using SalesManagement.Persistence.Features.Posts.CreateComment;

namespace SalesManagement.Persistence
{
    public static class PersistenceServiceCollectionExtensions
    {
        public static IServiceCollection AddPersistence(
            this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<ISepidarConnectionFactory, SepidarConnectionFactory>();
            services.AddSingleton<ISepidyarhesabConnectionFactory, SepidyarhesabConnectionFactory>();

            services.AddScoped<IPartyRepository, PartyRepository>();
            services.AddScoped<IItemRepository, ItemRepository>();
            services.AddScoped<ICategoryRepository, CategoryRepository>();
            services.AddScoped<IQuotationQueryRepository, QuotationQueryRepository>();
            services.AddScoped<ICustomerRepository, CustomerRepository>();
            services.AddScoped<ILocationRepository, LocationRepository>();
            services.AddScoped<IInvoiceQueryRepository, InvoiceQueryRepository>();
            services.AddScoped<IPartyProfileRepository, PartyProfileRepository>();
            services.AddScoped<IProfileQueryRepository, ProfileQueryRepository>();
            services.AddScoped<IPostRepository, PostRepository>();
            services.AddScoped<ICommentRepository, CommentRepository>();

            return services;
        }
    }
}