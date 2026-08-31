// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//
// Public shape from 4fd3fc53:src/CalcManager.Interop/CalcManagerInterop.idl:255-301.
// Bodies harvested from D:/Work/calculator/src/Calculator.Shared/CalcManager/CalculatorManager.cs
// (one-liners + UnmarshalHistoryItemResult) and ExpressionCommandInterface.cs (command readers).
//
// Structural rewrites vs the donor:
//   * the donor took (ref CalculatorDisplay, ref EngineResourceProvider) and constructed its own;
//     upstream passes twelve loose delegates, so the sink below replaces those two WinRT types.
//   * IsEngineRecording / MaxHistorySize are properties here, methods on the donor.
//   * history comes back as HistoryItemWrapper[] of HistoryToken[] + ExpressionCommandWrapper[],
//     not CalculatorList<(string,int)> + CalculatorList<IExpressionCommand>.
//   * expression commands are read eagerly into a snapshot. The donor kept the raw IntPtr, which
//     is a borrowed pointer into the native shared_ptr graph and a use-after-free waiting to
//     happen (CCalcManager.cpp:255-258 stores command.get()).

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace CalcManager.Interop;

/// <summary>
/// Managed facade over the native CalcManager engine.
/// </summary>
/// <remarks>
/// The constructor is allowed to throw, and on a machine with no native CalcManager binary it
/// will (DllNotFoundException from CalculatorManager_Create). That is the designed path:
/// StandardCalculatorViewModel wraps construction in try/catch, nulls its field, and null-guards
/// every later call, so the app renders without calculating.
/// </remarks>
public sealed unsafe class CalculatorManagerWrapper
{
    // Mono's wasm backend builds native->managed trampolines at compile time from
    // [UnmanagedCallersOnly] statics. Function pointers obtained from delegates via
    // Marshal.GetFunctionPointerForDelegate have no trampoline there, so the engine's
    // callbacks never arrive and the display never updates. Uno Workaround: none needed -
    // this is the documented .NET wasm contract, and it is correct on every other head too.

    // The sink is kept alive by _sinkHandle, which native dereferences on every callback.
    private readonly GCHandle _sinkHandle;
    private readonly IntPtr _nativeManager;

