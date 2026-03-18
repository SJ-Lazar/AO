using System.Globalization;
using System.Text;
using AO.Core.Features.Activities;
using AO.Core.Features.Companies;
using AO.Core.Features.Contacts;
using AO.Core.Features.Deals;
using AO.Core.Features.Tasks;
using AO.Core.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AO.Core.Features.Reports;

public sealed class GetReportsRequest
{
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
    public Guid? CompanyId { get; set; }
    public DealStage? Stage { get; set; }
}

public sealed record ReportSummaryDto
{
    public int OpenDealCount { get; init; }
    public int ClosedDealCount { get; init; }
    public int WonDealCount { get; init; }
    public int LostDealCount { get; init; }
    public decimal OpenPipelineValue { get; init; }
    public decimal ClosedRevenueValue { get; init; }
    public decimal AverageDealValue { get; init; }
    public decimal WinRate { get; init; }
    public int TotalTaskCount { get; init; }
    public int OpenTaskCount { get; init; }
    public int OverdueTaskCount { get; init; }
    public int TasksDueTodayCount { get; init; }
    public int RecentActivityCount { get; init; }
}

public sealed record ReportPipelineStageDto
{
    public string Stage { get; init; } = string.Empty;
    public int Count { get; init; }
    public decimal Value { get; init; }
}

public sealed record ReportDealTrendPointDto
{
    public string Label { get; init; } = string.Empty;
    public int Count { get; init; }
    public decimal Value { get; init; }
}

public sealed record ReportTaskTrendPointDto
{
    public string Label { get; init; } = string.Empty;
    public int CreatedCount { get; init; }
    public int CompletedCount { get; init; }
}

public sealed record ReportActivityBreakdownDto
{
    public string Type { get; init; } = string.Empty;
    public int Count { get; init; }
}

public sealed record ReportCompanySummaryDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Industry { get; init; }
    public int ContactCount { get; init; }
    public int OpenDealCount { get; init; }
    public decimal OpenPipelineValue { get; init; }
}

public sealed record ReportDealSummaryDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Stage { get; init; } = string.Empty;
    public decimal Value { get; init; }
    public bool IsClosed { get; init; }
    public string? CompanyName { get; init; }
    public string? ContactName { get; init; }
    public DateTime CreatedUtc { get; init; }
    public DateTime UpdatedUtc { get; init; }
}

public sealed record ReportTaskSummaryDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Priority { get; init; } = string.Empty;
    public bool IsCompleted { get; init; }
    public DateTime? DueAtUtc { get; init; }
    public string? CompanyName { get; init; }
    public string? ContactName { get; init; }
    public string? DealTitle { get; init; }
    public DateTime CreatedUtc { get; init; }
}

public sealed record ReportActivitySummaryDto
{
    public Guid Id { get; init; }
    public string Type { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? CompanyName { get; init; }
    public string? ContactName { get; init; }
    public string? DealTitle { get; init; }
    public string? TaskTitle { get; init; }
    public DateTime CreatedUtc { get; init; }
}

public sealed record ReportSnapshotDto
{
    public GetReportsRequest Filters { get; init; } = new();
    public ReportSummaryDto Summary { get; init; } = new();
    public IReadOnlyList<ReportPipelineStageDto> PipelineStages { get; init; } = [];
    public IReadOnlyList<ReportDealTrendPointDto> DealTrends { get; init; } = [];
    public IReadOnlyList<ReportTaskTrendPointDto> TaskTrends { get; init; } = [];
    public IReadOnlyList<ReportActivityBreakdownDto> ActivityBreakdown { get; init; } = [];
    public IReadOnlyList<ReportCompanySummaryDto> TopCompanies { get; init; } = [];
    public IReadOnlyList<ReportDealSummaryDto> Deals { get; init; } = [];
    public IReadOnlyList<ReportTaskSummaryDto> Tasks { get; init; } = [];
    public IReadOnlyList<ReportActivitySummaryDto> Activities { get; init; } = [];
}

public sealed record ReportExportDto
{
    public string FileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = "text/csv";
    public string ContentBase64 { get; init; } = string.Empty;
}

public static class ReportSlice
{
    private const int TrendPeriods = 6;
    private const int DetailTake = 8;
    private const int ActivityTake = 12;

