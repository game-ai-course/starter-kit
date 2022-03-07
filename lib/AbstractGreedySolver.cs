using System;
using System.Collections.Generic;
using System.Linq;

namespace bot
{
    public abstract class AbstractGreedySolver<TGameState, TMove> : ISolver<TGameState, SingleMoveSolution<TMove>>
    {
        private readonly IEstimator<TGameState> estimator;
        private readonly Random random;

        /// <summary>
        /// Решения с одинаковой оценкой выводит в случайном порядке, для чего специально их перемешивает с помощью random.
        /// Можно передавать random с зафиксированным seed, чтобы получить воспроизводимые результаты. 
        /// </summary>
        protected AbstractGreedySolver(IEstimator<TGameState> estimator, Random random)
        {
            this.estimator = estimator;
            this.random = random;
        }

        public string ShortName => $"G-{estimator}";

        public IEnumerable<SingleMoveSolution<TMove>> GetSolutions(TGameState problem, Countdown countdown)
        {
            var moves = GetMoves(problem)
                .Select(m => CreateGreedySolution(problem, m, countdown))
                .Shuffle(random) // randomization!
                .OrderBy(s => s.Score)
                .ToList();
            return moves;
        }

        private SingleMoveSolution<TMove> CreateGreedySolution(TGameState problem, TMove move, Countdown countdown)
        {
            var clone = ApplyMove(problem, move);
            var score = -estimator.GetScore(clone);
            return new SingleMoveSolution<TMove>(move, score)
            {
                DebugInfo = new SolutionDebugInfo(countdown, 0, 0, ShortName)
            };
        }

        protected abstract TGameState ApplyMove(TGameState problem, TMove move);
        protected abstract IEnumerable<TMove> GetMoves(TGameState problem);
    }
}