    public CalculatorManagerWrapper(
        SetPrimaryDisplayHandler onSetPrimaryDisplay,
        SetIsInErrorHandler onSetIsInError,
        SetExpressionDisplayHandler onSetExpressionDisplay,
        SetParenthesisNumberHandler onSetParenthesisNumber,
        SimpleHandler onNoRightParenAdded,
        SimpleHandler onMaxDigitsReached,
        SimpleHandler onBinaryOperatorReceived,
        OnHistoryItemAddedHandler onHistoryItemAdded,
        SetMemorizedNumbersHandler onSetMemorizedNumbers,
        MemoryItemChangedHandler onMemoryItemChanged,
        SimpleHandler onInputChanged,
        GetCEngineStringHandler onGetCEngineString)
    {
        CallbackSink sink = new()
        {
            SetPrimaryDisplay = onSetPrimaryDisplay,
            SetIsInError = onSetIsInError,
            SetExpressionDisplay = onSetExpressionDisplay,
            SetParenthesisNumber = onSetParenthesisNumber,
            NoRightParenAdded = onNoRightParenAdded,
            MaxDigitsReached = onMaxDigitsReached,
            BinaryOperatorReceived = onBinaryOperatorReceived,
            HistoryItemAdded = onHistoryItemAdded,
            SetMemorizedNumbers = onSetMemorizedNumbers,
            MemoryItemChanged = onMemoryItemChanged,
            InputChanged = onInputChanged,
            GetCEngineString = onGetCEngineString,
        };

        _sinkHandle = GCHandle.Alloc(sink);

        try
        {
            IntPtr state = GCHandle.ToIntPtr(_sinkHandle);

            // One sink serves both slots; every thunk casts back to CallbackSink.
            var parms = new CalculatorManager_CreateParams
            {
                CalculatorState = state,
                ResourceState = state,

                SetPrimaryDisplay = (IntPtr)(delegate* unmanaged[Cdecl]<IntPtr, IntPtr, byte, void>)&SetPrimaryDisplayThunk,
                SetIsInError = (IntPtr)(delegate* unmanaged[Cdecl]<IntPtr, byte, void>)&SetIsInErrorThunk,
                SetExpressionDisplay = (IntPtr)(delegate* unmanaged[Cdecl]<IntPtr, IntPtr, void>)&SetExpressionDisplayThunk,
                SetParenthesisNumber = (IntPtr)(delegate* unmanaged[Cdecl]<IntPtr, int, void>)&SetParenthesisNumberThunk,
                OnNoRightParenAdded = (IntPtr)(delegate* unmanaged[Cdecl]<IntPtr, void>)&NoRightParenAddedThunk,
                MaxDigitsReached = (IntPtr)(delegate* unmanaged[Cdecl]<IntPtr, void>)&MaxDigitsReachedThunk,
                BinaryOperatorReceived = (IntPtr)(delegate* unmanaged[Cdecl]<IntPtr, void>)&BinaryOperatorReceivedThunk,
                OnHistoryItemAdded = (IntPtr)(delegate* unmanaged[Cdecl]<IntPtr, int, void>)&HistoryItemAddedThunk,
                SetMemorizedNumbers = (IntPtr)(delegate* unmanaged[Cdecl]<IntPtr, int, IntPtr, void>)&SetMemorizedNumbersThunk,
                MemoryItemChanged = (IntPtr)(delegate* unmanaged[Cdecl]<IntPtr, int, void>)&MemoryItemChangedThunk,
                GetCEngineString = (IntPtr)(delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr>)&GetCEngineStringThunk,
            };

            _nativeManager = NativeDispatch.CalculatorManager_Create(ref parms);

            if (_nativeManager == IntPtr.Zero)
            {
                throw new InvalidOperationException("CalculatorManager_Create returned null.");
            }
        }
        catch
        {
            _sinkHandle.Free();
            throw;
        }
    }

    #region Mode and lifecycle

    public void Reset(bool clearMemory) => NativeDispatch.CalculatorManager_Reset(_nativeManager, clearMemory);

    public void SetStandardMode() => NativeDispatch.CalculatorManager_SetStandardMode(_nativeManager);

    public void SetScientificMode() => NativeDispatch.CalculatorManager_SetScientificMode(_nativeManager);

    public void SetProgrammerMode() => NativeDispatch.CalculatorManager_SetProgrammerMode(_nativeManager);

    public void SendCommand(CalculatorCommand command)
        => NativeDispatch.CalculatorManager_SendCommand(_nativeManager, (int)command);

    /// <param name="radixType">Matches the native RADIX_TYPE: Hex=0, Dec=1, Oct=2, Bin=3.</param>
    public void SetRadix(int radixType) => NativeDispatch.CalculatorManager_SetRadix(_nativeManager, radixType);

    #endregion

    #region Memory

    public void MemorizeNumber() => NativeDispatch.CalculatorManager_MemorizeNumber(_nativeManager);

    public void MemorizedNumberLoad(uint index)
        => NativeDispatch.CalculatorManager_MemorizedNumberLoad(_nativeManager, (int)index);

    public void MemorizedNumberAdd(uint index)
        => NativeDispatch.CalculatorManager_MemorizedNumberAdd(_nativeManager, (int)index);

    public void MemorizedNumberSubtract(uint index)
        => NativeDispatch.CalculatorManager_MemorizedNumberSubtract(_nativeManager, (int)index);

    public void MemorizedNumberClear(uint index)
        => NativeDispatch.CalculatorManager_MemorizedNumberClear(_nativeManager, (int)index);

    public void MemorizedNumberClearAll() => NativeDispatch.CalculatorManager_MemorizedNumberClearAll(_nativeManager);

