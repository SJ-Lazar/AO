using AO.Core.Shared.AuditTrail;

namespace AO.Tests.Shared.AuditTrail;

[TestFixture]
public sealed class AuditTests
{
    [Test]
    public void Constructor_SetsDefaultValues()
    {
        var before = DateTimeOffset.UtcNow;
        var audit = new Audit();
        var after = DateTimeOffset.UtcNow;

        Assert.That(audit.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(audit.EntityName, Is.Empty);
        Assert.That(audit.EntityId, Is.Empty);
        Assert.That(audit.Action, Is.Empty);
        Assert.That(audit.Changes, Is.Empty);
        Assert.That(audit.CreatedAtUtc, Is.InRange(before, after));
    }

    [Test]
    public void AddChange_AddsNewAuditChange()
    {
        var audit = new Audit();

        audit.AddChange("Status", "Pending", "Approved");

        Assert.That(audit.Changes, Has.Count.EqualTo(1));
        Assert.That(audit.Changes[0], Is.EqualTo(new AuditChange("Status", "Pending", "Approved")));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    public void AddChange_WithInvalidPropertyName_ThrowsArgumentException(string? propertyName)
    {
        var audit = new Audit();

        var action = () => audit.AddChange(propertyName!, "old", "new");

        Assert.That(action, Throws.InstanceOf<ArgumentException>());
    }

    [Test]
    public void Changes_ReturnsReadOnlyViewOfTrackedChanges()
    {
        var audit = new Audit();
        var changes = audit.Changes;

        audit.AddChange("Name", "Old", "New");

        Assert.That(changes, Has.Count.EqualTo(1));
        Assert.That(changes[0].PropertyName, Is.EqualTo("Name"));
    }
}
