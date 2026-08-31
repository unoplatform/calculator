// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//
// Harvested from D:/Work/calculator/src/Calculator.Shared/CalcManager/UnitConversionManager.cs.
// That file is a complete, working, pure-managed port of the unit converter engine - there is no
// native ABI for unit conversion at all (a repo-wide DLL_EXPORT sweep finds only the 34
// CalcManager exports), so this whole seam is real behaviour with no interop stubs.
//
// Changes vs the donor:
//   * namespace UnitConversionManager -> CalcManager.Interop.Internal, everything internal, so
//     that no type named Unit / Category / ConversionData ever lands in CalcManager.Interop and
//     collides with CalculatorApp.ViewModel.Unit / .Category in the files that import both.
//   * CalculatorList<T> -> List<T>, and the suggested-value sort now uses a proper total-order
//     Comparison<T>. The donor's bool comparator can report "greater" for equal elements, which
//     List<T>.Sort rejects at runtime with InvalidOperationException.
//   * the currency data loader is gone. It was always null in this configuration (the ingested
//     ViewModel routes currency through its own managed CurrencyDataLoader), and every currency
//     path in the donor was null-guarded anyway.
//   * Serialize / DeSerialize dropped: no consumer calls them, and the donor's DeSerialize
//     indexes m_categoryToUnits with a missing key. SaveUserPreferences / RestoreUserPreferences,
//     which the ViewModel does use, are kept in full.
//   * the C++-shim string extensions (npos/front/substr/size) are replaced with plain BCL calls.
//   * lookups into m_ratioMap and m_categoryToUnits now use TryGetValue. The donor indexed them
//     directly and threw KeyNotFoundException whenever a unit had no ratio row - reachable here
//     because currency units are supplied by a loader that fills in asynchronously.

#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace CalcManager.Interop.Internal;

/// <summary>A unit of measure. Reference type: the engine mutates isConversionSource/Target in place.</summary>
internal sealed class Unit : IEquatable<Unit>
{
    internal static readonly Unit Empty = new(-1, string.Empty, string.Empty, true, true, false);

    internal Unit()
    {
    }

    internal Unit(int id, string name, string abbreviation, bool isConversionSource, bool isConversionTarget, bool isWhimsical)
    {
        this.id = id;
        this.name = name;
        accessibleName = name;
        this.abbreviation = abbreviation;
        this.isConversionSource = isConversionSource;
        this.isConversionTarget = isConversionTarget;
        this.isWhimsical = isWhimsical;
    }

    internal int id = -1;
    internal string name = string.Empty;
    internal string accessibleName = string.Empty;
    internal string abbreviation = string.Empty;
    internal bool isConversionSource;
    internal bool isConversionTarget;
    internal bool isWhimsical;

    public bool Equals(Unit? other) => other is not null && id == other.id;

    public override bool Equals(object? obj) => Equals(obj as Unit);

    public override int GetHashCode() => id;
}

/// <summary>A converter category. Value type, matching the donor; equality is by id only.</summary>
internal struct Category : IEquatable<Category>
{
    internal Category(int id, string? name, bool supportsNegative)
    {
        this.id = id;
        this.name = name;
        this.supportsNegative = supportsNegative;
    }

    internal int id;
    internal string? name;
    internal bool supportsNegative;

    public bool Equals(Category other) => id == other.id;

    public override bool Equals(object? obj) => obj is Category other && id == other.id;

    public override int GetHashCode() => id;
}

/// <summary>Ratio/offset pair describing how to convert into one particular target unit.</summary>
internal sealed class ConversionData
{
    internal ConversionData()
    {
    }

    internal ConversionData(double ratio, double offset, bool offsetFirst)
    {
        this.ratio = ratio;
        this.offset = offset;
        this.offsetFirst = offsetFirst;
    }

    internal double ratio;
    internal double offset;
    internal bool offsetFirst;
}

internal struct SuggestedValueIntermediate
{
    internal double magnitude;
    internal double value;
    internal Unit type;
}

/// <summary>Supplies categories, units and ratios to the engine.</summary>
internal interface IConverterDataLoader
{
    void LoadData();

    List<Category> LoadOrderedCategories();

    List<Unit> LoadOrderedUnits(Category category);

    Dictionary<Unit, ConversionData> LoadOrderedRatios(Unit unit);

    bool SupportsCategory(Category target);
}

/// <summary>Receives display updates from the engine.</summary>
internal interface IUnitConverterVMCallback
{
    void DisplayCallback(string fromValue, string toValue);

