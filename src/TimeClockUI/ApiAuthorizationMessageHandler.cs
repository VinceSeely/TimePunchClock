using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

namespace TimeClockUI;

/// <summary>
/// Custom authorization message handler for API requests to external backend.
/// Configures the handler to attach access tokens to requests going to the backend API.
/// </summary>
public class ApiAuthorizationMessageHandler : AuthorizationMessageHandler
{
    public ApiAuthorizationMessageHandler(IAccessTokenProvider provider,
        NavigationManager navigationManager,
        IConfiguration configuration)
        : base(provider, navigationManager)
    {
        var apiBaseUrl = configuration[TimeClock.Client.Constants.TimeClientBaseUrl];

        if (string.IsNullOrEmpty(apiBaseUrl))
        {
            throw new InvalidOperationException(
                $"API base URL not configured. Please set '{TimeClock.Client.Constants.TimeClientBaseUrl}' in appsettings.json");
        }

        // Get the API scopes from configuration
        var scopes = new List<string>();
        var scopeValue = configuration["Api:Scopes:0"];

        if (!string.IsNullOrEmpty(scopeValue))
        {
            scopes.Add(scopeValue);
        }
        else
        {
            Console.WriteLine("Warning: No API scopes configured. Tokens may not have the required permissions.");
        }

        // Configure this handler to authorize requests to the backend API
        ConfigureHandler(
            authorizedUrls: new[] { apiBaseUrl },
            scopes: scopes);

        Console.WriteLine($"ApiAuthorizationMessageHandler configured for URL: {apiBaseUrl} with scopes: {string.Join(", ", scopes)}");
    }
}
