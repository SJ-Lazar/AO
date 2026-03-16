using System.Security.Cryptography;
using System.Text;

namespace AO.Core.Shared.Idempotents;

public static class IdempotencyHelper
{
    public static string CreateRequestHash(string method, string path, string? body)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(method);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var normalizedPayload = $"{method.Trim().ToUpperInvariant()}:{path.Trim()}:{body ?? string.Empty}";
        var payloadBytes = Encoding.UTF8.GetBytes(normalizedPayload);
        var hashBytes = SHA256.HashData(payloadBytes);

        return Convert.ToHexString(hashBytes);
    }
}
