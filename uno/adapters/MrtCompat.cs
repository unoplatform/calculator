// Uno Workaround #24284 - MRT exposes a resw name "a.b.c" as the path "a/b/c", and upstream asks for
// the path form everywhere: keyboard shortcuts, 426 accessibility names, pane labels. Uno only maps the
// first separator, so any key with a [using:...] attached-property segment comes back "" - which left
// every keyboard shortcut unregistered. Delete when #24284 ships.

using Windows.ApplicationModel.Resources;

namespace CalculatorApp.Uno;

internal static class MrtCompat
{
    public static string? GetString(ResourceLoader? loader, string key)
    {
        if (loader is null)
        {
            return null;
        }

        var value = loader.GetString(key);

        // Uno keys a resw name "uid.[using:Ns]Owner.Prop" as "uid/[using:Ns]Owner.Prop": only the
        // first separator is a path segment, so fold the later ones back into dots.
        var first = key.IndexOf('/');
        if (string.IsNullOrEmpty(value) && first >= 0 && key.IndexOf('/', first + 1) > first)
        {
            value = loader.GetString(key.Substring(0, first + 1) + key.Substring(first + 1).Replace('/', '.'));
        }

        return value;
    }
}
