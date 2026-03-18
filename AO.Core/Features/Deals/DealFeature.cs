using AO.Core.Features.Activities;
using AO.Core.Features.Companies;
using AO.Core.Features.Contacts;
using AO.Core.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AO.Core.Features.Deals;

public enum DealStage
{
    Lead = 1,
    Qualified = 2,
    Proposal = 3,
    Negotiation = 4,
    Won = 5,
    Lost = 6
}

public sealed class Deal
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Value { get; set; }
    public DealStage Stage { get; set; } = DealStage.Lead;
    public DateTime? ExpectedCloseDateUtc { get; set; }
    public Guid? CompanyId { get; set; }
    public Company? Company { get; set; }
    public Guid? ContactId { get; set; }
    public Contact? Contact { get; set; }
    public bool IsClosed { get; set; }
    public DateTime? ClosedUtc { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
}

public sealed record DealDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public decimal Value { get; init; }
    public string Stage { get; init; } = string.Empty;
    public DateTime? ExpectedCloseDateUtc { get; init; }
    public Guid? CompanyId { get; init; }
    public string? CompanyName { get; init; }
    public Guid? ContactId { get; init; }
    public string? ContactName { get; init; }
    public bool IsClosed { get; init; }
    public DateTime? ClosedUtc { get; init; }
    public DateTime CreatedUtc { get; init; }
    public DateTime UpdatedUtc { get; init; }
}

public sealed record CreateDealRequest
{
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public decimal Value { get; init; }
    public DealStage Stage { get; init; } = DealStage.Lead;
    public DateTime? ExpectedCloseDateUtc { get; init; }
    public Guid? CompanyId { get; init; }
    public Guid? ContactId { get; init; }
}

public sealed record UpdateDealRequest
{
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public decimal Value { get; init; }
    public DealStage Stage { get; init; } = DealStage.Lead;
    public DateTime? ExpectedCloseDateUtc { get; init; }
    public Guid? CompanyId { get; init; }
    public Guid? ContactId { get; init; }
}

public sealed record SetDealStageRequest
{
    public DealStage Stage { get; init; }
}

