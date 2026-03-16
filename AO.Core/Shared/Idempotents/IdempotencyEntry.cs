namespace AO.Core.Shared.Idempotents;

public sealed record IdempotencyEntry(
    string Key,
    string RequestHash,
    int StatusCode,
    string? ResponseBody,
    string? ContentType,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset ExpiresAtUtc)
{
    public bool IsExpired(DateTimeOffset utcNow) => ExpiresAtUtc <= utcNow;
}
