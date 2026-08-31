// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//
// Display/resource callback delegates. Spec: 4fd3fc53:src/CalcManager.Interop/CalcManagerInterop.idl:243-253.
// Parameter types are pinned by CalculatorApp.ViewModel.Common.CalculatorDisplay, which the
// twelve lambdas at StandardCalculatorViewModel.cs:173-186 forward to.

#nullable enable

namespace CalcManager.Interop;

public delegate void SetPrimaryDisplayHandler(string display, bool isError);

public delegate void SetIsInErrorHandler(bool isError);

public delegate void SetExpressionDisplayHandler(HistoryToken[] tokens, ExpressionCommandWrapper[] commands);

public delegate void SetParenthesisNumberHandler(uint count);

/// <summary>Used for onNoRightParenAdded, onMaxDigitsReached, onBinaryOperatorReceived and onInputChanged.</summary>
public delegate void SimpleHandler();

public delegate void OnHistoryItemAddedHandler(uint addedItemIndex);

public delegate void SetMemorizedNumbersHandler(string[] memorizedNumbers);

public delegate void MemoryItemChangedHandler(uint indexOfMemory);

public delegate string GetCEngineStringHandler(string id);
