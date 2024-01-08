namespace bot;

public abstract class AbstractMonteCarloSolver<TProblem, TSolution> : ISolver<TProblem, TSolution> where TSolution : ISolution
{
    public readonly StatValue ImprovementsCount = StatValue.CreateEmpty("Improvements");
    public readonly StatValue SimulationsCount = StatValue.CreateEmpty("Simulations");
    public readonly StatValue TimeToFindBestMs = StatValue.CreateEmpty("TimeOfBestMs");

    public override string ToString()
    {
        return new[]
        {
            SimulationsCount,
            ImprovementsCount,
            TimeToFindBestMs
        }.StrJoin("\n");
    }

    public string ShortName => "MC";

    protected abstract TSolution GenerateRandomSolution(TProblem problem);

    public IEnumerable<TSolution> GetSolutions(TProblem problem, Countdown countdown)
    {
        var simCount = 0;
        var improvementsCount = 0;
        var bestScore = double.NegativeInfinity;
        var steps = new List<TSolution>();
        while (!countdown.IsFinished())
        {
            var solution = GenerateRandomSolution(problem);
            simCount++;
            if (solution.Score > bestScore)
            {
                improvementsCount++;
                bestScore = solution.Score;
                solution.DebugInfo = new SolutionDebugInfo(countdown, simCount, improvementsCount, ShortName);
                steps.Add(solution);
            }
        }

        SimulationsCount.Add(simCount);
        ImprovementsCount.Add(improvementsCount);
        if (steps.Count > 0)
            TimeToFindBestMs.Add(steps.Last().DebugInfo.Time.TotalMilliseconds);
        return steps;
    }
}