    public void SetMemorizedNumbersString() => NativeDispatch.CalculatorManager_SetMemorizedNumbersString(_nativeManager);

    #endregion

    #region Display and formatting

    public bool IsEngineRecording => NativeDispatch.CalculatorManager_IsEngineRecording(_nativeManager);

    /// <summary>
    /// Whether the engine currently holds no operand input.
    /// </summary>
    /// <remarks>
    /// TODO(P5): needs a new C export (CalculatorManager_IsInputEmpty) surfacing CCalcEngine input
    /// state. There is no such method on the fork's CalculatorManager at all, so it cannot be
    /// bridged without an engine change. Returning true keeps StandardCalculatorViewModel on the
    /// same branch it takes when the manager is null.
    /// </remarks>
    public bool IsInputEmpty => true;

    public string GetResultForRadix(uint radix, int precision, bool groupDigitsPerRadix)
    {
        IntPtr pResult = NativeDispatch.CalculatorManager_GetResultForRadix(_nativeManager, (int)radix, precision, groupDigitsPerRadix);
        return NativeDispatch.PtrToString(pResult);
    }

    public void SetPrecision(int precision) => NativeDispatch.CalculatorManager_SetPrecision(_nativeManager, precision);

    public void UpdateMaxIntDigits() => NativeDispatch.CalculatorManager_UpdateMaxIntDigits(_nativeManager);

    public char DecimalSeparator
    {
        get
        {
            IntPtr pSeparator = NativeDispatch.CalculatorManager_DecimalSeparator(_nativeManager);
            return NativeDispatch.PtrToString(pSeparator).FirstOrDefault();
        }
    }

    public CalculatorCommand GetCurrentDegreeMode()
        => (CalculatorCommand)NativeDispatch.CalculatorManager_GetCurrentDegreeMode(_nativeManager);

    /// <summary>
    /// The commands currently on the display, as opposed to the ones already in history.
    /// </summary>
    /// <remarks>
    /// TODO(P5): needs a new C export exposing the engine's live display command vector. The
    /// caller already coalesces null/empty, so an empty array is the safe stand-in.
    /// </remarks>
    public ExpressionCommandWrapper[] GetDisplayCommandsSnapshot() => Array.Empty<ExpressionCommandWrapper>();

    #endregion

    #region History

    public HistoryItemWrapper[] GetHistoryItems()
        => UnmarshalHistoryItems(NativeDispatch.CalculatorManager_GetHistoryItems(_nativeManager));

    public HistoryItemWrapper[] GetHistoryItemsForMode(CalculatorMode mode)
        => UnmarshalHistoryItems(NativeDispatch.CalculatorManager_GetHistoryItemsWithMode(_nativeManager, (int)mode));

    public HistoryItemWrapper? GetHistoryItem(uint index)
    {
        IntPtr pResult = NativeDispatch.CalculatorManager_GetHistoryItem(_nativeManager, (int)index);
        if (pResult == IntPtr.Zero)
        {
            return null;
        }

        return UnmarshalHistoryItem(Marshal.PtrToStructure<GetHistoryItemResult>(pResult));
    }

    /// <summary>
    /// Replaces the engine history with a previously captured snapshot.
    /// </summary>
    /// <remarks>
    /// TODO(P5): needs a new C export (CalculatorManager_SetHistory) plus factory exports for the
    /// four IExpressionCommand shapes - the ABI is read-only over expression commands today, so
    /// there is no way to rebuild them natively. CalculatorManager::SetHistory exists in C++ but
    /// is not exported. Silently ignoring the call matches the donor, which also gave up here.
    /// </remarks>
    public void SetHistoryItems(HistoryItemWrapper[] historyItems)
    {
        _ = historyItems;
    }

    public bool RemoveHistoryItem(uint index)
        => NativeDispatch.CalculatorManager_RemoveHistoryItem(_nativeManager, (int)index);

