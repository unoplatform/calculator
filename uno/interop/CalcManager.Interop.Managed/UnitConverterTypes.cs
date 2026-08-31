// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//
// Public shape from 4fd3fc53:src/CalcManager.Interop/UnitConverterInterop.idl:1-70.
// The .idl declares UnitWrapper / CategoryWrapper / UnitConversionEntry as WinRT structs; they
// are sealed classes here because every consumer site is an object initializer or a member read
// (no wrapper is ever a dictionary key, and none are compared with == anywhere), and because a
// default struct instance would hand out null Name/Abbreviation to UnitConverterViewModel.

#nullable enable

using System;

namespace CalcManager.Interop;

/// <summary>
/// Keypad commands understood by the unit converter engine. Values match the fork's
/// UnitConversionManager.Command ordinals exactly.
/// </summary>
public enum UnitConverterCommand
{
    Zero = 0,
    One = 1,
    Two = 2,
    Three = 3,
    Four = 4,
    Five = 5,
    Six = 6,
    Seven = 7,
    Eight = 8,
    Nine = 9,
    Decimal = 10,
    Negate = 11,
    Backspace = 12,
    Clear = 13,
    Reset = 14,
    None = 15,
}

/// <summary>One unit of measure inside a category.</summary>
public sealed class UnitWrapper
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string AccessibleName { get; set; } = string.Empty;

    public string Abbreviation { get; set; } = string.Empty;

    public bool IsConversionSource { get; set; }

    public bool IsConversionTarget { get; set; }

    public bool IsWhimsical { get; set; }
}

/// <summary>One converter category (Length, Volume, Currency, ...).</summary>
public sealed class CategoryWrapper
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public bool SupportsNegative { get; set; }
}

/// <summary>A single source-unit to target-unit conversion rule.</summary>
public sealed class UnitConversionEntry
{
    public UnitWrapper? Unit { get; set; }

    public double Ratio { get; set; }

    public double Offset { get; set; }

    public bool OffsetFirst { get; set; }
}

/// <summary>One of the "also equals ..." rows shown under the converter result.</summary>
public sealed class SuggestedValueWrapper
{
    internal SuggestedValueWrapper(string value, UnitWrapper unit)
    {
        Value = value ?? string.Empty;
        Unit = unit;
    }

    public string Value { get; }

    public UnitWrapper Unit { get; }
}

/// <summary>
/// What <see cref="UnitConverterWrapper.SetCurrentCategory"/> hands back: the category's units
/// plus the engine's chosen from/to selection.
/// </summary>
public sealed class CategorySelectionResult
{
    internal CategorySelectionResult(UnitWrapper[]? units, UnitWrapper fromUnit, UnitWrapper toUnit)
    {
        Units = units ?? Array.Empty<UnitWrapper>();
        FromUnit = fromUnit;
        ToUnit = toUnit;
    }

    public UnitWrapper[] Units { get; }

    public UnitWrapper FromUnit { get; }

    public UnitWrapper ToUnit { get; }
}
