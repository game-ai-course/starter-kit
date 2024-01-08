using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Runtime.CompilerServices;
using System.Collections;

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

namespace bot
{
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
}

namespace bot
{
    public record BotCommand
    {
        private static readonly Dictionary<string, Func<object, object>[]> ParamGetters = new();

        private Func<object, object> ParamGetter(ParameterInfo p)
        {
            var fields = GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var field = fields.SingleOrDefault(f => f.Name.Equals(p.Name, StringComparison.OrdinalIgnoreCase));
            if (field != null)
                return o => field.GetValue(o);
            var properties = GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var property = properties.SingleOrDefault(f => f.Name.Equals(p.Name, StringComparison.OrdinalIgnoreCase));
            if (property != null)
                return o => property.GetValue(o);
            return null;
        }

        public string Message;

        public sealed override string ToString()
        {
            var commandName = GetType().Name;
            if (commandName.EndsWith("Command"))
                commandName = commandName.Substring(0, commandName.Length - 7); //"Command".Length

            var commandParams = GetParamGetters(commandName).Select(get => get(this));
            var parts = commandParams.Prepend(commandName.ToUpper());
            if (!string.IsNullOrWhiteSpace(Message))
                parts = parts.Append(Message);
            return parts.StrJoin(" ");
        }

        private Func<object, object>[] GetParamGetters(string commandName)
        {
            if (ParamGetters.TryGetValue(commandName, out var getters))
                return getters;
            return ParamGetters[commandName] = 
                GetType()
                .GetConstructors().Single().GetParameters()
                .Select(ParamGetter)
                .Where(g => g != null)
                .ToArray();
        }
    }
}

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

namespace bot
{
    public class ConsoleReader
    {
        private int curLine;
        private readonly string[] textLines;
        private readonly List<string> linesFromConsole = new List<string>();
        
        public ConsoleReader(string text = null)
        {
            textLines = text?.Split('|');
        }

        public int ReadNum()
        {
            return ReadLine().ToInt();
        }

        public long ReadLong()
        {
            return ReadLine().ToLong();
        }
        
        public int[] ReadNums()
        {
            return ReadLine().Split(' ').Select(int.Parse).ToArray();
        }
        
        public string ReadLine()
        {
            if (textLines != null) return textLines[curLine++];
            linesFromConsole.Add(Console.ReadLine());
            return linesFromConsole.Last();
        }

        public void FlushToStdErr()
        {
            Console.Error.WriteLine(linesFromConsole.StrJoin('|'));
            linesFromConsole.Clear();
        }
    }
}

namespace bot
{
    public class Countdown
    {
        public readonly TimeSpan Duration;
        private readonly Stopwatch sw;

        public Countdown(TimeSpan duration)
        {
            sw = Stopwatch.StartNew();
            Duration = duration;
        }

        public Countdown(double durationMs)
            : this(TimeSpan.FromMilliseconds(durationMs))
        {
        }

        public TimeSpan TimeElapsed => sw.Elapsed;

        public TimeSpan TimeAvailable => IsFinished() ? TimeSpan.Zero : Duration - TimeElapsed;

        public static Countdown operator /(Countdown cd, double v)
        {
            return new Countdown(cd.TimeAvailable.TotalMilliseconds / v);
        }

        public static Countdown operator *(Countdown cd, double v)
        {
            return new Countdown(cd.TimeAvailable.TotalMilliseconds * v);
        }

        public static Countdown operator *(double v, Countdown cd)
        {
            return new Countdown(cd.TimeAvailable.TotalMilliseconds * v);
        }

        public bool IsFinished()
        {
            return sw.Elapsed >= Duration;
        }

        public override string ToString()
        {
            return $"Elapsed {TimeElapsed.TotalMilliseconds:0} ms. Available {TimeAvailable.TotalMilliseconds:0} ms";
        }

        public static implicit operator Countdown(int milliseconds)
        {
            return new Countdown(milliseconds);
        }

        public static implicit operator Countdown(long milliseconds)
        {
            return new Countdown(milliseconds);
        }
    }
}

namespace bot
{
    public class Disk
    {
        public Disk(V pos, int radius)
        {
            Radius = radius;
            Pos = pos;
        }

        public int Radius { get; }
        public V Pos { get; set; }

