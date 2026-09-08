using SalesManagement.API.Common;
using SalesManagement.API.Common.Middlewares;
using SalesManagement.Application.Features.Auth.PartyLogin;
using SalesManagement.Application.Features.Items.GetCategories;
using SalesManagement.Application.Features.Items.GetItemById;
using SalesManagement.Application.Features.Items.GetItems;
using SalesManagement.Application.Features.Items.GetItemsByCategory;
using SalesManagement.Application.Features.Quotations.GetVendorQuotations;
using SalesManagement.Application.Features.Quotations.GetVendorQuotationById;
using SalesManagement.Application.Features.Quotations.GetCustomerQuotations;
using SalesManagement.Application.Features.Invoices.GetVendorInvoices;
using SalesManagement.Application.Features.Invoices.GetVendorInvoiceById;
using SalesManagement.Application.Features.Invoices.GetCustomerInvoices;
using Microsoft.Extensions.Caching.Hybrid;
using SalesManagement.Application.Common.Interfaces;
using SalesManagement.Application.Features.Customers.CreateCustomer;
using SalesManagement.Application.Features.Locations.GetLocations;
using SalesManagement.Infrastructure;
using SalesManagement.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "sm:";
});

builder.Services.AddHybridCache(options =>
{
    options.DefaultEntryOptions = new HybridCacheEntryOptions
    {
        Expiration = TimeSpan.FromMinutes(10),
        LocalCacheExpiration = TimeSpan.FromMinutes(2)
    };
});

builder.Services.AddScoped<PartyLoginHandler>();
builder.Services.AddScoped<GetItemsHandler>();
builder.Services.AddScoped<GetItemByIdHandler>();
builder.Services.AddScoped<GetCategoriesHandler>();
builder.Services.AddScoped<GetItemsByCategoryHandler>();
builder.Services.AddScoped<GetVendorQuotationsHandler>();
builder.Services.AddScoped<GetVendorQuotationByIdHandler>();
builder.Services.AddScoped<GetCustomerQuotationsHandler>();
builder.Services.AddScoped<GetVendorInvoicesHandler>();
builder.Services.AddScoped<GetVendorInvoiceByIdHandler>();
builder.Services.AddScoped<GetCustomerInvoicesHandler>();
builder.Services.AddScoped<CreateCustomerHandler>();
builder.Services.AddSingleton<IFileStorage, LocalFileStorage>();
builder.Services.AddScoped<GetLocationsHandler>();
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
