// WinUI 3 removed CoreWindow, so upstream's KeyboardShortcutManager has nothing to hook.
// This rehosts its four CoreWindow/CoreDispatcher handlers on the window's root visual, which
// sees keys for the page and for popups alike. The ingest rewrites only the handler parameter
// types, so the handler bodies stay byte-identical to upstream.

using System;
using System.Runtime.CompilerServices;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.System;
using Windows.UI.Core;

namespace CalculatorApp.Uno;

/// <summary>Stand-in for <c>Windows.UI.Core.KeyEventArgs</c>.</summary>
internal sealed class CoreKeyArgs
{
    public CoreKeyArgs(VirtualKey virtualKey, bool handled)
    {
        VirtualKey = virtualKey;
        Handled = handled;
    }

    public VirtualKey VirtualKey { get; }

    public bool Handled { get; set; }
}

/// <summary>Stand-in for <c>Windows.UI.Core.CharacterReceivedEventArgs</c>.</summary>
internal sealed class CoreCharacterArgs
{
    public CoreCharacterArgs(char character) => KeyCode = character;

    public uint KeyCode { get; }
}

/// <summary>Stand-in for <c>Windows.UI.Core.AcceleratorKeyEventArgs</c>.</summary>
internal sealed class CoreAcceleratorArgs
{
    public CoreAcceleratorArgs(VirtualKey virtualKey, bool isKeyReleased, bool isMenuKeyDown)
    {
        VirtualKey = virtualKey;
        KeyStatus = new CoreKeyStatus(isKeyReleased, isMenuKeyDown);
    }

    public VirtualKey VirtualKey { get; }

    public CoreKeyStatus KeyStatus { get; }
}

internal readonly record struct CoreKeyStatus(bool IsKeyReleased, bool IsMenuKeyDown);

internal static class KeyboardBridge
{
    private static Action<object, CoreCharacterArgs>? s_onCharacter;
    private static Action<object, CoreKeyArgs>? s_onKeyDown;
    private static Action<object, CoreKeyArgs>? s_onKeyUp;
    private static Action<object, CoreAcceleratorArgs>? s_onAccelerator;
    private static UIElement? s_hooked;

    public static void Attach(
        Action<object, CoreCharacterArgs> onCharacter,
        Action<object, CoreKeyArgs> onKeyDown,
        Action<object, CoreKeyArgs> onKeyUp,
        Action<object, CoreAcceleratorArgs> onAccelerator)
    {
        s_onCharacter = onCharacter;
        s_onKeyDown = onKeyDown;
        s_onKeyUp = onKeyUp;
        s_onAccelerator = onAccelerator;
        TryHook();
    }

    public static bool IsDown(VirtualKey key)
        => (InputKeyboardSource.GetKeyStateForCurrentThread(key) & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;

    // Window.Content is not set yet when upstream initializes, and is not in the tree until loaded.
    private static void TryHook()
    {
        if (Window.Current?.Content is not FrameworkElement content)
        {
            Window.Current?.DispatcherQueue.TryEnqueue(TryHook);
            return;
        }

        if (content.XamlRoot is null)
        {
            content.Loaded += OnContentLoaded;
            return;
        }

        UIElement root = content;
        while (VisualTreeHelper.GetParent(root) is UIElement parent)
        {
            root = parent;
        }

        if (ReferenceEquals(root, s_hooked))
        {
            return;
        }

        s_hooked = root;
        root.AddHandler(UIElement.KeyDownEvent, new KeyEventHandler(OnKeyDown), handledEventsToo: true);
        root.AddHandler(UIElement.KeyUpEvent, new KeyEventHandler(OnKeyUp), handledEventsToo: true);
    }

    private static void OnContentLoaded(object sender, RoutedEventArgs e)
    {
        ((FrameworkElement)sender).Loaded -= OnContentLoaded;
        TryHook();
    }

    private static void OnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        s_onKeyDown?.Invoke(sender, new CoreKeyArgs(e.Key, e.Handled));

        // CoreWindow raised CharacterReceived after KeyDown; Ctrl/Alt chords produce no text.
        if (!IsDown(VirtualKey.Control) && !IsDown(VirtualKey.Menu) && GetUnicodeKey(e) is char c && c != '\0')
        {
            s_onCharacter?.Invoke(sender, new CoreCharacterArgs(c));
        }
    }

    private static void OnKeyUp(object sender, KeyRoutedEventArgs e)
    {
        s_onKeyUp?.Invoke(sender, new CoreKeyArgs(e.Key, e.Handled));
        s_onAccelerator?.Invoke(sender, new CoreAcceleratorArgs(e.Key, isKeyReleased: true, isMenuKeyDown: IsDown(VirtualKey.Menu)));
    }

    // Uno resolves the typed character per platform but keeps it internal; WinUI would raise
    // UIElement.CharacterReceived, which Uno declares but never raises.
    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_UnicodeKey")]
    private static extern char? GetUnicodeKey(KeyRoutedEventArgs args);
}
