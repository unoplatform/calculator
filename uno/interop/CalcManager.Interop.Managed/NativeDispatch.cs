// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//
// Harvested from D:/Work/calculator/src/Calculator.Shared/CalcManager/CalculatorManager.Interop.cs
// and re-checked line by line against src/CalcManager/CCalcManager.h (34 exports).
//
// Deviations from the donor, all deliberate:
//   * namespace CalculationManager -> CalcManager.Interop
//   * CalculatorManager_GetResultForRadix returns IntPtr, not string. The donor declared it
//     [DllImport(CharSet = Unicode)] string, which makes the CLR free the buffer with
//     CoTaskMemFree while CCalcManager.cpp allocated it with new wchar_t[] - heap corruption,
//     and flatly wrong wherever wchar_t is 4 bytes.
//   * every bool is marshalled as U1. C++ bool is one byte; .NET defaults to the 4-byte Win32 BOOL.
//   * MaxHistorySize is nuint (native size_t), not int, which truncated on x64.
//   * the __WASM__ / __IOS__ conditional blocks are dropped for this desktop-only milestone.

#nullable enable

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace CalcManager.Interop;

/// <summary>
/// The flat C ABI exposed by the native CalcManager library, plus the wchar_t-width-aware string
/// marshalling it needs: the engine is built with 2-byte wchar_t on Windows and 4-byte elsewhere,
/// so neither Marshal.PtrToStringUni nor a UTF-32 decode is universally correct.
/// </summary>
internal static class NativeDispatch
{
    private const string DllPath = "CalcManager";

    #region Lifecycle and mode

    [DllImport(DllPath)]
    internal static extern IntPtr CalculatorManager_Create(ref CalculatorManager_CreateParams parms);

    [DllImport(DllPath)]
    internal static extern void CalculatorManager_SendCommand(IntPtr nativeManager, int command);

    [DllImport(DllPath)]
    internal static extern void CalculatorManager_SetRadix(IntPtr nativeManager, int iRadixType);

    [DllImport(DllPath)]
    internal static extern void CalculatorManager_Reset(
        IntPtr nativeManager,
        [MarshalAs(UnmanagedType.U1)] bool clearMemory);

    [DllImport(DllPath)]
    internal static extern void CalculatorManager_SetStandardMode(IntPtr nativeManager);

    [DllImport(DllPath)]
    internal static extern void CalculatorManager_SetScientificMode(IntPtr nativeManager);

    [DllImport(DllPath)]
    internal static extern void CalculatorManager_SetProgrammerMode(IntPtr nativeManager);

    #endregion

    #region Memory

    [DllImport(DllPath)]
    internal static extern void CalculatorManager_MemorizeNumber(IntPtr nativeManager);

    [DllImport(DllPath)]
    internal static extern void CalculatorManager_MemorizedNumberLoad(IntPtr nativeManager, int value);

    [DllImport(DllPath)]
    internal static extern void CalculatorManager_MemorizedNumberAdd(IntPtr nativeManager, int value);

    [DllImport(DllPath)]
    internal static extern void CalculatorManager_MemorizedNumberSubtract(IntPtr nativeManager, int value);

    [DllImport(DllPath)]
    internal static extern void CalculatorManager_MemorizedNumberClear(IntPtr nativeManager, int value);

    [DllImport(DllPath)]
    internal static extern void CalculatorManager_MemorizedNumberClearAll(IntPtr nativeManager);

    [DllImport(DllPath)]
    internal static extern void CalculatorManager_SetMemorizedNumbersString(IntPtr nativeManager);

    #endregion

    #region Display and formatting

    [DllImport(DllPath)]
    [return: MarshalAs(UnmanagedType.U1)]
    internal static extern bool CalculatorManager_IsEngineRecording(IntPtr nativeManager);

    /// <summary>Returns a heap-allocated const wchar_t*; convert with <see cref="PtrToString"/>.</summary>
    [DllImport(DllPath)]
    internal static extern IntPtr CalculatorManager_GetResultForRadix(IntPtr nativeManager, int radix, int precision);

    [DllImport(DllPath)]
    internal static extern void CalculatorManager_SetPrecision(IntPtr nativeManager, int precision);

    [DllImport(DllPath)]
    internal static extern void CalculatorManager_UpdateMaxIntDigits(IntPtr nativeManager);

    [DllImport(DllPath)]
    internal static extern IntPtr CalculatorManager_DecimalSeparator(IntPtr nativeManager);

    [DllImport(DllPath)]
    internal static extern int CalculatorManager_GetCurrentDegreeMode(IntPtr nativeManager);

    #endregion

    #region History

    [DllImport(DllPath)]
    [return: MarshalAs(UnmanagedType.U1)]
    internal static extern bool CalculatorManager_RemoveHistoryItem(IntPtr nativeManager, int uIdx);

    [DllImport(DllPath)]
    internal static extern void CalculatorManager_ClearHistory(IntPtr nativeManager);

    [DllImport(DllPath)]
    internal static extern nuint CalculatorManager_MaxHistorySize(IntPtr nativeManager);

    [DllImport(DllPath)]
    internal static extern void CalculatorManager_SetInHistoryItemLoadMode(
        IntPtr nativeManager,
        [MarshalAs(UnmanagedType.U1)] bool isHistoryItemLoadMode);

    [DllImport(DllPath)]
    internal static extern IntPtr CalculatorManager_GetHistoryItems(IntPtr nativeManager);

    [DllImport(DllPath)]
    internal static extern IntPtr CalculatorManager_GetHistoryItemsWithMode(IntPtr nativeManager, int mode);

    [DllImport(DllPath)]
    internal static extern IntPtr CalculatorManager_GetHistoryItem(IntPtr nativeManager, int uIdx);