        public bool Intersect(Disk disk)
        {
            var minDist = disk.Radius + Radius;
            return minDist * minDist >= (Pos - disk.Pos).Len2;
        }

        public bool Contains(V point)
        {
            return (point - Pos).Len2 <= Radius * Radius;
        }

        public static Disk ParseDisk(string s)
        {
            var parts = s.Split(new []{','}).Select(int.Parse).ToList();
            return new Disk(new V(parts[0], parts[1]), parts[2]);
        }

        public override string ToString()
        {
            return $"[{Pos.X},{Pos.Y},{Radius}]";
        }
    }
}

namespace bot
{
    public static class Extensions
    {
        public static int SetBit(this int x, int bitIndex)
        {
            return x | (1 << bitIndex);
        }

        public static int GetBit(this int x, int bitIndex)
        {
            return (x >> bitIndex) & 1;
        }
        public static double Distance(this double a, double b)
        {
            return Math.Abs(a - b);
        }

        public static double Squared(this double x) => x * x;

        public static int IndexOf<T>(this IEnumerable<T> items, Func<T, bool> predicate)
        {
            var i = 0;
            foreach (var item in items)
            {
                if (predicate(item)) return i;
                i++;
            }

            return -1;
        }

        public static int IndexOf<T>(this IReadOnlyList<T> readOnlyList, T value)
        {
            var count = readOnlyList.Count;
            var equalityComparer = EqualityComparer<T>.Default;
            for (var i = 0; i < count; i++)
            {
                var current = readOnlyList[i];
                if (equalityComparer.Equals(current, value)) return i;
            }

            return -1;
        }

        public static T MinBy<T>(this IEnumerable<T> items, Func<T, IComparable> getKey)
        {
            var best = default(T);
            IComparable bestKey = null;
            var found = false;
            foreach (var item in items)
                if (!found || getKey(item).CompareTo(bestKey) < 0)
                {
                    best = item;
                    bestKey = getKey(best);
                    found = true;
                }

            return best;
        }

        public static double MaxOrDefault<T>(this IEnumerable<T> items, Func<T, double> getCost, double defaultValue)
        {
            var bestCost = double.NegativeInfinity;
            foreach (var item in items)
            {
                var cost = getCost(item);
                if (cost > bestCost)
                    bestCost = cost;
            }

            return double.IsNegativeInfinity(bestCost) ? defaultValue : bestCost;
        }

        public static T MaxBy<T>(this IEnumerable<T> items, Func<T, IComparable> getKey)
        {
            var best = default(T);
            IComparable bestKey = null;
            var found = false;
            foreach (var item in items)
                if (!found || getKey(item).CompareTo(bestKey) > 0)
                {
                    best = item;
                    bestKey = getKey(best);
                    found = true;
                }

            return best;
        }

        public static IList<T> AllMaxBy<T>(this IEnumerable<T> items, Func<T, double> getKey)
        {
            IList<T> result = null;
            double bestKey = double.MinValue;
            foreach (var item in items)
            {
                var itemKey = getKey(item);
                if (result == null || bestKey < itemKey)
                {
                    result = new List<T> { item };
                    bestKey = itemKey;
                }
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                else if (bestKey == itemKey)
                {
                    result.Add(item);
                }
            }
            return result ?? Array.Empty<T>();
        }

        public static int BoundTo(this int v, int left, int right)
        {
            if (v < left) return left;
            if (v > right) return right;
            return v;
        }


        public static double ToDegrees(this double radians)
        {
            return 180 * radians / Math.PI;
        }

        public static double ToRadians(this double degrees)
        {
            return degrees * Math.PI / 180;
        }

        public static double ToRadians(this int degrees)
        {
            return degrees * Math.PI / 180;
        }

        public static double BoundTo(this double v, double left, double right)
        {
            if (v < left) return left;
            if (v > right) return right;
            return v;
        }

        public static double TruncateAbs(this double v, double maxAbs)
        {
            if (v < -maxAbs) return -maxAbs;
            if (v > maxAbs) return maxAbs;
            return v;
        }

        public static int TruncateAbs(this int v, int maxAbs)
        {
            if (v < -maxAbs) return -maxAbs;
            if (v > maxAbs) return maxAbs;
            return v;
        }

        public static IEnumerable<T> Times<T>(this int count, Func<int, T> create)
        {
            return Enumerable.Range(0, count).Select(create);
        }

