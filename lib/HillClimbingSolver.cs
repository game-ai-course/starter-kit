namespace bot;

public class HillClimbingSolver<TProblem, TSolution> : ISolver<TProblem, TSolution>
    where TSolution : class, ISolution 
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
        
    private readonly ISolver<TProblem, TSolution> baseSolver;
    protected readonly IMutator<TProblem, TSolution> Mutator;
    private readonly double baseSolverTimeFraction;

    public HillClimbingSolver(
        ISolver<TProblem, TSolution> baseSolver, 
        IMutator<TProblem, TSolution> mutator,
        double baseSolverTimeFraction = 0.1)
    {
        this.baseSolver = baseSolver;
        Mutator = mutator;
        this.baseSolverTimeFraction = baseSolverTimeFraction;
    }

    public string ShortName => $"HC_({Mutator})_({baseSolver})_{baseSolverTimeFraction:#%}";

    [SuppressMessage("ReSharper", "SuspiciousTypeConversion.Global")]
    public IEnumerable<TSolution> GetSolutions(TProblem problem, Countdown countdown)
    {
        var mutationsCount = 0;
        var improvementsCount = 0;
        var baseSolution = baseSolver.GetSolutions(problem, countdown * baseSolverTimeFraction).Last();
        var steps = new List<TSolution> { baseSolution };
        while (!countdown.IsFinished())
        {
            var improvements = TryImprove(problem, steps.Last());
            mutationsCount++;
            foreach (var solution in improvements)
            {
                improvementsCount++;
                solution.DebugInfo = new SolutionDebugInfo(countdown, mutationsCount, 
                    improvementsCount, ShortName);
                steps.Add(solution);
            }
        }
        SimulationsCount.Add(mutationsCount);
        ImprovementsCount.Add(improvementsCount);
        if (steps.Count > 0)
            TimeToFindBestMs.Add(steps.Last().DebugInfo.Time.TotalMilliseconds);
        return steps;
    }

    protected IEnumerable<TSolution> TryImprove(TProblem problem, TSolution bestSolution)
    {
        var mutation = Mutator.Mutate(problem, bestSolution);
        if (!(mutation.Score > bestSolution.Score)) yield break;
        bestSolution = mutation.GetResult();
        yield return bestSolution;
    }
}