using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;

namespace AO.UI.Shared.Components;

public sealed class CrmErrorBoundary : ErrorBoundary
{
    [Inject]
    private ILogger<CrmErrorBoundary> Logger { get; set; } = default!;

    protected override Task OnErrorAsync(Exception exception)
    {
        Logger.LogError(exception, "Unhandled UI exception.");
        return Task.CompletedTask;
    }
}
