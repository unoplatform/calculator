using System;
using System.Threading.Tasks;
using Uno.UI.Hosting;

namespace CalculatorApp;

/// <summary>
/// Browser entry point. Same App as every other head; only the host differs.
/// The browser host must be awaited - blocking Run() would deadlock the event loop.
/// </summary>
internal static class Program
{
    public static async Task Main(string[] args)
    {
        // Uno swallows exceptions thrown from OnLaunched: a startup failure shows up only as a
        // blank canvas with the loader still up and an empty console. These hooks do not catch
        // that case either, but they surface anything that escapes the host itself.
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            Console.WriteLine($"[boot] unhandled: {e.ExceptionObject}");
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            Console.WriteLine($"[boot] unobserved: {e.Exception}");
            e.SetObserved();
        };

        var host = UnoPlatformHostBuilder.Create()
            .App(() => new App())
            .UseWebAssembly()
            .Build();

        await host.RunAsync();
    }
}
