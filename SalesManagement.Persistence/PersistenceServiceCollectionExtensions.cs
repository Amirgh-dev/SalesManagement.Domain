using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SalesManagement.Domain.Features.Auth.PartyLogin;
using SalesManagement.Domain.Features.Items;
using SalesManagement.Persistence.ConnectionFactories;
using SalesManagement.Persistence.Features.Auth.PartyLogin;
using SalesManagement.Persistence.Features.Items.QueryItems;
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
using SalesManagement.Persistence.Features.Posts.LikePost;
using SalesManagement.Domain.Features.Notifications;
using SalesManagement.Persistence.Features.Notifications.GetNotifications;

namespace SalesManagement.Persistence
{
    public static class PersistenceServiceCollectionExtensions
    {
        public static IServiceCollection AddPersistence(
            this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<ISepidarConnectionFactory, SepidarConnectionFactory>();
            services.AddSingleton<ISepidyarhesabConnectionFactory, SepidyarhesabConnectionFactory>();
            services.AddSingleton<IZanisServicesConnectionFactory, ZanisServicesConnectionFactory>();
            services.AddSingleton<SalesManagement.Domain.Features.Auth.PartyLogin.IRevokedTokenStore, SalesManagement.Persistence.Features.Auth.Logout.RevokedTokenStore>();

            services.AddScoped<IPartyRepository, PartyRepository>();
            services.AddScoped<IItemQueryRepository, ItemQueryRepository>();
            services.AddScoped<IQuotationQueryRepository, QuotationQueryRepository>();
            services.AddScoped<ICustomerRepository, CustomerRepository>();
            services.AddScoped<ILocationRepository, LocationRepository>();
            services.AddScoped<IInvoiceQueryRepository, InvoiceQueryRepository>();
            services.AddScoped<IPartyProfileRepository, PartyProfileRepository>();
            services.AddScoped<SalesManagement.Domain.Features.PartyProfiles.IProfilePhotoRepository, SalesManagement.Persistence.Features.PartyProfiles.SaveProfilePhoto.ProfilePhotoRepository>();
            services.AddScoped<IProfileQueryRepository, ProfileQueryRepository>();
            services.AddScoped<IPostRepository, PostRepository>();
            services.AddScoped<IPostLikeRepository, PostLikeRepository>();
            services.AddScoped<SalesManagement.Domain.Features.Professors.IProfessorRepository, SalesManagement.Persistence.Features.Professors.CreateProfessor.ProfessorRepository>();
            services.AddScoped<SalesManagement.Domain.Features.Courses.ICourseRepository, SalesManagement.Persistence.Features.Courses.SaveCourse.CourseRepository>();
            services.AddScoped<IPostNotificationRepository, PostNotificationRepository>();
            services.AddScoped<ICommentRepository, CommentRepository>();
            services.AddScoped<INotificationQueryRepository, NotificationQueryRepository>();

            return services;
        }
    }
}