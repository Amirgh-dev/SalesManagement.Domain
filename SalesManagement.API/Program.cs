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
using SalesManagement.Application.Features.Notifications.GetNotifications;
using SalesManagement.Infrastructure;
using SalesManagement.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddScoped<PartyLoginHandler>();
builder.Services.AddScoped<QueryItemsHandler>();
builder.Services.AddScoped<QueryQuotationsHandler>();
builder.Services.AddScoped<QueryInvoicesHandler>();
builder.Services.AddScoped<CreateCustomerHandler>();
builder.Services.AddScoped<GetLocationsHandler>();
builder.Services.AddScoped<SavePartyProfileHandler>();
builder.Services.AddScoped<QueryProfilesHandler>();
builder.Services.AddScoped<CreatePostHandler>();
builder.Services.AddScoped<CreateCommentHandler>();
builder.Services.AddScoped<GetNotificationsHandler>();
builder.Services.AddApiServices(builder.Configuration);

var app = builder.Build();

app.UseResponseCompression();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

await app.RunAsync();