    void SuggestedValueCallback(List<(string Value, Unit Unit)> suggestedValues);

    void MaxDigitsReached();
}

/// <summary>
/// The unit conversion engine: holds the current category, the from/to selection, the entered
/// digits, and the ratio tables, and pushes formatted results at the view model.
/// </summary>
internal sealed class UnitConverter
{
    private const int MaximumDigitsAllowed = 15;
    private const int OptimalDigitsAllowed = 7;
    private const char LeftEscapeChar = '{';
    private const char RightEscapeChar = '}';
    private const int ExpectedSerializedCategoryTokenCount = 3;
    private const int ExpectedSerializedUnitTokenCount = 6;

    private static readonly double OptimalDecimalAllowed = Math.Pow(10, -1 * (OptimalDigitsAllowed - 1));
    private static readonly double MinimumDecimalAllowed = Math.Pow(10, -1 * (MaximumDigitsAllowed - 1));

    private static readonly Dictionary<char, string> s_quoteConversions = new()
    {
        ['|'] = "{p}",
        ['['] = "{lc}",
        [']'] = "{rc}",
        [':'] = "{co}",
        [','] = "{cm}",
        [';'] = "{sc}",
        [LeftEscapeChar] = "{lb}",
        [RightEscapeChar] = "{rb}",
    };

    private static readonly Dictionary<string, char> s_unquoteConversions = new()
    {
        ["{p}"] = '|',
        ["{lc}"] = '[',
        ["{rc}"] = ']',
        ["{co}"] = ':',
        ["{cm}"] = ',',
        ["{sc}"] = ';',
        ["{lb}"] = LeftEscapeChar,
        ["{rb}"] = RightEscapeChar,
    };

    private readonly IConverterDataLoader _dataLoader;
    private readonly Dictionary<Category, List<Unit>> _categoryToUnits = new();
    private readonly Dictionary<Unit, Dictionary<Unit, ConversionData>> _ratioMap = new();

    private IUnitConverterVMCallback? _vmCallback;
    private List<Category> _categories = new();
    private Category _currentCategory;
    private Unit _fromType = Unit.Empty;
    private Unit _toType = Unit.Empty;
    private string _currentDisplay = "0";
    private string _returnDisplay = "0";
    private bool _currentHasDecimal;
    private bool _returnHasDecimal;
    private bool _switchedActive;

    internal UnitConverter(IConverterDataLoader dataLoader)
    {
        _dataLoader = dataLoader ?? throw new ArgumentNullException(nameof(dataLoader));
        ClearValues();
        ResetCategoriesAndRatios();
    }

    internal bool IsSwitchedActive => _switchedActive;

    internal void Initialize() => _dataLoader.LoadData();

    internal List<Category> GetCategories()
    {
        CheckLoad();
        return _categories;
    }

    internal Category GetCurrentCategory() => _currentCategory;

    /// <summary>
    /// Selects a category and returns its units along with the resulting from/to selection.
    /// </summary>
    internal (List<Unit> Units, Unit FromUnit, Unit ToUnit) SetCurrentCategory(Category input)
    {
        var newUnitList = new List<Unit>();

        if (CheckLoad())
        {
            if (_currentCategory.id != input.id)
            {
                if (_categoryToUnits.TryGetValue(_currentCategory, out List<Unit>? previousUnits))
                {
                    foreach (Unit unit in previousUnits)
                    {
                        unit.isConversionSource = unit.id == _fromType.id;
                        unit.isConversionTarget = unit.id == _toType.id;
                    }
                }

                _currentCategory = input;

                if (!_currentCategory.supportsNegative && _currentDisplay.Length > 0 && _currentDisplay[0] == '-')
                {
                    _currentDisplay = _currentDisplay.Substring(1);
                }
            }

            if (_categoryToUnits.TryGetValue(input, out List<Unit>? units))
            {
                newUnitList = units;
            }
        }

        InitializeSelectedUnits();
        return (newUnitList, _fromType, _toType);
    }

    internal void SetCurrentUnitTypes(Unit fromType, Unit toType)
    {
        if (!CheckLoad())
        {
            return;
        }

        _fromType = fromType;
        _toType = toType;
        Calculate();
    }

