using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Authentication.WebAssembly.Msal;
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

// Configure Azure AD MSAL authentication
builder.Services.AddMsalAuthentication(options =>
{
    builder.Configuration.Bind("AzureAd", options.ProviderOptions.Authentication);
    options.ProviderOptions.DefaultAccessTokenScopes.Add(builder.Configuration["Api:Scopes:0"] ?? "");
});

builder.Services.RegsiterTimeClient(builder.Configuration);
builder.Services.AddMudServices();

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

await builder.Build().RunAsync();
