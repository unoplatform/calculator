// Uno Workaround #24297 - the shipping CommunityToolkit SettingsControls package is
// built against `Uno` and `Uno.UI.Toolkit`, assemblies Uno 7 no longer produces, so it
// fails to load at runtime. This is the minimal surface upstream's Settings.xaml uses.
// Delete once a toolkit build targeting Uno 7 exists (#24297).

using System.Collections.Generic;
using Microsoft.UI.Xaml.Controls;

namespace CommunityToolkit.WinUI.Controls;

public enum ContentAlignment
{
    Right,
    Left,
    Vertical,
}

public partial class SettingsCard : ContentControl
{
    public object? Header { get; set; }

    public object? Description { get; set; }

    public IconElement? HeaderIcon { get; set; }

    public ContentAlignment ContentAlignment { get; set; } = ContentAlignment.Right;
}

public partial class SettingsExpander : ContentControl
{
    public object? Header { get; set; }

    public object? Description { get; set; }

    public IconElement? HeaderIcon { get; set; }

    public bool IsExpanded { get; set; }

    public IList<object> Items { get; } = new List<object>();
}
