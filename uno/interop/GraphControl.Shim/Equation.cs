// GraphControl shim — port of 4fd3fc53:src/GraphControl/Models/Equation.h (the ref class block).
// A pure data holder: every OBSERVABLE_NAMED_PROPERTY_RW becomes a notifying auto-property.
// Nothing here needs the native graphing engine.
#nullable enable

using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

using Windows.UI;

namespace GraphControl;

[Microsoft.UI.Xaml.Data.Bindable]
public sealed class Equation : INotifyPropertyChanged
{
    private string _expression = string.Empty;
    private Color _lineColor;
    private bool _isLineEnabled;
    private bool _isValidated;
    private bool _hasGraphError;
    private bool _isInequality;
    private bool _isSelected;
    private EquationLineStyle _equationStyle = EquationLineStyle.Solid;
    private ErrorType _graphErrorType;
    private int _graphErrorCode;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Expression
    {
        get => _expression;
        set => Set(ref _expression, value ?? string.Empty);
    }

    public Color LineColor
    {
        get => _lineColor;
        set => Set(ref _lineColor, value);
    }

    public bool IsLineEnabled
    {
        get => _isLineEnabled;
        set => Set(ref _isLineEnabled, value);
    }

    public bool IsValidated
    {
        get => _isValidated;
        set => Set(ref _isValidated, value);
    }

    public bool HasGraphError
    {
        get => _hasGraphError;
        set => Set(ref _hasGraphError, value);
    }

    public bool IsInequality
    {
        get => _isInequality;
        set => Set(ref _isInequality, value);
    }

    public bool IsSelected
    {
        get => _isSelected;
        set => Set(ref _isSelected, value);
    }

    // TwoWay x:Bind target at EquationInputArea.xaml:869 — must notify.
    public EquationLineStyle EquationStyle
    {
        get => _equationStyle;
        set => Set(ref _equationStyle, value);
    }

    public ErrorType GraphErrorType
    {
        get => _graphErrorType;
        set => Set(ref _graphErrorType, value);
    }

    public int GraphErrorCode
    {
        get => _graphErrorCode;
        set => Set(ref _graphErrorCode, value);
    }

    private void Set<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
