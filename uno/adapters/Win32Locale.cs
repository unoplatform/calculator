// Upstream portability shim, not an Uno workaround.
//
// LocalizationSettings and LocalizationStringUtil P/Invoke kernel32.dll (ResolveLocaleName,
// GetLocaleInfoEx, FormatMessageW). Those resolve only on Windows, so MainPage's constructor
// throws DllNotFoundException on every other head - wasm, Android, iOS, Linux, macOS. On wasm
// the symptom is silent: Uno swallows the OnLaunched exception and leaves a blank canvas.
//
// Every LCTYPE upstream asks for has a CultureInfo equivalent, so these reimplement the four
// imports in managed code with the same contracts. The ingest rewriter redirects the extern
// declarations here; no upstream file is modified.
//
// Contracts preserved from Win32:
//   * ResolveLocaleName / GetLocaleInfoEx(char[]) return the character count INCLUDING the
//     null terminator, and 0 on failure. Callers do `new string(buf, 0, result - 1)`.
//   * GetLocaleInfoEx(ref int) is used with LOCALE_RETURN_NUMBER and returns a non-zero count.
//   * FormatMessageW returns the character count EXCLUDING the null terminator.

using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace CalculatorApp.Uno;

internal static class Win32Locale
{
    private const uint LOCALE_RETURN_NUMBER = 0x20000000;

    private const uint LOCALE_SLIST = 0x0000000C;
    private const uint LOCALE_SDECIMAL = 0x0000000E;
    private const uint LOCALE_STHOUSAND = 0x0000000F;
    private const uint LOCALE_SGROUPING = 0x00000010;
    private const uint LOCALE_ICURRDIGITS = 0x00000019;
    private const uint LOCALE_IPOSSYMPRECEDES = 0x00000034;
    private const uint LOCALE_ICALENDARTYPE = 0x00001009;
    private const uint LOCALE_IFIRSTDAYOFWEEK = 0x0000100C;

    private static CultureInfo Resolve(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return CultureInfo.CurrentCulture;
        }

