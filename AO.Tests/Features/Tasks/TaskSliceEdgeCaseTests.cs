using AO.Core.Features.Companies;
using AO.Core.Features.Contacts;
using AO.Core.Features.Deals;
using AO.Core.Features.Tasks;
using AO.Tests.Features;

namespace AO.Tests.Features.Tasks;

[TestFixture]
public sealed class TaskSliceEdgeCaseTests
{
    [Test]
    public void CreateAsync_WithContactThatDoesNotMatchDeal_ThrowsArgumentException()
    {
        Assert.That(async () =>
        {
            await using var scope = await FeatureTestDbScope.CreateAsync();
            var company = new Company { Name = "Atlas" };
            var dealContact = new Contact { FirstName = "Mikasa", LastName = "Ackerman", Company = company };
            var otherContact = new Contact { FirstName = "Armin", LastName = "Arlert", Company = company };
            var deal = new Deal { Title = "Renewal", Value = 1200m, Company = company, Contact = dealContact };

            scope.DbContext.AddRange(company, dealContact, otherContact, deal);
            await scope.DbContext.SaveChangesAsync();

            await TaskSlice.CreateAsync(
                scope.DbContext,
                new CreateCrmTaskRequest
                {
                    Title = "Follow up",
                    DealId = deal.Id,
                    ContactId = otherContact.Id
                },
                CancellationToken.None);
        }, Throws.InstanceOf<ArgumentException>().With.Message.EqualTo("The selected contact does not match the selected deal."));
    }
}
