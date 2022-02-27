using System;
using System.Collections.Generic;
using System.Text;

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