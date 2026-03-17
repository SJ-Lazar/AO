using AO.Core.Shared.Idempotents;

namespace AO.Tests.Shared.Idempotents;

[TestFixture]
public sealed class IdempotencyHelperTests
{
    [Test]
    public void CreateRequestHash_SameNormalizedInput_ReturnsSameHash()
    {
        var firstHash = IdempotencyHelper.CreateRequestHash("post", " /orders ", "{\"id\":1}");
        var secondHash = IdempotencyHelper.CreateRequestHash("POST", "/orders", "{\"id\":1}");

        Assert.That(firstHash, Is.EqualTo(secondHash));
        Assert.That(firstHash, Has.Length.EqualTo(64));
    }

    [Test]
    public void CreateRequestHash_DifferentBodies_ReturnsDifferentHashes()
    {
        var firstHash = IdempotencyHelper.CreateRequestHash("POST", "/orders", "{\"id\":1}");
        var secondHash = IdempotencyHelper.CreateRequestHash("POST", "/orders", "{\"id\":2}");

        Assert.That(firstHash, Is.Not.EqualTo(secondHash));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    public void CreateRequestHash_WithInvalidMethod_ThrowsArgumentException(string? method)
    {
        var action = () => IdempotencyHelper.CreateRequestHash(method!, "/orders", "body");

        Assert.That(action, Throws.InstanceOf<ArgumentException>());
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    public void CreateRequestHash_WithInvalidPath_ThrowsArgumentException(string? path)
    {
        var action = () => IdempotencyHelper.CreateRequestHash("POST", path!, "body");

        Assert.That(action, Throws.InstanceOf<ArgumentException>());
    }

    [Test]
    public void DefaultExpiration_IsTwentyFourHours()
    {
        Assert.That(IdempotencyDefaults.DefaultExpiration, Is.EqualTo(TimeSpan.FromHours(24)));
        Assert.That(IdempotencyDefaults.HeaderName, Is.EqualTo("Idempotency-Key"));
    }
}
