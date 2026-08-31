// GraphControl shim — the surface of 4fd3fc53:src/GraphControl/Control/Grapher.h, with none of
// the DirectX renderer, the CAS solver or the MathRichEdit wrapper behind it.
//
// It has to be a real Microsoft.UI.Xaml.Controls.Control, not a POCO: GraphingCalculator.xaml:587
// instantiates <graphControl:Grapher> as a Grid child and sets UseSystemFocusVisuals, FlowDirection,
// ContextFlyout, LosingFocus and LostFocus on it, and the code-behind reads ActualWidth and hands
// the instance to FrameworkElementAutomationPeer.FromElement and ThemeShadow.Receivers.
//
// AxesColor / GraphBackground / GridLinesColor must be DependencyProperties because three
// VisualState Setters target them by path (GraphingCalculator.xaml:534-550). ActiveTracing,
// IsKeepCurrentView and LineWidth must notify because they sit on TwoWay x:Binds.
#nullable enable

using System.Collections.Generic;
using System.ComponentModel;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using Windows.Storage.Streams;
using Windows.UI;

namespace GraphControl;

public sealed class Grapher : Control, INotifyPropertyChanged
{
    private readonly EquationCollection _equations = new();
    private readonly IDictionary<string, Variable> _variables = new Dictionary<string, Variable>();

    private double _xAxisMin = -10.0;
    private double _xAxisMax = 10.0;
    private double _yAxisMin = -10.0;
    private double _yAxisMax = 10.0;

    public event PropertyChangedEventHandler? PropertyChanged;

    // TODO(P5): all six are declared for the consumers' += and XAML handler attributes but are
    // never raised - RenderMain owns every trigger. Until then the trace popup never shows,
    // the graph automation name never refreshes, and the variable list never repopulates.
#pragma warning disable CS0067
    public event TracingChangedEventHandler? TracingChangedEvent;

    public event TracingValueChangedEventHandler? TracingValueChangedEvent;

    public event PointerValueChangedEventHandler? PointerValueChangedEvent;

    public event System.EventHandler<GraphViewChangedReason>? GraphViewChangedEvent;

    public event RoutedEventHandler? GraphPlottedEvent;

    // Upstream types this EventHandler<IMap<string, Variable>>; the only consumer handler is
    // (object sender, object args) — EventHandler<object> matches it with no variance risk.
    public event System.EventHandler<object>? VariablesUpdated;
#pragma warning restore CS0067

    public static readonly DependencyProperty ForceProportionalAxesProperty =
        DependencyProperty.Register(
            nameof(ForceProportionalAxes),
            typeof(bool),
            typeof(Grapher),
            new PropertyMetadata(true));

    public static readonly DependencyProperty IsKeepCurrentViewProperty =
        DependencyProperty.Register(
            nameof(IsKeepCurrentView),
            typeof(bool),
            typeof(Grapher),
            new PropertyMetadata(false, (d, e) => ((Grapher)d).RaisePropertyChanged(nameof(IsKeepCurrentView))));

    public static readonly DependencyProperty ActiveTracingProperty =
        DependencyProperty.Register(
            nameof(ActiveTracing),
            typeof(bool),
            typeof(Grapher),
            new PropertyMetadata(false, (d, e) => ((Grapher)d).RaisePropertyChanged(nameof(ActiveTracing))));

    public static readonly DependencyProperty LineWidthProperty =
        DependencyProperty.Register(
            nameof(LineWidth),
            typeof(double),
            typeof(Grapher),
            new PropertyMetadata(2.0, (d, e) => ((Grapher)d).RaisePropertyChanged(nameof(LineWidth))));

    public static readonly DependencyProperty AxesColorProperty =
        DependencyProperty.Register(
            nameof(AxesColor),
            typeof(Color),
            typeof(Grapher),
            new PropertyMetadata(Microsoft.UI.Colors.Transparent));

    public static readonly DependencyProperty GraphBackgroundProperty =
        DependencyProperty.Register(
            nameof(GraphBackground),
            typeof(Color),
            typeof(Grapher),
            new PropertyMetadata(Microsoft.UI.Colors.Transparent));

    public static readonly DependencyProperty GridLinesColorProperty =
        DependencyProperty.Register(
            nameof(GridLinesColor),
            typeof(Color),
            typeof(Grapher),
            new PropertyMetadata(Microsoft.UI.Colors.Transparent));

    public bool ForceProportionalAxes
    {
        get => (bool)GetValue(ForceProportionalAxesProperty);
        set => SetValue(ForceProportionalAxesProperty, value);
    }

    public bool IsKeepCurrentView
    {
        get => (bool)GetValue(IsKeepCurrentViewProperty);
        set => SetValue(IsKeepCurrentViewProperty, value);
    }

