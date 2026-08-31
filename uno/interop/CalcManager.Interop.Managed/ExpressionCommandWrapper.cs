// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//
// Port of 4fd3fc53:src/CalcManager.Interop/ExpressionCommandWrapper.{h,cpp}.

#nullable enable

using System;
using System.Collections.Generic;

namespace CalcManager.Interop;

/// <summary>
/// A flattened snapshot of a native IExpressionCommand. The four native command shapes
/// (unary / binary / operand / parentheses) all collapse into this one type, discriminated by
/// <see cref="Type"/>. Every field is read eagerly at unmarshal time so no native pointer
/// outlives the history item it came from.
/// </summary>
public sealed class ExpressionCommandWrapper
{
    private readonly int[] _commands;

    public ExpressionCommandWrapper()
    {
        _commands = Array.Empty<int>();
    }

    public ExpressionCommandWrapper(
        CommandType type,
        int command,
        IReadOnlyList<int>? commands,
        bool isNegative,
        bool isDecimalPresent,
        bool isSciFmt)
    {
        Type = type;
        Command = command;
        IsNegative = isNegative;
        IsDecimalPresent = isDecimalPresent;
        IsSciFmt = isSciFmt;

        // Copy: JsonUtils hands us an array it also hands to a JSON alias.
        if (commands is null || commands.Count == 0)
        {
            _commands = Array.Empty<int>();
        }
        else
        {
            var copy = new int[commands.Count];
            for (int i = 0; i < commands.Count; i++)
            {
                copy[i] = commands[i];
            }

            _commands = copy;
        }
    }

    public CommandType Type { get; }

    public int Command { get; }

    /// <summary>Never null; empty for binary and parenthesis commands.</summary>
    public int[] Commands => _commands;

    public bool IsNegative { get; }

    public bool IsDecimalPresent { get; }

    public bool IsSciFmt { get; }
}
