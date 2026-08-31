// Uno Workaround #24293 - x:Name on an object inside a Resources dictionary generates
// no backing field, yet both Uno's generated code and upstream's code-behind reference
// those names as members. The objects ARE placed in Resources, so reading them back
// restores UWP/WinUI behaviour without touching any upstream file. Delete when #24293 ships.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace CalculatorApp;

public sealed partial class HistoryList
{
    internal MenuFlyout HistoryContextMenu => (MenuFlyout)Resources["HistoryContextMenu"];

    internal ViewModel.Common.Automation.NarratorNotifier NarratorNotifier
        => (ViewModel.Common.Automation.NarratorNotifier)Resources["NarratorNotifier"];
}

public sealed partial class DateCalculator
{
    internal MenuFlyout ResultsContextMenu => (MenuFlyout)Resources["ResultsContextMenu"];
}

public sealed partial class SupplementaryResults
{
    internal DataTemplate SupplementaryValueTemplate => (DataTemplate)Resources["SupplementaryValueTemplate"];

    internal DataTemplate DelighterValueTemplate => (DataTemplate)Resources["DelighterValueTemplate"];
}

public sealed partial class Settings
{
    internal ViewModel.Common.Automation.NarratorNotifier NarratorNotifier
        => (ViewModel.Common.Automation.NarratorNotifier)Resources["NarratorNotifier"];
}

public sealed partial class GraphingCalculator
{
    internal ThemeShadow SharedShadow => (ThemeShadow)Resources["SharedShadow"];
}

public sealed partial class KeyGraphFeaturesPanel
{
    internal Brush EquationButtonBackgroundBrush => (Brush)Resources["EquationButtonBackgroundBrush"];
}