    public void ClearHistory() => NativeDispatch.CalculatorManager_ClearHistory(_nativeManager);

    public ulong MaxHistorySize => (ulong)NativeDispatch.CalculatorManager_MaxHistorySize(_nativeManager);

    public void SetInHistoryItemLoadMode(bool isHistoryItemLoadMode)
        => NativeDispatch.CalculatorManager_SetInHistoryItemLoadMode(_nativeManager, isHistoryItemLoadMode);

    #endregion

    #region Unmarshalling

    private static HistoryItemWrapper[] UnmarshalHistoryItems(IntPtr pResult)
    {
        if (pResult == IntPtr.Zero)
        {
            return Array.Empty<HistoryItemWrapper>();
        }

        var result = Marshal.PtrToStructure<GetHistoryItemsResult>(pResult);
        if (result.ItemsCount <= 0 || result.HistoryItems == IntPtr.Zero)
        {
            return Array.Empty<HistoryItemWrapper>();
        }

        var items = new HistoryItemWrapper[result.ItemsCount];
        for (int i = 0; i < result.ItemsCount; i++)
        {
            IntPtr pItem = Marshal.ReadIntPtr(result.HistoryItems, i * IntPtr.Size);
            items[i] = UnmarshalHistoryItem(Marshal.PtrToStructure<GetHistoryItemResult>(pItem));
        }

        return items;
    }

    private static HistoryItemWrapper UnmarshalHistoryItem(GetHistoryItemResult item)
    {
        string expression = NativeDispatch.PtrToString(item.expression);
        string result = NativeDispatch.PtrToString(item.result);

        var tokens = new HistoryToken[Math.Max(item.TokenCount, 0)];
        for (int i = 0; i < tokens.Length; i++)
        {
            // TokenStrings strides by pointer, TokenValues by int32 - do not merge these.
            string text = NativeDispatch.PtrToString(Marshal.ReadIntPtr(item.TokenStrings, i * IntPtr.Size));
            int commandIndex = Marshal.ReadInt32(item.TokenValues, i * sizeof(int));
            tokens[i] = new HistoryToken(text, commandIndex);
        }

        var commands = new ExpressionCommandWrapper[Math.Max(item.CommandCount, 0)];
        for (int i = 0; i < commands.Length; i++)
        {
            IntPtr pCommand = Marshal.ReadIntPtr(item.Commands, i * IntPtr.Size);
            commands[i] = UnmarshalExpressionCommand(pCommand);
        }

        return new HistoryItemWrapper(tokens, commands, expression, result);
    }

    /// <summary>
    /// Flattens one native IExpressionCommand into a value snapshot. Everything is read here and
    /// the pointer is dropped: it is borrowed from the native shared_ptr graph and only valid
    /// while the owning history item lives.
    /// </summary>
    private static ExpressionCommandWrapper UnmarshalExpressionCommand(IntPtr pCommand)
    {
        if (pCommand == IntPtr.Zero)
        {
            return new ExpressionCommandWrapper();
        }

        var type = (CommandType)NativeDispatch.IExpressionCommand_GetCommandType(pCommand);

        switch (type)
        {
            case CommandType.UnaryCommand:
                return new ExpressionCommandWrapper(
                    CommandType.UnaryCommand,
                    0,
                    ReadUnaryCommands(pCommand),
                    false,
                    false,
                    false);

            case CommandType.BinaryCommand:
                return new ExpressionCommandWrapper(
                    CommandType.BinaryCommand,
                    NativeDispatch.CBinaryCommand_GetCommand(pCommand),
                    Array.Empty<int>(),
                    false,
                    false,
                    false);

            case CommandType.OperandCommand:
                // TODO(P5): IsDecimalPresent / IsSciFmt default to false - the ABI exposes only
                // COpndCommand_IsNegative. Two one-line C exports (COpndCommand_IsDecimalPresent,
                // COpndCommand_IsSciFmt) would make these real.
                return new ExpressionCommandWrapper(
                    CommandType.OperandCommand,
                    0,
                    ReadOperandCommands(pCommand),
                    NativeDispatch.COpndCommand_IsNegative(pCommand),
                    false,
                    false);

            case CommandType.Parentheses:
                // TODO(P5): Command stays 0 - there is no CParentheses_GetCommand export, though
                // CBinaryCommand_GetCommand exists, so this looks like an omission in the C shim.
                return new ExpressionCommandWrapper(CommandType.Parentheses, 0, Array.Empty<int>(), false, false, false);

            default:
                return new ExpressionCommandWrapper();
        }
    }

