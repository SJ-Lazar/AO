using AO.Core.Features.Activities;
using AO.Core.Features.Contacts;
using AO.Core.Features.Deals;
using AO.Core.Features.Users;
using AO.Core.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AO.Core.Features.Companies;

public sealed class Company
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? Industry { get; set; }
    public string? Website { get; set; }
    public string? Phone { get; set; }
    public string? Notes { get; set; }
    public Guid? AssignedUserId { get; set; }
    public CrmUser? AssignedUser { get; set; }
    public bool IsArchived { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
    public ICollection<Contact> Contacts { get; set; } = [];
    public ICollection<Deal> Deals { get; set; } = [];
}

public sealed record CompanyDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Industry { get; init; }
    public string? Website { get; init; }
    public string? Phone { get; init; }
    public string? Notes { get; init; }
    public Guid? AssignedUserId { get; init; }
    public string? AssignedUserName { get; init; }
    public bool IsArchived { get; init; }
    public int ContactCount { get; init; }
    public int OpenDealCount { get; init; }
    public DateTime CreatedUtc { get; init; }
    public DateTime UpdatedUtc { get; init; }
}

public sealed record CreateCompanyRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Industry { get; init; }
    public string? Website { get; init; }
    public string? Phone { get; init; }
    public string? Notes { get; init; }
    public Guid? AssignedUserId { get; init; }
}

public sealed record UpdateCompanyRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Industry { get; init; }
    public string? Website { get; init; }
    public string? Phone { get; init; }
    public string? Notes { get; init; }
    public Guid? AssignedUserId { get; init; }
    public bool IsArchived { get; init; }
}

internal sealed class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.ToTable("Companies");
        builder.HasKey(company => company.Id);

        builder.Property(company => company.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(company => company.Industry)
            .HasMaxLength(120);

        builder.Property(company => company.Website)
            .HasMaxLength(256);

        builder.Property(company => company.Phone)
            .HasMaxLength(40);

        builder.Property(company => company.Notes)
            .HasMaxLength(2000);

        builder.HasOne(company => company.AssignedUser)
            .WithMany()
            .HasForeignKey(company => company.AssignedUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(company => company.Name);
    }
}

public static class CompanySlice
{
    public static async Task<IReadOnlyList<CompanyDto>> GetAllAsync(AOContext dbContext, CancellationToken cancellationToken)
    {
        return await ProjectCompanies(dbContext.Companies.AsNoTracking())
            .OrderBy(company => company.Name)
            .ToListAsync(cancellationToken);
    }

    public static async Task<CompanyDto?> GetByIdAsync(AOContext dbContext, Guid id, CancellationToken cancellationToken)
    {
        return await ProjectCompanies(dbContext.Companies.AsNoTracking())
            .FirstOrDefaultAsync(company => company.Id == id, cancellationToken);
    }

    public static async Task<CompanyDto> CreateAsync(AOContext dbContext, CreateCompanyRequest request, CancellationToken cancellationToken)
    {
        Validate(request.Name);
        await CrmUserSlice.EnsureExistsAsync(dbContext, request.AssignedUserId, cancellationToken);

        var entity = new Company
        {
            Name = request.Name.Trim(),
            Industry = request.Industry?.Trim(),
            Website = request.Website?.Trim(),
            Phone = request.Phone?.Trim(),
            Notes = request.Notes?.Trim(),
            AssignedUserId = request.AssignedUserId,
            CreatedUtc = DateTime.UtcNow,
            UpdatedUtc = DateTime.UtcNow
        };

        dbContext.Companies.Add(entity);
        dbContext.Activities.Add(ActivitySlice.CreateEntry(new CreateActivityRequest
        {
            Type = CrmActivityType.CompanyCreated,
            Title = $"Company created: {entity.Name}",
            Description = entity.Industry,
            CompanyId = entity.Id
        }));
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(dbContext, entity.Id, cancellationToken)
            ?? throw new InvalidOperationException("Failed to load created company.");
    }

    public static async Task<CompanyDto?> UpdateAsync(AOContext dbContext, Guid id, UpdateCompanyRequest request, CancellationToken cancellationToken)
    {
        Validate(request.Name);
        await CrmUserSlice.EnsureExistsAsync(dbContext, request.AssignedUserId, cancellationToken);

        var entity = await dbContext.Companies.FirstOrDefaultAsync(company => company.Id == id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        entity.Name = request.Name.Trim();
        entity.Industry = request.Industry?.Trim();
        entity.Website = request.Website?.Trim();
        entity.Phone = request.Phone?.Trim();
        entity.Notes = request.Notes?.Trim();
        entity.AssignedUserId = request.AssignedUserId;
        entity.IsArchived = request.IsArchived;
        entity.UpdatedUtc = DateTime.UtcNow;

        dbContext.Activities.Add(ActivitySlice.CreateEntry(new CreateActivityRequest
        {
            Type = CrmActivityType.CompanyUpdated,
            Title = $"Company updated: {entity.Name}",
            Description = entity.Industry,
            CompanyId = entity.Id
        }));
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(dbContext, entity.Id, cancellationToken);
    }

    private static IQueryable<CompanyDto> ProjectCompanies(IQueryable<Company> query)
    {
        return query.Select(company => new CompanyDto
        {
            Id = company.Id,
            Name = company.Name,
            Industry = company.Industry,
            Website = company.Website,
            Phone = company.Phone,
            Notes = company.Notes,
            AssignedUserId = company.AssignedUserId,
            AssignedUserName = company.AssignedUser != null ? company.AssignedUser.FirstName + " " + company.AssignedUser.LastName : null,
            IsArchived = company.IsArchived,
            ContactCount = company.Contacts.Count,
            OpenDealCount = company.Deals.Count(deal => !deal.IsClosed),
            CreatedUtc = company.CreatedUtc,
            UpdatedUtc = company.UpdatedUtc
        });
    }

    private static void Validate(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Company name is required.");
        }
    }
}
