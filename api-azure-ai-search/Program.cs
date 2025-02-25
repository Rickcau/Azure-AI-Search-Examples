using Azure.Core;
using Azure.Identity;
using api_azure_ai_search.Helper;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

// Add services to the container.


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
// This allows me to run the API locally and use different accounts for testing
// If this is deployed to azure it runs using the managed identity 
TokenCredential azureCredential;
if (builder.Environment.IsDevelopment())
{
    var accountToUse = builder.Configuration["Azure:AccountToUse"];
   // var accountToUse = ConfigHelper.GetEnvironmentVariable("Azure:AccountToUser");
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

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen(c =>
//{
//    c.EnableAnnotations();
//    c.MapType<IFormFile>(() => new OpenApiSchema
//        {
//          Type = "string",
//          Format = "binary"
//        });
//});

builder.Services.AddSwaggerGen(c =>
{
    c.EnableAnnotations();
    c.MapType(typeof(IFormFile), () => new Microsoft.OpenApi.Models.OpenApiSchema
    {
        Type = "string",
        Format = "binary"
    });
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowMyApp");

app.UseAuthorization();

app.MapControllers();

app.Run();
