using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TimeApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddControllers();
builder.Services.AddSwaggerGen();

// Add CORS configuration for frontend
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials();
    });
});

// Check if authentication is enabled (default to true for production)
var authEnabled = builder.Configuration.GetValue<bool>("Authentication:Enabled", true);

if (authEnabled)
{
    // Add Authentication & Authorization
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            // Support both local IdentityServer and Azure AD
            var authority = builder.Environment.IsDevelopment()
                ? builder.Configuration["IdentityServer:Authority"]
                : builder.Configuration["AzureAd:Authority"];

            options.Authority = authority;

            // Azure AD tokens can have audience as either:
            // 1. The client ID (GUID) - most common with delegated scopes
            // 2. The API identifier URI (api://guid) - less common
            // We need to accept both formats
            var audience = builder.Configuration["AzureAd:Audience"];
            var clientId = builder.Configuration["AzureAd:ClientId"];
            var tenantId = builder.Configuration["AzureAd:TenantId"];

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = !builder.Environment.IsDevelopment(), // Disable for local dev
                ValidateIssuer = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                // Accept both the API identifier URI and the client ID as valid audiences
                ValidAudiences = new[] { audience, clientId }.Where(a => !string.IsNullOrEmpty(a)),
                // Azure AD tokens can come from two different issuer endpoints:
                // 1. https://login.microsoftonline.com/{tenantId}/v2.0 (modern/v2 endpoint)
                // 2. https://sts.windows.net/{tenantId}/ (legacy/v1 endpoint)
                // Both are valid and we need to accept tokens from either
                ValidIssuers = new[]
                {
                    $"https://login.microsoftonline.com/{tenantId}/v2.0",
                    $"https://sts.windows.net/{tenantId}/"
                }.Where(i => !string.IsNullOrEmpty(tenantId))
            };

            // For local development with IdentityServer
            if (builder.Environment.IsDevelopment())
            {
                options.RequireHttpsMetadata = false;
            }

            // Enable detailed authentication logging in non-production environments
            if (!builder.Environment.IsProduction())
            {
                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        Console.WriteLine($"Authentication failed: {context.Exception.Message}");
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        Console.WriteLine($"Token validated for: {context.Principal?.Identity?.Name}");
                        return Task.CompletedTask;
                    },
                    OnChallenge = context =>
                    {
                        Console.WriteLine($"Authentication challenge: {context.Error}, {context.ErrorDescription}");
                        return Task.CompletedTask;
                    }
                };
            }
        });

    builder.Services.AddAuthorization();
}
else
{
    // When auth is disabled, add a permissive authorization policy
    builder.Services.AddAuthorization(options =>
    {
        options.DefaultPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
            .RequireAssertion(_ => true) // Always allow
            .Build();
    });
}

builder.Services.AddScoped<ITimePunchRepository, TimePunchRepository>();

//TODO add proper registration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<TimeClockDbContext>(options =>
    options.UseSqlServer(connectionString));
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");

app.UseHttpsRedirection();

// Always use authorization middleware (even when auth is disabled)
// When disabled, the permissive policy will allow all requests
if (authEnabled)
{
    app.UseAuthentication();
}
app.UseAuthorization();

// Map controller endpoints
app.MapControllers();

// Ensure database is created on startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<TimeClockDbContext>();
    dbContext.Database.EnsureCreated();
}

app.Run();

// Make Program class accessible for integration testing
public partial class Program { }
