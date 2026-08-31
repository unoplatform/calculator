using System;
using Uno.UI.Hosting;

namespace CalculatorApp;

/// <summary>
/// Desktop entry point. Upstream's App.xaml.cs drives everything from here on; this only
/// stands up the Uno host. One host covers Win32, X11 and macOS from the same TFM.
/// </summary>
internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var host = UnoPlatformHostBuilder.Create()
            .App(() => new App())
            .UseWin32()
            .UseX11()
            .UseLinuxFrameBuffer()
            .UseMacOS()
            .Build();

        host.Run();
    }
}