    public static async Task<ReportSnapshotDto> GetAsync(AOContext dbContext, GetReportsRequest? request, CancellationToken cancellationToken)
    {
        var normalizedRequest = NormalizeRequest(request);
        var utcNow = DateTime.UtcNow;
        var today = utcNow.Date;
        var dealsQuery = ApplyDealFilters(dbContext.Deals.AsNoTracking(), normalizedRequest);
        var tasksQuery = ApplyTaskFilters(dbContext.Tasks.AsNoTracking(), normalizedRequest);
        var activitiesQuery = ApplyActivityFilters(dbContext.Activities.AsNoTracking(), normalizedRequest);

        var totalDealCount = await dealsQuery.CountAsync(cancellationToken);
        var closedDealCount = await dealsQuery.CountAsync(deal => deal.IsClosed, cancellationToken);
        var openDealCount = totalDealCount - closedDealCount;
        var wonDealCount = await dealsQuery.CountAsync(deal => deal.Stage == DealStage.Won, cancellationToken);
        var lostDealCount = await dealsQuery.CountAsync(deal => deal.Stage == DealStage.Lost, cancellationToken);
        var openPipelineValue = await SumDealValueAsync(dealsQuery.Where(deal => !deal.IsClosed), cancellationToken);
        var closedRevenueValue = await SumDealValueAsync(dealsQuery.Where(deal => deal.IsClosed), cancellationToken);
        var totalDealValue = await SumDealValueAsync(dealsQuery, cancellationToken);
        var totalTaskCount = await tasksQuery.CountAsync(cancellationToken);
        var openTaskCount = await tasksQuery.CountAsync(task => !task.IsCompleted, cancellationToken);
        var overdueTaskCount = await tasksQuery.CountAsync(task => !task.IsCompleted && task.DueAtUtc.HasValue && task.DueAtUtc.Value.Date < today, cancellationToken);
        var tasksDueTodayCount = await tasksQuery.CountAsync(task => !task.IsCompleted && task.DueAtUtc.HasValue && task.DueAtUtc.Value.Date == today, cancellationToken);
        var recentActivityCount = await activitiesQuery.CountAsync(activity => activity.CreatedUtc >= utcNow.AddDays(-7), cancellationToken);

        var pipelineStages = new List<ReportPipelineStageDto>();
        foreach (var stage in Enum.GetValues<DealStage>())
        {
            var stageQuery = dealsQuery.Where(deal => deal.Stage == stage);
            pipelineStages.Add(new ReportPipelineStageDto
            {
                Stage = stage.ToString(),
                Count = await stageQuery.CountAsync(cancellationToken),
                Value = await SumDealValueAsync(stageQuery, cancellationToken)
            });
        }

        var topCompanies = await BuildTopCompaniesAsync(dbContext, normalizedRequest, cancellationToken);
        var dealTrends = await BuildDealTrendsAsync(dealsQuery, normalizedRequest, utcNow, cancellationToken);
        var taskTrends = await BuildTaskTrendsAsync(tasksQuery, normalizedRequest, utcNow, cancellationToken);
        var activityBreakdown = await activitiesQuery
            .GroupBy(activity => activity.Type)
            .Select(group => new ReportActivityBreakdownDto
            {
                Type = group.Key.ToString(),
                Count = group.Count()
            })
            .OrderByDescending(item => item.Count)
            .ThenBy(item => item.Type)
            .ToListAsync(cancellationToken);

        var deals = await dealsQuery
            .OrderByDescending(deal => deal.UpdatedUtc)
            .Take(DetailTake)
            .Select(deal => new ReportDealSummaryDto
            {
                Id = deal.Id,
                Title = deal.Title,
                Stage = deal.Stage.ToString(),
                Value = deal.Value,
                IsClosed = deal.IsClosed,
                CompanyName = deal.Company != null ? deal.Company.Name : null,
                ContactName = deal.Contact != null ? deal.Contact.FirstName + " " + deal.Contact.LastName : null,
                CreatedUtc = deal.CreatedUtc,
                UpdatedUtc = deal.UpdatedUtc
            })
            .ToListAsync(cancellationToken);

        var tasks = await tasksQuery
            .OrderBy(task => task.IsCompleted)
            .ThenBy(task => task.DueAtUtc)
            .ThenByDescending(task => task.CreatedUtc)
            .Take(DetailTake)
            .Select(task => new ReportTaskSummaryDto
            {
                Id = task.Id,
                Title = task.Title,
                Priority = task.Priority.ToString(),
                IsCompleted = task.IsCompleted,
                DueAtUtc = task.DueAtUtc,
                CompanyName = task.Deal != null && task.Deal.Company != null
                    ? task.Deal.Company.Name
                    : task.Contact != null && task.Contact.Company != null
                        ? task.Contact.Company.Name
                        : null,
                ContactName = task.Contact != null ? task.Contact.FirstName + " " + task.Contact.LastName : null,
                DealTitle = task.Deal != null ? task.Deal.Title : null,
                CreatedUtc = task.CreatedUtc
            })
            .ToListAsync(cancellationToken);

        var activities = await activitiesQuery
            .OrderByDescending(activity => activity.CreatedUtc)
            .Take(ActivityTake)
            .Select(activity => new ReportActivitySummaryDto
            {
                Id = activity.Id,
                Type = activity.Type.ToString(),
                Title = activity.Title,
                Description = activity.Description,
                CompanyName = activity.Company != null
                    ? activity.Company.Name
                    : activity.Deal != null && activity.Deal.Company != null
                        ? activity.Deal.Company.Name
                        : activity.Contact != null && activity.Contact.Company != null
                            ? activity.Contact.Company.Name
                            : null,
                ContactName = activity.Contact != null ? activity.Contact.FirstName + " " + activity.Contact.LastName : null,
                DealTitle = activity.Deal != null ? activity.Deal.Title : null,
                TaskTitle = activity.Task != null ? activity.Task.Title : null,
                CreatedUtc = activity.CreatedUtc
            })
            .ToListAsync(cancellationToken);

        return new ReportSnapshotDto
        {
            Filters = new GetReportsRequest
            {
                FromUtc = normalizedRequest.FromUtc,
                ToUtc = normalizedRequest.ToUtc,
                CompanyId = normalizedRequest.CompanyId,
                Stage = normalizedRequest.Stage
            },
            Summary = new ReportSummaryDto
            {
                OpenDealCount = openDealCount,
                ClosedDealCount = closedDealCount,
                WonDealCount = wonDealCount,
                LostDealCount = lostDealCount,
                OpenPipelineValue = openPipelineValue,
                ClosedRevenueValue = closedRevenueValue,
                AverageDealValue = totalDealCount == 0 ? 0m : Math.Round(totalDealValue / totalDealCount, 2),
                WinRate = closedDealCount == 0 ? 0m : Math.Round((decimal)wonDealCount / closedDealCount * 100m, 2),
                TotalTaskCount = totalTaskCount,
                OpenTaskCount = openTaskCount,
                OverdueTaskCount = overdueTaskCount,
                TasksDueTodayCount = tasksDueTodayCount,
                RecentActivityCount = recentActivityCount
            },
            PipelineStages = pipelineStages,
            DealTrends = dealTrends,
            TaskTrends = taskTrends,
            ActivityBreakdown = activityBreakdown,
            TopCompanies = topCompanies,
            Deals = deals,
            Tasks = tasks,
            Activities = activities
        };
    }

