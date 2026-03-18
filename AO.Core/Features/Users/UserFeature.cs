using AO.Core.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AO.Core.Features.Users;

public sealed class CrmUser
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
}

public sealed record CrmUserDto
{
    public Guid Id { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string? Email { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedUtc { get; init; }
    public DateTime UpdatedUtc { get; init; }
}

public sealed record CreateCrmUserRequest
{
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string? Email { get; init; }
}

internal sealed class CrmUserConfiguration : IEntityTypeConfiguration<CrmUser>
{
    public void Configure(EntityTypeBuilder<CrmUser> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(user => user.Id);

        builder.Property(user => user.FirstName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(user => user.LastName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(user => user.Email)
            .HasMaxLength(256);

        builder.HasIndex(user => user.Email);
    }
}

public static class CrmUserSlice
{
    public static async Task<IReadOnlyList<CrmUserDto>> GetAllAsync(AOContext dbContext, CancellationToken cancellationToken)
    {
        return await ProjectUsers(dbContext.Users.AsNoTracking())
            .OrderBy(user => user.FirstName)
            .ThenBy(user => user.LastName)
            .ToListAsync(cancellationToken);
    }

    public static async Task<CrmUserDto?> GetByIdAsync(AOContext dbContext, Guid id, CancellationToken cancellationToken)
    {
        return await ProjectUsers(dbContext.Users.AsNoTracking())
            .FirstOrDefaultAsync(user => user.Id == id, cancellationToken);
    }

    public static async Task<CrmUserDto> CreateAsync(AOContext dbContext, CreateCrmUserRequest request, CancellationToken cancellationToken)
    {
        Validate(request.FirstName, request.LastName);

        var entity = new CrmUser
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = request.Email?.Trim(),
            CreatedUtc = DateTime.UtcNow,
            UpdatedUtc = DateTime.UtcNow
        };

        dbContext.Users.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(dbContext, entity.Id, cancellationToken)
            ?? throw new InvalidOperationException("Failed to load created user.");
    }

    public static async Task EnsureExistsAsync(AOContext dbContext, Guid? userId, CancellationToken cancellationToken)
    {
        if (!userId.HasValue)
        {
            return;
        }

        var exists = await dbContext.Users.AnyAsync(user => user.Id == userId.Value, cancellationToken);
        if (!exists)
        {
            throw new ArgumentException("The selected user was not found.");
        }
    }

    private static IQueryable<CrmUserDto> ProjectUsers(IQueryable<CrmUser> query)
    {
        return query.Select(user => new CrmUserDto
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            FullName = user.FirstName + " " + user.LastName,
            Email = user.Email,
            IsActive = user.IsActive,
            CreatedUtc = user.CreatedUtc,
            UpdatedUtc = user.UpdatedUtc
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
}
