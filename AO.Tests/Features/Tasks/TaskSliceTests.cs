using AO.Core.Features.Companies;
using AO.Core.Features.Contacts;
using AO.Core.Features.Deals;
using AO.Core.Features.Tasks;
using AO.Core.Features.Users;
using AO.Tests.Features;

namespace AO.Tests.Features.Tasks;

[TestFixture]
public sealed class TaskSliceTests
{
    [Test]
    public void CreateAsync_WithUnknownDeal_ThrowsArgumentException()
    {
        Assert.That(async () =>
        {
            await using var scope = await FeatureTestDbScope.CreateAsync();
            await TaskSlice.CreateAsync(
                scope.DbContext,
                new CreateCrmTaskRequest
                {
                    Title = "Follow up",
                    DealId = Guid.NewGuid()
                },
                CancellationToken.None);
        }, Throws.InstanceOf<ArgumentException>().With.Message.EqualTo("The selected deal was not found."));
    }

    [Test]
    public async Task CompleteAsync_WithExistingTask_MarksTaskAsCompleted()
    {
        await using var scope = await FeatureTestDbScope.CreateAsync();
        var user = new CrmUser { FirstName = "Levi", LastName = "Ackerman", Email = "levi@ao.example" };
        var company = new Company { Name = "Acme" };
        var contact = new Contact { FirstName = "Armin", LastName = "Arlert", Company = company };
        var deal = new Deal { Title = "Renewal", Value = 5000m, Company = company, Contact = contact };
        var task = new CrmTask { Title = "Prepare summary", Contact = contact, Deal = deal, AssignedUser = user };

        scope.DbContext.AddRange(company, contact, deal, task, user);
        await scope.DbContext.SaveChangesAsync();

        var result = await TaskSlice.CompleteAsync(scope.DbContext, task.Id, CancellationToken.None);

        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result!.IsCompleted, Is.True);
            Assert.That(result.CompletedAtUtc, Is.Not.Null);
            Assert.That(result.ContactName, Is.EqualTo("Armin Arlert"));
            Assert.That(result.DealTitle, Is.EqualTo("Renewal"));
            Assert.That(result.AssignedUserName, Is.EqualTo("Levi Ackerman"));
        });
    }
}
