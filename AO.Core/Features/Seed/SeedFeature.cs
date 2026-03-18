using AO.Core.Features.Companies;
using AO.Core.Features.Contacts;
using AO.Core.Features.Deals;
using AO.Core.Features.Tasks;
using AO.Core.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AO.Core.Features.Seed;

public static class SeedSlice
{
    public static async Task SeedAsync(AOContext dbContext, CancellationToken cancellationToken = default)
    {
        if (await dbContext.Companies.AnyAsync(cancellationToken)
            || await dbContext.Contacts.AnyAsync(cancellationToken)
            || await dbContext.Deals.AnyAsync(cancellationToken)
            || await dbContext.Tasks.AnyAsync(cancellationToken))
        {
            return;
        }

        var atlas = await CompanySlice.CreateAsync(
            dbContext,
            new CreateCompanyRequest
            {
                Name = "Atlas Manufacturing",
                Industry = "Manufacturing",
                Website = "https://atlas.example",
                Phone = "+1 555-0100",
                Notes = "Strategic account"
            },
            cancellationToken);

        var beacon = await CompanySlice.CreateAsync(
            dbContext,
            new CreateCompanyRequest
            {
                Name = "Beacon Logistics",
                Industry = "Logistics",
                Website = "https://beacon.example",
                Phone = "+1 555-0140",
                Notes = "Expansion opportunity"
            },
            cancellationToken);

        var cloud = await CompanySlice.CreateAsync(
            dbContext,
            new CreateCompanyRequest
            {
                Name = "Cloud23",
                Industry = "Software",
                Website = "https://cloud23.example",
                Phone = "+1 555-0172",
                Notes = "High-growth customer"
            },
            cancellationToken);

        var mikasa = await ContactSlice.CreateAsync(
            dbContext,
            new CreateContactRequest
            {
                FirstName = "Mikasa",
                LastName = "Ackerman",
                Email = "mikasa@atlas.example",
                Phone = "+1 555-0101",
                JobTitle = "Procurement Lead",
                CompanyId = atlas.Id,
                Notes = "Primary decision maker"
            },
            cancellationToken);

        var armin = await ContactSlice.CreateAsync(
            dbContext,
            new CreateContactRequest
            {
                FirstName = "Armin",
                LastName = "Arlert",
                Email = "armin@cloud23.example",
                Phone = "+1 555-0173",
                JobTitle = "Revenue Operations",
                CompanyId = cloud.Id,
                Notes = "Owns platform rollout"
            },
            cancellationToken);

        var eren = await ContactSlice.CreateAsync(
            dbContext,
            new CreateContactRequest
            {
                FirstName = "Eren",
                LastName = "Yeager",
                Email = "eren@beacon.example",
                Phone = "+1 555-0141",
                JobTitle = "Operations Director",
                CompanyId = beacon.Id,
                Notes = "Interested in multi-site deployment"
            },
            cancellationToken);

        var renewal = await DealSlice.CreateAsync(
            dbContext,
            new CreateDealRequest
            {
                Title = "Atlas Renewal",
                Description = "Annual CRM renewal with add-on seats.",
                Value = 156841m,
                Stage = DealStage.Negotiation,
                ExpectedCloseDateUtc = DateTime.UtcNow.Date.AddDays(10),
                CompanyId = atlas.Id,
                ContactId = mikasa.Id
            },
            cancellationToken);

        var rollout = await DealSlice.CreateAsync(
            dbContext,
            new CreateDealRequest
            {
                Title = "Cloud23 Rollout",
                Description = "Cross-team CRM rollout for sales and support.",
                Value = 82450m,
                Stage = DealStage.Proposal,
                ExpectedCloseDateUtc = DateTime.UtcNow.Date.AddDays(21),
                CompanyId = cloud.Id,
                ContactId = armin.Id
            },
            cancellationToken);

        var expansion = await DealSlice.CreateAsync(
            dbContext,
            new CreateDealRequest
            {
                Title = "Beacon Expansion",
                Description = "Regional expansion with workflow automation.",
                Value = 42300m,
                Stage = DealStage.Qualified,
                ExpectedCloseDateUtc = DateTime.UtcNow.Date.AddDays(30),
                CompanyId = beacon.Id,
                ContactId = eren.Id
            },
            cancellationToken);

        await TaskSlice.CreateAsync(
            dbContext,
            new CreateCrmTaskRequest
            {
                Title = "Send renewal redlines",
                Description = "Share contract redlines and pricing options.",
                DueAtUtc = DateTime.UtcNow.AddDays(-1),
                Priority = CrmTaskPriority.High,
                ContactId = mikasa.Id,
                DealId = renewal.Id
            },
            cancellationToken);

        await TaskSlice.CreateAsync(
            dbContext,
            new CreateCrmTaskRequest
            {
                Title = "Finalize rollout proposal",
                Description = "Prepare deployment timeline and onboarding plan.",
                DueAtUtc = DateTime.UtcNow.AddHours(8),
                Priority = CrmTaskPriority.High,
                ContactId = armin.Id,
                DealId = rollout.Id
            },
            cancellationToken);

        var followUpTask = await TaskSlice.CreateAsync(
            dbContext,
            new CreateCrmTaskRequest
            {
                Title = "Book expansion review",
                Description = "Confirm Beacon review call with regional ops.",
                DueAtUtc = DateTime.UtcNow.AddDays(2),
                Priority = CrmTaskPriority.Normal,
                ContactId = eren.Id,
                DealId = expansion.Id
            },
            cancellationToken);

        await TaskSlice.CompleteAsync(dbContext, followUpTask.Id, cancellationToken);
    }
}
