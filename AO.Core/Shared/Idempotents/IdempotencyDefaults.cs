namespace AO.Core.Shared.Idempotents;

public static class IdempotencyDefaults
{
    public const string HeaderName = "Idempotency-Key";
    public static readonly TimeSpan DefaultExpiration = TimeSpan.FromHours(24);
}