        try
        {
            return CultureInfo.GetCultureInfo(name);
        }
        catch (CultureNotFoundException)
        {
            return CultureInfo.CurrentCulture;
        }
    }

    /// <summary>Writes the resolved locale name; returns its length including the terminator.</summary>
    internal static int ResolveLocaleName(string lpNameToResolve, char[] lpLocaleName, int cchLocaleName)
        => Write(Resolve(lpNameToResolve).Name, lpLocaleName, cchLocaleName);

    internal static int GetLocaleInfoEx(string lpLocaleName, uint lcType, char[] lpLCData, int cchData)
    {
        var culture = Resolve(lpLocaleName);
        var value = (lcType & ~LOCALE_RETURN_NUMBER) switch
        {
            LOCALE_SDECIMAL => culture.NumberFormat.NumberDecimalSeparator,
            LOCALE_STHOUSAND => culture.NumberFormat.NumberGroupSeparator,
            LOCALE_SGROUPING => FormatGrouping(culture.NumberFormat.NumberGroupSizes),
            LOCALE_SLIST => culture.TextInfo.ListSeparator,
            LOCALE_ICURRDIGITS => culture.NumberFormat.CurrencyDecimalDigits.ToString(CultureInfo.InvariantCulture),
            LOCALE_IPOSSYMPRECEDES => SymbolPrecedes(culture).ToString(CultureInfo.InvariantCulture),
            LOCALE_ICALENDARTYPE => CalendarType(culture).ToString(CultureInfo.InvariantCulture),
            LOCALE_IFIRSTDAYOFWEEK => FirstDayOfWeek(culture).ToString(CultureInfo.InvariantCulture),
            _ => null,
        };

        return value is null ? 0 : Write(value, lpLCData, cchData);
    }

    internal static int GetLocaleInfoEx(string lpLocaleName, uint lcType, ref int lpLCData, int cchData)
    {
        var culture = Resolve(lpLocaleName);
        switch (lcType & ~LOCALE_RETURN_NUMBER)
        {
            case LOCALE_ICURRDIGITS:
                lpLCData = culture.NumberFormat.CurrencyDecimalDigits;
                return sizeof(int) / sizeof(char);
            case LOCALE_IPOSSYMPRECEDES:
                lpLCData = SymbolPrecedes(culture);
                return sizeof(int) / sizeof(char);
            case LOCALE_ICALENDARTYPE:
                lpLCData = CalendarType(culture);
                return sizeof(int) / sizeof(char);
            case LOCALE_IFIRSTDAYOFWEEK:
                lpLCData = FirstDayOfWeek(culture);
                return sizeof(int) / sizeof(char);
            default:
                return 0;
        }
    }

    /// <summary>
    /// FORMAT_MESSAGE_FROM_STRING with an LPWSTR argument array: substitutes %1..%n.
    /// Returns the character count excluding the terminator, or 0 on failure.
    /// </summary>
    internal static uint FormatMessageW(
        uint dwFlags,
        string lpSource,
        uint dwMessageId,
        uint dwLanguageId,
        char[] lpBuffer,
        uint nSize,
        IntPtr arguments)
    {
        if (lpSource is null || lpBuffer is null)
        {
            return 0;
        }

        var result = new StringBuilder(lpSource.Length);
        for (int i = 0; i < lpSource.Length; i++)
        {
            if (lpSource[i] != '%' || i + 1 >= lpSource.Length)
            {
                result.Append(lpSource[i]);
                continue;
            }

            char next = lpSource[i + 1];
            if (next is >= '1' and <= '9')
            {
                // Win32 inserts are 1-based; the array holds one LPWSTR per insert.
                int index = next - '1';
                result.Append(ReadArgument(arguments, index));
                i++;
            }
            else if (next == '%')
            {
                result.Append('%');
                i++;
            }
            else
            {
                result.Append(lpSource[i]);
            }
        }

        int length = Math.Min(result.Length, (int)nSize);
        if (length > lpBuffer.Length)
        {
            return 0;
        }

        result.CopyTo(0, lpBuffer, 0, length);
        return (uint)length;
    }

    private static string ReadArgument(IntPtr arguments, int index)
    {
        if (arguments == IntPtr.Zero)
        {
            return string.Empty;
        }

        IntPtr p = Marshal.ReadIntPtr(arguments, index * IntPtr.Size);
        return p == IntPtr.Zero ? string.Empty : (Marshal.PtrToStringUni(p) ?? string.Empty);
    }

    /// <summary>Win32 spells grouping as sizes joined by ';' with a trailing ";0" for no repeat.</summary>
    private static string FormatGrouping(int[] groupSizes)
    {
        if (groupSizes is null || groupSizes.Length == 0)
        {
            return "3;0";
        }

        return string.Join(";", groupSizes) + ";0";
    }

    /// <summary>LOCALE_IPOSSYMPRECEDES: 1 when the currency symbol precedes the amount.</summary>
    private static int SymbolPrecedes(CultureInfo culture)
        => culture.NumberFormat.CurrencyPositivePattern is 0 or 2 ? 1 : 0;

    /// <summary>CAL_GREGORIAN(1), CAL_HIJRI(6), CAL_HEBREW(8), CAL_JAPAN(3), CAL_TAIWAN(4), CAL_KOREA(5), CAL_THAI(7).</summary>
    private static int CalendarType(CultureInfo culture) => culture.Calendar switch
    {
        HijriCalendar => 6,
        UmAlQuraCalendar => 23,
        HebrewCalendar => 8,
        JapaneseCalendar => 3,
        TaiwanCalendar => 4,
        KoreanCalendar => 5,
        ThaiBuddhistCalendar => 7,
        PersianCalendar => 22,
        _ => 1,
    };

    /// <summary>LOCALE_IFIRSTDAYOFWEEK counts from Monday=0; DayOfWeek counts from Sunday=0.</summary>
    private static int FirstDayOfWeek(CultureInfo culture)
        => ((int)culture.DateTimeFormat.FirstDayOfWeek + 6) % 7;

    private static int Write(string value, char[] buffer, int capacity)
    {
        if (buffer is null || value is null)
        {
            return 0;
        }

        // Win32 counts the terminator; a buffer that cannot hold it is a failure.
        int needed = value.Length + 1;
        if (needed > capacity || needed > buffer.Length)
        {
            return 0;
        }

        value.CopyTo(0, buffer, 0, value.Length);
        buffer[value.Length] = '\0';
        return needed;
    }
}
