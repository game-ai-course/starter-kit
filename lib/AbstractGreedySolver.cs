using System;
using System.Collections.Generic;
using System.Linq;

namespace bot
{
    public abstract class AbstractGreedySolver<TGameState, TMove> : ISolver<TGameState, SingleMoveSolution<TMove>>
    {
        private readonly IEstimator<TGameState> estimator;
        private readonly int numberOfBestMovesToLog;

        protected AbstractGreedySolver(IEstimator<TGameState> estimator, int numberOfBestMovesToLog = 0)
        {
            this.estimator = estimator;
            this.numberOfBestMovesToLog = numberOfBestMovesToLog;
        }

        public IEnumerable<SingleMoveSolution<TMove>> GetSolutions(TGameState problem, Countdown countdown)
        {
            var moves = GetMoves(problem)
                .Select(m => CreateGreedySolution(problem, m, countdown))
                .OrderBy(s => s.Score)
                .ToList();
            if (numberOfBestMovesToLog > 0)
            {
                var text = moves.Cast<SingleMoveSolution<TMove>>()
                    .Reverse().Take(numberOfBestMovesToLog).StrJoin("\n");
                Console.Error.WriteLine(text);
            }

            return moves;
        }

        private SingleMoveSolution<TMove> CreateGreedySolution(TGameState problem, TMove move, Countdown countdown)
        {
            var clone = ApplyMove(problem, move);
            var score = -estimator.GetScore(clone);
            return new SingleMoveSolution<TMove>(move, score)
            {
                DebugInfo = new SolutionDebugInfo(countdown, 0, 0, ToString())
            };
        }

        public override string ToString()
        {
            return $"G-{estimator}";
        }

        protected abstract TGameState ApplyMove(TGameState problem, TMove move);
        protected abstract IEnumerable<TMove> GetMoves(TGameState problem);
    }
}