        public static IEnumerable<T> Times<T>(this int count, T item)
        {
            return Enumerable.Repeat(item, count);
        }

        public static string FormatAsMap<T>(this T[,] map, Func<T, V, string> formatCell)
        {
            var h = map.GetLength(1);
            var w = map.GetLength(0);
            return Enumerable.Range(0, h).Select(y =>
                Enumerable.Range(0, w).Select(x => formatCell(map[x, y], new V(x, y))).StrJoin("")
            ).StrJoin("\n");
        }

        public static T GetOrDefault<T>(this T[,] grid, int x, int y, T defaultValue = default)
        {
            if (x < 0 || y < 0 || x >= grid.GetLength(0) || y >= grid.GetLength(1)) return defaultValue;
            return grid[x, y];
        }

        public static T GetOrDefault<T>(this T[,] grid, V pos, T defaultValue = default)
        {
            var (x, y) = pos;
            if (x < 0 || y < 0 || x >= grid.GetLength(0) || y >= grid.GetLength(1)) return defaultValue;
            return grid[x, y];
        }

        public static bool InRange(this int v, int min, int max)
        {
            return v >= min && v <= max;
        }

        public static bool InRange(this double v, double min, double max)
        {
            return v >= min && v <= max;
        }

        public static TV GetOrCreate<TK, TV>(this IDictionary<TK, TV> d, TK key, Func<TK, TV> create)
        {
            TV v;
            if (d.TryGetValue(key, out v)) return v;
            return d[key] = create(key);
        }

        public static TV GetOrDefault<TK, TV>(this IDictionary<TK, TV> d, TK key, TV def = default)
        {
            TV v;
            if (d.TryGetValue(key, out v)) return v;
            return def;
        }

        public static void Increment<TK>(this IDictionary<TK, int> d, TK key)
        {
            if (d.TryGetValue(key, out var v))
                d[key] = v + 1;
            else
                d[key] = 1;
        }

        public static int ElementwiseHashcode<T>(this IEnumerable<T> items)
        {
            unchecked
            {
                return items.Select(t => t.GetHashCode()).Aggregate((res, next) => (res * 379) ^ next);
            }
        }

        public static List<T> Shuffle<T>(this IEnumerable<T> items, Random random)
        {
            var copy = items.ToList();
            for (var i = 0; i < copy.Count; i++)
            {
                var nextIndex = random.Next(i, copy.Count);
                (copy[nextIndex], copy[i]) = (copy[i], copy[nextIndex]);
            }

            return copy;
        }

        public static double NormAngleInRadians(this double angle)
        {
            angle %= 2*Math.PI;
            if (angle < 0) angle += 2*Math.PI;
            if (angle > Math.PI) angle -= 2*Math.PI;
            return angle;
        }

        public static int ToInt(this string s)
        {
            return int.Parse(s);
        }

        public static long ToLong(this string s)
        {
            return long.Parse(s);
        }

        public static string StrJoin<T>(this IEnumerable<T> items, string delimiter)
        {
            return string.Join(delimiter, items);
        }
        
        public static string StrJoin<T>(this IEnumerable<T> items, char delimiter)
        {
            return string.Join(delimiter, items);
        }

        public static string StrJoin<T>(this IEnumerable<T> items, string delimiter, Func<T, string> toString)
        {
            return items.Select(toString).StrJoin(delimiter);
        }
        
        public static bool IsOneOf<T>(this T item, params T[] set)
        {
            return set.IndexOf(item) >= 0;
        }

        public static string ToCompactString(this double x)
        {
            if (Math.Abs(x) > 100) return x.ToString("0", CultureInfo.InvariantCulture);
            if (Math.Abs(x) > 10) return x.ToString("0.#", CultureInfo.InvariantCulture);
            if (Math.Abs(x) > 1) return x.ToString("0.##", CultureInfo.InvariantCulture);
            if (Math.Abs(x) > 0.1) return x.ToString("0.###", CultureInfo.InvariantCulture);
            if (Math.Abs(x) > 0.01) return x.ToString("0.####", CultureInfo.InvariantCulture);
            return x.ToString(CultureInfo.InvariantCulture);
        }
        
    }

    public static class RandomExtensions
    {
        public static T GetRandomBest<T>(this IEnumerable<T> items, Func<T, double> getKey, Random random)
        {
            return items.AllMaxBy(getKey).SelectOne(random);
        }

