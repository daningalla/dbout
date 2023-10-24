namespace DbOut.Reporting;

public class TrackedNumeric
{
    public required double Min { get; set; }
    public required double Max { get; set; }
    public required int MeanCount { get; set; }
    public required double MeanTotal { get; set; }

    public required double Value { get; set; }
    public double Mean => MeanCount > 0 ? MeanTotal / MeanCount : 0;

    public void Update(double value)
    {
        Min = Math.Min(Value, value);
        Max = Math.Max(Value, value);
        MeanCount++;
        MeanTotal += value;
        Value = value;
    }

    public TrackedNumeric Copy()
    {
        return new TrackedNumeric
        {
            Max = Max,
            Min = Min,
            MeanCount = MeanCount,
            MeanTotal = MeanTotal,
            Value = Value
        };
    }
}