using Azure.Core;
using Azure.Identity;
using Azure_AI_Search_API.Interfaces;
using Azure_AI_Search_API.Services;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// allow for loading of environment variables from these two files
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

// Adding Cors settings so a client can make use of this API when testing locally
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowMyApp",
        policy =>
        {
            policy.WithOrigins("http://localhost:3000", "https://localhost:3443", "https://localhost:3001")
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

// Configure Azure credential based on environment
// This allows the API to run locally and use different accounts for testing
// If this is deployed to azure it runs using the managed identity 
TokenCredential azureCredential;
if (builder.Environment.IsDevelopment())
{
    var accountToUse = builder.Configuration["Azure:AccountToUse"];
    if (string.IsNullOrEmpty(accountToUse))
        throw new ArgumentException("Azure:AccountToUse is not configured for development");

    azureCredential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
    {
        TenantId = builder.Configuration["Azure:TenantId"],
        SharedTokenCacheUsername = accountToUse,
        // Exclude other credential types to ensure we only use CLI credentials
        ExcludeEnvironmentCredential = true,
        ExcludeManagedIdentityCredential = true,
        ExcludeVisualStudioCredential = true,
        ExcludeVisualStudioCodeCredential = true,
        ExcludeInteractiveBrowserCredential = true
    });

    // azureCredential = new InteractiveBrowserCredential();
}
else
{
    // Use Managed Identity in production
    azureCredential = new ManagedIdentityCredential(builder.Configuration["AZURE_MANAGED_IDENTITY"]);
}
// Register the TokenCredential as a singleton so it can be used with dependency injection
builder.Services.AddSingleton<TokenCredential>(azureCredential);


// Add services to the container.

builder.Services.AddScoped<IIndexService, IndexService>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
// builder.Services.AddSwaggerGen();
builder.Services.AddSwaggerGen(c =>
{
    c.EnableAnnotations();
    // Other swagger configuration
});



var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Azure-AI-Search-API v1");

        // This CSS will:
        // 1. Hide the redundant controller name
        // 2. Make the tag description text larger and bolder like the main header
        c.HeadContent = @"
        <style>
            /* Hide the controller name (first span) */
            .opblock-tag-section h3.opblock-tag span:first-child {
                display: none;
            }
            
            /* Make the tag description (second span) look like the main header */
            .opblock-tag-section h3.opblock-tag small {
                font-size: inherit;
                font-weight: bold;
                color: inherit;
                margin: 0;
                padding: 0;
            }
        </style>
    ";
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
