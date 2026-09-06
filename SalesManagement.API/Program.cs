using SalesManagement.API.Common;
using SalesManagement.API.Common.Middlewares;
using SalesManagement.Application.Features.Auth.PartyLogin;
using SalesManagement.Application.Features.Items.GetCategories;
using SalesManagement.Application.Features.Items.GetItemById;
using SalesManagement.Application.Features.Items.GetItems;
using SalesManagement.Application.Features.Items.GetItemsByCategory;
using SalesManagement.Infrastructure;
using SalesManagement.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<PartyLoginHandler>();
builder.Services.AddScoped<GetItemsHandler>();
builder.Services.AddScoped<GetItemByIdHandler>();
builder.Services.AddScoped<GetCategoriesHandler>();
builder.Services.AddScoped<GetItemsByCategoryHandler>();
builder.Services.AddApiServices(builder.Configuration);

var app = builder.Build();

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
