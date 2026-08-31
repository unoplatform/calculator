// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//
// Port of 4fd3fc53:src/CalcManager.Interop/HistoryToken.h.

#nullable enable

namespace CalcManager.Interop;

/// <summary>
/// One token of a rendered expression: the display text plus the index of the command that
/// produced it. Settable because StandardCalculatorViewModel builds these with an object
/// initializer when restoring a snapshot.
/// </summary>
public sealed class HistoryToken
{
    public HistoryToken()
    {
    }

    public HistoryToken(string value, int commandIndex)
    {
        Value = value ?? string.Empty;
        CommandIndex = commandIndex;
    }

    public string Value { get; set; } = string.Empty;

    public int CommandIndex { get; set; }
}
