// "Keep on top" for desktop heads. Upstream drives it through ApplicationView compact overlay, which WinUI 3
// does not have (and Uno throws on, #24298). AppWindow's OverlappedPresenter.IsAlwaysOnTop is the WinUI 3
// equivalent and Uno implements it on Win32, X11 and macOS; the browser and mobile heads report unsupported,
// which hides the button exactly as an unsupported device does on Windows.

using System.Threading.Tasks;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.Foundation;
using Windows.Graphics;

namespace CalculatorApp.Uno;

/// <summary>Stand-in for <c>Windows.UI.ViewManagement.ViewModePreferences</c>.</summary>
internal sealed class CompactOverlayOptions
{
    public Size CustomSize { get; set; }
}

internal static class AlwaysOnTopCompat
{
    private static SizeInt32? s_restoreSize;

    public static bool IsSupported
        => (System.OperatingSystem.IsWindows() || System.OperatingSystem.IsLinux() || System.OperatingSystem.IsMacOS())
           && Presenter is not null;

    public static bool IsActive => Presenter?.IsAlwaysOnTop == true;

    public static CompactOverlayOptions CreateOptions() => new();

    public static Task<bool> Enter(Size size)
    {
        if (Presenter is not { } presenter || Window.Current?.AppWindow is not { } appWindow)
        {
            return Task.FromResult(false);
        }

        s_restoreSize = appWindow.Size;
        presenter.IsAlwaysOnTop = true;
        if (size.Width > 0 && size.Height > 0)
        {
            appWindow.Resize(ToPhysical(size));
        }
        return Task.FromResult(true);
    }

    public static Task<bool> Exit()
    {
        if (Presenter is not { } presenter || Window.Current?.AppWindow is not { } appWindow)
        {
            return Task.FromResult(false);
        }

        presenter.IsAlwaysOnTop = false;
        if (s_restoreSize is { } restore)
        {
            appWindow.Resize(restore);
            s_restoreSize = null;
        }
        return Task.FromResult(true);
    }

    private static OverlappedPresenter? Presenter => Window.Current?.AppWindow?.Presenter as OverlappedPresenter;

    private static SizeInt32 ToPhysical(Size size)
    {
        var scale = (Window.Current?.Content as FrameworkElement)?.XamlRoot?.RasterizationScale ?? 1.0;
        return new SizeInt32((int)(size.Width * scale), (int)(size.Height * scale));
    }
}
