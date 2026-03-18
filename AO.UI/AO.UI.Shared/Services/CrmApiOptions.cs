namespace AO.UI.Shared.Services;

public sealed class CrmApiOptions
{
    public string BaseAddress { get; init; } = "http://localhost:5259/";

    public string ApiKey { get; init; } = "ABC";
}
