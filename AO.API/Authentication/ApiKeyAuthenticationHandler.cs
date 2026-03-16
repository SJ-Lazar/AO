using AO.API.Helpers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace AO.API.Authentication;

public sealed class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "ApiKey";

    private readonly IConfiguration _configuration;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IConfiguration configuration)
        : base(options, logger, encoder)
    {
        _configuration = configuration;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var configuredApiKey = ApiKeyHelper.GetConfiguredApiKey(_configuration);

        if (!configuredApiKey.IsValid)
        {
            Logger.LogError("API key authentication failed because the API key is not configured.");
            return Task.FromResult(AuthenticateResult.Fail(configuredApiKey.Message));
        }

        if (!ApiKeyHelper.TryGetProvidedApiKey(Request, out var providedApiKey))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        if (!ApiKeyHelper.ApiKeysMatch(configuredApiKey.Value!, providedApiKey))
        {
            return Task.FromResult(AuthenticateResult.Fail("API key is invalid."));
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, SchemeName),
            new Claim(ClaimTypes.Name, "ApiKeyClient")
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
