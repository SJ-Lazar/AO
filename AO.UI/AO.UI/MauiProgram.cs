using AO.UI.Services;
using AO.UI.Shared.Services;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using System.Net.Http.Headers;

namespace AO.UI;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var crmApiBaseAddress = "http://localhost:5259/";

#if ANDROID
        crmApiBaseAddress = "http://10.0.2.2:5259/";
#endif

        var logDirectory = Path.Combine(FileSystem.Current.AppDataDirectory, "Logs");
        Directory.CreateDirectory(logDirectory);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.File(
                Path.Combine(logDirectory, "ao-maui-.log"),
                rollingInterval: RollingInterval.Day,
                restrictedToMinimumLevel: LogEventLevel.Information,
                retainedFileCountLimit: 14,
                shared: true,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        try
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            // Add device-specific services used by the AO.UI.Shared project
            builder.Services.AddSingleton<IFormFactor, FormFactor>();
            builder.Services.AddSingleton(new CrmApiOptions
            {
                BaseAddress = crmApiBaseAddress,
                ApiKey = "ABC"
            });
            builder.Services.AddScoped(serviceProvider =>
            {
                var options = serviceProvider.GetRequiredService<CrmApiOptions>();
                var client = new HttpClient();
                client.BaseAddress = new Uri(options.BaseAddress, UriKind.Absolute);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("X-API-Key", options.ApiKey);
                return new CrmApiClient(client, serviceProvider.GetRequiredService<ILogger<CrmApiClient>>());
            });

            builder.Services.AddMauiBlazorWebView();
            builder.Logging.AddSerilog(Log.Logger, dispose: true);

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
        catch (Exception exception)
        {
            Log.Fatal(exception, "AO MAUI failed to start.");
            throw;
        }
    }
}
