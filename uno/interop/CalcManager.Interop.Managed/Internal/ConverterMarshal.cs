// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//
// Translation between the public CalcManager.Interop wrapper types (PascalCase properties, the
// shape the .idl and the ingested ViewModels agree on) and the internal engine types (lowerCamel
// fields, harvested from the fork). Equality on both engine types is by id alone, so crossing
// this seam repeatedly does not break the ratio or category lookups even though object identity
// is lost each time.

#nullable enable

using System;
using System.Collections.Generic;

namespace CalcManager.Interop.Internal;

internal static class ConverterMarshal
{
    internal static Category ToCategory(CategoryWrapper? wrapper)
        => wrapper is null
            ? default
            : new Category(wrapper.Id, wrapper.Name, wrapper.SupportsNegative);

    internal static CategoryWrapper ToWrapper(Category category) => new()
    {
        Id = category.id,
        Name = category.name ?? string.Empty,
        SupportsNegative = category.supportsNegative,
    };

    internal static Unit ToUnit(UnitWrapper? wrapper)
    {
        if (wrapper is null)
        {
            return Unit.Empty;
        }

        return new Unit(
            wrapper.Id,
            wrapper.Name ?? string.Empty,
            wrapper.Abbreviation ?? string.Empty,
            wrapper.IsConversionSource,
            wrapper.IsConversionTarget,
            wrapper.IsWhimsical)
        {
            // The six-argument constructor mirrors accessibleName from name; the wrapper carries
            // its own, which is what the narrator reads.
            accessibleName = wrapper.AccessibleName ?? wrapper.Name ?? string.Empty,
        };
    }

    internal static UnitWrapper ToWrapper(Unit unit) => new()
    {
        Id = unit.id,
        Name = unit.name,
        AccessibleName = unit.accessibleName,
        Abbreviation = unit.abbreviation,
        IsConversionSource = unit.isConversionSource,
        IsConversionTarget = unit.isConversionTarget,
        IsWhimsical = unit.isWhimsical,
    };

    internal static List<Category> ToCategoryList(CategoryWrapper[]? wrappers)
    {
        if (wrappers is null || wrappers.Length == 0)
        {
            return new List<Category>();
        }

        var result = new List<Category>(wrappers.Length);
        foreach (CategoryWrapper wrapper in wrappers)
        {
            result.Add(ToCategory(wrapper));
        }

        return result;
    }

    internal static List<Unit> ToUnitList(UnitWrapper[]? wrappers)
    {
        if (wrappers is null || wrappers.Length == 0)
        {
            return new List<Unit>();
        }

        var result = new List<Unit>(wrappers.Length);
        foreach (UnitWrapper wrapper in wrappers)
        {
            result.Add(ToUnit(wrapper));
        }

        return result;
    }

    internal static UnitWrapper[] ToWrapperArray(List<Unit>? units)
    {
        if (units is null || units.Count == 0)
        {
            return Array.Empty<UnitWrapper>();
        }

        var result = new UnitWrapper[units.Count];
        for (int i = 0; i < units.Count; i++)
        {
            result[i] = ToWrapper(units[i]);
        }

        return result;
    }

    internal static CategoryWrapper[] ToWrapperArray(List<Category>? categories)
    {
        if (categories is null || categories.Count == 0)
        {
            return Array.Empty<CategoryWrapper>();
        }

        var result = new CategoryWrapper[categories.Count];
        for (int i = 0; i < categories.Count; i++)
        {
            result[i] = ToWrapper(categories[i]);
        }

        return result;
    }

    /// <summary>
    /// The engine keys conversion data by target unit; the wrapper carries the target inline.
    /// Later entries win on a duplicate id, matching the engine's own map semantics.
    /// </summary>
    internal static Dictionary<Unit, ConversionData> ToRatioMap(UnitConversionEntry[]? entries)
    {
        var result = new Dictionary<Unit, ConversionData>();
        if (entries is null)
        {
            return result;
        }

        foreach (UnitConversionEntry entry in entries)
        {
            if (entry?.Unit is null)
            {
                continue;
            }

            result[ToUnit(entry.Unit)] = new ConversionData(entry.Ratio, entry.Offset, entry.OffsetFirst);
        }

        return result;
    }

    internal static SuggestedValueWrapper[] ToSuggestedValues(List<(string Value, Unit Unit)>? suggested)
    {
        if (suggested is null || suggested.Count == 0)
        {
            return Array.Empty<SuggestedValueWrapper>();
        }

        var result = new SuggestedValueWrapper[suggested.Count];
        for (int i = 0; i < suggested.Count; i++)
        {
            result[i] = new SuggestedValueWrapper(suggested[i].Value, ToWrapper(suggested[i].Unit));
        }

        return result;
    }
}
