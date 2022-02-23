using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace bot
{
    public abstract class AbstractMonteCarloSolver<TProblem, TSolution> : ISolver<TProblem, TSolution> where TSolution : ISolution
    {
        public readonly StatValue ImprovementsCount = StatValue.CreateEmpty();
        public readonly StatValue SimulationsCount = StatValue.CreateEmpty();
        public readonly StatValue BestSolutionTimeMs = StatValue.CreateEmpty();

        public string GetDebugStats()
        {
            return new[]
            {
                $"sims: {SimulationsCount.ToDetailedString()}",
                $"imps: {ImprovementsCount.ToDetailedString()}",
                $"time: {BestSolutionTimeMs.ToDetailedString()}"
            }.StrJoin("\n");
        }

        protected abstract TSolution GenerateRandomSolution(TProblem problem);

        public override string ToString()
        {
            return "MC";
        }

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
                    solution.DebugInfo = new SolutionDebugInfo(countdown, simCount, improvementsCount, ToString());
                    steps.Add(solution);
                }
            }

            SimulationsCount.Add(simCount);
            ImprovementsCount.Add(improvementsCount);
            if (steps.Count > 0)
                BestSolutionTimeMs.Add(steps.Last().DebugInfo.Time.TotalMilliseconds);
            return steps;
        }
    }
}