    /// <summary>
    /// Swaps which field the user is typing into. Values are swapped but no callback fires,
    /// because nothing has actually changed value.
    /// </summary>
    internal void SwitchActive(string newValue)
    {
        if (!CheckLoad())
        {
            return;
        }

        Swap(ref _fromType, ref _toType);
        Swap(ref _currentHasDecimal, ref _returnHasDecimal);
        _returnDisplay = _currentDisplay;
        _currentDisplay = newValue ?? "0";
        _currentHasDecimal = _currentDisplay.IndexOf('.') >= 0;
        _switchedActive = true;
    }

    internal void SendCommand(UnitConverterCommand command)
    {
        if (!CheckLoad())
        {
            return;
        }

        bool clearFront = _currentDisplay == "0";
        bool clearBack = (_currentHasDecimal && _currentDisplay.Length - 1 >= MaximumDigitsAllowed)
            || (!_currentHasDecimal && _currentDisplay.Length >= MaximumDigitsAllowed);

        if (command != UnitConverterCommand.Negate && _switchedActive)
        {
            ClearValues();
            _switchedActive = false;
            clearFront = true;
            clearBack = false;
        }

        switch (command)
        {
            case UnitConverterCommand.Zero:
            case UnitConverterCommand.One:
            case UnitConverterCommand.Two:
            case UnitConverterCommand.Three:
            case UnitConverterCommand.Four:
            case UnitConverterCommand.Five:
            case UnitConverterCommand.Six:
            case UnitConverterCommand.Seven:
            case UnitConverterCommand.Eight:
            case UnitConverterCommand.Nine:
                _currentDisplay += (char)('0' + (int)command);
                break;

            case UnitConverterCommand.Decimal:
                clearFront = false;
                clearBack = false;
                if (!_currentHasDecimal)
                {
                    _currentDisplay += ".";
                    _currentHasDecimal = true;
                }

                break;

            case UnitConverterCommand.Backspace:
                clearFront = false;
                clearBack = false;
                if ((_currentDisplay.Length > 1 && _currentDisplay[0] != '-') || _currentDisplay.Length > 2)
                {
                    if (_currentDisplay[_currentDisplay.Length - 1] == '.')
                    {
                        _currentHasDecimal = false;
                    }

                    _currentDisplay = _currentDisplay.Substring(0, _currentDisplay.Length - 1);
                }
                else
                {
                    _currentDisplay = "0";
                    _currentHasDecimal = false;
                }

                break;

            case UnitConverterCommand.Negate:
                clearFront = false;
                clearBack = false;
                if (_currentCategory.supportsNegative && _currentDisplay.Length > 0)
                {
                    _currentDisplay = _currentDisplay[0] == '-'
                        ? _currentDisplay.Substring(1)
                        : "-" + _currentDisplay;
                }

                break;

            case UnitConverterCommand.Clear:
                clearFront = false;
                clearBack = false;
                ClearValues();
                break;

            case UnitConverterCommand.Reset:
                clearFront = false;
                clearBack = false;
                ClearValues();
                ResetCategoriesAndRatios();
                break;

            default:
                break;
        }

        if (clearFront && _currentDisplay.Length > 0)
        {
            _currentDisplay = _currentDisplay.Substring(1);
        }

        if (clearBack && _currentDisplay.Length > 0)
        {
            _currentDisplay = _currentDisplay.Substring(0, _currentDisplay.Length - 1);
            _vmCallback?.MaxDigitsReached();
        }

        Calculate();
    }

    internal void SetViewModelCallback(IUnitConverterVMCallback? newCallback)
    {
        _vmCallback = newCallback;
        if (CheckLoad())
        {
            UpdateViewModel();
        }
    }

    /// <summary>Serializes just the current category and unit selection, for roaming settings.</summary>
    internal string SaveUserPreferences()
    {
        const char delimiter = ';';

        using var writer = new StringWriter(CultureInfo.InvariantCulture);
        writer.Write(UnitToString(_fromType, delimiter));
        writer.Write('|');
        writer.Write(UnitToString(_toType, delimiter));
        writer.Write('|');
        writer.Write(CategoryToString(_currentCategory, delimiter));
        writer.Write('|');
        return writer.ToString();
    }

