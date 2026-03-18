using AO.UI.Shared.Services;
using AO.UI.Web.Components;
using AO.UI.Web.Services;
using Microsoft.AspNetCore.Diagnostics;
using Serilog;
using System.Net.Http.Headers;

try
{
var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

var crmApiOptions = new CrmApiOptions();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add device-specific services used by the AO.UI.Shared project
builder.Services.AddSingleton<IFormFactor, FormFactor>();
builder.Services.AddSingleton(crmApiOptions);
builder.Services.AddScoped(sp =>
{
    var client = new HttpClient();
    client.BaseAddress = new Uri(crmApiOptions.BaseAddress, UriKind.Absolute);
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    client.DefaultRequestHeaders.Add("X-API-Key", crmApiOptions.ApiKey);
    return new CrmApiClient(client, sp.GetRequiredService<ILogger<CrmApiClient>>());
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(context =>
    {
        var exceptionFeature = context.Features.Get<IExceptionHandlerFeature>();
        if (exceptionFeature?.Error is not null)
        {
            app.Logger.LogError(exceptionFeature.Error, "Unhandled AO UI Web exception.");
        }

        context.Response.Redirect("/error");
        return Task.CompletedTask;
    });
});

if (!app.Environment.IsDevelopment())
{
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(
        typeof(AO.UI.Shared._Imports).Assembly);

app.Run();
}
catch (Exception exception)
{
    Log.Fatal(exception, "AO UI Web terminated unexpectedly.");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}
