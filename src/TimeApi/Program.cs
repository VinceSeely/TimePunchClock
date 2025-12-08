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

// Add HttpClient for diagnostics and external API calls
builder.Services.AddHttpClient();

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
            // Prefer IdentityServer for local development only when explicitly configured
            // Otherwise use Azure AD authority (even in Development environment)
            var identityServerAuthority = builder.Configuration["IdentityServer:Authority"];
            var azureAdAuthority = builder.Configuration["AzureAd:Authority"];
            var tenantId = builder.Configuration["AzureAd:TenantId"];

            var isUsingIdentityServer = !string.IsNullOrEmpty(identityServerAuthority) &&
                                       builder.Environment.IsDevelopment() &&
                                       builder.Configuration.GetValue<bool>("Authentication:UseIdentityServer", false);

            var authority = isUsingIdentityServer ? identityServerAuthority : azureAdAuthority;

            if (string.IsNullOrEmpty(authority))
            {
                throw new InvalidOperationException(
                    "Authentication authority not configured. Set either 'IdentityServer:Authority' for local dev or 'AzureAd:Authority' for Azure AD.");
            }

            options.Authority = authority;

            // CRITICAL FIX: Explicitly set MetadataAddress for Azure AD
            // This ensures the middleware can retrieve signing keys even in containerized environments
            // The auto-discovery can fail in Azure Container Apps due to DNS or connectivity issues
            if (!isUsingIdentityServer && !string.IsNullOrEmpty(tenantId))
            {
                options.MetadataAddress = $"https://login.microsoftonline.com/{tenantId}/v2.0/.well-known/openid-configuration";

                // Increase timeout for metadata retrieval in containerized environments
                options.BackchannelTimeout = TimeSpan.FromSeconds(30);

                // Force HTTPS for Azure AD metadata (security best practice)
                options.RequireHttpsMetadata = true;
            }
            else if (isUsingIdentityServer)
            {
                // For local IdentityServer development only
                options.RequireHttpsMetadata = false;
            }

            // Azure AD tokens can have audience as either:
            // 1. The client ID (GUID) - most common with delegated scopes
            // 2. The API identifier URI (api://guid) - less common
            // We need to accept both formats
            var audience = builder.Configuration["AzureAd:Audience"];
            var clientId = builder.Configuration["AzureAd:ClientId"];

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

            // Enable detailed authentication logging in non-production environments
            if (!builder.Environment.IsProduction())
            {
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        if (context.Request.Headers.TryGetValue("Authorization", out var authHeader))
                        {
                            Console.WriteLine($"[AUTH] Received authorization header (length: {authHeader.ToString().Length})");
                        }
                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context =>
                    {
                        Console.WriteLine($"[AUTH ERROR] Authentication failed: {context.Exception.GetType().Name}");
                        Console.WriteLine($"[AUTH ERROR] Message: {context.Exception.Message}");
                        if (context.Exception.InnerException != null)
                        {
                            Console.WriteLine($"[AUTH ERROR] Inner: {context.Exception.InnerException.Message}");
                        }
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        var name = context.Principal?.Identity?.Name ?? "Unknown";
                        var claims = context.Principal?.Claims.Select(c => $"{c.Type}={c.Value}").ToList() ?? new List<string>();
                        Console.WriteLine($"[AUTH SUCCESS] Token validated for: {name}");
                        Console.WriteLine($"[AUTH SUCCESS] Claims count: {claims.Count}");
                        return Task.CompletedTask;
                    },
                    OnChallenge = context =>
                    {
                        Console.WriteLine($"[AUTH CHALLENGE] Error: {context.Error}");
                        Console.WriteLine($"[AUTH CHALLENGE] Description: {context.ErrorDescription}");
                        Console.WriteLine($"[AUTH CHALLENGE] URI: {context.ErrorUri}");
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
