using AO.Core.Features.Companies;
using AO.Core.Features.Contacts;
using AO.Core.Features.Deals;
using AO.Core.Features.Users;
using AO.Tests.Features;

namespace AO.Tests.Features.Deals;

[TestFixture]
public sealed class DealSliceTests
{
    [Test]
    public async Task CreateAsync_WithWonStage_MarksDealAsClosed()
    {
        await using var scope = await FeatureTestDbScope.CreateAsync();
        var user = new CrmUser { FirstName = "Levi", LastName = "Ackerman", Email = "levi@ao.example" };
        var company = new Company { Name = "Acme" };
        var contact = new Contact { FirstName = "Mikasa", LastName = "Ackerman", Company = company };
        scope.DbContext.AddRange(company, contact, user);
        await scope.DbContext.SaveChangesAsync();

        var result = await DealSlice.CreateAsync(
            scope.DbContext,
            new CreateDealRequest
            {
                Title = " Enterprise renewal ",
                Description = " Annual contract ",
                Value = 125000m,
                Stage = DealStage.Won,
                CompanyId = company.Id,
                ContactId = contact.Id,
                AssignedUserId = user.Id
            },
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Title, Is.EqualTo("Enterprise renewal"));
            Assert.That(result.Stage, Is.EqualTo(nameof(DealStage.Won)));
            Assert.That(result.IsClosed, Is.True);
            Assert.That(result.ClosedUtc, Is.Not.Null);
            Assert.That(result.CompanyName, Is.EqualTo("Acme"));
            Assert.That(result.ContactName, Is.EqualTo("Mikasa Ackerman"));
            Assert.That(result.AssignedUserName, Is.EqualTo("Levi Ackerman"));
        });
    }

    [Test]
    public async Task SetStageAsync_WhenStageBecomesLost_MarksDealAsClosed()
    {
        await using var scope = await FeatureTestDbScope.CreateAsync();
        var deal = new Deal { Title = "Expansion", Value = 42000m, Stage = DealStage.Proposal };
        scope.DbContext.Deals.Add(deal);
        await scope.DbContext.SaveChangesAsync();

        var result = await DealSlice.SetStageAsync(
            scope.DbContext,
            deal.Id,
            new SetDealStageRequest { Stage = DealStage.Lost },
            CancellationToken.None);

        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result!.Stage, Is.EqualTo(nameof(DealStage.Lost)));
            Assert.That(result.IsClosed, Is.True);
            Assert.That(result.ClosedUtc, Is.Not.Null);
        });
    }
}
