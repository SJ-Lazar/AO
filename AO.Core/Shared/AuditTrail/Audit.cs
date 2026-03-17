using System;
using System.Collections.Generic;

namespace AO.Core.Shared.AuditTrail;

public sealed class Audit
{
    private readonly List<AuditChange> _changes = [];

    public Guid Id { get; init; } = Guid.CreateVersion7();
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? PerformedBy { get; set; }
    public string? CorrelationId { get; set; }
    public string? IpAddress { get; set; }
    public DateTimeOffset CreatedAtUtc { get; init; } = DateTimeOffset.UtcNow;
    public IReadOnlyList<AuditChange> Changes => _changes;

    public void AddChange(string propertyName, string? oldValue, string? newValue)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);

        _changes.Add(new AuditChange(propertyName, oldValue, newValue));
    }
}

public sealed record AuditChange(string PropertyName, string? OldValue, string? NewValue);
