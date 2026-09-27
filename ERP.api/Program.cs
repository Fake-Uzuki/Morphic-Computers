using Microsoft.EntityFrameworkCore;
using ERP.infrastructure.data;
using ERP.infrastructure.services;
using ERP.domain.entities;
using ERP.api.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Register MasterErpDbContext
builder.Services.AddDbContext<MasterErpDbContext>(options =>
  options.UseSqlServer(
    builder.Configuration.GetConnectionString("MasterErp"),
    sqlOptions => sqlOptions.EnableRetryOnFailure(
        maxRetryCount: 3,
        maxRetryDelay: TimeSpan.FromSeconds(5),
        errorNumbersToAdd: null)));

// Register TenantErpDbContext
builder.Services.AddDbContext<TenantErpDbContext>(options =>
  options.UseSqlServer(
    builder.Configuration.GetConnectionString("TenantErp"),
    sqlOptions => sqlOptions.EnableRetryOnFailure(
        maxRetryCount: 3,
        maxRetryDelay: TimeSpan.FromSeconds(5),
        errorNumbersToAdd: null)));

// Register Multi-Tenant Services
builder.Services.AddScoped<ITenantDatabaseResolver, TenantDatabaseResolver>();
builder.Services.AddScoped<ITenantDbContextFactory, TenantDbContextFactory>();

// Register Controllers
builder.Services.AddControllers();

// OpenAPI Documentation
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Security Gate: Verify X-API-KEY on all incoming requests
app.UseMiddleware<ApiKeyAuthMiddleware>();

app.UseAuthorization();

// Map all modular controllers (AuthController, ProductsController, OrdersController, CompaniesController, etc.)
app.MapControllers();

app.Run();
