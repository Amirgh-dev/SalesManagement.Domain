using SalesManagement.API.Common;
using SalesManagement.API.Common.Middlewares;
using SalesManagement.Application.Features.Auth.PartyLogin;
using SalesManagement.Application.Features.Items.QueryItems;
using SalesManagement.Application.Features.Quotations.QueryQuotations;
using SalesManagement.Application.Features.Invoices.QueryInvoices;
using SalesManagement.Application.Features.Customers.CreateCustomer;
using SalesManagement.Application.Features.Locations.GetLocations;
using SalesManagement.Application.Features.PartyProfiles.QueryProfiles;
using SalesManagement.Application.Features.PartyProfiles.SavePartyProfile;
using SalesManagement.Application.Features.Posts.CreateComment;
using SalesManagement.Application.Features.Posts.CreatePost;
using SalesManagement.Application.Features.Posts.LikePost;
using SalesManagement.Application.Features.Notifications.GetNotifications;
using SalesManagement.Infrastructure;
using SalesManagement.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<SalesManagement.Domain.Features.Auth.PartyLogin.ILoginAttemptTracker, SalesManagement.Domain.Features.Auth.PartyLogin.LoginAttemptTracker>();
builder.Services.AddScoped<SalesManagement.Application.Features.Auth.PartyLogin.ProfilePhotoUrlResolver>();
builder.Services.AddScoped<PartyLoginHandler>();
builder.Services.AddScoped<SalesManagement.Application.Features.Auth.RefreshToken.RefreshTokenHandler>();
builder.Services.AddScoped<SalesManagement.Application.Features.Auth.Logout.LogoutHandler>();
builder.Services.AddScoped<SalesManagement.Application.Features.Users.QueryUser.QueryUserHandler>();
builder.Services.AddScoped<QueryItemsHandler>();
builder.Services.AddScoped<QueryQuotationsHandler>();
builder.Services.AddScoped<QueryInvoicesHandler>();
builder.Services.AddScoped<CreateCustomerHandler>();
builder.Services.AddScoped<GetLocationsHandler>();
builder.Services.AddScoped<SalesManagement.Application.Features.SaleTypeDiscounts.GetSaleTypeDiscounts.GetSaleTypeDiscountsHandler>();
builder.Services.AddScoped<SavePartyProfileHandler>();
builder.Services.AddScoped<SalesManagement.Application.Features.PartyProfiles.SaveProfilePhoto.SaveProfilePhotoHandler>();
builder.Services.AddScoped<QueryProfilesHandler>();
builder.Services.AddScoped<CreatePostHandler>();
builder.Services.AddScoped<CreateCommentHandler>();
builder.Services.AddScoped<SalesManagement.Application.Features.Posts.DeletePost.DeletePostHandler>();
builder.Services.AddScoped<SalesManagement.Application.Features.Posts.DeleteComment.DeleteCommentHandler>();
builder.Services.AddScoped<LikePostHandler>();
builder.Services.AddScoped<SalesManagement.Application.Features.Professors.CreateProfessor.CreateProfessorHandler>();
builder.Services.AddScoped<SalesManagement.Application.Features.Courses.SaveCourse.SaveCourseHandler>();
builder.Services.AddScoped<GetNotificationsHandler>();
builder.Services.AddScoped<SalesManagement.Application.Features.Payments.RequestPayment.RequestPaymentHandler>();
builder.Services.AddScoped<SalesManagement.Application.Features.Payments.VerifyPayment.VerifyPaymentHandler>();
builder.Services.AddScoped<SalesManagement.Application.Features.Payments.PayInvoice.PayInvoiceHandler>();
builder.Services.AddScoped<SalesManagement.Application.Features.Files.DeleteFile.DeleteFileHandler>();
builder.Services.AddApiServices(builder.Configuration);

var app = builder.Build();

app.UseResponseCompression();

app.UseSwagger();
app.UseSwaggerUI();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseCors(ApiServiceCollectionExtensions.CorsPolicyName);
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseHttpsRedirection();

foreach (var folder in new[] { "Profile", "PostFile", "Course" })
{
    var physical = Path.Combine(app.Environment.ContentRootPath, "wwwroot", "media", "images", folder);
    Directory.CreateDirectory(physical);

    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(physical),
        RequestPath = "/media/images/" + folder,
        OnPrepareResponse = ctx =>
            ctx.Context.Response.Headers.CacheControl = "public,max-age=31536000,immutable",
    });
}

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapFallback("/api/{**catchall}", (HttpContext context) =>
    SalesManagement.API.Common.ProblemResponseWriter.WriteAsync(
        context, System.Net.HttpStatusCode.NotFound, "پیدا نشد", "مسیر مورد نظر پیدا نشد"));

await app.RunAsync();

[System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S1118:Utility classes should not have public constructors",
    Justification = "Required by WebApplicationFactory<Program> in integration tests.")]
public partial class Program;
