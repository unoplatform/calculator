// Uno Workaround (unfiled: itemsstackpanel-in-itemscontrol) - an ItemsStackPanel used as the
// ItemsPanel of a plain ItemsControl measures to zero width, stacks every item at the same offset
// and keeps stale containers, so the calculator's expression line never draws. The panel is
// swapped for the non-virtualizing equivalent; an expression is a handful of tokens.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;

namespace CalculatorApp.Uno;

internal static class ItemsPanelCompat
{
    private static ItemsPanelTemplate? s_rightAlignedHorizontal;

    public static void UseHorizontalStackPanel(ItemsControl itemsControl)
    {
        s_rightAlignedHorizontal ??= (ItemsPanelTemplate)XamlReader.Load(
            "<ItemsPanelTemplate xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\">" +
            "<StackPanel HorizontalAlignment=\"Right\" VerticalAlignment=\"Stretch\" Orientation=\"Horizontal\"/>" +
            "</ItemsPanelTemplate>");

        itemsControl.ItemsPanel = s_rightAlignedHorizontal;
    }
}
