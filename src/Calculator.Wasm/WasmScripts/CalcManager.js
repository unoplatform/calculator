class CalcManager {

    static async initializeExports() {
        // Call as early as possible to that the exports are available after the next
        // main thread tick.

        if (Module.getAssemblyExports !== undefined) {
            try {
                CalcManager._calculatorExports = await Module.getAssemblyExports("Calculator.Wasm");
            }
            catch (e) {
                console.error(e);
            }
        }
    }

    static registerCallbacks() {

        var _getCEngineStringCallback       = CalcManager._calculatorExports.CalculationManager.NativeDispatch.GetCEngineStringCallback;
        var _binaryOperatorReceivedCallback = CalcManager._calculatorExports.CalculationManager.NativeDispatch.BinaryOperatorReceivedCallback;
        var _setPrimaryDisplayCallback      = CalcManager._calculatorExports.CalculationManager.NativeDispatch.SetPrimaryDisplayCallback;
        var _setIsInErrorCallback           = CalcManager._calculatorExports.CalculationManager.NativeDispatch.SetIsInErrorCallback;
        var _setParenthesisNumberCallback   = CalcManager._calculatorExports.CalculationManager.NativeDispatch.SetParenthesisNumberCallback;
        var _maxDigitsReachedCallback       = CalcManager._calculatorExports.CalculationManager.NativeDispatch.MaxDigitsReachedCallback;
        var _memoryItemChangedCallback      = CalcManager._calculatorExports.CalculationManager.NativeDispatch.MemoryItemChangedCallback;
        var _onHistoryItemAddedCallback     = CalcManager._calculatorExports.CalculationManager.NativeDispatch.OnHistoryItemAddedCallback;
        var _onNoRightParenAddedCallback    = CalcManager._calculatorExports.CalculationManager.NativeDispatch.OnNoRightParenAddedCallback;
        var _setExpressionDisplayCallback   = CalcManager._calculatorExports.CalculationManager.NativeDispatch.SetExpressionDisplayCallback;
        var _setMemorizedNumbersCallback    = CalcManager._calculatorExports.CalculationManager.NativeDispatch.SetMemorizedNumbersCallback;

        var fGetCEngineStringCallback       = Module.addFunction((state, id) => _getCEngineStringCallback(state, id), 'iii');

        var fBinaryOperatorReceivedCallback = Module.addFunction((state) => _binaryOperatorReceivedCallback(state), 'vi');
        var fSetPrimaryDisplayCallback      = Module.addFunction((state, displayStringValue, isError) => _setPrimaryDisplayCallback(state, displayStringValue, Boolean(isError)), 'viii');
        var fSetIsInErrorCallback           = Module.addFunction((state, isError) => _setIsInErrorCallback(state, Boolean(isError)), 'vii');
        var fSetParenthesisNumberCallback   = Module.addFunction((state, parenthesisCount) => _setParenthesisNumberCallback(state, parenthesisCount), 'vii');
        var fMaxDigitsReachedCallback       = Module.addFunction((state) => _maxDigitsReachedCallback(state), 'vii');
        var fMemoryItemChangedCallback      = Module.addFunction((state, indexOfMemory) => _memoryItemChangedCallback(state, indexOfMemory), 'vii');
        var fOnHistoryItemAddedCallback     = Module.addFunction((state, addedItemIndex) => _onHistoryItemAddedCallback(state, addedItemIndex), 'vii');
        var fOnNoRightParenAddedCallback    = Module.addFunction((state) => _onNoRightParenAddedCallback(state), 'vi');
        var fSetExpressionDisplayCallback   = Module.addFunction((state, historyItem) => _setExpressionDisplayCallback(state, historyItem), 'vii');
        var fSetMemorizedNumbersCallback    = Module.addFunction((state, size, numbers) => _setMemorizedNumbersCallback(state, size, numbers), 'viii');

        var ret = [
            fGetCEngineStringCallback
            , fBinaryOperatorReceivedCallback
            , fSetPrimaryDisplayCallback
            , fSetIsInErrorCallback
            , fSetParenthesisNumberCallback
            , fMaxDigitsReachedCallback
            , fMemoryItemChangedCallback
            , fOnHistoryItemAddedCallback
            , fOnNoRightParenAddedCallback
            , fSetExpressionDisplayCallback
            , fSetMemorizedNumbersCallback
        ];

        return ret;
    }
}

globalThis.CalcManager = CalcManager;
