namespace bot;

public class StatValue
{
    public static StatValue CreateEmpty(string name = null) => new StatValue(name);
        
    private StatValue(string name)
    {
        Name = name;
    }
        
    public string Name { get; }

    public StatValue(long count, double sum, double sum2, double min, double max, string name)
    {
        Count = count;
        Sum = sum;
        Sum2 = sum2;
        Min = min;
        Max = max;
        Name = name;
    }

    public long Count { get; private set; }
    public double Sum { get; private set; }
    public double Sum2 { get; private set; }
    public double Min { get; private set; } = double.PositiveInfinity;
    public double Max { get; private set; } = double.NegativeInfinity;

    /// <summary>
    /// Standard deviation = sigma = sqrt(Dispersion)
    /// sigma^2 = /(n-1)
    /// </summary>
    public double StdDeviation => Math.Sqrt(Dispersion);

    /// <summary>
    /// D = sum{(xi - mean)²}/(count-1) =
    ///   = sum{xi² - 2 xi mean + mean²} / (count-1) =
    ///   = (sum2 + sum*sum/count - 2 sum * sum / count) / (count-1) =
    ///   = (sum2 - sum*sum / count) / (count - 1)
    /// </summary>
    public double Dispersion => (Sum2 - Sum*Sum/Count) / (Count-1);

    /// <summary>
    ///     2 sigma confidence interval for mean value of random value
    /// </summary>
    public double ConfIntervalSize2Sigma => 2 * StdDeviation / Math.Sqrt(Count);

    public double Mean => Sum / Count;

    public void Add(double value)
    {
        Count++;
        Sum += value;
        Sum2 += value * value;
        Min = Math.Min(Min, value);
        Max = Math.Max(Max, value);
    }

    public void AddAll(StatValue value)
    {
        Count += value.Count;
        Sum += value.Sum;
        Sum2 += value.Sum2;
        Min = Math.Min(Min, value.Min);
        Max = Math.Max(Max, value.Max);
    }

    private string NamePrefix() =>
        Name == null ? "" : $"{Name}: ";
        
    public override string ToString()
    {
        return ToString(true);
    }

    public string ToString(bool humanReadable)
    {
        if (humanReadable)
            return NamePrefix() + 
                   $"{Mean.ToCompactString()} " +
                   $"stdd={StdDeviation.ToCompactString()} " +
                   $"min..max={Min.ToCompactString()}..{Max.ToCompactString()} " +
                   $"confInt={ConfIntervalSize2Sigma.ToCompactString()} " +
                   $"count={Count}";
        FormattableString line = $"{Mean}\t{StdDeviation}\t{ConfIntervalSize2Sigma}\t{Count}";
        return line.ToString(CultureInfo.InvariantCulture);
    }

    public StatValue Clone()
    {
        return new StatValue(Count, Sum, Sum2, Min, Max, Name);
    }
}