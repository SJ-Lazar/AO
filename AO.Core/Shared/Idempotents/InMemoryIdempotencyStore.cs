using System.Collections.Concurrent;

namespace AO.Core.Shared.Idempotents;

public sealed class InMemoryIdempotencyStore : IIdempotencyStore
{
    private readonly ConcurrentDictionary<string, IdempotencyEntry> _entries = new(StringComparer.Ordinal);

    public Task<IdempotencyEntry?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        cancellationToken.ThrowIfCancellationRequested();

        _entries.TryGetValue(key, out var entry);
        return Task.FromResult(entry);
    }

    public Task SetAsync(IdempotencyEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);
        cancellationToken.ThrowIfCancellationRequested();

        _entries[entry.Key] = entry;
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        cancellationToken.ThrowIfCancellationRequested();

        _entries.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    public Task RemoveExpiredAsync(DateTimeOffset utcNow, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        foreach (var entry in _entries)
        {
            if (entry.Value.IsExpired(utcNow))
            {
                _entries.TryRemove(entry.Key, out _);
            }
        }

        return Task.CompletedTask;
    }
}
