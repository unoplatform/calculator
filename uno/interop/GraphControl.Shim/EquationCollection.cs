// GraphControl shim — stands in for 4fd3fc53:src/GraphControl/Models/EquationCollection.h.
// Upstream hand-rolls IObservableVector<Equation>; consumers only use the indexer, Add, Clear,
// RemoveAt and Count (GraphingCalculator.xaml.cs:285-317), all of which ObservableCollection gives.
#nullable enable

using System.Collections.ObjectModel;

namespace GraphControl;

public sealed class EquationCollection : ObservableCollection<Equation>
{
}