internal sealed class DealConfiguration : IEntityTypeConfiguration<Deal>
{
    public void Configure(EntityTypeBuilder<Deal> builder)
    {
        builder.ToTable("Deals");
        builder.HasKey(deal => deal.Id);

        builder.Property(deal => deal.Title)
            .HasMaxLength(160)
            .IsRequired();

        builder.Property(deal => deal.Description)
            .HasMaxLength(2000);

        builder.Property(deal => deal.Stage)
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(deal => deal.Value)
            .HasPrecision(18, 2);

        builder.HasOne(deal => deal.Company)
            .WithMany(company => company.Deals)
            .HasForeignKey(deal => deal.CompanyId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(deal => deal.Contact)
            .WithMany()
            .HasForeignKey(deal => deal.ContactId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(deal => deal.Stage);
    }
}

public static class DealSlice
{
    public static async Task<IReadOnlyList<DealDto>> GetAllAsync(AOContext dbContext, CancellationToken cancellationToken)
    {
        return await ProjectDeals(dbContext.Deals.AsNoTracking())
            .OrderByDescending(deal => deal.UpdatedUtc)
            .ToListAsync(cancellationToken);
    }

    public static async Task<DealDto?> GetByIdAsync(AOContext dbContext, Guid id, CancellationToken cancellationToken)
    {
        return await ProjectDeals(dbContext.Deals.AsNoTracking())
            .FirstOrDefaultAsync(deal => deal.Id == id, cancellationToken);
    }

    public static async Task<DealDto> CreateAsync(AOContext dbContext, CreateDealRequest request, CancellationToken cancellationToken)
    {
        Validate(request.Title, request.Value);
        await EnsureRelatedRecordsExistAsync(dbContext, request.CompanyId, request.ContactId, cancellationToken);

        var entity = new Deal
        {
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            Value = request.Value,
            Stage = request.Stage,
            ExpectedCloseDateUtc = request.ExpectedCloseDateUtc,
            CompanyId = request.CompanyId,
            ContactId = request.ContactId,
            IsClosed = IsClosedStage(request.Stage),
            ClosedUtc = IsClosedStage(request.Stage) ? DateTime.UtcNow : null,
            CreatedUtc = DateTime.UtcNow,
            UpdatedUtc = DateTime.UtcNow
        };

        dbContext.Deals.Add(entity);
        dbContext.Activities.Add(ActivitySlice.CreateEntry(new CreateActivityRequest
        {
            Type = CrmActivityType.DealCreated,
            Title = $"Deal created: {entity.Title}",
            Description = entity.Stage.ToString(),
            CompanyId = entity.CompanyId,
            ContactId = entity.ContactId,
            DealId = entity.Id
        }));
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(dbContext, entity.Id, cancellationToken)
            ?? throw new InvalidOperationException("Failed to load created deal.");
    }

    public static async Task<DealDto?> UpdateAsync(AOContext dbContext, Guid id, UpdateDealRequest request, CancellationToken cancellationToken)
    {
        Validate(request.Title, request.Value);
        await EnsureRelatedRecordsExistAsync(dbContext, request.CompanyId, request.ContactId, cancellationToken);

        var entity = await dbContext.Deals.FirstOrDefaultAsync(deal => deal.Id == id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        entity.Title = request.Title.Trim();
        entity.Description = request.Description?.Trim();
        entity.Value = request.Value;
        entity.Stage = request.Stage;
        entity.ExpectedCloseDateUtc = request.ExpectedCloseDateUtc;
        entity.CompanyId = request.CompanyId;
        entity.ContactId = request.ContactId;
        entity.IsClosed = IsClosedStage(request.Stage);
        entity.ClosedUtc = entity.IsClosed ? entity.ClosedUtc ?? DateTime.UtcNow : null;
        entity.UpdatedUtc = DateTime.UtcNow;

        dbContext.Activities.Add(ActivitySlice.CreateEntry(new CreateActivityRequest
        {
            Type = CrmActivityType.DealUpdated,
            Title = $"Deal updated: {entity.Title}",
            Description = entity.Stage.ToString(),
            CompanyId = entity.CompanyId,
            ContactId = entity.ContactId,
            DealId = entity.Id
        }));
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(dbContext, entity.Id, cancellationToken);
    }

    public static async Task<DealDto?> SetStageAsync(AOContext dbContext, Guid id, SetDealStageRequest request, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Deals.FirstOrDefaultAsync(deal => deal.Id == id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        entity.Stage = request.Stage;
        entity.IsClosed = IsClosedStage(request.Stage);
        entity.ClosedUtc = entity.IsClosed ? entity.ClosedUtc ?? DateTime.UtcNow : null;
        entity.UpdatedUtc = DateTime.UtcNow;

        dbContext.Activities.Add(ActivitySlice.CreateEntry(new CreateActivityRequest
        {
            Type = CrmActivityType.DealStageChanged,
            Title = $"Deal stage changed: {entity.Title}",
            Description = request.Stage.ToString(),
            CompanyId = entity.CompanyId,
            ContactId = entity.ContactId,
            DealId = entity.Id
        }));
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(dbContext, entity.Id, cancellationToken);
    }

    private static IQueryable<DealDto> ProjectDeals(IQueryable<Deal> query)
    {
        return query.Select(deal => new DealDto
        {
            Id = deal.Id,
            Title = deal.Title,
            Description = deal.Description,
            Value = deal.Value,
            Stage = deal.Stage.ToString(),
            ExpectedCloseDateUtc = deal.ExpectedCloseDateUtc,
            CompanyId = deal.CompanyId,
            CompanyName = deal.Company != null ? deal.Company.Name : null,
            ContactId = deal.ContactId,
            ContactName = deal.Contact != null ? deal.Contact.FirstName + " " + deal.Contact.LastName : null,
            IsClosed = deal.IsClosed,
            ClosedUtc = deal.ClosedUtc,
            CreatedUtc = deal.CreatedUtc,
            UpdatedUtc = deal.UpdatedUtc
        });
    }

    private static void Validate(string title, decimal value)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Deal title is required.");
        }

        if (value < 0)
        {
            throw new ArgumentException("Deal value cannot be negative.");
        }
    }

    private static async Task EnsureRelatedRecordsExistAsync(AOContext dbContext, Guid? companyId, Guid? contactId, CancellationToken cancellationToken)
    {
        Guid? contactCompanyId = null;

        if (companyId.HasValue)
        {
            var companyExists = await dbContext.Companies.AnyAsync(company => company.Id == companyId.Value, cancellationToken);
            if (!companyExists)
            {
                throw new ArgumentException("The selected company was not found.");
            }
        }

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

            contactCompanyId = contact.CompanyId;
        }

        if (companyId.HasValue && contactCompanyId.HasValue && contactCompanyId.Value != companyId.Value)
        {
            throw new ArgumentException("The selected contact does not belong to the selected company.");
        }
    }

    private static bool IsClosedStage(DealStage stage)
    {
        return stage is DealStage.Won or DealStage.Lost;
    }
}
