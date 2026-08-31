// GraphControl shim — event handler delegates from 4fd3fc53:src/GraphControl/Control/Grapher.h:20-26.
// GraphingCalculator.xaml.cs subscribes method groups to all three (lines 43, 46, 49).
#nullable enable

namespace GraphControl;

public delegate void TracingChangedEventHandler(bool newValue);

public delegate void TracingValueChangedEventHandler(double xPointValue, double yPointValue);

public delegate void PointerValueChangedEventHandler(Windows.Foundation.Point value);
