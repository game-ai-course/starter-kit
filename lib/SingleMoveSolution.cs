using System;

namespace bot
{
    public class SingleMoveSolution<TMove> : ISolution, IComparable<SingleMoveSolution<TMove>>
    {
        public readonly TMove Move;

        public SingleMoveSolution(TMove move, double score)
        {
            Move = move;
            Score = score;
        }

        public double Score { get; }
        public SolutionDebugInfo DebugInfo { get; set; }

        public override string ToString()
        {
            return $"{Score} {Move} {DebugInfo}";
        }

        public int CompareTo(SingleMoveSolution<TMove> other)
        {
            return Score.CompareTo(other.Score);
        }
    }
}