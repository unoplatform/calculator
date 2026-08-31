// GraphControl shim — port of the PROPERTY_R list in
// 4fd3fc53:src/GraphControl/Models/KeyGraphFeaturesInfo.h:20-37.
//
// The property TYPES drive overload resolution in EquationViewModel: Domain/Range/XIntercept/
// YIntercept pick AddKeyGraphFeature(string, string, string) while the six collections pick
// AddKeyGraphFeature(string, IEnumerable<string>, string). Monotonicity's VALUE is a string —
// EquationViewModel.cs:403 compares it against "0".."3".
#nullable enable

using System.Collections.Generic;

namespace GraphControl;

public sealed class KeyGraphFeaturesInfo
{
    /// <summary>
    /// Mirrors CalculatorApp.ViewModel.AnalysisErrorType. Defaults to 1
    /// (AnalysisCouldNotBePerformed) so a stubbed analysis lands on the error path rather than
    /// rendering empty feature rows.
    /// </summary>
    public int AnalysisError { get; init; } = 1;

    public string Domain { get; init; } = string.Empty;

    public string Range { get; init; } = string.Empty;

    public string XIntercept { get; init; } = string.Empty;

    public string YIntercept { get; init; } = string.Empty;

    public IList<string> Minima { get; init; } = new List<string>();

    public IList<string> Maxima { get; init; } = new List<string>();

    public IList<string> InflectionPoints { get; init; } = new List<string>();

    public IList<string> VerticalAsymptotes { get; init; } = new List<string>();

    public IList<string> HorizontalAsymptotes { get; init; } = new List<string>();

    public IList<string> ObliqueAsymptotes { get; init; } = new List<string>();

    public int Parity { get; init; }

    public int PeriodicityDirection { get; init; }

    public string PeriodicityExpression { get; init; } = string.Empty;

    public IDictionary<string, string> Monotonicity { get; init; } = new Dictionary<string, string>();

    public int TooComplexFeatures { get; init; }
}
