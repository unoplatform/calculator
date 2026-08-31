// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//
// Shape from 4fd3fc53:src/CalcManager.Interop/UnitConverterInterop.idl:73-79, with the same
// protected-not-public correction as ConverterDataLoaderBase: UnitConverterViewModel's nested
// UnitConverterVMCallback declares all three as `protected override`.

#nullable enable

using System.Collections.Generic;
using CalcManager.Interop.Internal;

namespace CalcManager.Interop;

/// <summary>
/// Base class for the object that receives display updates from the unit converter engine.
/// </summary>
public class UnitConverterVMCallbackBase : IUnitConverterVMCallback
{
    public UnitConverterVMCallbackBase()
    {
    }

    /// <summary>The two converter fields have new values.</summary>
    protected virtual void DisplayCallback(string fromValue, string toValue)
    {
    }

    /// <summary>The "also equals ..." rows have been recomputed.</summary>
    protected virtual void SuggestedValueCallback(SuggestedValueWrapper[] suggestedValues)
    {
    }

    /// <summary>The entered value hit the engine's digit limit; the last keystroke was dropped.</summary>
    protected virtual void MaxDigitsReached()
    {
    }

    // Explicit: identical signatures to the protected virtuals above, so an implicit
    // implementation would be a duplicate member.

    void IUnitConverterVMCallback.DisplayCallback(string fromValue, string toValue)
        => DisplayCallback(fromValue, toValue);

    void IUnitConverterVMCallback.SuggestedValueCallback(List<(string Value, Unit Unit)> suggestedValues)
        => SuggestedValueCallback(ConverterMarshal.ToSuggestedValues(suggestedValues));

    void IUnitConverterVMCallback.MaxDigitsReached() => MaxDigitsReached();
}
