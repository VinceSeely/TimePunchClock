using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text.Json;

namespace TimeApi.Api;

[ApiController]
[Route("api/[controller]")]
public class DiagnosticsController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<DiagnosticsController> _logger;

    public DiagnosticsController(
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILogger<DiagnosticsController> logger)
    {
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Health check endpoint - no authentication required
    /// </summary>
    [HttpGet("health")]
    [AllowAnonymous]
    public IActionResult Health()
    {
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
        });
    }

    /// <summary>
    /// Test Azure AD metadata endpoint connectivity
    /// This helps diagnose IDX10500 errors related to signing key retrieval
    /// </summary>
    [HttpGet("azure-ad-connectivity")]
    [AllowAnonymous]
    public async Task<IActionResult> TestAzureAdConnectivity()
    {
        var tenantId = _configuration["AzureAd:TenantId"];

        if (string.IsNullOrEmpty(tenantId))
        {
            return BadRequest(new { error = "AzureAd:TenantId not configured" });
        }

        var metadataUrl = $"https://login.microsoftonline.com/{tenantId}/v2.0/.well-known/openid-configuration";

        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);

            _logger.LogInformation("Testing connectivity to Azure AD metadata endpoint: {MetadataUrl}", metadataUrl);

            var startTime = DateTime.UtcNow;
            var response = await httpClient.GetAsync(metadataUrl);
            var duration = DateTime.UtcNow - startTime;

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var metadata = JsonSerializer.Deserialize<JsonElement>(content);

                var hasJwksUri = metadata.TryGetProperty("jwks_uri", out var jwksUri);

                // Also test the jwks_uri endpoint where the signing keys are
                string? jwksContent = null;
                TimeSpan? jwksDuration = null;

                if (hasJwksUri && jwksUri.ValueKind == JsonValueKind.String)
                {
                    var jwksUriValue = jwksUri.GetString();
                    var jwksStartTime = DateTime.UtcNow;
                    var jwksResponse = await httpClient.GetAsync(jwksUriValue);
                    jwksDuration = DateTime.UtcNow - jwksStartTime;

                    if (jwksResponse.IsSuccessStatusCode)
                    {
                        jwksContent = await jwksResponse.Content.ReadAsStringAsync();
                    }
                }

                return Ok(new
                {
                    success = true,
                    metadataUrl,
                    responseTime = $"{duration.TotalMilliseconds}ms",
                    hasJwksUri,
                    jwksUri = hasJwksUri ? jwksUri.GetString() : null,
                    jwksResponseTime = jwksDuration != null ? $"{jwksDuration.Value.TotalMilliseconds}ms" : null,
                    jwksRetrieved = jwksContent != null,
                    jwksKeyCount = jwksContent != null ?
                        JsonSerializer.Deserialize<JsonElement>(jwksContent).GetProperty("keys").GetArrayLength() : 0,
                    message = "Successfully connected to Azure AD metadata endpoint",
                    timestamp = DateTime.UtcNow
                });
            }
            else
            {
                return StatusCode((int)response.StatusCode, new
                {
                    success = false,
                    metadataUrl,
                    statusCode = (int)response.StatusCode,
                    responseTime = $"{duration.TotalMilliseconds}ms",
                    error = $"HTTP {response.StatusCode}: {response.ReasonPhrase}",
                    timestamp = DateTime.UtcNow
                });
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request exception when connecting to Azure AD");
            return StatusCode(503, new
            {
                success = false,
                metadataUrl,
                error = "HTTP request failed",
                message = ex.Message,
                innerMessage = ex.InnerException?.Message,
                timestamp = DateTime.UtcNow
            });
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout when connecting to Azure AD");
            return StatusCode(504, new
            {
                success = false,
                metadataUrl,
                error = "Request timeout",
                message = "Connection to Azure AD timed out after 30 seconds",
                innerMessage = ex.Message,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected exception when testing Azure AD connectivity");
            return StatusCode(500, new
            {
                success = false,
                metadataUrl,
                error = ex.GetType().Name,
                message = ex.Message,
                timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Display current authentication configuration (sanitized)
    /// </summary>
    [HttpGet("auth-config")]
    [AllowAnonymous]
    public IActionResult GetAuthConfig()
    {
        var authEnabled = _configuration.GetValue<bool>("Authentication:Enabled", true);
        var tenantId = _configuration["AzureAd:TenantId"];
        var hasClientId = !string.IsNullOrEmpty(_configuration["AzureAd:ClientId"]);
        var hasAudience = !string.IsNullOrEmpty(_configuration["AzureAd:Audience"]);
        var authority = _configuration["AzureAd:Authority"];

        return Ok(new
        {
            authenticationEnabled = authEnabled,
            environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
            azureAd = new
            {
                configured = !string.IsNullOrEmpty(tenantId),
                tenantId = !string.IsNullOrEmpty(tenantId) ? $"{tenantId[..8]}..." : null,
                authority,
                hasClientId,
                hasAudience
            },
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Test endpoint that requires authentication
    /// Use this to verify your JWT token is working
    /// </summary>
    [HttpGet("test-auth")]
    [Authorize]
    public IActionResult TestAuth()
    {
        var claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();

        return Ok(new
        {
            authenticated = true,
            userId = User.Identity?.Name,
            claims,
            timestamp = DateTime.UtcNow
        });
    }
}
