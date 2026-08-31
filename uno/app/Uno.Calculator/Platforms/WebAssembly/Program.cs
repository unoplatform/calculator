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
        var host = UnoPlatformHostBuilder.Create()
            .App(() => new App())
            .UseWebAssembly()
            .Build();

        await host.RunAsync();
    }
}