    public static async Task<ReportExportDto> ExportAsync(AOContext dbContext, GetReportsRequest? request, CancellationToken cancellationToken)
    {
        var report = await GetAsync(dbContext, request, cancellationToken);
        var builder = new StringBuilder();

        builder.AppendLine("Section,Label,Value,Extra");
        builder.AppendLine(CsvRow("Summary", "Open deals", report.Summary.OpenDealCount.ToString(CultureInfo.InvariantCulture), string.Empty));
        builder.AppendLine(CsvRow("Summary", "Closed deals", report.Summary.ClosedDealCount.ToString(CultureInfo.InvariantCulture), string.Empty));
        builder.AppendLine(CsvRow("Summary", "Open pipeline", FormatDecimalForCsv(report.Summary.OpenPipelineValue), string.Empty));
        builder.AppendLine(CsvRow("Summary", "Closed revenue", FormatDecimalForCsv(report.Summary.ClosedRevenueValue), string.Empty));
        builder.AppendLine(CsvRow("Summary", "Average deal value", FormatDecimalForCsv(report.Summary.AverageDealValue), string.Empty));
        builder.AppendLine(CsvRow("Summary", "Win rate", report.Summary.WinRate.ToString(CultureInfo.InvariantCulture), "%"));
        builder.AppendLine(CsvRow("Summary", "Open tasks", report.Summary.OpenTaskCount.ToString(CultureInfo.InvariantCulture), string.Empty));
        builder.AppendLine(CsvRow("Summary", "Overdue tasks", report.Summary.OverdueTaskCount.ToString(CultureInfo.InvariantCulture), string.Empty));
        builder.AppendLine(CsvRow("Summary", "Recent activity", report.Summary.RecentActivityCount.ToString(CultureInfo.InvariantCulture), string.Empty));

        foreach (var stage in report.PipelineStages)
        {
            builder.AppendLine(CsvRow("Pipeline", stage.Stage, stage.Count.ToString(CultureInfo.InvariantCulture), FormatDecimalForCsv(stage.Value)));
        }

        foreach (var company in report.TopCompanies)
        {
            builder.AppendLine(CsvRow("Company", company.Name, company.OpenDealCount.ToString(CultureInfo.InvariantCulture), FormatDecimalForCsv(company.OpenPipelineValue)));
        }

        foreach (var deal in report.Deals)
        {
            builder.AppendLine(CsvRow("Deal", deal.Title, deal.Stage, FormatDecimalForCsv(deal.Value)));
        }

        foreach (var task in report.Tasks)
        {
            builder.AppendLine(CsvRow("Task", task.Title, task.Priority, task.IsCompleted ? "Done" : "Open"));
        }

        foreach (var activity in report.Activities)
        {
            builder.AppendLine(CsvRow("Activity", activity.Title, activity.Type, activity.CreatedUtc.ToString("O", CultureInfo.InvariantCulture)));
        }

        return new ReportExportDto
        {
            FileName = $"ao-reports-{DateTime.UtcNow:yyyyMMddHHmmss}.csv",
            ContentType = "text/csv",
            ContentBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(builder.ToString()))
        };
    }

