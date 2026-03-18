using AO.Core.Features.Activities;
using AO.Core.Features.Companies;
using AO.Core.Features.Users;
using AO.Core.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AO.Core.Features.Contacts;

public sealed class Contact
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? JobTitle { get; set; }
    public string? Notes { get; set; }
    public Guid? CompanyId { get; set; }
    public Company? Company { get; set; }
    public Guid? AssignedUserId { get; set; }
    public CrmUser? AssignedUser { get; set; }
    public bool IsArchived { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
}

public sealed record ContactDto
{
    public Guid Id { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? JobTitle { get; init; }
    public string? Notes { get; init; }
    public Guid? CompanyId { get; init; }
    public string? CompanyName { get; init; }
    public Guid? AssignedUserId { get; init; }
    public string? AssignedUserName { get; init; }
    public bool IsArchived { get; init; }
    public DateTime CreatedUtc { get; init; }
    public DateTime UpdatedUtc { get; init; }
}

public sealed record CreateContactRequest
{
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? JobTitle { get; init; }
    public string? Notes { get; init; }
    public Guid? CompanyId { get; init; }
    public Guid? AssignedUserId { get; init; }
}

public sealed record UpdateContactRequest
{
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? JobTitle { get; init; }
    public string? Notes { get; init; }
    public Guid? CompanyId { get; init; }
    public Guid? AssignedUserId { get; init; }
    public bool IsArchived { get; init; }
}

internal sealed class ContactConfiguration : IEntityTypeConfiguration<Contact>
{
    public void Configure(EntityTypeBuilder<Contact> builder)
    {
        builder.ToTable("Contacts");
        builder.HasKey(contact => contact.Id);

        builder.Property(contact => contact.FirstName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(contact => contact.LastName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(contact => contact.Email)
            .HasMaxLength(256);

        builder.Property(contact => contact.Phone)
            .HasMaxLength(40);

        builder.Property(contact => contact.JobTitle)
            .HasMaxLength(120);

        builder.Property(contact => contact.Notes)
            .HasMaxLength(2000);

        builder.HasIndex(contact => contact.Email);

        builder.HasOne(contact => contact.Company)
            .WithMany(company => company.Contacts)
            .HasForeignKey(contact => contact.CompanyId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(contact => contact.AssignedUser)
            .WithMany()
            .HasForeignKey(contact => contact.AssignedUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public static class ContactSlice
{
    public static async Task<IReadOnlyList<ContactDto>> GetAllAsync(AOContext dbContext, CancellationToken cancellationToken)
    {
        return await ProjectContacts(dbContext.Contacts.AsNoTracking())
            .OrderBy(contact => contact.FirstName)
            .ThenBy(contact => contact.LastName)
            .ToListAsync(cancellationToken);
    }

    public static async Task<ContactDto?> GetByIdAsync(AOContext dbContext, Guid id, CancellationToken cancellationToken)
    {
        return await ProjectContacts(dbContext.Contacts.AsNoTracking())
            .FirstOrDefaultAsync(contact => contact.Id == id, cancellationToken);
    }

    public static async Task<ContactDto> CreateAsync(AOContext dbContext, CreateContactRequest request, CancellationToken cancellationToken)
    {
        Validate(request.FirstName, request.LastName);
        await EnsureCompanyExistsAsync(dbContext, request.CompanyId, cancellationToken);
        await CrmUserSlice.EnsureExistsAsync(dbContext, request.AssignedUserId, cancellationToken);

        var entity = new Contact
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = request.Email?.Trim(),
            Phone = request.Phone?.Trim(),
            JobTitle = request.JobTitle?.Trim(),
            Notes = request.Notes?.Trim(),
            CompanyId = request.CompanyId,
            AssignedUserId = request.AssignedUserId,
            CreatedUtc = DateTime.UtcNow,
            UpdatedUtc = DateTime.UtcNow
        };

        dbContext.Contacts.Add(entity);
        dbContext.Activities.Add(ActivitySlice.CreateEntry(new CreateActivityRequest
        {
            Type = CrmActivityType.ContactCreated,
            Title = $"Contact created: {entity.FirstName} {entity.LastName}",
            Description = entity.Email,
            CompanyId = entity.CompanyId,
            ContactId = entity.Id
        }));
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(dbContext, entity.Id, cancellationToken)
            ?? throw new InvalidOperationException("Failed to load created contact.");
    }

    public static async Task<ContactDto?> UpdateAsync(AOContext dbContext, Guid id, UpdateContactRequest request, CancellationToken cancellationToken)
    {
        Validate(request.FirstName, request.LastName);
        await EnsureCompanyExistsAsync(dbContext, request.CompanyId, cancellationToken);
        await CrmUserSlice.EnsureExistsAsync(dbContext, request.AssignedUserId, cancellationToken);

        var entity = await dbContext.Contacts.FirstOrDefaultAsync(contact => contact.Id == id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        entity.FirstName = request.FirstName.Trim();
        entity.LastName = request.LastName.Trim();
        entity.Email = request.Email?.Trim();
        entity.Phone = request.Phone?.Trim();
        entity.JobTitle = request.JobTitle?.Trim();
        entity.Notes = request.Notes?.Trim();
        entity.CompanyId = request.CompanyId;
        entity.AssignedUserId = request.AssignedUserId;
        entity.IsArchived = request.IsArchived;
        entity.UpdatedUtc = DateTime.UtcNow;

        dbContext.Activities.Add(ActivitySlice.CreateEntry(new CreateActivityRequest
        {
            Type = CrmActivityType.ContactUpdated,
            Title = $"Contact updated: {entity.FirstName} {entity.LastName}",
            Description = entity.Email,
            CompanyId = entity.CompanyId,
            ContactId = entity.Id
        }));
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(dbContext, entity.Id, cancellationToken);
    }

    private static IQueryable<ContactDto> ProjectContacts(IQueryable<Contact> query)
    {
        return query.Select(contact => new ContactDto
        {
            Id = contact.Id,
            FirstName = contact.FirstName,
            LastName = contact.LastName,
            FullName = contact.FirstName + " " + contact.LastName,
            Email = contact.Email,
            Phone = contact.Phone,
            JobTitle = contact.JobTitle,
            Notes = contact.Notes,
            CompanyId = contact.CompanyId,
            CompanyName = contact.Company != null ? contact.Company.Name : null,
            AssignedUserId = contact.AssignedUserId,
            AssignedUserName = contact.AssignedUser != null ? contact.AssignedUser.FirstName + " " + contact.AssignedUser.LastName : null,
            IsArchived = contact.IsArchived,
            CreatedUtc = contact.CreatedUtc,
            UpdatedUtc = contact.UpdatedUtc
        });
    }

    private static void Validate(string firstName, string lastName)
    {
        if (string.IsNullOrWhiteSpace(firstName))
        {
            throw new ArgumentException("First name is required.");
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            throw new ArgumentException("Last name is required.");
        }
    }

    private static async Task EnsureCompanyExistsAsync(AOContext dbContext, Guid? companyId, CancellationToken cancellationToken)
    {
        if (!companyId.HasValue)
        {
            return;
        }

        var exists = await dbContext.Companies.AnyAsync(company => company.Id == companyId.Value, cancellationToken);
        if (!exists)
        {
            throw new ArgumentException("The selected company was not found.");
        }
    }
}
