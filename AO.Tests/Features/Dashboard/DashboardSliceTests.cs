using AO.Core.Features.Activities;
using AO.Core.Features.Companies;
using AO.Core.Features.Contacts;
using AO.Core.Features.Dashboard;
using AO.Core.Features.Deals;
using AO.Core.Features.Tasks;
using AO.Tests.Features;

namespace AO.Tests.Features.Dashboard;

[TestFixture]
public sealed class DashboardSliceTests
{
    [Test]
    public async Task GetAsync_ReturnsAggregatedCrmSummary()
    {
        await using var scope = await FeatureTestDbScope.CreateAsync();
        var utcNow = DateTime.UtcNow;
        var company = new Company { Name = "Acme" };
        var contact = new Contact { FirstName = "Mikasa", LastName = "Ackerman", Company = company };
        var openDeal = new Deal { Title = "Renewal", Value = 125000m, Stage = DealStage.Negotiation, Company = company, Contact = contact, IsClosed = false };
        var closedDeal = new Deal { Title = "Upsell", Value = 25000m, Stage = DealStage.Won, Company = company, Contact = contact, IsClosed = true, ClosedUtc = utcNow };
        var overdueTask = new CrmTask { Title = "Follow up", Deal = openDeal, Contact = contact, DueAtUtc = utcNow.AddDays(-1) };
        var dueTodayTask = new CrmTask { Title = "Send proposal", Deal = openDeal, Contact = contact, DueAtUtc = utcNow.Date.AddHours(23) };
        var doneTask = new CrmTask { Title = "Archive note", Deal = closedDeal, Contact = contact, IsCompleted = true, CompletedAtUtc = utcNow };
        var activity = new CrmActivity { Title = "Deal stage changed", Type = CrmActivityType.DealStageChanged, Deal = openDeal, CreatedUtc = utcNow };

        scope.DbContext.AddRange(company, contact, openDeal, closedDeal, overdueTask, dueTodayTask, doneTask, activity);
        await scope.DbContext.SaveChangesAsync();

        var result = await DashboardSlice.GetAsync(scope.DbContext, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.TotalCompanies, Is.EqualTo(1));
            Assert.That(result.TotalContacts, Is.EqualTo(1));
            Assert.That(result.OpenDealCount, Is.EqualTo(1));
            Assert.That(result.ClosedDealCount, Is.EqualTo(1));
            Assert.That(result.OpenPipelineValue, Is.EqualTo(125000m));
            Assert.That(result.TotalTaskCount, Is.EqualTo(3));
            Assert.That(result.OpenTaskCount, Is.EqualTo(2));
            Assert.That(result.OverdueTaskCount, Is.EqualTo(1));
            Assert.That(result.TasksDueTodayCount, Is.EqualTo(1));
            Assert.That(result.RecentActivityCount, Is.EqualTo(1));
            Assert.That(result.PipelineStages.Single(stage => stage.Stage == nameof(DealStage.Negotiation)).Count, Is.EqualTo(1));
            Assert.That(result.PipelineStages.Single(stage => stage.Stage == nameof(DealStage.Won)).Value, Is.EqualTo(25000m));
        });
    }
}
