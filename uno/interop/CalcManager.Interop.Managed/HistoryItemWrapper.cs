// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//
// Port of 4fd3fc53:src/CalcManager.Interop/CalcManagerInterop.idl:227-241.

#nullable enable

using System;

namespace CalcManager.Interop;

/// <summary>
/// One entry of the calculator history: the rendered token stream, the commands that produced it,
/// and the final expression/result strings.
/// </summary>
public sealed class HistoryItemWrapper
{
    public HistoryItemWrapper()
    {
        Tokens = Array.Empty<HistoryToken>();
        Commands = Array.Empty<ExpressionCommandWrapper>();
        Expression = string.Empty;
        Result = string.Empty;
    }

    public HistoryItemWrapper(
        HistoryToken[]? tokens,
        ExpressionCommandWrapper[]? commands,
        string? expression,
        string? result)
    {
        Tokens = tokens ?? Array.Empty<HistoryToken>();
        Commands = commands ?? Array.Empty<ExpressionCommandWrapper>();
        Expression = expression ?? string.Empty;
        Result = result ?? string.Empty;
    }

    public HistoryToken[] Tokens { get; }

    public ExpressionCommandWrapper[] Commands { get; }

    public string Expression { get; }

    public string Result { get; }
}