    internal void RestoreUserPreferences(string? userPreferences)
    {
        if (string.IsNullOrEmpty(userPreferences))
        {
            return;
        }

        List<string> outerTokens = Split(userPreferences!, '|');
        if (outerTokens.Count != 3)
        {
            return;
        }

        Unit? fromType = StringToUnit(outerTokens[0]);
        Unit? toType = StringToUnit(outerTokens[1]);
        Category? category = StringToCategory(outerTokens[2]);
        if (fromType is null || toType is null || category is null)
        {
            return;
        }

        _currentCategory = category.Value;

        // Only restore the saved units if they are still valid in the current unit list.
        if (_categoryToUnits.TryGetValue(_currentCategory, out List<Unit>? currentUnits))
        {
            if (currentUnits.Contains(fromType))
            {
                _fromType = fromType;
            }

            if (currentUnits.Contains(toType))
            {
                _toType = toType;
            }
        }
    }

    /// <summary>Re-reads every category, unit and ratio from the data loader.</summary>
    internal void ResetCategoriesAndRatios()
    {
        _categories = _dataLoader.LoadOrderedCategories() ?? new List<Category>();
        _switchedActive = false;

        if (_categories.Count == 0)
        {
            return;
        }

        _currentCategory = _categories[0];
        _categoryToUnits.Clear();
        _ratioMap.Clear();

        bool readyCategoryFound = false;
        foreach (Category category in _categories)
        {
            List<Unit> units = _dataLoader.LoadOrderedUnits(category) ?? new List<Unit>();
            _categoryToUnits[category] = units;

            // An empty category is still selectable, we just do not want it to be the default.
            if (units.Count == 0)
            {
                continue;
            }

            foreach (Unit unit in units)
            {
                _ratioMap[unit] = _dataLoader.LoadOrderedRatios(unit) ?? new Dictionary<Unit, ConversionData>();
            }

            if (!readyCategoryFound)
            {
                _currentCategory = category;
                readyCategoryFound = true;
            }
        }

        InitializeSelectedUnits();
    }

    /// <summary>Recomputes the target field from the entered value and pushes it at the view model.</summary>
    internal void Calculate()
    {
        if (AnyUnitIsEmpty()
            || !_ratioMap.TryGetValue(_fromType, out Dictionary<Unit, ConversionData>? conversionTable)
            || !conversionTable.TryGetValue(_toType, out ConversionData? conversion))
        {
            _returnDisplay = _currentDisplay;
            _returnHasDecimal = _currentHasDecimal;
            _returnDisplay = TrimString(_returnDisplay);
            UpdateViewModel();
            return;
        }

        double returnValue = ToDouble(_currentDisplay);

        if (conversion.ratio == 1.0 && conversion.offset == 0.0)
        {
            _returnDisplay = TrimString(_currentDisplay);
            _returnHasDecimal = _currentHasDecimal;
        }
        else
        {
            returnValue = Convert(returnValue, conversion);
            _returnDisplay = TrimString(RoundSignificant(returnValue, MaximumDigitsAllowed));

            int decimalIndex = _returnDisplay.IndexOf('.');
            int numPreDecimal = decimalIndex >= 0 ? decimalIndex : _returnDisplay.Length;
            if (returnValue < 0)
            {
                numPreDecimal--;
            }

            if (numPreDecimal > MaximumDigitsAllowed
                || (returnValue != 0 && Math.Abs(returnValue) < MinimumDecimalAllowed))
            {
                _returnDisplay = returnValue.ToString("e", CultureInfo.InvariantCulture);
            }
            else
            {
                returnValue = ToDouble(_returnDisplay);
                string rounded = _currentDisplay.Length <= OptimalDigitsAllowed && Math.Abs(returnValue) >= OptimalDecimalAllowed
                    ? RoundSignificant(returnValue, OptimalDigitsAllowed - Math.Min(numPreDecimal, OptimalDigitsAllowed))
                    : RoundSignificant(returnValue, MaximumDigitsAllowed - Math.Min(numPreDecimal, MaximumDigitsAllowed));

                _returnDisplay = TrimString(rounded);
            }

            _returnHasDecimal = _returnDisplay.IndexOf('.') >= 0;
        }

        UpdateViewModel();
    }

    #region Internals

    private bool CheckLoad()
    {
        if (_categories.Count == 0)
        {
            ResetCategoriesAndRatios();
        }

        return _categories.Count != 0;
    }

    private bool AnyUnitIsEmpty() => _fromType.id == Unit.Empty.id || _toType.id == Unit.Empty.id;

    private void ClearValues()
    {
        _currentHasDecimal = false;
        _returnHasDecimal = false;
        _currentDisplay = "0";
    }

