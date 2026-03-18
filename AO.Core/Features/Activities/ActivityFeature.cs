using AO.Core.Features.Companies;
using AO.Core.Features.Contacts;
using AO.Core.Features.Deals;
using AO.Core.Features.Tasks;
using AO.Core.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AO.Core.Features.Activities;

public enum CrmActivityType
{
    CompanyCreated = 1,
    CompanyUpdated = 2,
    ContactCreated = 3,
    ContactUpdated = 4,
    DealCreated = 5,
    DealUpdated = 6,
    DealStageChanged = 7,
    TaskCreated = 8,
    TaskUpdated = 9,
    TaskCompleted = 10
}

public sealed class CrmActivity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public CrmActivityType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? CompanyId { get; set; }
    public Company? Company { get; set; }
    public Guid? ContactId { get; set; }
    public Contact? Contact { get; set; }
    public Guid? DealId { get; set; }
    public Deal? Deal { get; set; }
    public Guid? TaskId { get; set; }
    public CrmTask? Task { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}

public sealed record CrmActivityDto
{
    public Guid Id { get; init; }
    public string Type { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public Guid? CompanyId { get; init; }
    public string? CompanyName { get; init; }
    public Guid? ContactId { get; init; }
    public string? ContactName { get; init; }
    public Guid? DealId { get; init; }
    public string? DealTitle { get; init; }
    public Guid? TaskId { get; init; }
    public string? TaskTitle { get; init; }
    public DateTime CreatedUtc { get; init; }
}

public sealed record CreateActivityRequest
{
    public CrmActivityType Type { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public Guid? CompanyId { get; init; }
    public Guid? ContactId { get; init; }
    public Guid? DealId { get; init; }
    public Guid? TaskId { get; init; }
}

internal sealed class CrmActivityConfiguration : IEntityTypeConfiguration<CrmActivity>
{
    public void Configure(EntityTypeBuilder<CrmActivity> builder)
    {
        builder.ToTable("Activities");
        builder.HasKey(activity => activity.Id);

        builder.Property(activity => activity.Type)
            .HasConversion<string>()
            .HasMaxLength(64);

        builder.Property(activity => activity.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(activity => activity.Description)
            .HasMaxLength(2000);

        builder.HasOne(activity => activity.Company)
            .WithMany()
            .HasForeignKey(activity => activity.CompanyId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(activity => activity.Contact)
            .WithMany()
            .HasForeignKey(activity => activity.ContactId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(activity => activity.Deal)
            .WithMany()
            .HasForeignKey(activity => activity.DealId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(activity => activity.Task)
            .WithMany()
            .HasForeignKey(activity => activity.TaskId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(activity => activity.CreatedUtc);
    }
}

public static class ActivitySlice
{
    public static async Task<IReadOnlyList<CrmActivityDto>> GetRecentAsync(AOContext dbContext, int take, CancellationToken cancellationToken)
    {
        take = Math.Clamp(take, 1, 50);

        return await ProjectActivities(dbContext.Activities.AsNoTracking())
            .OrderByDescending(activity => activity.CreatedUtc)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public static async Task<CrmActivityDto> CreateAsync(AOContext dbContext, CreateActivityRequest request, CancellationToken cancellationToken)
    {
        Validate(request.Title);
        var entity = CreateEntry(request);
        dbContext.Activities.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await ProjectActivities(dbContext.Activities.AsNoTracking())
            .SingleAsync(activity => activity.Id == entity.Id, cancellationToken);
    }

    public static CrmActivity CreateEntry(CreateActivityRequest request)
    {
        Validate(request.Title);

        return new CrmActivity
        {
            Type = request.Type,
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            CompanyId = request.CompanyId,
            ContactId = request.ContactId,
            DealId = request.DealId,
            TaskId = request.TaskId,
            CreatedUtc = DateTime.UtcNow
        };
    }

    private static IQueryable<CrmActivityDto> ProjectActivities(IQueryable<CrmActivity> query)
    {
        return query.Select(activity => new CrmActivityDto
        {
            Id = activity.Id,
            Type = activity.Type.ToString(),
            Title = activity.Title,
            Description = activity.Description,
            CompanyId = activity.CompanyId,
            CompanyName = activity.Company != null ? activity.Company.Name : null,
            ContactId = activity.ContactId,
            ContactName = activity.Contact != null ? activity.Contact.FirstName + " " + activity.Contact.LastName : null,
            DealId = activity.DealId,
            DealTitle = activity.Deal != null ? activity.Deal.Title : null,
            TaskId = activity.TaskId,
            TaskTitle = activity.Task != null ? activity.Task.Title : null,
            CreatedUtc = activity.CreatedUtc
        });
    }

    private static void Validate(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Activity title is required.");
        }
    }
}