    public bool ActiveTracing
    {
        get => (bool)GetValue(ActiveTracingProperty);
        set => SetValue(ActiveTracingProperty, value);
    }

    public double LineWidth
    {
        get => (double)GetValue(LineWidthProperty);
        set => SetValue(LineWidthProperty, value);
    }

    public Color AxesColor
    {
        get => (Color)GetValue(AxesColorProperty);
        set => SetValue(AxesColorProperty, value);
    }

    public Color GraphBackground
    {
        get => (Color)GetValue(GraphBackgroundProperty);
        set => SetValue(GraphBackgroundProperty, value);
    }

    public Color GridLinesColor
    {
        get => (Color)GetValue(GridLinesColorProperty);
        set => SetValue(GridLinesColorProperty, value);
    }

    /// <summary>Upstream spells it "Seperator"; the typo is part of the contract.</summary>
    public bool UseCommaDecimalSeperator { get; set; }

    public EquationCollection Equations => _equations;

    // TODO(P5): needs the native solver to publish its variable table. Stays empty, so
    // GraphingCalculator.xaml.cs:528 clears the ViewModel's variable list on every callback.
    public IDictionary<string, Variable> Variables => _variables;

    // TODO(P5): needs RenderMain's trace cursor. Reporting (0,0) keeps PositionGraphPopup
    // (GraphingCalculator.xaml.cs:571-589) arithmetic-safe; the popup just parks top-left.
    public Windows.Foundation.Point TraceLocation => default;

    /// <summary>0 = invalid, 1 = radians, 2 = degrees, 3 = gradians.</summary>
    public int TrigUnitMode { get; set; }

    public double XAxisMin
    {
        get => _xAxisMin;
        set => _xAxisMin = value;
    }

    public double XAxisMax
    {
        get => _xAxisMax;
        set => _xAxisMax = value;
    }

    public double YAxisMin
    {
        get => _yAxisMin;
        set => _yAxisMin = value;
    }

    public double YAxisMax
    {
        get => _yAxisMax;
        set => _yAxisMax = value;
    }

    // GraphingSettingsViewModel.InitRanges calls this unconditionally when the settings flyout
    // opens, so it reports the stored range rather than throwing.
    public void GetDisplayRanges(out double xMin, out double xMax, out double yMin, out double yMax)
    {
        xMin = _xAxisMin;
        xMax = _xAxisMax;
        yMin = _yAxisMin;
        yMax = _yAxisMax;
    }

    public void SetDisplayRanges(double xMin, double xMax, double yMin, double yMax)
    {
        _xAxisMin = xMin;
        _xAxisMax = xMax;
        _yAxisMin = yMin;
        _yAxisMax = yMax;
    }

    // TODO(P5): needs Graphing::IGraph / RenderMain. No-op so adding equations cannot crash the page.
    public void PlotGraph(bool keepCurrentView)
    {
    }

    // TODO(P5): needs RenderMain's viewport transform.
    public void ZoomFromCenter(double scale)
    {
    }

    // TODO(P5): needs RenderMain to restore the default grid.
    public void ResetGrid()
    {
        _xAxisMin = -10.0;
        _xAxisMax = 10.0;
        _yAxisMin = -10.0;
        _yAxisMax = 10.0;
    }

    // TODO(P5): needs the native solver's variable table; the entry is kept so the slider UI round-trips.
    public void SetVariable(string variableName, double newValue)
    {
        if (_variables.TryGetValue(variableName, out var variable))
        {
            variable.Value = newValue;
        }
    }

    // TODO(P5): needs the MathRichEdit wrapper (MathML -> linear syntax). Echoes its input.
    public string ConvertToLinear(string mmlString) => mmlString;

    // TODO(P5): needs the MathRichEdit wrapper (linear syntax -> MathML). Echoes its input.
    public string FormatMathML(string mmlString) => mmlString;

    // TODO(P5): needs a DirectX swap-chain capture. Returning null is deliberate: the share
    // path (GraphingCalculator.xaml.cs:483) is inside a try/catch and degrades to the
    // "share failed" dialog. The context-menu Copy path (:835) is NOT guarded and will
    // fault - fix that by returning a real stream here, not by editing the consumer.
    public RandomAccessStreamReference GetGraphBitmapStream() => null!;

    // TODO(P5): needs Graphing::IGraphAnalyzer. Reports AnalysisCouldNotBePerformed, which is the
    // path EquationViewModel.PopulateKeyGraphFeatures already handles; it throws on null.
    public KeyGraphFeaturesInfo AnalyzeEquation(Equation equation) => new();

    private void RaisePropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
