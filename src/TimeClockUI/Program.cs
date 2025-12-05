using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using MudBlazor.Services;
using TimeClock.Client;
using TimeClockUI;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Add configuration from appsettings.json with optional set to true
builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

// Add fallback values for critical settings if not found in configuration
var apiBaseUrl = builder.Configuration[Constants.TimeClientBaseUrl];
if (string.IsNullOrEmpty(apiBaseUrl))
{
    Console.WriteLine("No API base URL found in configuration, using default: http://localhost:5000");
    builder.Configuration[Constants.TimeClientBaseUrl] = "http://localhost:5000";
}

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Check if authentication is enabled (default to true for production)
var authEnabled = builder.Configuration.GetValue<bool>("Authentication:Enabled", true);

if (authEnabled)
{
    // Configure authentication based on AuthProvider setting
    var authProvider = builder.Configuration["AuthProvider"] ?? "Oidc";

    if (authProvider == "AzureAd")
    {
        builder.Services.AddMsalAuthentication(options =>
        {
            builder.Configuration.Bind("AzureAd", options.ProviderOptions.Authentication);

            // Add the API scope
            var apiScope = builder.Configuration["Api:Scopes:0"];
            if (!string.IsNullOrEmpty(apiScope))
            {
                options.ProviderOptions.DefaultAccessTokenScopes.Add(apiScope);
            }
        });
    }
    else
    {
        builder.Services.AddOidcAuthentication(options =>
        {
            builder.Configuration.Bind("Oidc", options.ProviderOptions);

            // Add the API scope
            var apiScope = builder.Configuration["Api:Scopes:0"];
            if (!string.IsNullOrEmpty(apiScope))
            {
                options.ProviderOptions.DefaultScopes.Add(apiScope);
            }
        });
    }
}

builder.Services.RegsiterTimeClient(builder.Configuration);
builder.Services.AddMudServices();

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

await builder.Build().RunAsync();