    private static int[] ReadUnaryCommands(IntPtr pCommand)
    {
        IntPtr pResult = NativeDispatch.CUnaryCommand_GetCommands(pCommand);
        if (pResult == IntPtr.Zero)
        {
            return Array.Empty<int>();
        }

        var result = Marshal.PtrToStructure<CUnaryCommand_GetCommandsResult>(pResult);
        return CopyAndFree(pResult, result.CommandCount, result.Commands);
    }

    private static int[] ReadOperandCommands(IntPtr pCommand)
    {
        IntPtr pResult = NativeDispatch.COpndCommand_GetCommands(pCommand);
        if (pResult == IntPtr.Zero)
        {
            return Array.Empty<int>();
        }

        var result = Marshal.PtrToStructure<COpndCommand_GetCommandsResult>(pResult);
        return CopyAndFree(pResult, result.CommandCount, result.Commands);
    }

    /// <summary>Both command results are malloc-allocated, so free() is the correct deallocator.</summary>
    private static int[] CopyAndFree(IntPtr pResult, int count, IntPtr pCommands)
    {
        int[] commands;
        if (count > 0 && pCommands != IntPtr.Zero)
        {
            commands = new int[count];
            Marshal.Copy(pCommands, commands, 0, count);
        }
        else
        {
            commands = Array.Empty<int>();
        }

        if (pCommands != IntPtr.Zero)
        {
            NativeDispatch.Free(pCommands);
        }

        NativeDispatch.Free(pResult);
        return commands;
    }

    #endregion

    #region Native callbacks

    private static CallbackSink? SinkFrom(IntPtr state)
    {
        if (state == IntPtr.Zero)
        {
            return null;
        }

        return GCHandle.FromIntPtr(state).Target as CallbackSink;
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static void SetPrimaryDisplayThunk(IntPtr state, IntPtr pDisplay, byte isError)
    {
        try
        {
            SinkFrom(state)?.SetPrimaryDisplay?.Invoke(NativeDispatch.PtrToString(pDisplay), isError != 0);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"SetPrimaryDisplay callback failed: {ex}");
        }
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static void SetIsInErrorThunk(IntPtr state, byte isError)
    {
        try
        {
            SinkFrom(state)?.SetIsInError?.Invoke(isError != 0);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"SetIsInError callback failed: {ex}");
        }
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static void SetExpressionDisplayThunk(IntPtr state, IntPtr pHistoryItem)
    {
        try
        {
            CallbackSink? sink = SinkFrom(state);
            if (sink?.SetExpressionDisplay is null || pHistoryItem == IntPtr.Zero)
            {
                return;
            }

            HistoryItemWrapper item = UnmarshalHistoryItem(Marshal.PtrToStructure<GetHistoryItemResult>(pHistoryItem));
            sink.SetExpressionDisplay(item.Tokens, item.Commands);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"SetExpressionDisplay callback failed: {ex}");
        }
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static void SetParenthesisNumberThunk(IntPtr state, int parenthesisCount)
    {
        try
        {
            SinkFrom(state)?.SetParenthesisNumber?.Invoke((uint)parenthesisCount);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"SetParenthesisNumber callback failed: {ex}");
        }
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static void NoRightParenAddedThunk(IntPtr state)
    {
        try
        {
            SinkFrom(state)?.NoRightParenAdded?.Invoke();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"OnNoRightParenAdded callback failed: {ex}");
        }
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static void MaxDigitsReachedThunk(IntPtr state)
    {
        try
        {
            SinkFrom(state)?.MaxDigitsReached?.Invoke();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"MaxDigitsReached callback failed: {ex}");
        }
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static void BinaryOperatorReceivedThunk(IntPtr state)
    {
        try
        {
            SinkFrom(state)?.BinaryOperatorReceived?.Invoke();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"BinaryOperatorReceived callback failed: {ex}");
        }
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static void HistoryItemAddedThunk(IntPtr state, int addedItemIndex)
    {
        try
        {
            SinkFrom(state)?.HistoryItemAdded?.Invoke((uint)addedItemIndex);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"OnHistoryItemAdded callback failed: {ex}");
        }
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static void SetMemorizedNumbersThunk(IntPtr state, int count, IntPtr pNumbers)
    {
        try
        {
            CallbackSink? sink = SinkFrom(state);
            if (sink?.SetMemorizedNumbers is null)
            {
                return;
            }

            var numbers = new string[Math.Max(count, 0)];
            for (int i = 0; i < numbers.Length; i++)
            {
                numbers[i] = NativeDispatch.PtrToString(Marshal.ReadIntPtr(pNumbers, i * IntPtr.Size));
            }

            sink.SetMemorizedNumbers(numbers);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"SetMemorizedNumbers callback failed: {ex}");
        }
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static void MemoryItemChangedThunk(IntPtr state, int indexOfMemory)
    {
        try
        {
            SinkFrom(state)?.MemoryItemChanged?.Invoke((uint)indexOfMemory);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"MemoryItemChanged callback failed: {ex}");
        }
    }

