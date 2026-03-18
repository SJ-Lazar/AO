using AO.Core.Features.Activities;
using AO.Core.Features.Contacts;
using AO.Core.Features.Deals;
using AO.Core.Features.Users;
using AO.Core.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AO.Core.Features.Tasks;

public enum CrmTaskPriority
{
    Low = 1,
    Normal = 2,
    High = 3
}

public sealed class CrmTask
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? DueAtUtc { get; set; }
    public CrmTaskPriority Priority { get; set; } = CrmTaskPriority.Normal;
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public Guid? ContactId { get; set; }
    public Contact? Contact { get; set; }
    public Guid? DealId { get; set; }
    public Deal? Deal { get; set; }
    public Guid? AssignedUserId { get; set; }
    public CrmUser? AssignedUser { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
}

public sealed record CrmTaskDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public DateTime? DueAtUtc { get; init; }
    public string Priority { get; init; } = string.Empty;
    public bool IsCompleted { get; init; }
    public DateTime? CompletedAtUtc { get; init; }
    public Guid? ContactId { get; init; }
    public string? ContactName { get; init; }
    public Guid? DealId { get; init; }
    public string? DealTitle { get; init; }
    public Guid? AssignedUserId { get; init; }
    public string? AssignedUserName { get; init; }
    public DateTime CreatedUtc { get; init; }
    public DateTime UpdatedUtc { get; init; }
}

public sealed record CreateCrmTaskRequest
{
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public DateTime? DueAtUtc { get; init; }
    public CrmTaskPriority Priority { get; init; } = CrmTaskPriority.Normal;
    public Guid? ContactId { get; init; }
    public Guid? DealId { get; init; }
    public Guid? AssignedUserId { get; init; }
}

public sealed record UpdateCrmTaskRequest
{
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public DateTime? DueAtUtc { get; init; }
    public CrmTaskPriority Priority { get; init; } = CrmTaskPriority.Normal;
    public Guid? ContactId { get; init; }
    public Guid? DealId { get; init; }
    public Guid? AssignedUserId { get; init; }
    public bool IsCompleted { get; init; }
}