        public static T SelectOne<T>(this IEnumerable<T> items, Random random)
        {
            if (!(items is ICollection<T> col))
                col = items.ToList();
            if (col.Count == 0) return default;
            var index = random.Next(col.Count);
            if (col is IList<T> list) return list[index];
            if (col is IReadOnlyList<T> roList) return roList[index];
            return col.ElementAt(index);
        }

        public static T[] Sample<T>(this Random r, IList<T> list, int sampleSize)
        {
            var sample = new T[sampleSize];
            for (var i = 0; i < sampleSize; i++)
                sample[i] = list[r.Next(list.Count)];
            return sample;
        }
        
        public static bool Chance(this Random r, double probability)
        {
            return r.NextDouble() < probability;
        }

        public static ulong NextUlong(this Random r)
        {
            var a = unchecked((ulong) r.Next());
            var b = unchecked((ulong) r.Next());
            return (a << 32) | b;
        }

        public static double NextDouble(this Random r, double min, double max)
        {
            return r.NextDouble() * (max - min) + min;
        }
    }
}

namespace bot
{
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
}

namespace bot
{
    public interface IEstimator<in TState>
    {
        double GetScore(TState state);
    }
}

namespace bot
{
    /// <summary>
    /// Иногда Score можно посчитать быстрее, чем сформировать мутированное состояние.
    /// Тогда эффективнее разделять дешевую в создании IMutation, чтобы не создавать мутированное состояние в случае,
    /// если мутация будет забракована из-за низкого Score.
    /// </summary>
    public interface IMutator<in TProblem, TSolution>
    {
        IMutation<TSolution> Mutate(TProblem problem, TSolution parentSolution);
    }

    public interface IMutation<out TResult>
    {
        double Score { get; }
        TResult GetResult();
    }
}

namespace bot
{
    public interface ISolver<in TProblem, out TSolution> where TSolution : ISolution
    {
        /// <returns>
        ///     Последовательность улучшающихся решений. Последнее решение должно быть лучшим.
        ///     Можно вернуть только самое лучшее решение, но возвращать промежуточные результаты
        ///     может быть полезно при отладке и исследовании алгоритма.
        /// </returns>
        IEnumerable<TSolution> GetSolutions(TProblem problem, Countdown countdown);
        string ShortName { get; }
    }

    public interface ISolution
    {
        double Score { get; }
        SolutionDebugInfo DebugInfo { get; set; }
    }

    public class SolutionDebugInfo
    {
        public SolutionDebugInfo(Countdown countdown, int index, int improvementIndex, string solverName)
            : this(countdown.TimeElapsed, index, improvementIndex, solverName)
        {
        }

        public SolutionDebugInfo(TimeSpan time, int index, int improvementIndex, string solverName)
        {
            Time = time;
            Index = index;
            ImprovementIndex = improvementIndex;
            SolverName = solverName;
        }

        /// <summary>
        ///     Время от старта работы алгоритма, в которое было найдено это решение.
        ///     Глядя на него можно делать выводы о скорости сходимости алгоритма.
        ///     Если время лучшего решения очень маленькое, значит алгоритм успевает сойтись слишком быстро
        ///     и фактически не использует всё отведенное время.
        ///     Если время лучшего решения близко к общему ограничению по времени, значит алгоритм не успевает сойтись
        ///     и следует либо его оптимизировать, либо упростить, уменьшив вычислительную сложность.
        /// </summary>
        public TimeSpan Time { get; }

        /// <summary>
        ///     Номер рассмотренного решения.
        ///     Глядя на него можно сделать вывод о количестве решений,
        ///     которое успевает рассмотреть алгоритм за отведенное время.
        /// </summary>
        public int Index { get; }

        /// <summary>
        ///     Номер улучшения.
        ///     Глядя на него можно сделать вывод о характере и скорости сходимости алгоритма.
        /// </summary>
        public int ImprovementIndex { get; }

        /// <summary>
        ///     Имя солвера, породившего это решение.
        /// </summary>
        public string SolverName { get; }

        public override string ToString()
        {
            var res = new StringBuilder(Time.ToString());
            if (ImprovementIndex > 0 || Index > 0)
                res.Append($" improvement {ImprovementIndex} of {Index}");
            if (!string.IsNullOrEmpty(SolverName))
                res.Append($" by {SolverName}");
            return res.ToString();
        }
    }
}

