using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace TimeClockUI;

/// <summary>
/// Authentication state provider that always returns an authenticated anonymous user.
/// Used for local development when authentication is disabled.
/// </summary>
public class AnonymousAuthenticationStateProvider : AuthenticationStateProvider
{
    private static readonly AuthenticationState _anonymousState = new(
        new ClaimsPrincipal(
            new ClaimsIdentity(
                new[]
                {
                    new Claim(ClaimTypes.Name, "dev-user"),
                    new Claim(ClaimTypes.NameIdentifier, "dev-user"),
                    new Claim("name", "Development User")
                },
                authenticationType: "Development"
            )
        )
    );

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        return Task.FromResult(_anonymousState);
    }
}