    /// <summary>
    /// Picks the initial from/to units, preferring any selection already restored from user
    /// preferences. Falls back to <see cref="Unit.Empty"/> when the category has nothing usable.
    /// </summary>
    private void InitializeSelectedUnits()
    {
        if (_categoryToUnits.Count == 0
            || !_categoryToUnits.TryGetValue(_currentCategory, out List<Unit>? currentUnits))
        {
            return;
        }

        if (currentUnits.Count > 0)
        {
            bool isFromUnitValid = _fromType.id != Unit.Empty.id && currentUnits.Contains(_fromType);
            bool isToUnitValid = _toType.id != Unit.Empty.id && currentUnits.Contains(_toType);

            if (isFromUnitValid && isToUnitValid)
            {
                return;
            }

            bool conversionSourceSet = false;
            bool conversionTargetSet = false;
            foreach (Unit unit in currentUnits)
            {
                if (!conversionSourceSet && unit.isConversionSource && !isFromUnitValid)
                {
                    _fromType = unit;
                    conversionSourceSet = true;
                }

                if (!conversionTargetSet && unit.isConversionTarget && !isToUnitValid)
                {
                    _toType = unit;
                    conversionTargetSet = true;
                }

                if (conversionSourceSet && conversionTargetSet)
                {
                    return;
                }
            }
        }

        _fromType = Unit.Empty;
        _toType = Unit.Empty;
    }

    private static double Convert(double value, ConversionData conversionData)
        => conversionData.offsetFirst
            ? (value + conversionData.offset) * conversionData.ratio
            : (value * conversionData.ratio) + conversionData.offset;

    /// <summary>
    /// Builds the "also equals" rows: every other unit in the category, ordered by how close its
    /// converted magnitude is to 1, with the single best whimsical unit appended.
    /// </summary>
    private List<(string Value, Unit Unit)> CalculateSuggested()
    {
        var results = new List<(string, Unit)>();
        if (!_ratioMap.TryGetValue(_fromType, out Dictionary<Unit, ConversionData>? ratios))
        {
            return results;
        }

        double currentValue = ToDouble(_currentDisplay);
        var ordinary = new List<SuggestedValueIntermediate>();
        var whimsical = new List<SuggestedValueIntermediate>();

        foreach (KeyValuePair<Unit, ConversionData> ratio in ratios)
        {
            if (ratio.Key.id == _fromType.id || ratio.Key.id == _toType.id)
            {
                continue;
            }

            double converted = Convert(currentValue, ratio.Value);
            var entry = new SuggestedValueIntermediate
            {
                magnitude = Math.Log10(converted),
                value = converted,
                type = ratio.Key,
            };

            if (entry.type.isWhimsical)
            {
                whimsical.Add(entry);
            }
            else
            {
                ordinary.Add(entry);
            }
        }

        ordinary.Sort(CompareByMagnitude);
        whimsical.Sort(CompareByMagnitude);

        foreach (SuggestedValueIntermediate entry in ordinary)
        {
            string rounded = RoundForSuggestion(entry.value);
            if (ToDouble(rounded) != 0.0 || _currentCategory.supportsNegative)
            {
                results.Add((TrimString(rounded), entry.type));
            }
        }

        // Only the single best whimsical value is shown.
        foreach (SuggestedValueIntermediate entry in whimsical)
        {
            string rounded = RoundForSuggestion(entry.value);
            if (ToDouble(rounded) != 0.0)
            {
                results.Add((TrimString(rounded), entry.type));
                break;
            }
        }

        return results;
    }

    /// <summary>
    /// Smallest absolute magnitude first; ties broken in favour of the positive one. Written as a
    /// real total order because List&lt;T&gt;.Sort rejects an inconsistent comparer at runtime.
    /// </summary>
    private static int CompareByMagnitude(SuggestedValueIntermediate first, SuggestedValueIntermediate second)
    {
        double left = Math.Abs(first.magnitude);
        double right = Math.Abs(second.magnitude);
        int byAbsolute = left.CompareTo(right);
        return byAbsolute != 0 ? byAbsolute : second.magnitude.CompareTo(first.magnitude);
    }

    private static string RoundForSuggestion(double value)
    {
        double magnitude = Math.Abs(value);
        if (magnitude < 100)
        {
            return RoundSignificant(value, 2);
        }

        return magnitude < 1000 ? RoundSignificant(value, 1) : RoundSignificant(value, 0);
    }

    private void UpdateViewModel()
    {
        if (_vmCallback is null)
        {
            return;
        }

        _vmCallback.DisplayCallback(_currentDisplay, _returnDisplay);
        _vmCallback.SuggestedValueCallback(CalculateSuggested());
    }

