using AO.Core.Features.Activities;
using AO.Core.Features.Deals;
using AO.Core.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AO.Core.Features.Dashboard;

public sealed record PipelineStageSummaryDto
{
    public string Stage { get; init; } = string.Empty;
    public int Count { get; init; }
    public decimal Value { get; init; }
}

public sealed record DashboardSummaryDto
{
    public int TotalCompanies { get; init; }
    public int TotalContacts { get; init; }
    public int OpenDealCount { get; init; }
    public int ClosedDealCount { get; init; }
    public decimal OpenPipelineValue { get; init; }
    public int TotalTaskCount { get; init; }
    public int OpenTaskCount { get; init; }
    public int OverdueTaskCount { get; init; }
    public int TasksDueTodayCount { get; init; }
    public int RecentActivityCount { get; init; }
    public IReadOnlyList<PipelineStageSummaryDto> PipelineStages { get; init; } = [];
}

public static class DashboardSlice
{
    public static async Task<DashboardSummaryDto> GetAsync(AOContext dbContext, CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;
        var today = utcNow.Date;
        var deals = await dbContext.Deals.AsNoTracking().ToListAsync(cancellationToken);
        var tasks = await dbContext.Tasks.AsNoTracking().ToListAsync(cancellationToken);
        var totalCompaniesTask = dbContext.Companies.CountAsync(cancellationToken);
        var totalContactsTask = dbContext.Contacts.CountAsync(cancellationToken);
        var recentActivityCountTask = dbContext.Activities.CountAsync(activity => activity.CreatedUtc >= utcNow.AddDays(-7), cancellationToken);

        await Task.WhenAll(totalCompaniesTask, totalContactsTask, recentActivityCountTask);

        var pipelineStages = Enum.GetValues<DealStage>()
            .Select(stage => new PipelineStageSummaryDto
            {
                Stage = stage.ToString(),
                Count = deals.Count(deal => deal.Stage == stage),
                Value = deals.Where(deal => deal.Stage == stage).Sum(deal => deal.Value)
            })
            .ToList();

        return new DashboardSummaryDto
        {
            TotalCompanies = totalCompaniesTask.Result,
            TotalContacts = totalContactsTask.Result,
            OpenDealCount = deals.Count(deal => !deal.IsClosed),
            ClosedDealCount = deals.Count(deal => deal.IsClosed),
            OpenPipelineValue = deals.Where(deal => !deal.IsClosed).Sum(deal => deal.Value),
            TotalTaskCount = tasks.Count,
            OpenTaskCount = tasks.Count(task => !task.IsCompleted),
            OverdueTaskCount = tasks.Count(task => !task.IsCompleted && task.DueAtUtc.HasValue && task.DueAtUtc.Value.Date < today),
            TasksDueTodayCount = tasks.Count(task => !task.IsCompleted && task.DueAtUtc.HasValue && task.DueAtUtc.Value.Date == today),
            RecentActivityCount = recentActivityCountTask.Result,
            PipelineStages = pipelineStages
        };
    }
}
