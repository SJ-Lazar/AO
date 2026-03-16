namespace AO.Core.Shared.Idempotents;

public interface IIdempotencyStore
{
    Task<IdempotencyEntry?> GetAsync(string key, CancellationToken cancellationToken = default);
    Task SetAsync(IdempotencyEntry entry, CancellationToken cancellationToken = default);
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
    Task RemoveExpiredAsync(DateTimeOffset utcNow, CancellationToken cancellationToken = default);
}
