namespace bot;

public class Countdown
{
    public readonly TimeSpan Duration;
    private readonly Stopwatch sw;

    public Countdown(TimeSpan duration)
    {
        sw = Stopwatch.StartNew();
        Duration = duration;
    }

    public Countdown(double durationMs)
        : this(TimeSpan.FromMilliseconds(durationMs))
    {
    }

    public TimeSpan TimeElapsed => sw.Elapsed;

    public TimeSpan TimeAvailable => IsFinished() ? TimeSpan.Zero : Duration - TimeElapsed;

    public static Countdown operator /(Countdown cd, double v)
    {
        return new Countdown(cd.TimeAvailable.TotalMilliseconds / v);
    }

    public static Countdown operator *(Countdown cd, double v)
    {
        return new Countdown(cd.TimeAvailable.TotalMilliseconds * v);
    }

    public static Countdown operator *(double v, Countdown cd)
    {
        return new Countdown(cd.TimeAvailable.TotalMilliseconds * v);
    }

    public bool IsFinished()
    {
        return sw.Elapsed >= Duration;
    }

    public override string ToString()
    {
        return $"Elapsed {TimeElapsed.TotalMilliseconds:0} ms. Available {TimeAvailable.TotalMilliseconds:0} ms";
    }

    public static implicit operator Countdown(int milliseconds)
    {
        return new Countdown(milliseconds);
    }

    public static implicit operator Countdown(long milliseconds)
    {
        return new Countdown(milliseconds);
    }
}