    // TODO(P5): the returned buffer leaks. Native copies it into a std::wstring and drops the
    // pointer (CCalcManager.cpp:167-172) without ever calling back to free it, and resource
    // lookups are frequent. Fixing it needs either a free callback in the ABI or a caching layer.
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static IntPtr GetCEngineStringThunk(IntPtr state, IntPtr pResourceId)
    {
        try
        {
            GetCEngineStringHandler? handler = SinkFrom(state)?.GetCEngineString;
            string id = NativeDispatch.PtrToString(pResourceId);
            string value = handler?.Invoke(id) ?? string.Empty;
            return NativeDispatch.StringToHGlobal(value);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"GetCEngineString callback failed: {ex}");

            // Never hand the engine a null pointer; it constructs a wstring from it unguarded.
            return NativeDispatch.StringToHGlobal(string.Empty);
        }
    }

    #endregion

    /// <summary>
    /// Holds the twelve managed handlers. Pinned with a GCHandle and passed to native as the
    /// opaque CalculatorState / ResourceState.
    /// </summary>
    private sealed class CallbackSink
    {
        public SetPrimaryDisplayHandler? SetPrimaryDisplay { get; init; }

        public SetIsInErrorHandler? SetIsInError { get; init; }

        public SetExpressionDisplayHandler? SetExpressionDisplay { get; init; }

        public SetParenthesisNumberHandler? SetParenthesisNumber { get; init; }

        public SimpleHandler? NoRightParenAdded { get; init; }

        public SimpleHandler? MaxDigitsReached { get; init; }

        public SimpleHandler? BinaryOperatorReceived { get; init; }

        public OnHistoryItemAddedHandler? HistoryItemAdded { get; init; }

        public SetMemorizedNumbersHandler? SetMemorizedNumbers { get; init; }

        public MemoryItemChangedHandler? MemoryItemChanged { get; init; }

        /// <summary>
        /// TODO(P5): stored but never raised. CalculatorManager_CreateParams has ten display
        /// callbacks and no InputChanged slot, and native ICalcDisplay has no such virtual, so
        /// the engine cannot signal it. Needs an ABI addition.
        /// </summary>
        public SimpleHandler? InputChanged { get; init; }

        public GetCEngineStringHandler? GetCEngineString { get; init; }
    }
}
