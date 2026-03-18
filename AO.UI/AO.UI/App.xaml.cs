using Microsoft.Maui.Dispatching;
using Serilog;

namespace AO.UI;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        RegisterGlobalExceptionHandling();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new MainPage()) { Title = "AO.UI" };
    }

    private static void RegisterGlobalExceptionHandling()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, eventArgs) =>
        {
            if (eventArgs.ExceptionObject is Exception exception)
            {
                Log.Error(exception, "Unhandled AppDomain exception.");
            }
        };

        TaskScheduler.UnobservedTaskException += (_, eventArgs) =>
        {
            Log.Error(eventArgs.Exception, "Unobserved task exception.");
            eventArgs.SetObserved();
        };

#if WINDOWS
        Microsoft.UI.Xaml.Application.Current.UnhandledException += (_, eventArgs) =>
        {
            Log.Error(eventArgs.Exception, "Unhandled Windows UI exception.");
            eventArgs.Handled = true;
        };
#endif
    }
}
