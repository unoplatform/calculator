// Uno Workaround (unfiled: pivot-items-mutation) - when a Pivot's Items change, Uno indexes its header
// row with the selected index before rebuilding it (ArgumentOutOfRange out of Items.Insert/RemoveAt, which
// also skips the header rebuild), and gives the rebuilt headers their Selected/Unselected state before they
// are live, so it never sticks. Upstream's Calculator swaps the History PivotItem in and out on every
// Programmer toggle, which left two underlined headers or none.

using Microsoft.UI.Xaml.Controls;

namespace CalculatorApp.Uno;

internal static class PivotCompat
{
    // SynchronizeSelectedItem bails on -1, so the Items mutation that follows cannot throw.
    public static void PrepareForItemsChange(Pivot pivot) => pivot.SelectedIndex = -1;

    public static void RefreshHeaders(Pivot pivot)
        => pivot.DispatcherQueue.TryEnqueue(() =>
        {
            var index = pivot.SelectedIndex;
            pivot.SelectedIndex = -1;
            pivot.SelectedIndex = index;
        });
}
