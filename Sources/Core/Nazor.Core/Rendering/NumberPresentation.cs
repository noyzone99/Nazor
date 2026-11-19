namespace Nazor.Rendering;

internal sealed class NumberPresentation : IDisposable
{
    private Action? changed;
    private bool initialized;
    public NumberPresentation(Action changed) => this.changed = changed;
    public double Target { get; private set; }
    public double Minimum { get; private set; }
    public double Maximum { get; private set; } = 1;
    public double Value { get; private set; }
    public bool AtStart { get; private set; } = true;
    public bool AtEnd { get; private set; }
    public bool IsDisposed => changed is null;

    public void SetTarget(double value, double minimum, double maximum)
    {
        (Target, Minimum, Maximum) = (Math.Clamp(value, minimum, maximum), minimum, maximum);
        if (initialized) return;
        initialized = true;
        Value = minimum;
        AtStart = true;
        AtEnd = minimum == maximum;
    }

    public bool Publish(double value, bool atStart, bool atEnd)
    {
        if (IsDisposed || Value == value && AtStart == atStart && AtEnd == atEnd) return false;
        (Value, AtStart, AtEnd) = (value, atStart, atEnd);
        changed?.Invoke();
        return true;
    }

    public void Dispose() => changed = null;
}
