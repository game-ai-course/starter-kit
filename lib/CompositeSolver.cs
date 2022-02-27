using System.Collections.Generic;
using System.Linq;

namespace bot
{
    public class CompositeSolver<TGameState, TSolution> : ISolver<TGameState, TSolution> where TSolution : ISolution
    {
        private readonly (ISolver<TGameState, TSolution> solver, double timeFraction)[] solvers;

        protected CompositeSolver(params (ISolver<TGameState, TSolution> solver, double timeFraction)[] solvers)
        {
            var total = solvers.Sum(s => s.timeFraction);
            this.solvers = solvers.ToArray();
            for (var index = 0; index < solvers.Length; index++)
            {
                var (solver, timeFraction) = solvers[index];
                solvers[index] = (solver, timeFraction / total);
                total -= timeFraction;
            }
        }

        public string ShortName => $"[{solvers.StrJoin(" → ")}]";

        public override string ToString()
        {
            return solvers.StrJoin("\n\n", s => $"{s.solver.ShortName} {s.timeFraction}:\n{s.solver}");
        }

        public IEnumerable<TSolution> GetSolutions(TGameState problem, Countdown countdown)
        {
            var bestScore = double.NegativeInfinity;
            foreach (var (solver, timeFraction) in solvers)
            {
                foreach (var solution in solver.GetSolutions(problem, countdown * timeFraction))
                {
                    if (solution.Score >= bestScore)
                    {
                        yield return solution;
                        bestScore = solution.Score;
                    }
                }
            }
        }
    }
}