using AO.Core.Features.Activities;
using System.Net.Http.Json;
using AO.Core.Features.Companies;
using AO.Core.Features.Contacts;
using AO.Core.Features.Dashboard;
using AO.Core.Features.Deals;
using AO.Core.Features.Reports;
using AO.Core.Features.Tasks;
using AO.Core.Shared.ApiResponses;

namespace AO.UI.Shared.Services;

public sealed class CrmApiClient(HttpClient httpClient)
{
    public Task<IReadOnlyList<CompanyDto>> GetCompaniesAsync(CancellationToken cancellationToken = default)
        => SendAsync<IReadOnlyList<CompanyDto>>(HttpMethod.Get, "api/companies", cancellationToken: cancellationToken);

    public Task<IReadOnlyList<ContactDto>> GetContactsAsync(CancellationToken cancellationToken = default)
        => SendAsync<IReadOnlyList<ContactDto>>(HttpMethod.Get, "api/contacts", cancellationToken: cancellationToken);

    public Task<IReadOnlyList<DealDto>> GetDealsAsync(CancellationToken cancellationToken = default)
        => SendAsync<IReadOnlyList<DealDto>>(HttpMethod.Get, "api/deals", cancellationToken: cancellationToken);

    public Task<IReadOnlyList<CrmTaskDto>> GetTasksAsync(CancellationToken cancellationToken = default)
        => SendAsync<IReadOnlyList<CrmTaskDto>>(HttpMethod.Get, "api/tasks", cancellationToken: cancellationToken);

    public Task<DashboardSummaryDto> GetDashboardAsync(CancellationToken cancellationToken = default)
        => SendAsync<DashboardSummaryDto>(HttpMethod.Get, "api/dashboard", cancellationToken: cancellationToken);

    public Task<ReportSnapshotDto> GetReportsAsync(GetReportsRequest request, CancellationToken cancellationToken = default)
        => SendAsync<ReportSnapshotDto>(HttpMethod.Get, $"api/reports{BuildReportsQueryString(request)}", cancellationToken: cancellationToken);

    public Task<ReportExportDto> GetReportsExportAsync(GetReportsRequest request, CancellationToken cancellationToken = default)
        => SendAsync<ReportExportDto>(HttpMethod.Get, $"api/reports/export{BuildReportsQueryString(request)}", cancellationToken: cancellationToken);

    public Task<IReadOnlyList<CrmActivityDto>> GetActivitiesAsync(int take = 10, CancellationToken cancellationToken = default)
        => SendAsync<IReadOnlyList<CrmActivityDto>>(HttpMethod.Get, $"api/activities?take={take}", cancellationToken: cancellationToken);

    public Task<CompanyDto> CreateCompanyAsync(CreateCompanyRequest request, CancellationToken cancellationToken = default)
        => SendAsync<CompanyDto>(HttpMethod.Post, "api/companies", request, cancellationToken);

    public Task<ContactDto> CreateContactAsync(CreateContactRequest request, CancellationToken cancellationToken = default)
        => SendAsync<ContactDto>(HttpMethod.Post, "api/contacts", request, cancellationToken);

    public Task<DealDto> CreateDealAsync(CreateDealRequest request, CancellationToken cancellationToken = default)
        => SendAsync<DealDto>(HttpMethod.Post, "api/deals", request, cancellationToken);

    public Task<DealDto> SetDealStageAsync(Guid dealId, SetDealStageRequest request, CancellationToken cancellationToken = default)
        => SendAsync<DealDto>(HttpMethod.Patch, $"api/deals/{dealId}/stage", request, cancellationToken);

    public Task<CrmTaskDto> CreateTaskAsync(CreateCrmTaskRequest request, CancellationToken cancellationToken = default)
        => SendAsync<CrmTaskDto>(HttpMethod.Post, "api/tasks", request, cancellationToken);

    public Task<CrmTaskDto> CompleteTaskAsync(Guid taskId, CancellationToken cancellationToken = default)
        => SendAsync<CrmTaskDto>(HttpMethod.Patch, $"api/tasks/{taskId}/complete", cancellationToken: cancellationToken);

    private async Task<T> SendAsync<T>(HttpMethod method, string requestUri, object? body = null, CancellationToken cancellationToken = default)
        where T : class
    {
        using var request = new HttpRequestMessage(method, requestUri);

        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        using var response = await httpClient.SendAsync(request, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var envelope = await response.Content.ReadFromJsonAsync<Response<T>>(cancellationToken: cancellationToken);
            if (envelope?.Data is null)
            {
                throw new InvalidOperationException("The CRM API returned an empty response.");
            }

            return envelope.Data;
        }

        var errorEnvelope = await response.Content.ReadFromJsonAsync<Response<object?>>(cancellationToken: cancellationToken);
        var errorMessage = errorEnvelope?.Message;

        if (string.IsNullOrWhiteSpace(errorMessage))
        {
            errorMessage = $"CRM API request failed with status code {(int)response.StatusCode}.";
        }

        throw new InvalidOperationException(errorMessage);
    }

    private static string BuildReportsQueryString(GetReportsRequest request)
    {
        var parameters = new List<string>();

        if (request.FromUtc.HasValue)
        {
            parameters.Add($"fromUtc={Uri.EscapeDataString(request.FromUtc.Value.ToString("yyyy-MM-dd"))}");
        }

        if (request.ToUtc.HasValue)
        {
            parameters.Add($"toUtc={Uri.EscapeDataString(request.ToUtc.Value.ToString("yyyy-MM-dd"))}");
        }

        if (request.CompanyId.HasValue)
        {
            parameters.Add($"companyId={Uri.EscapeDataString(request.CompanyId.Value.ToString())}");
        }

        if (request.Stage.HasValue)
        {
            parameters.Add($"stage={Uri.EscapeDataString(request.Stage.Value.ToString())}");
        }

        return parameters.Count == 0 ? string.Empty : $"?{string.Join("&", parameters)}";
    }
}
