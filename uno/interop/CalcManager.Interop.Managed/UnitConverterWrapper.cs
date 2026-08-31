// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//
// Shape from 4fd3fc53:src/CalcManager.Interop/UnitConverterInterop.idl:101-118, minus
// SetViewModelCurrencyCallback: no consumer calls it (the ingested ViewModel routes currency
// through its own managed DataLoaders.IViewModelCurrencyCallback), and omitting it means
// ViewModelCurrencyCallbackBase does not have to exist as a dangling parameter type.
//
// This wrapper is 100% managed. There is no native ABI for unit conversion.

#nullable enable

using System;
using CalcManager.Interop.Internal;

namespace CalcManager.Interop;

/// <summary>
/// Managed facade over the unit converter engine.
/// </summary>
public sealed class UnitConverterWrapper
{
    private readonly UnitConverter _converter;

    public UnitConverterWrapper(ConverterDataLoaderBase dataLoader)
    {
        if (dataLoader is null)
        {
            throw new ArgumentNullException(nameof(dataLoader));
        }

        _converter = new UnitConverter(dataLoader);
    }

    /// <summary>True while the user has swapped fields but not yet typed anything new.</summary>
    public bool IsSwitchedActive => _converter.IsSwitchedActive;

    public void Initialize() => _converter.Initialize();

    public CategoryWrapper[] GetCategories() => ConverterMarshal.ToWrapperArray(_converter.GetCategories());

    public CategoryWrapper GetCurrentCategory() => ConverterMarshal.ToWrapper(_converter.GetCurrentCategory());

    public CategorySelectionResult SetCurrentCategory(CategoryWrapper category)
    {
        (System.Collections.Generic.List<Unit> units, Unit fromUnit, Unit toUnit) =
            _converter.SetCurrentCategory(ConverterMarshal.ToCategory(category));

        return new CategorySelectionResult(
            ConverterMarshal.ToWrapperArray(units),
            ConverterMarshal.ToWrapper(fromUnit),
            ConverterMarshal.ToWrapper(toUnit));
    }

    public void SetCurrentUnitTypes(UnitWrapper fromType, UnitWrapper toType)
        => _converter.SetCurrentUnitTypes(ConverterMarshal.ToUnit(fromType), ConverterMarshal.ToUnit(toType));

    public void SwitchActive(string newValue) => _converter.SwitchActive(newValue);

    public string SaveUserPreferences() => _converter.SaveUserPreferences();

    public void RestoreUserPreferences(string userPreference) => _converter.RestoreUserPreferences(userPreference);

    public void SendCommand(UnitConverterCommand command) => _converter.SendCommand(command);

    public void SetViewModelCallback(UnitConverterVMCallbackBase callback)
        => _converter.SetViewModelCallback(callback);

    public void Calculate() => _converter.Calculate();

    public void ResetCategoriesAndRatios() => _converter.ResetCategoriesAndRatios();
}