internal sealed class CrmTaskConfiguration : IEntityTypeConfiguration<CrmTask>
{
    public void Configure(EntityTypeBuilder<CrmTask> builder)
    {
        builder.ToTable("Tasks");
        builder.HasKey(task => task.Id);

        builder.Property(task => task.Title)
            .HasMaxLength(160)
            .IsRequired();

        builder.Property(task => task.Description)
            .HasMaxLength(2000);

        builder.Property(task => task.Priority)
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.HasOne(task => task.Contact)
            .WithMany()
            .HasForeignKey(task => task.ContactId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(task => task.Deal)
            .WithMany()
            .HasForeignKey(task => task.DealId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(task => task.AssignedUser)
            .WithMany()
            .HasForeignKey(task => task.AssignedUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(task => new { task.IsCompleted, task.DueAtUtc });
    }
}

public static class TaskSlice
{
    public static async Task<IReadOnlyList<CrmTaskDto>> GetAllAsync(AOContext dbContext, CancellationToken cancellationToken)
    {
        return await ProjectTasks(dbContext.Tasks.AsNoTracking())
            .OrderBy(task => task.IsCompleted)
            .ThenBy(task => task.DueAtUtc)
            .ToListAsync(cancellationToken);
    }

    public static async Task<CrmTaskDto?> GetByIdAsync(AOContext dbContext, Guid id, CancellationToken cancellationToken)
    {
        return await ProjectTasks(dbContext.Tasks.AsNoTracking())
            .FirstOrDefaultAsync(task => task.Id == id, cancellationToken);
    }

    public static async Task<CrmTaskDto> CreateAsync(AOContext dbContext, CreateCrmTaskRequest request, CancellationToken cancellationToken)
    {
        Validate(request.Title);
        await EnsureRelatedRecordsExistAsync(dbContext, request.ContactId, request.DealId, request.AssignedUserId, cancellationToken);

        var entity = new CrmTask
        {
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            DueAtUtc = request.DueAtUtc,
            Priority = request.Priority,
            ContactId = request.ContactId,
            DealId = request.DealId,
            AssignedUserId = request.AssignedUserId,
            CreatedUtc = DateTime.UtcNow,
            UpdatedUtc = DateTime.UtcNow
        };

        dbContext.Tasks.Add(entity);
        dbContext.Activities.Add(ActivitySlice.CreateEntry(new CreateActivityRequest
        {
            Type = CrmActivityType.TaskCreated,
            Title = $"Task created: {entity.Title}",
            Description = entity.Priority.ToString(),
            ContactId = entity.ContactId,
            DealId = entity.DealId,
            TaskId = entity.Id
        }));
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(dbContext, entity.Id, cancellationToken)
            ?? throw new InvalidOperationException("Failed to load created task.");
    }

    public static async Task<CrmTaskDto?> UpdateAsync(AOContext dbContext, Guid id, UpdateCrmTaskRequest request, CancellationToken cancellationToken)
    {
        Validate(request.Title);
        await EnsureRelatedRecordsExistAsync(dbContext, request.ContactId, request.DealId, request.AssignedUserId, cancellationToken);

        var entity = await dbContext.Tasks.FirstOrDefaultAsync(task => task.Id == id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        entity.Title = request.Title.Trim();
        entity.Description = request.Description?.Trim();
        entity.DueAtUtc = request.DueAtUtc;
        entity.Priority = request.Priority;
        entity.ContactId = request.ContactId;
        entity.DealId = request.DealId;
        entity.AssignedUserId = request.AssignedUserId;
        entity.IsCompleted = request.IsCompleted;
        entity.CompletedAtUtc = request.IsCompleted ? entity.CompletedAtUtc ?? DateTime.UtcNow : null;
        entity.UpdatedUtc = DateTime.UtcNow;

        dbContext.Activities.Add(ActivitySlice.CreateEntry(new CreateActivityRequest
        {
            Type = CrmActivityType.TaskUpdated,
            Title = $"Task updated: {entity.Title}",
            Description = entity.Priority.ToString(),
            ContactId = entity.ContactId,
            DealId = entity.DealId,
            TaskId = entity.Id
        }));
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(dbContext, entity.Id, cancellationToken);
    }

    public static async Task<CrmTaskDto?> CompleteAsync(AOContext dbContext, Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Tasks.FirstOrDefaultAsync(task => task.Id == id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        entity.IsCompleted = true;
        entity.CompletedAtUtc = DateTime.UtcNow;
        entity.UpdatedUtc = DateTime.UtcNow;

        dbContext.Activities.Add(ActivitySlice.CreateEntry(new CreateActivityRequest
        {
            Type = CrmActivityType.TaskCompleted,
            Title = $"Task completed: {entity.Title}",
            Description = entity.Priority.ToString(),
            ContactId = entity.ContactId,
            DealId = entity.DealId,
            TaskId = entity.Id
        }));
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(dbContext, entity.Id, cancellationToken);
    }

    private static IQueryable<CrmTaskDto> ProjectTasks(IQueryable<CrmTask> query)
    {
        return query.Select(task => new CrmTaskDto
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            DueAtUtc = task.DueAtUtc,
            Priority = task.Priority.ToString(),
            IsCompleted = task.IsCompleted,
            CompletedAtUtc = task.CompletedAtUtc,
            ContactId = task.ContactId,
            ContactName = task.Contact != null ? task.Contact.FirstName + " " + task.Contact.LastName : null,
            DealId = task.DealId,
            DealTitle = task.Deal != null ? task.Deal.Title : null,
            AssignedUserId = task.AssignedUserId,
            AssignedUserName = task.AssignedUser != null ? task.AssignedUser.FirstName + " " + task.AssignedUser.LastName : null,
            CreatedUtc = task.CreatedUtc,
            UpdatedUtc = task.UpdatedUtc
        });
    }

    private static void Validate(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Task title is required.");
        }
    }

    private static async Task EnsureRelatedRecordsExistAsync(AOContext dbContext, Guid? contactId, Guid? dealId, Guid? assignedUserId, CancellationToken cancellationToken)
    {
        Guid? resolvedContactId = null;
        Guid? resolvedContactCompanyId = null;
        Guid? resolvedDealContactId = null;
        Guid? resolvedDealCompanyId = null;

        await CrmUserSlice.EnsureExistsAsync(dbContext, assignedUserId, cancellationToken);

        if (contactId.HasValue)
        {
            var contact = await dbContext.Contacts
                .AsNoTracking()
                .Where(contact => contact.Id == contactId.Value)
                .Select(contact => new { contact.Id, contact.CompanyId })
                .FirstOrDefaultAsync(cancellationToken);

            if (contact is null)
            {
                throw new ArgumentException("The selected contact was not found.");
            }

            resolvedContactId = contact.Id;
            resolvedContactCompanyId = contact.CompanyId;
        }

        if (dealId.HasValue)
        {
            var deal = await dbContext.Deals
                .AsNoTracking()
                .Where(deal => deal.Id == dealId.Value)
                .Select(deal => new { deal.Id, deal.ContactId, deal.CompanyId })
                .FirstOrDefaultAsync(cancellationToken);

            if (deal is null)
            {
                throw new ArgumentException("The selected deal was not found.");
            }

            resolvedDealContactId = deal.ContactId;
            resolvedDealCompanyId = deal.CompanyId;
        }

        if (resolvedContactId.HasValue && resolvedDealContactId.HasValue && resolvedDealContactId.Value != resolvedContactId.Value)
        {
            throw new ArgumentException("The selected contact does not match the selected deal.");
        }

        if (resolvedContactCompanyId.HasValue && resolvedDealCompanyId.HasValue && resolvedContactCompanyId.Value != resolvedDealCompanyId.Value)
        {
            throw new ArgumentException("The selected contact does not belong to the selected deal's company.");
        }
    }
}
