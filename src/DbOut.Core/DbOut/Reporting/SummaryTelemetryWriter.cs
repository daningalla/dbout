namespace DbOut.Reporting;

internal static class SummaryTelemetryWriter
{
    public static void Write(SummaryTelemetryData telemetryData, TextWriter textWriter)
    {
        var count = 0;
        foreach (var (key, value) in telemetryData.Counters.OrderBy(kv => kv.Key))
        {
            if (count++ == 0)
            {
                textWriter.WriteLine("Counters:");
            }

            textWriter.WriteLine($"\t{key}={value}");
        }

        if (count > 0) textWriter.WriteLine();
        count = 0;

        foreach (var (key, value) in telemetryData.Numerics.OrderBy(kv => kv.Key))
        {
            if (count++ == 0)
            {
                textWriter.WriteLine("Statistics");
            }

            var display = $"min={value.Min:F2}, max={value.Max:F2}, count={value.MeanCount:F2}, mean={value.Mean:F2}";
            textWriter.WriteLine($"\t{key}: {display}");
        }

        if (count > 0) textWriter.WriteLine();
        count = 0;

        var listGroups = telemetryData.Lists.GroupBy(kv => kv.Key, kv => kv.Value);
        foreach (var group in listGroups)
        {
            textWriter.WriteLine();
            textWriter.WriteLine(group.Key);
            foreach (var entry in group)
            {
                textWriter.WriteLine($"\t{entry}");
            }

            count++;
        }

        if (count > 0) textWriter.WriteLine();
        var hashGroups = telemetryData.HashSets.GroupBy(kv => kv.Key, kv => kv.Value);
        foreach (var group in hashGroups)
        {
            textWriter.WriteLine();
            textWriter.WriteLine(group.Key);
            foreach (var entry in group)
            {
                textWriter.WriteLine($"\t{entry}");
            }
        }
    }
}