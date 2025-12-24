using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SSCA.website.API.Data;
using SSCA.website.API.Services;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// Add EF Core with PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PostgreSQL")));

// Add MeetingService
builder.Services.AddScoped<IMeetingService, MeetingService>();

// Add Azure Blob Storage
builder.Services.AddSingleton(sp =>
{
    var connectionString = builder.Configuration.GetConnectionString("AzureStorage") 
        ?? "UseDevelopmentStorage=true";
    return new BlobServiceClient(connectionString);
});

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Build().Run();
