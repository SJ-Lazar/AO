using AO.Core.Features.Activities;
using AO.Core.Features.Companies;
using AO.Tests.Features;

namespace AO.Tests.Features.Activities;

[TestFixture]
public sealed class ActivitySliceTests
{
    [Test]
    public async Task CreateAsync_WithValidRequest_CreatesProjectedActivity()
    {
        await using var scope = await FeatureTestDbScope.CreateAsync();
        var company = new Company { Name = "Acme" };
        scope.DbContext.Companies.Add(company);
        await scope.DbContext.SaveChangesAsync();

        var result = await ActivitySlice.CreateAsync(
            scope.DbContext,
            new CreateActivityRequest
            {
                Type = CrmActivityType.CompanyCreated,
                Title = " Company created: Acme ",
                Description = " New account ",
                CompanyId = company.Id
            },
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Title, Is.EqualTo("Company created: Acme"));
            Assert.That(result.Description, Is.EqualTo("New account"));
            Assert.That(result.CompanyName, Is.EqualTo("Acme"));
            Assert.That(result.Type, Is.EqualTo(nameof(CrmActivityType.CompanyCreated)));
        });
    }

    [Test]
    public async Task GetRecentAsync_ReturnsNewestActivitiesFirst()
    {
        await using var scope = await FeatureTestDbScope.CreateAsync();
        scope.DbContext.Activities.Add(new CrmActivity
        {
            Title = "Older",
            Type = CrmActivityType.TaskCreated,
            CreatedUtc = DateTime.UtcNow.AddMinutes(-10)
        });
        scope.DbContext.Activities.Add(new CrmActivity
        {
            Title = "Newer",
            Type = CrmActivityType.TaskCompleted,
            CreatedUtc = DateTime.UtcNow
        });
        await scope.DbContext.SaveChangesAsync();

        var results = await ActivitySlice.GetRecentAsync(scope.DbContext, 10, CancellationToken.None);

        Assert.That(results.Select(activity => activity.Title).ToArray(), Is.EqualTo(new[] { "Newer", "Older" }));
    }
}
