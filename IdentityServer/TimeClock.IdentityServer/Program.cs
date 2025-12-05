using Duende.IdentityServer.Models;
using Duende.IdentityServer.Test;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddIdentityServer(options =>
{
    options.EmitStaticAudienceClaim = true;
})
.AddInMemoryClients(new[]
{
    new Client
    {
        ClientId = "timeclock-blazor-local",
        ClientName = "TimeClock Blazor App (Local)",
        AllowedGrantTypes = GrantTypes.Code,
        RequireClientSecret = false,
        RequirePkce = true,
        RedirectUris = { "http://localhost:5001/authentication/login-callback" },
        PostLogoutRedirectUris = { "http://localhost:5001" },
        AllowedScopes = { "openid", "profile", "api://timeclock-api-local/access_as_user" },
        AllowedCorsOrigins = { "http://localhost:5001" }
    }
})
.AddInMemoryApiScopes(new[]
{
    new ApiScope("api://timeclock-api-local/access_as_user", "TimeClock API Access")
})
.AddInMemoryIdentityResources(new IdentityResource[]
{
    new IdentityResources.OpenId(),
    new IdentityResources.Profile()
})
.AddTestUsers(new List<TestUser>
{
    new TestUser
    {
        SubjectId = "1",
        Username = "testuser",
        Password = "password",
        Claims =
        {
            new Claim("name", "Test User"),
            new Claim("email", "testuser@localhost.com"),
            new Claim("preferred_username", "testuser"),
            new Claim("oid", Guid.NewGuid().ToString())
        }
    }
});

var app = builder.Build();

app.UseIdentityServer();

app.Run();
