using AO.Core.Features.Activities;
using AO.Core.Features.Companies;
using AO.Core.Features.Contacts;
using AO.Core.Features.Dashboard;
using AO.Core.Features.Deals;
using AO.Core.Features.Tasks;
using Microsoft.EntityFrameworkCore;

namespace AO.Core.Infrastructure;

public class AOContext : DbContext
{
    public AOContext(DbContextOptions<AOContext> options)
        : base(options)
    {
    }

    public DbSet<Company> Companies => Set<Company>();

    public DbSet<CrmActivity> Activities => Set<CrmActivity>();

    public DbSet<Contact> Contacts => Set<Contact>();

    public DbSet<Deal> Deals => Set<Deal>();

    public DbSet<CrmTask> Tasks => Set<CrmTask>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AOContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
