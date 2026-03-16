using Microsoft.AspNetCore.Http;
using System.Security.Cryptography;
using System.Text;

namespace AO.API.Helpers;

public enum ApiKeyValidationResult
{
    Valid,
    MissingConfiguration,
    MissingRequestApiKey,
    InvalidRequestApiKey
}

public readonly record struct ConfiguredApiKeyResult(
    string? Value,
    ApiKeyValidationResult ValidationResult,
    string Message)
{
    public bool IsValid => ValidationResult == ApiKeyValidationResult.Valid;
}

public readonly record struct ApiKeyValidationState(ApiKeyValidationResult ValidationResult,string Message)
{
    public bool IsValid => ValidationResult == ApiKeyValidationResult.Valid;
}

public static class ApiKeyHelper
{
    public const string HeaderName = "X-API-Key";
    public const string ConfigurationKey = "Authentication:ApiKey";

    public static ConfiguredApiKeyResult GetConfiguredApiKey(IConfiguration configuration)
    {
        var configuredApiKey = configuration[ConfigurationKey];

        return string.IsNullOrWhiteSpace(configuredApiKey)
            ? new ConfiguredApiKeyResult(null, ApiKeyValidationResult.MissingConfiguration, "API key authentication is not configured.")
            : new ConfiguredApiKeyResult(configuredApiKey, ApiKeyValidationResult.Valid, string.Empty);
    }

    public static bool TryGetProvidedApiKey(HttpRequest request, out string providedApiKey)
    {
        providedApiKey = string.Empty;

        if (!request.Headers.TryGetValue(HeaderName, out var headerValue) || string.IsNullOrWhiteSpace(headerValue))
        {
            return false;
        }

        providedApiKey = headerValue.ToString();
        return true;
    }

    public static bool ApiKeysMatch(string expectedApiKey, string providedApiKey)
    {
        var expectedBytes = Encoding.UTF8.GetBytes(expectedApiKey);
        var providedBytes = Encoding.UTF8.GetBytes(providedApiKey);

        return CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes);
    }

    public static ApiKeyValidationState ValidateRequest(IConfiguration configuration, HttpRequest request)
    {
        var configuredApiKey = GetConfiguredApiKey(configuration);

        if (!configuredApiKey.IsValid)
        {
            return new ApiKeyValidationState(configuredApiKey.ValidationResult, configuredApiKey.Message);
        }

        if (!TryGetProvidedApiKey(request, out var providedApiKey))
        {
            return new ApiKeyValidationState(ApiKeyValidationResult.MissingRequestApiKey, "API key is missing from the request.");
        }

        return ApiKeysMatch(configuredApiKey.Value!, providedApiKey)
            ? new ApiKeyValidationState(ApiKeyValidationResult.Valid, string.Empty)
            : new ApiKeyValidationState(ApiKeyValidationResult.InvalidRequestApiKey, "API key is invalid.");
    }
}