    private static string RoundSignificant(double value, int digits)
        => value.ToString("F" + Math.Max(digits, 0).ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);

    private static string TrimString(string value)
    {
        if (value.IndexOf('.') < 0)
        {
            return value;
        }

        return value.TrimEnd('0').TrimEnd('.');
    }

    private static double ToDouble(string value)
        => double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double result) ? result : 0.0;

    private static void Swap<T>(ref T first, ref T second) => (first, second) = (second, first);

    #endregion

    #region User preference serialization

    private static string CategoryToString(Category category, char delimiter)
    {
        using var writer = new StringWriter(CultureInfo.InvariantCulture);
        writer.Write(Quote(category.id.ToString(CultureInfo.InvariantCulture)));
        writer.Write(delimiter);
        writer.Write(Quote(category.supportsNegative ? "1" : "0"));
        writer.Write(delimiter);
        writer.Write(Quote(category.name ?? string.Empty));
        writer.Write(delimiter);
        return writer.ToString();
    }

    private static Category? StringToCategory(string serialized)
    {
        List<string> tokens = Split(serialized, ';');
        if (tokens.Count < ExpectedSerializedCategoryTokenCount
            || !int.TryParse(Unquote(tokens[0]), NumberStyles.Integer, CultureInfo.InvariantCulture, out int id))
        {
            return null;
        }

        return new Category(id, Unquote(tokens[2]), Unquote(tokens[1]) == "1");
    }

    private static string UnitToString(Unit unit, char delimiter)
    {
        using var writer = new StringWriter(CultureInfo.InvariantCulture);
        writer.Write(Quote(unit.id.ToString(CultureInfo.InvariantCulture)));
        writer.Write(delimiter);
        writer.Write(Quote(unit.name));
        writer.Write(delimiter);
        writer.Write(Quote(unit.abbreviation));
        writer.Write(delimiter);
        writer.Write(unit.isConversionSource ? "1" : "0");
        writer.Write(delimiter);
        writer.Write(unit.isConversionTarget ? "1" : "0");
        writer.Write(delimiter);
        writer.Write(unit.isWhimsical ? "1" : "0");
        writer.Write(delimiter);
        return writer.ToString();
    }

    private static Unit? StringToUnit(string serialized)
    {
        List<string> tokens = Split(serialized, ';');
        if (tokens.Count < ExpectedSerializedUnitTokenCount
            || !int.TryParse(Unquote(tokens[0]), NumberStyles.Integer, CultureInfo.InvariantCulture, out int id))
        {
            return null;
        }

        return new Unit(
            id,
            Unquote(tokens[1]),
            Unquote(tokens[2]),
            tokens[3] == "1",
            tokens[4] == "1",
            tokens[5] == "1");
    }

    private static List<string> Split(string value, char delimiter)
    {
        var tokens = new List<string>();
        int start = 0;
        int index = value.IndexOf(delimiter);
        while (index >= 0)
        {
            tokens.Add(value.Substring(start, index - start));
            start = index + 1;
            index = value.IndexOf(delimiter, start);
        }

        return tokens;
    }

    /// <summary>Escapes the characters used as delimiters by the preference format.</summary>
    private static string Quote(string? value)
    {
        using var writer = new StringWriter(CultureInfo.InvariantCulture);
        foreach (char c in value ?? string.Empty)
        {
            if (s_quoteConversions.TryGetValue(c, out string? escaped))
            {
                writer.Write(escaped);
            }
            else
            {
                writer.Write(c);
            }
        }

        return writer.ToString();
    }

    private static string Unquote(string? value)
    {
        using var writer = new StringWriter(CultureInfo.InvariantCulture);
        using IEnumerator<char> cursor = ((IEnumerable<char>)(value ?? string.Empty)).GetEnumerator();

        while (cursor.MoveNext())
        {
            if (cursor.Current != LeftEscapeChar)
            {
                writer.Write(cursor.Current);
                continue;
            }

            using var escaped = new StringWriter(CultureInfo.InvariantCulture);
            while (cursor.MoveNext() && cursor.Current != RightEscapeChar)
            {
                escaped.Write(cursor.Current);
            }

            if (!s_unquoteConversions.TryGetValue(LeftEscapeChar + escaped.ToString() + RightEscapeChar, out char unescaped))
            {
                // Badly formatted; stop rather than emit garbage.
                break;
            }

            writer.Write(unescaped);
        }

        return writer.ToString();
    }

    #endregion
}
