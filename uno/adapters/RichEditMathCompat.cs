// Uno Workaround #24298 - RichEditTextDocument has GetMathMode/SetMathMode but not
// WinUI's GetMath/SetMath, so upstream's MathRichEditBox does not compile. Extension
// methods let it compile unmodified. Delete when #24298 ships.

namespace CalculatorApp.Controls;

using Microsoft.UI.Text;

internal static class RichEditMathCompat
{
    public static void GetMath(this RichEditTextDocument document, out string value) => value = string.Empty;

    public static void SetMath(this RichEditTextDocument document, string value)
    {
        // Uno Workaround #24298: no math-mode text support in Uno RichEditTextDocument yet.
    }
}
