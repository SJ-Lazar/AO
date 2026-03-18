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
        var dealsQuery = dbContext.Deals.AsNoTracking();
        var tasksQuery = dbContext.Tasks.AsNoTracking();
        var totalCompanies = await dbContext.Companies.CountAsync(cancellationToken);
        var totalContacts = await dbContext.Contacts.CountAsync(cancellationToken);
        var openDealCount = await dealsQuery.CountAsync(deal => !deal.IsClosed, cancellationToken);
        var closedDealCount = await dealsQuery.CountAsync(deal => deal.IsClosed, cancellationToken);
        var openPipelineValue = await dealsQuery.Where(deal => !deal.IsClosed).Select(deal => (decimal?)deal.Value).SumAsync(cancellationToken) ?? 0m;
        var totalTaskCount = await tasksQuery.CountAsync(cancellationToken);
        var openTaskCount = await tasksQuery.CountAsync(task => !task.IsCompleted, cancellationToken);
        var overdueTaskCount = await tasksQuery.CountAsync(task => !task.IsCompleted && task.DueAtUtc.HasValue && task.DueAtUtc.Value.Date < today, cancellationToken);
        var tasksDueTodayCount = await tasksQuery.CountAsync(task => !task.IsCompleted && task.DueAtUtc.HasValue && task.DueAtUtc.Value.Date == today, cancellationToken);
        var recentActivityCount = await dbContext.Activities.CountAsync(activity => activity.CreatedUtc >= utcNow.AddDays(-7), cancellationToken);

        var pipelineStages = Enum.GetValues<DealStage>()
            .Select(async stage => new PipelineStageSummaryDto
            {
                Stage = stage.ToString(),
                Count = await dealsQuery.CountAsync(deal => deal.Stage == stage, cancellationToken),
                Value = await dealsQuery.Where(deal => deal.Stage == stage).Select(deal => (decimal?)deal.Value).SumAsync(cancellationToken) ?? 0m
            })
            .ToList();

        var resolvedPipelineStages = new List<PipelineStageSummaryDto>(pipelineStages.Count);
        foreach (var pipelineStageTask in pipelineStages)
        {
            resolvedPipelineStages.Add(await pipelineStageTask);
        }

        return new DashboardSummaryDto
        {
            TotalCompanies = totalCompanies,
            TotalContacts = totalContacts,
            OpenDealCount = openDealCount,
            ClosedDealCount = closedDealCount,
            OpenPipelineValue = openPipelineValue,
            TotalTaskCount = totalTaskCount,
            OpenTaskCount = openTaskCount,
            OverdueTaskCount = overdueTaskCount,
            TasksDueTodayCount = tasksDueTodayCount,
            RecentActivityCount = recentActivityCount,
            PipelineStages = resolvedPipelineStages
        };
    }
}
