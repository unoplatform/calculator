// GraphControl shim — 1:1 port of 4fd3fc53:src/GraphControl/Models/Variable.h.
// VariableViewModel mutates this through a stored reference, so it must stay a class.
#nullable enable

namespace GraphControl;

public sealed class Variable
{
    public Variable()
    {
    }

    public Variable(double value)
    {
        Value = value;
    }

    public double Value { get; set; }

    public double Step { get; set; } = 0.1;

    public double Min { get; set; } = -5.0;

    public double Max { get; set; } = 5.0;
}
