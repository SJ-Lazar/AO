using AO.Core.Shared.Idempotents;

namespace AO.Tests.Shared.Idempotents;

[TestFixture]
public sealed class InMemoryIdempotencyStoreTests
{
    [Test]
    public async Task SetAsync_ThenGetAsync_ReturnsStoredEntry()
    {
        var store = new InMemoryIdempotencyStore();
        var entry = CreateEntry("key-1", DateTimeOffset.UtcNow.AddMinutes(10));

        await store.SetAsync(entry);
        var result = await store.GetAsync(entry.Key);

        Assert.That(result, Is.EqualTo(entry));
    }

    [Test]
    public async Task RemoveAsync_RemovesStoredEntry()
    {
        var store = new InMemoryIdempotencyStore();
        var entry = CreateEntry("key-2", DateTimeOffset.UtcNow.AddMinutes(10));
        await store.SetAsync(entry);

        await store.RemoveAsync(entry.Key);
        var result = await store.GetAsync(entry.Key);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task RemoveExpiredAsync_RemovesOnlyExpiredEntries()
    {
        var store = new InMemoryIdempotencyStore();
        var utcNow = DateTimeOffset.UtcNow;
        var expiredEntry = CreateEntry("expired", utcNow.AddMinutes(-1));
        var activeEntry = CreateEntry("active", utcNow.AddMinutes(10));

        await store.SetAsync(expiredEntry);
        await store.SetAsync(activeEntry);

        await store.RemoveExpiredAsync(utcNow);

        Assert.That(await store.GetAsync(expiredEntry.Key), Is.Null);
        Assert.That(await store.GetAsync(activeEntry.Key), Is.EqualTo(activeEntry));
    }

    [Test]
    public void GetAsync_WithCanceledToken_ThrowsOperationCanceledException()
    {
        var store = new InMemoryIdempotencyStore();
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        var action = async () => await store.GetAsync("key", cancellationTokenSource.Token);

        Assert.That(action, Throws.InstanceOf<OperationCanceledException>());
    }

    [Test]
    public void SetAsync_WithNullEntry_ThrowsArgumentNullException()
    {
        var store = new InMemoryIdempotencyStore();

        var action = async () => await store.SetAsync(null!);

        Assert.That(action, Throws.InstanceOf<ArgumentNullException>());
    }

    [Test]
    public void IdempotencyEntry_IsExpired_ReturnsExpectedValue()
    {
        var utcNow = DateTimeOffset.UtcNow;
        var expiredEntry = CreateEntry("expired", utcNow);
        var activeEntry = CreateEntry("active", utcNow.AddSeconds(1));

        Assert.That(expiredEntry.IsExpired(utcNow), Is.True);
        Assert.That(activeEntry.IsExpired(utcNow), Is.False);
    }

    private static IdempotencyEntry CreateEntry(string key, DateTimeOffset expiresAtUtc)
    {
        return new IdempotencyEntry(
            key,
            $"hash-{key}",
            200,
            "{\"status\":\"ok\"}",
            "application/json",
            DateTimeOffset.UtcNow,
            expiresAtUtc);
    }
}
