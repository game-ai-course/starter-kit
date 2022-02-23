using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;

namespace bot
{
    public static class SolverLoggingExtension
    {
        public static ISolver<TState, TSolution> WithLogging<TState, TSolution>(this ISolver<TState, TSolution> solver, int bestSolutionsCountToLog) where TSolution : ISolution
        {
            return new LogWrapper<TState, TSolution>(solver, bestSolutionsCountToLog);
        }
    }
    
    public class LogWrapper<TGameState, TSolution> : ISolver<TGameState, TSolution> where TSolution : ISolution
    {
        private readonly ISolver<TGameState, TSolution> solver;
        private readonly int solutionsCountToLog;

        public LogWrapper(ISolver<TGameState, TSolution> solver, int solutionsCountToLog)
        {
            this.solver = solver;
            this.solutionsCountToLog = solutionsCountToLog;
        }

        public IEnumerable<TSolution> GetSolutions(TGameState problem, Countdown countdown)
        {
            var items = solver.GetSolutions(problem, countdown).ToList();
            var list = items.ToList();
            var bestItems = list.Cast<TSolution>().Reverse().Take(solutionsCountToLog).ToList();
            Console.Error.WriteLine(bestItems.StrJoin("\n"));
            Console.Error.WriteLine($"Time spent: {countdown.TimeElapsed.TotalMilliseconds} ms");
            return list;
        }
    }
}