namespace bot
{
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
}

namespace bot
{
    public class MaxHeap<T> where T : IComparable<T>
    {
        private readonly List<T> values;

        public MaxHeap()
        {
            values = new List<T> { default };
        }

        public int Count => values.Count - 1;
        public T Max => values[1];

        public override string ToString()
        {
            var max = values.Count > 1 ? Max.ToString() : "NA";
            return $"Count = {values.Count} Max = {max}";
        }

        public bool TryDequeue(out T max)
        {
            var count = Count;
            if (count == 0)
            {
                max = default;
                return false;
            }

            max = Max;
            values[1] = values[count];
            values.RemoveAt(count);

            if (values.Count > 1)
            {
                BubbleDown(1);
            }

            return true;
        }

        public void Add(T item)
        {
            values.Add(item);
            BubbleUp(Count);
        }

        private void BubbleUp(int index)
        {
            int parent = index / 2;

            while (index > 1 && CompareResult(parent, index) < 0)
            {
                Exchange(index, parent);
                index = parent;
                parent /= 2;
            }
        }

        private void BubbleDown(int index)
        {
            int max;

            while (true)
            {
                int left = index * 2;
                int right = index * 2 + 1;

                if (left < values.Count &&
                    CompareResult(left, index) > 0)
                {
                    max = left;
                }
                else
                {
                    max = index;
                }

                if (right < values.Count &&
                    CompareResult(right, max) > 0)
                {
                    max = right;
                }

                if (max != index)
                {
                    Exchange(index, max);
                    index = max;
                }
                else
                {
                    return;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int CompareResult(int index1, int index2)
        {
            return values[index1].CompareTo(values[index2]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Exchange(int index1, int index2)
        {
            (values[index1], values[index2]) = (values[index2], values[index1]);
        }
    }
}

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

namespace bot
{
    public class StatValue
    {
        public static StatValue CreateEmpty(string name = null) => new StatValue(name);
        
        private StatValue(string name)
        {
            Name = name;
        }
        
        public string Name { get; }

        public StatValue(long count, double sum, double sum2, double min, double max, string name)
        {
            Count = count;
            Sum = sum;
            Sum2 = sum2;
            Min = min;
            Max = max;
            Name = name;
        }

        public long Count { get; private set; }
        public double Sum { get; private set; }
        public double Sum2 { get; private set; }
        public double Min { get; private set; } = double.PositiveInfinity;
        public double Max { get; private set; } = double.NegativeInfinity;

        /// <summary>
        /// Standard deviation = sigma = sqrt(Dispersion)
        /// sigma^2 = /(n-1)
        /// </summary>
        public double StdDeviation => Math.Sqrt(Dispersion);

        /// <summary>
        /// D = sum{(xi - mean)²}/(count-1) =
        ///   = sum{xi² - 2 xi mean + mean²} / (count-1) =
        ///   = (sum2 + sum*sum/count - 2 sum * sum / count) / (count-1) =
        ///   = (sum2 - sum*sum / count) / (count - 1)
        /// </summary>
        public double Dispersion => (Sum2 - Sum*Sum/Count) / (Count-1);

        /// <summary>
        ///     2 sigma confidence interval for mean value of random value
        /// </summary>
        public double ConfIntervalSize2Sigma => 2 * StdDeviation / Math.Sqrt(Count);

        public double Mean => Sum / Count;

        public void Add(double value)
        {
            Count++;
            Sum += value;
            Sum2 += value * value;
            Min = Math.Min(Min, value);
            Max = Math.Max(Max, value);
        }

        public void AddAll(StatValue value)
        {
            Count += value.Count;
            Sum += value.Sum;
            Sum2 += value.Sum2;
            Min = Math.Min(Min, value.Min);
            Max = Math.Max(Max, value.Max);
        }

        private string NamePrefix() =>
            Name == null ? "" : $"{Name}: ";
        
        public override string ToString()
        {
            return ToString(true);
        }

        public string ToString(bool humanReadable)
        {
            if (humanReadable)
                return NamePrefix() + 
                       $"{Mean.ToCompactString()} " +
                       $"stdd={StdDeviation.ToCompactString()} " +
                       $"min..max={Min.ToCompactString()}..{Max.ToCompactString()} " +
                       $"confInt={ConfIntervalSize2Sigma.ToCompactString()} " +
                       $"count={Count}";
            FormattableString line = $"{Mean}\t{StdDeviation}\t{ConfIntervalSize2Sigma}\t{Count}";
            return line.ToString(CultureInfo.InvariantCulture);
        }

        public StatValue Clone()
        {
            return new StatValue(Count, Sum, Sum2, Min, Max, Name);
        }
    }
}

namespace bot
{
    public class V : IEquatable<V>
    {
        public static readonly V Zero = new V(0, 0);

        public readonly int X;
        public readonly int Y;

        public static V Parse(string s)
        {
            var parts = s.Split(' ');
            return new V(int.Parse(parts[0]), int.Parse(parts[1]));
        }

        public V(int x, int y)
        {
            X = x;
            Y = y;
        }
        public V(double x, double y)
            :this((int)Math.Round(x), (int)Math.Round(y))
        {
        }


        public bool Equals(V other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return X.Equals(other.X) && Y.Equals(other.Y);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != GetType())
                return false;
            return Equals((V)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (X.GetHashCode() * 397) ^ Y.GetHashCode();
            }
        }

        public static bool operator ==(V left, V right) => Equals(left, right);
        public static bool operator !=(V left, V right) => !Equals(left, right);

        public long Len2 => (long)X * X + (long)Y * Y;
        public static readonly V None = new V(-1, -1);
        public static readonly V Up = new V(0, -1);
        public static readonly V Down = new V(0, 1);
        public static readonly V Left = new V(-1, 0);
        public static readonly V Right = new V(1, 0);

        public static readonly V[] Directions2 = { new V(1, 0), new V(0, 1) }; 
        public static readonly V[] Directions4 = { new V(1, 0), new V(0, 1), new V(-1, 0), new V(0, -1) }; 
        public static readonly V[] Directions5 = { Zero, new V(1, 0), new V(0, 1), new V(-1, 0), new V(0, -1) }; 
        public static readonly V[] Directions8 = {
            new V(-1, -1), new V(0, -1), new V(1, -1), 
            new V(-1, 0), new V(0, 0), new V(1, 0), 
            new V(-1, 1), new V(0, 1), new V(1, 1), 
        }; 

        public static readonly V[] Directions9 = {
            Zero,
            new V(-1, -1), new V(0, -1), new V(1, -1), 
            new V(-1, 0), new V(0, 0), new V(1, 0), 
            new V(-1, 1), new V(0, 1), new V(1, 1), 
        }; 

        public override string ToString()
        {
            return $"{X.ToString(CultureInfo.InvariantCulture)} {Y.ToString(CultureInfo.InvariantCulture)}";
        }

        public static V operator +(V a, V b) => new V(a.X + b.X, a.Y + b.Y);
        public static V operator -(V a, V b) => new V(a.X - b.X, a.Y - b.Y);
        public static V operator -(V a) => new V(-a.X, -a.Y);
        public static V operator *(V a, int k) => new V(k * a.X, k * a.Y);
        public static V operator *(int k, V a) => new V(k * a.X, k * a.Y);
        public static V operator /(V a, int k) => new V(a.X / k, a.Y / k);
        public long ScalarProd(V b) => X * b.X + Y * b.Y;
        public long VectorProd(V b) => X * b.Y - Y * b.X;

        public long Dist2To(V point) => (this - point).Len2;

        public double DistTo(V b) => Math.Sqrt(Dist2To(b));
        
        public double GetCollisionTime(V speed, V other, double radius) {
            if (DistTo(other) <= radius)
                return 0.0;

            if (speed.Equals(Zero))
                return double.PositiveInfinity;
            /*
             * x = x2 + vx * t
             * y = y2 + vy * t
             * x² + y² = radius²
             * ↓
             * (x2² + 2*vx*x2 * t + vx² * t²)  +  (y2² + 2*vy*y2 * t + vy² * t²) = radius²
             * ↓
             * t² * (vx² + vy²)  +  t * 2*(x2*vx + y2*vy) + x2² + y2² - radius² = 0
             */

            var x2 = X - other.X;
            var y2 = Y - other.Y;
            var vx = speed.X;
            var vy = speed.Y;

            var a = vx * vx + vy * vy;
            var b = 2.0 * (x2 * vx + y2 * vy);
            var c = x2 * x2 + y2 * y2 - radius * radius;
            var d = b * b - 4.0 * a * c;

            if (d < 0.0)
                return double.PositiveInfinity;

            var t = (-b - Math.Sqrt(d)) / (2.0 * a);
            return t <= 0.0 ? double.PositiveInfinity : t;
        }
    

        public double GetAngleTo(V p2)
        {
            var (x, y) = p2;
            return Math.Atan2(y-Y, x-X);
        }

        public void Deconstruct(out int x, out int y)
        {
            x = X;
            y = Y;
        }

        public static IEnumerable<V> AllInRange(int width, int height)
        {
            for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                yield return new V(x, y);
            }
        }

        public int MDistTo(V v2)
        {
            var (x, y) = v2;
            return Math.Abs(x-X) + Math.Abs(y-Y);
        }

        public int MLen =>  Math.Abs(X) + Math.Abs(Y);

        public int CDistTo(V v2)
        {
            var (x, y) = v2;
            return Math.Max(Math.Abs(x-X), Math.Abs(y-Y));
        }

        public int CLen => Math.Max(Math.Abs(X), Math.Abs(Y));

        public bool InRange(int width, int height)
        {
            return X >= 0 && X < width && Y >= 0 && Y < height;
        }
        
        public IEnumerable<V> Area9()
        {
            for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
                yield return new V(X + dx, Y + dy);
        }

        public IEnumerable<V> Area8()
        {
            for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
                if (dx != 0 || dy != 0)
                    yield return new V(X + dx, Y + dy);
        }

        public IEnumerable<V> Area4()
        {
            yield return new V(X - 1, Y);
            yield return new V(X + 1, Y);
            yield return new V(X, Y - 1);
            yield return new V(X, Y + 1);
        }

        public IEnumerable<V> Area5()
        {
            yield return this;
            yield return new V(X - 1, Y);
            yield return new V(X + 1, Y);
            yield return new V(X, Y - 1);
            yield return new V(X, Y + 1);
        }
    }
}

namespace bot
{
    public class VD : IEquatable<VD>
    {
        public static readonly VD Zero = new VD(0, 0);

        public readonly double X;
        public readonly double Y;

        public static VD Parse(string s)
        {
            var parts = s.Split(' ');
            return new VD(double.Parse(parts[0]), double.Parse(parts[1]));
        }

        public VD(double x, double y)
        {
            X = x;
            Y = y;
        }

        public bool Equals(VD other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return X.Equals(other.X) && Y.Equals(other.Y);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != GetType())
                return false;
            return Equals((VD)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (X.GetHashCode() * 397) ^ Y.GetHashCode();
            }
        }

        public static bool operator ==(VD left, VD right) => Equals(left, right);
        public static bool operator !=(VD left, VD right) => !Equals(left, right);

        public double Len2 => X * X + Y * Y;
        public static readonly VD None = new VD(-1, -1);
        public static readonly VD Up = new VD(0, -1);
        public static readonly VD Down = new VD(0, 1);
        public static readonly VD Left = new VD(-1, 0);
        public static readonly VD Right = new VD(1, 0);

        public static readonly VD[] Directions2 = { new VD(1, 0), new VD(0, 1) }; 
        public static readonly VD[] Directions4 = { new VD(1, 0), new VD(0, 1), new VD(-1, 0), new VD(0, -1) }; 
        public static readonly VD[] Directions5 = { Zero, new VD(1, 0), new VD(0, 1), new VD(-1, 0), new VD(0, -1) }; 
        public static readonly VD[] Directions8 = {
            new VD(-1, -1), new VD(0, -1), new VD(1, -1), 
            new VD(-1, 0), new VD(0, 0), new VD(1, 0), 
            new VD(-1, 1), new VD(0, 1), new VD(1, 1), 
        }; 

        public static readonly VD[] Directions9 = {
            Zero,
            new VD(-1, -1), new VD(0, -1), new VD(1, -1), 
            new VD(-1, 0), new VD(0, 0), new VD(1, 0), 
            new VD(-1, 1), new VD(0, 1), new VD(1, 1), 
        }; 

        public override string ToString()
        {
            return $"{X.ToString(CultureInfo.InvariantCulture)} {Y.ToString(CultureInfo.InvariantCulture)}";
        }

        public static VD operator +(VD a, VD b) => new VD(a.X + b.X, a.Y + b.Y);
        public static VD operator -(VD a, VD b) => new VD(a.X - b.X, a.Y - b.Y);
        public static VD operator -(VD a) => new VD(-a.X, -a.Y);
        public static VD operator *(VD a, int k) => new VD(k * a.X, k * a.Y);
        public static VD operator *(int k, VD a) => new VD(k * a.X, k * a.Y);
        public static VD operator /(VD a, int k) => new VD(a.X / k, a.Y / k);
        public double ScalarProd(VD b) => X * b.X + Y * b.Y;
        public double VectorProd(VD b) => X * b.Y - Y * b.X;

        public double Dist2To(VD point) => (this - point).Len2;

        public double DistTo(VD b) => Math.Sqrt(Dist2To(b));
        
        public double GetCollisionTime(VD speed, VD other, double radius) {
            if (DistTo(other) <= radius)
                return 0.0;

            if (speed.Equals(Zero))
                return double.PositiveInfinity;
            /*
             * x = x2 + vx * t
             * y = y2 + vy * t
             * x² + y² = radius²
             * ↓
             * (x2² + 2*vx*x2 * t + vx² * t²)  +  (y2² + 2*vy*y2 * t + vy² * t²) = radius²
             * ↓
             * t² * (vx² + vy²)  +  t * 2*(x2*vx + y2*vy) + x2² + y2² - radius² = 0
             */

            var x2 = X - other.X;
            var y2 = Y - other.Y;
            var vx = speed.X;
            var vy = speed.Y;

            var a = vx * vx + vy * vy;
            var b = 2.0 * (x2 * vx + y2 * vy);
            var c = x2 * x2 + y2 * y2 - radius * radius;
            var d = b * b - 4.0 * a * c;

            if (d < 0.0)
                return double.PositiveInfinity;

            var t = (-b - Math.Sqrt(d)) / (2.0 * a);
            return t <= 0.0 ? double.PositiveInfinity : t;
        }
    

        public double GetAngleTo(VD p2)
        {
            var (x, y) = p2;
            return Math.Atan2(y-Y, x-X);
        }

        public void Deconstruct(out double x, out double y)
        {
            x = X;
            y = Y;
        }

        public double MDistTo(VD v2)
        {
            var (x, y) = v2;
            return Math.Abs(x-X) + Math.Abs(y-Y);
        }

        public double MLen =>  Math.Abs(X) + Math.Abs(Y);

        public double CDistTo(VD v2)
        {
            var (x, y) = v2;
            return Math.Max(Math.Abs(x-X), Math.Abs(y-Y));
        }

        public double CLen => Math.Max(Math.Abs(X), Math.Abs(Y));

        public bool InRange(int width, int height)
        {
            return X >= 0 && X < width && Y >= 0 && Y < height;
        }
    }
}

namespace bot
{
    public static class App
    {
        [SuppressMessage("ReSharper", "FunctionNeverReturns")]
        private static void Main(string[] args)
        {
            var reader = new ConsoleReader();
            var solver = new Solver();
            var init = reader.ReadInit();
            reader.FlushToStdErr();
            var first = true;
            while (true)
            {
                var state = reader.ReadState(init);
                var timer = new Countdown(first ? 500 : 50); //TODO fix timeouts
                reader.FlushToStdErr();
                var command = solver.GetCommand(state, timer);
                Console.WriteLine(command);
                Console.Error.WriteLine(timer);
                first = false;
            }
        }
    }
}

namespace bot
{
    public record Move(V Destination) : BotCommand;
}

namespace bot
{
    public class Solver
    {
        public BotCommand GetCommand(State state, Countdown countdown)
        {
            return new Move(V.Zero) { Message = "Nothing to do..." };
        }
    }
}

namespace bot
{
    public class StateInit
    {
    }

    public class State
    {
    }
}

namespace bot
{
    public static class StateReader
    {
        public static State ReadState(this ConsoleReader reader)
        {
            var init = reader.ReadInit();
            return reader.ReadState(init);
        }
        
        // ReSharper disable once InconsistentNaming
        public static State ReadState(this ConsoleReader Console, StateInit init)
        {
            // Copy paste here the code for input turn data
            return new State();
        }

        // ReSharper disable once InconsistentNaming
        public static StateInit ReadInit(this ConsoleReader Console)
        {
            // Copy paste here the code for initialization input data (or delete if no initialization data in this game)
            return new StateInit();
        }
    }
}