    #endregion

    #region Expression commands and utilities

    [DllImport(DllPath)]
    internal static extern int IExpressionCommand_GetCommandType(IntPtr pExpressionCommand);

    [DllImport(DllPath)]
    internal static extern IntPtr COpndCommand_GetCommands(IntPtr pExpressionCommand);

    [DllImport(DllPath)]
    [return: MarshalAs(UnmanagedType.U1)]
    internal static extern bool COpndCommand_IsNegative(IntPtr pExpressionCommand);

    [DllImport(DllPath)]
    internal static extern IntPtr CUnaryCommand_GetCommands(IntPtr pExpressionCommand);

    [DllImport(DllPath)]
    internal static extern int CBinaryCommand_GetCommand(IntPtr pExpressionCommand);

    /// <summary>Plain free(). Valid only for the malloc-allocated COpnd/CUnary command results.</summary>
    [DllImport(DllPath)]
    internal static extern void Free(IntPtr ptr);

    [DllImport(DllPath)]
    internal static extern int GetWChar_t_Size();

    #endregion

    #region Native callback signatures

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void SetPrimaryDisplayCallbackFunc(
        IntPtr state,
        IntPtr pDisplayStringValue,
        [MarshalAs(UnmanagedType.U1)] bool isError);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void SetIsInErrorCallbackFunc(IntPtr state, [MarshalAs(UnmanagedType.U1)] bool isError);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void SetExpressionDisplayCallbackFunc(IntPtr state, IntPtr data);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void SetParenthesisNumberCallbackFunc(IntPtr state, int parenthesisCount);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void OnNoRightParenAddedCallbackFunc(IntPtr state);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void MaxDigitsReachedCallbackFunc(IntPtr state);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void BinaryOperatorReceivedFunc(IntPtr state);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void OnHistoryItemAddedCallbackFunc(IntPtr state, int addedItemIndex);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void SetMemorizedNumbersCallbackFunc(IntPtr state, int count, IntPtr newMemorizedNumbers);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void MemoryItemChangedCallbackFunc(IntPtr state, int indexOfMemory);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate IntPtr GetCEngineStringFunc(IntPtr state, IntPtr id);

    #endregion

    #region String marshalling

    private static bool IsStringUTF32 => GetWChar_t_Size() == 4;

    internal static IntPtr StringToHGlobal(string value)
        => IsStringUTF32 ? StringToHGlobalUTF32(value) : Marshal.StringToHGlobalUni(value);

    internal static string PtrToString(IntPtr p)
    {
        if (p == IntPtr.Zero)
        {
            return string.Empty;
        }

        return (IsStringUTF32 ? PtrToStringUTF32(p) : Marshal.PtrToStringUni(p)) ?? string.Empty;
    }

    private static string PtrToStringUTF32(IntPtr ptr)
    {
        IntPtr endPtr = ptr;
        while (Marshal.ReadInt32(endPtr) != 0)
        {
            endPtr += 4;
        }

        if (endPtr == ptr)
        {
            return string.Empty;
        }

        var bytes = new byte[(long)endPtr - (long)ptr];
        Marshal.Copy(ptr, bytes, 0, bytes.Length);
        return Encoding.UTF32.GetString(bytes);
    }

    private static IntPtr StringToHGlobalUTF32(string value)
    {
        byte[] encoded = Encoding.UTF32.GetBytes(value);
        IntPtr buffer = Marshal.AllocHGlobal(encoded.Length + 4);
        Marshal.Copy(encoded, 0, buffer, encoded.Length);
        Marshal.WriteInt32(buffer + encoded.Length, 0);
        return buffer;
    }

    #endregion

    [Conditional("DEBUG")]
    internal static void DebugTrace(string message) => Debug.WriteLine(message);
}

/// <summary>
/// Mirrors CCalcManager.h:25-42. Field order IS the ABI; do not reorder, and do not add an
/// InputChanged slot - the native struct ends at GetCEngineString.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct CalculatorManager_CreateParams
{
    public IntPtr CalculatorState;

    public IntPtr SetPrimaryDisplay;
    public IntPtr SetIsInError;
    public IntPtr SetExpressionDisplay;
    public IntPtr SetParenthesisNumber;
    public IntPtr OnNoRightParenAdded;
    public IntPtr MaxDigitsReached;
    public IntPtr BinaryOperatorReceived;
    public IntPtr OnHistoryItemAdded;
    public IntPtr SetMemorizedNumbers;
    public IntPtr MemoryItemChanged;

    public IntPtr ResourceState;
    public IntPtr GetCEngineString;
}

/// <summary>Mirrors CCalcManager.h:55-59.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct GetHistoryItemsResult
{
    public int ItemsCount;
    public IntPtr HistoryItems;
}

/// <summary>
/// Mirrors CCalcManager.h:61-72. TokenStrings is const wchar_t** while TokenValues is int32_t*,
/// so the two arrays have different strides - walk them with SizeOf&lt;IntPtr&gt; and SizeOf&lt;int&gt;.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct GetHistoryItemResult
{
    public IntPtr expression;
    public IntPtr result;

    public int TokenCount;
    public IntPtr TokenStrings;
    public IntPtr TokenValues;

    public int CommandCount;
    public IntPtr Commands;
}

/// <summary>Mirrors CCalcManager.h:74-78 (native field name is pCommands; layout is sequential).</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct COpndCommand_GetCommandsResult
{
    public int CommandCount;
    public IntPtr Commands;
}

/// <summary>Mirrors CCalcManager.h:80-84.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct CUnaryCommand_GetCommandsResult
{
    public int CommandCount;
    public IntPtr Commands;
}
