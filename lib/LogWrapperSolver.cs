namespace bot;

public static class SolverLoggingExtension
{
    public static ISolver<TState, TSolution> WithLogging<TState, TSolution>(this ISolver<TState, TSolution> solver, int bestSolutionsCountToLog) where TSolution : ISolution
    {
        return new LogWrapperSolver<TState, TSolution>(solver, bestSolutionsCountToLog);
    }
}
    
public class LogWrapperSolver<TGameState, TSolution> : ISolver<TGameState, TSolution> where TSolution : ISolution
{
    private readonly ISolver<TGameState, TSolution> solver;
    private readonly int solutionsCountToLog;

    public LogWrapperSolver(ISolver<TGameState, TSolution> solver, int solutionsCountToLog)
    {
        this.solver = solver;
        this.solutionsCountToLog = solutionsCountToLog;
    }

    public string ShortName => solver.ShortName;
        
    public override string ToString() => solver.ToString();

    public IEnumerable<TSolution> GetSolutions(TGameState problem, Countdown countdown)
    {
        var items = solver.GetSolutions(problem, countdown).ToList();
        var list = items.ToList();
        var bestItems = list.Cast<TSolution>().Reverse().Take(solutionsCountToLog).ToList();
        Console.Error.WriteLine("## Best found:");
        Console.Error.WriteLine(bestItems.StrJoin("\n"));
        Console.Error.WriteLine("## Solver debug info:");
        Console.Error.WriteLine(solver.ToString());
        Console.Error.WriteLine($"Time spent: {countdown.TimeElapsed.TotalMilliseconds} ms");
        return list;
    }
}