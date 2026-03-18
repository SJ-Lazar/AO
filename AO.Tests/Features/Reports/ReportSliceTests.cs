using System.Text;
using AO.Core.Features.Activities;
using AO.Core.Features.Companies;
using AO.Core.Features.Contacts;
using AO.Core.Features.Deals;
using AO.Core.Features.Reports;
using AO.Core.Features.Tasks;
using AO.Tests.Features;

namespace AO.Tests.Features.Reports;

[TestFixture]
public sealed class ReportSliceTests
{
    [Test]
    public async Task GetAsync_AppliesFiltersAndReturnsReportDetails()
    {
        await using var scope = await FeatureTestDbScope.CreateAsync();
        var utcNow = DateTime.UtcNow;
        var companyA = new Company { Name = "Acme" };
        var companyB = new Company { Name = "Globex" };
        var contactA = new Contact { FirstName = "Mikasa", LastName = "Ackerman", Company = companyA };
        var contactB = new Contact { FirstName = "Levi", LastName = "Ackerman", Company = companyB };
        var matchingDeal = new Deal
        {
            Title = "Renewal",
            Value = 125000m,
            Stage = DealStage.Negotiation,
            Company = companyA,
            Contact = contactA,
            CreatedUtc = utcNow.AddDays(-3),
            UpdatedUtc = utcNow.AddDays(-1)
        };
        var otherDeal = new Deal
        {
            Title = "Expansion",
            Value = 95000m,
            Stage = DealStage.Won,
            Company = companyB,
            Contact = contactB,
            IsClosed = true,
            ClosedUtc = utcNow.AddDays(-12),
            CreatedUtc = utcNow.AddDays(-15),
            UpdatedUtc = utcNow.AddDays(-12)
        };
        var matchingTask = new CrmTask
        {
            Title = "Send proposal",
            Deal = matchingDeal,
            Contact = contactA,
            DueAtUtc = utcNow.Date.AddHours(12),
            CreatedUtc = utcNow.AddDays(-2),
            UpdatedUtc = utcNow.AddDays(-2)
        };
        var otherTask = new CrmTask
        {
            Title = "Archive note",
            Deal = otherDeal,
            Contact = contactB,
            IsCompleted = true,
            CompletedAtUtc = utcNow.AddDays(-10),
            CreatedUtc = utcNow.AddDays(-14),
            UpdatedUtc = utcNow.AddDays(-10)
        };
        var matchingActivity = new CrmActivity
        {
            Title = "Deal stage changed",
            Type = CrmActivityType.DealStageChanged,
            Deal = matchingDeal,
            Contact = contactA,
            CreatedUtc = utcNow.AddDays(-1)
        };
        var otherActivity = new CrmActivity
        {
            Title = "Task completed",
            Type = CrmActivityType.TaskCompleted,
            Deal = otherDeal,
            Contact = contactB,
            Task = otherTask,
            CreatedUtc = utcNow.AddDays(-10)
        };

        scope.DbContext.AddRange(companyA, companyB, contactA, contactB, matchingDeal, otherDeal, matchingTask, otherTask, matchingActivity, otherActivity);
        await scope.DbContext.SaveChangesAsync();

        var result = await ReportSlice.GetAsync(
            scope.DbContext,
            new GetReportsRequest
            {
                FromUtc = utcNow.AddDays(-7),
                ToUtc = utcNow,
                CompanyId = companyA.Id,
                Stage = DealStage.Negotiation
            },
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Summary.OpenDealCount, Is.EqualTo(1));
            Assert.That(result.Summary.ClosedDealCount, Is.EqualTo(0));
            Assert.That(result.Summary.OpenPipelineValue, Is.EqualTo(125000m));
            Assert.That(result.Summary.TotalTaskCount, Is.EqualTo(1));
            Assert.That(result.Summary.TasksDueTodayCount, Is.EqualTo(1));
            Assert.That(result.Summary.RecentActivityCount, Is.EqualTo(1));
            Assert.That(result.PipelineStages.Single(stage => stage.Stage == nameof(DealStage.Negotiation)).Count, Is.EqualTo(1));
            Assert.That(result.TopCompanies.Select(company => company.Name), Is.EqualTo(new[] { "Acme" }));
            Assert.That(result.Deals.Select(deal => deal.Title), Is.EqualTo(new[] { "Renewal" }));
            Assert.That(result.Tasks.Select(task => task.Title), Is.EqualTo(new[] { "Send proposal" }));
            Assert.That(result.Activities.Select(activity => activity.Title), Is.EqualTo(new[] { "Deal stage changed" }));
            Assert.That(result.ActivityBreakdown.Single(item => item.Type == nameof(CrmActivityType.DealStageChanged)).Count, Is.EqualTo(1));
            Assert.That(result.DealTrends, Has.Count.EqualTo(6));
            Assert.That(result.TaskTrends, Has.Count.EqualTo(6));
        });
    }

    [Test]
    public async Task ExportAsync_ReturnsCsvPayload()
    {
        await using var scope = await FeatureTestDbScope.CreateAsync();
        var company = new Company { Name = "Acme" };
        var contact = new Contact { FirstName = "Armin", LastName = "Arlert", Company = company };
        var deal = new Deal
        {
            Title = "Renewal",
            Value = 25000m,
            Stage = DealStage.Qualified,
            Company = company,
            Contact = contact
        };

        scope.DbContext.AddRange(company, contact, deal);
        await scope.DbContext.SaveChangesAsync();

        var export = await ReportSlice.ExportAsync(scope.DbContext, new GetReportsRequest(), CancellationToken.None);
        var csv = Encoding.UTF8.GetString(Convert.FromBase64String(export.ContentBase64));

        Assert.Multiple(() =>
        {
            Assert.That(export.FileName, Does.EndWith(".csv"));
            Assert.That(export.ContentType, Is.EqualTo("text/csv"));
            Assert.That(csv, Does.Contain("\"Summary\",\"Open deals\",\"1\",\"\""));
            Assert.That(csv, Does.Contain("\"Deal\",\"Renewal\",\"Qualified\",\"25000\""));
        });
    }
}
