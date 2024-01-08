namespace bot;

public interface IEstimator<in TState>
{
    double GetScore(TState state);
}