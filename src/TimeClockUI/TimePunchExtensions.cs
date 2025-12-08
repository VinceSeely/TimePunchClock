using TimeClock.Client;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

namespace TimeClockUI;

public static class TimepunchServiceExtensions
{
    public static IServiceCollection RegsiterTimeClient(this IServiceCollection services, IConfiguration config)
    {
        var baseUrl = config.GetValue<String>(Constants.TimeClientBaseUrl) ?? string.Empty;
        Console.WriteLine("base url: " + baseUrl);

        var authEnabled = config.GetValue<bool>("Authentication:Enabled", true);

        if (authEnabled)
        {
            // Register the custom authorization message handler for API requests
            services.AddScoped<ApiAuthorizationMessageHandler>();
        }

        var httpClient = services.AddHttpClient(Constants.TimeClientString, client =>
        {
            client.BaseAddress = new Uri(baseUrl);
        });

        if (authEnabled)
        {
            // Use our custom handler that authorizes requests to the external backend API
            httpClient.AddHttpMessageHandler<ApiAuthorizationMessageHandler>();
        }

        services.AddScoped<TimePunchClient>();
        return services;
    }
}