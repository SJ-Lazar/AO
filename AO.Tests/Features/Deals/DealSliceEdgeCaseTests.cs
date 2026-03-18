using AO.Core.Features.Companies;
using AO.Core.Features.Contacts;
using AO.Core.Features.Deals;
using AO.Tests.Features;

namespace AO.Tests.Features.Deals;

[TestFixture]
public sealed class DealSliceEdgeCaseTests
{
    [Test]
    public void CreateAsync_WithContactFromDifferentCompany_ThrowsArgumentException()
    {
        Assert.That(async () =>
        {
            await using var scope = await FeatureTestDbScope.CreateAsync();
            var companyA = new Company { Name = "Atlas" };
            var companyB = new Company { Name = "Beacon" };
            var contact = new Contact { FirstName = "Mikasa", LastName = "Ackerman", Company = companyA };

            scope.DbContext.AddRange(companyA, companyB, contact);
            await scope.DbContext.SaveChangesAsync();

            await DealSlice.CreateAsync(
                scope.DbContext,
                new CreateDealRequest
                {
                    Title = "Mismatch",
                    Value = 1000m,
                    CompanyId = companyB.Id,
                    ContactId = contact.Id
                },
                CancellationToken.None);
        }, Throws.InstanceOf<ArgumentException>().With.Message.EqualTo("The selected contact does not belong to the selected company."));
    }
}