    private static async Task<IReadOnlyList<ReportCompanySummaryDto>> BuildTopCompaniesAsync(AOContext dbContext, GetReportsRequest request, CancellationToken cancellationToken)
    {
        var fromUtc = request.FromUtc?.Date;
        var toExclusiveUtc = request.ToUtc?.Date.AddDays(1);
        var companiesQuery = dbContext.Companies.AsNoTracking();

        if (request.CompanyId.HasValue)
        {
            companiesQuery = companiesQuery.Where(company => company.Id == request.CompanyId.Value);
        }

        return await companiesQuery
            .Select(company => new ReportCompanySummaryDto
            {
                Id = company.Id,
                Name = company.Name,
                Industry = company.Industry,
                ContactCount = company.Contacts.Count(),
                OpenDealCount = company.Deals.Count(deal =>
                    !deal.IsClosed
                    && (!request.Stage.HasValue || deal.Stage == request.Stage.Value)
                    && (!fromUtc.HasValue || deal.CreatedUtc >= fromUtc.Value)
                    && (!toExclusiveUtc.HasValue || deal.CreatedUtc < toExclusiveUtc.Value)),
                OpenPipelineValue = company.Deals
                    .Where(deal =>
                        !deal.IsClosed
                        && (!request.Stage.HasValue || deal.Stage == request.Stage.Value)
                        && (!fromUtc.HasValue || deal.CreatedUtc >= fromUtc.Value)
                        && (!toExclusiveUtc.HasValue || deal.CreatedUtc < toExclusiveUtc.Value))
                    .Select(deal => (decimal?)deal.Value)
                    .Sum() ?? 0m
            })
            .Where(company => company.ContactCount > 0 || company.OpenDealCount > 0)
            .OrderByDescending(company => company.OpenDealCount)
            .ThenByDescending(company => company.OpenPipelineValue)
            .ThenBy(company => company.Name)
            .Take(6)
            .ToListAsync(cancellationToken);
    }

