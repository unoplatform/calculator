// Uno Workaround #24390 - several Windows globalization members throw NotImplementedException:
//   Windows.Globalization.GeographicRegion.CodeTwoLetter
//   Windows.System.UserProfile.GlobalizationPreferences.Currencies
//   Windows.System.UserProfile.GlobalizationPreferences.Languages
//
// Upstream reads them to pick region-appropriate default units (US customary vs metric), a
// date-format region, the user's currency and the currency-service response language. Its
// `?? "US"` and `.Count > 0` guards do not help, because the getters throw rather than
// returning null or an empty list.
//
// The whole converter section and the date calculator construct their ViewModel through these,
// so none of those pages open at all. CultureInfo and RegionInfo carry the same information.
// Delete when #24390 ships.

using System;
using System.Collections.Generic;
using System.Globalization;

namespace CalculatorApp.Uno;

internal static class GlobalizationCompat
{
    /// <summary>Two-letter ISO 3166 region code, matching GeographicRegion.CodeTwoLetter.</summary>
    internal static string CurrentRegionCode => Safe(
        () => RegionInfo.CurrentRegion?.TwoLetterISORegionName, "US");

    /// <summary>Three-letter ISO 4217 currency code, matching GlobalizationPreferences.Currencies[0].</summary>
    internal static string CurrentCurrencyCode => Safe(
        () => RegionInfo.CurrentRegion?.ISOCurrencySymbol, "USD");

    /// <summary>BCP-47 language tags in preference order, matching GlobalizationPreferences.Languages.</summary>
    internal static IReadOnlyList<string> CurrentLanguages
    {
        get
        {
            var name = Safe(() => CultureInfo.CurrentUICulture?.Name, "en-US");
            return new[] { string.IsNullOrEmpty(name) ? "en-US" : name };
        }
    }

    // An invariant-globalization runtime has no current region or culture.
    private static string Safe(Func<string> read, string fallback)
    {
        try
        {
            string value = read();
            return string.IsNullOrEmpty(value) ? fallback : value;
        }
        catch (Exception)
        {
            return fallback;
        }
    }
}
