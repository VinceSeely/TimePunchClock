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
            options.Audience = builder.Configuration["AzureAd:Audience"];

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = !builder.Environment.IsDevelopment(), // Disable for local dev
                ValidateIssuer = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true
            };

            // For local development with IdentityServer
            if (builder.Environment.IsDevelopment())
            {
                options.RequireHttpsMetadata = false;
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