    private static async Task<IReadOnlyList<ReportDealTrendPointDto>> BuildDealTrendsAsync(IQueryable<Deal> dealsQuery, GetReportsRequest request, DateTime utcNow, CancellationToken cancellationToken)
    {
        var periods = GetMonthlyPeriods(request, utcNow);
        var results = new List<ReportDealTrendPointDto>(periods.Count);

        foreach (var period in periods)
        {
            var periodQuery = dealsQuery.Where(deal => deal.CreatedUtc >= period.StartUtc && deal.CreatedUtc < period.EndUtc);
            results.Add(new ReportDealTrendPointDto
            {
                Label = period.Label,
                Count = await periodQuery.CountAsync(cancellationToken),
                Value = await SumDealValueAsync(periodQuery, cancellationToken)
            });
        }

        return results;
    }

    private static async Task<IReadOnlyList<ReportTaskTrendPointDto>> BuildTaskTrendsAsync(IQueryable<CrmTask> tasksQuery, GetReportsRequest request, DateTime utcNow, CancellationToken cancellationToken)
    {
        var periods = GetMonthlyPeriods(request, utcNow);
        var results = new List<ReportTaskTrendPointDto>(periods.Count);

        foreach (var period in periods)
        {
            results.Add(new ReportTaskTrendPointDto
            {
                Label = period.Label,
                CreatedCount = await tasksQuery.CountAsync(task => task.CreatedUtc >= period.StartUtc && task.CreatedUtc < period.EndUtc, cancellationToken),
                CompletedCount = await tasksQuery.CountAsync(task => task.CompletedAtUtc.HasValue && task.CompletedAtUtc.Value >= period.StartUtc && task.CompletedAtUtc.Value < period.EndUtc, cancellationToken)
            });
        }

        return results;
    }

    private static IQueryable<Deal> ApplyDealFilters(IQueryable<Deal> query, GetReportsRequest request)
    {
        var fromUtc = request.FromUtc?.Date;
        var toExclusiveUtc = request.ToUtc?.Date.AddDays(1);

        if (fromUtc.HasValue)
        {
            query = query.Where(deal => deal.CreatedUtc >= fromUtc.Value);
        }

        if (toExclusiveUtc.HasValue)
        {
            query = query.Where(deal => deal.CreatedUtc < toExclusiveUtc.Value);
        }

        if (request.CompanyId.HasValue)
        {
            query = query.Where(deal => deal.CompanyId == request.CompanyId.Value);
        }

        if (request.Stage.HasValue)
        {
            query = query.Where(deal => deal.Stage == request.Stage.Value);
        }

        return query;
    }

