using AO.Core.Features.Companies;
using AO.Core.Features.Contacts;
using AO.Core.Features.Deals;
using AO.Tests.Features;

namespace AO.Tests.Features.Companies;

[TestFixture]
public sealed class CompanySliceTests
{
    [Test]
    public async Task CreateAsync_WithValidRequest_CreatesCompany()
    {
        await using var scope = await FeatureTestDbScope.CreateAsync();

        var result = await CompanySlice.CreateAsync(
            scope.DbContext,
            new CreateCompanyRequest
            {
                Name = "  Acme Corp  ",
                Industry = "  Software  ",
                Website = " https://acme.example ",
                Phone = " 12345 ",
                Notes = " Important account "
            },
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Id, Is.Not.EqualTo(Guid.Empty));
            Assert.That(result.Name, Is.EqualTo("Acme Corp"));
            Assert.That(result.Industry, Is.EqualTo("Software"));
            Assert.That(result.Website, Is.EqualTo("https://acme.example"));
            Assert.That(result.Phone, Is.EqualTo("12345"));
            Assert.That(result.Notes, Is.EqualTo("Important account"));
            Assert.That(result.ContactCount, Is.EqualTo(0));
            Assert.That(result.OpenDealCount, Is.EqualTo(0));
        });
    }

    [Test]
    public async Task GetAllAsync_ProjectsCountsFromRelatedContactsAndDeals()
    {
        await using var scope = await FeatureTestDbScope.CreateAsync();
        var company = new Company { Name = "Mikasa" };
        scope.DbContext.Companies.Add(company);
        scope.DbContext.Contacts.Add(new Contact { FirstName = "Eren", LastName = "Yeager", Company = company });
        scope.DbContext.Deals.AddRange(
            new Deal { Title = "Open", Value = 10, Company = company, Stage = DealStage.Qualified, IsClosed = false },
            new Deal { Title = "Closed", Value = 20, Company = company, Stage = DealStage.Won, IsClosed = true, ClosedUtc = DateTime.UtcNow });
        await scope.DbContext.SaveChangesAsync();

        var results = await CompanySlice.GetAllAsync(scope.DbContext, CancellationToken.None);
        var result = results.Single();

        Assert.Multiple(() =>
        {
            Assert.That(result.Name, Is.EqualTo("Mikasa"));
            Assert.That(result.ContactCount, Is.EqualTo(1));
            Assert.That(result.OpenDealCount, Is.EqualTo(1));
        });
    }
}
