// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//
// Shape from 4fd3fc53:src/CalcManager.Interop/UnitConverterInterop.idl:88-95, but with the
// overridables declared PROTECTED, not public. The .idl says `overridable`, which reads as
// public virtual; the ingested UnitConverterDataLoader declares every one of them
// `protected override` and compiles into this same assembly, so protected is what C# requires.
// Getting this wrong is CS0507 on all five members.

#nullable enable

using System;
using System.Collections.Generic;
using CalcManager.Interop.Internal;

namespace CalcManager.Interop;

/// <summary>
/// Base class for the object that feeds categories, units and conversion ratios to the unit
/// converter engine. Derived types override the five protected members; the engine talks to the
/// explicit <c>IConverterDataLoader</c> implementation below, which adapts the wrapper types to
/// the engine's own.
/// </summary>
public class ConverterDataLoaderBase : IConverterDataLoader
{
    public ConverterDataLoaderBase()
    {
    }

    /// <summary>Prepares the data. Called once by the engine before anything else is read.</summary>
    protected virtual void LoadData()
    {
    }

    protected virtual CategoryWrapper[] GetOrderedCategories() => Array.Empty<CategoryWrapper>();

    protected virtual UnitWrapper[] GetOrderedUnits(CategoryWrapper category) => Array.Empty<UnitWrapper>();

    protected virtual UnitConversionEntry[] LoadOrderedRatios(UnitWrapper unit) => Array.Empty<UnitConversionEntry>();

    protected virtual bool SupportsCategory(CategoryWrapper target) => true;

    // Explicit implementations: each of these has a signature that would otherwise collide with
    // the protected virtual of the same name. Explicit members are excluded from ordinary member
    // lookup, so an unqualified LoadData() inside this class still binds to the virtual above.

    void IConverterDataLoader.LoadData() => LoadData();

    List<Category> IConverterDataLoader.LoadOrderedCategories()
        => ConverterMarshal.ToCategoryList(GetOrderedCategories());

    List<Unit> IConverterDataLoader.LoadOrderedUnits(Category category)
        => ConverterMarshal.ToUnitList(GetOrderedUnits(ConverterMarshal.ToWrapper(category)));

    Dictionary<Unit, ConversionData> IConverterDataLoader.LoadOrderedRatios(Unit unit)
        => ConverterMarshal.ToRatioMap(LoadOrderedRatios(ConverterMarshal.ToWrapper(unit)));

    bool IConverterDataLoader.SupportsCategory(Category target)
        => SupportsCategory(ConverterMarshal.ToWrapper(target));
}