    private static IQueryable<CrmTask> ApplyTaskFilters(IQueryable<CrmTask> query, GetReportsRequest request)
    {
        var fromUtc = request.FromUtc?.Date;
        var toExclusiveUtc = request.ToUtc?.Date.AddDays(1);

        if (fromUtc.HasValue)
        {
            query = query.Where(task => task.CreatedUtc >= fromUtc.Value);
        }

        if (toExclusiveUtc.HasValue)
        {
            query = query.Where(task => task.CreatedUtc < toExclusiveUtc.Value);
        }

        if (request.CompanyId.HasValue)
        {
            query = query.Where(task =>
                task.Deal != null && task.Deal.CompanyId == request.CompanyId.Value
                || task.Contact != null && task.Contact.CompanyId == request.CompanyId.Value);
        }

        if (request.Stage.HasValue)
        {
            query = query.Where(task => task.Deal != null && task.Deal.Stage == request.Stage.Value);
        }

        return query;
    }

    private static IQueryable<CrmActivity> ApplyActivityFilters(IQueryable<CrmActivity> query, GetReportsRequest request)
    {
        var fromUtc = request.FromUtc?.Date;
        var toExclusiveUtc = request.ToUtc?.Date.AddDays(1);

        if (fromUtc.HasValue)
        {
            query = query.Where(activity => activity.CreatedUtc >= fromUtc.Value);
        }

        if (toExclusiveUtc.HasValue)
        {
            query = query.Where(activity => activity.CreatedUtc < toExclusiveUtc.Value);
        }

        if (request.CompanyId.HasValue)
        {
            query = query.Where(activity =>
                activity.CompanyId == request.CompanyId.Value
                || activity.Deal != null && activity.Deal.CompanyId == request.CompanyId.Value
                || activity.Contact != null && activity.Contact.CompanyId == request.CompanyId.Value);
        }

        if (request.Stage.HasValue)
        {
            query = query.Where(activity => activity.Deal != null && activity.Deal.Stage == request.Stage.Value);
        }

        return query;
    }

    private static async Task<decimal> SumDealValueAsync(IQueryable<Deal> query, CancellationToken cancellationToken)
    {
        return await query.Select(deal => (decimal?)deal.Value).SumAsync(cancellationToken) ?? 0m;
    }

    private static GetReportsRequest NormalizeRequest(GetReportsRequest? request)
    {
        return new GetReportsRequest
        {
            FromUtc = request?.FromUtc?.Date,
            ToUtc = request?.ToUtc?.Date,
            CompanyId = request?.CompanyId,
            Stage = request?.Stage
        };
    }

    private static List<TrendPeriod> GetMonthlyPeriods(GetReportsRequest request, DateTime utcNow)
    {
        var endMonth = new DateTime((request.ToUtc ?? utcNow).Year, (request.ToUtc ?? utcNow).Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var periods = new List<TrendPeriod>(TrendPeriods);

        for (var offset = TrendPeriods - 1; offset >= 0; offset--)
        {
            var startUtc = endMonth.AddMonths(-offset);
            var endUtc = startUtc.AddMonths(1);
            periods.Add(new TrendPeriod(startUtc, endUtc, startUtc.ToString("MMM", CultureInfo.InvariantCulture)));
        }

        return periods;
    }

    private static string CsvRow(string section, string label, string value, string extra)
    {
        return string.Join(",", EscapeCsv(section), EscapeCsv(label), EscapeCsv(value), EscapeCsv(extra));
    }

    private static string EscapeCsv(string? value)
    {
        var normalizedValue = value ?? string.Empty;
        return string.Concat("\"", normalizedValue.Replace("\"", "\"\""), "\"");
    }

    private static string FormatDecimalForCsv(decimal value)
    {
        return value.ToString("0.##", CultureInfo.InvariantCulture);
    }

    private sealed record TrendPeriod(DateTime StartUtc, DateTime EndUtc, string Label);
}
