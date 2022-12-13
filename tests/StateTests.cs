using System;
using NUnit.Framework;

namespace bot
{
    [TestFixture]
    public class StateTests
    {
        /*
         * Как отлаживать алгоритм:
         *
         * ConsoleReader после каждого хода пишет в отладочный вывод весь ввод, в котором для удобства
         * переводы строк заменены на "|". Получается одна строка, которую удобно скопировать из интерфейса CG
         * и вставить в этот тест. Аналогично поступить с инизиализационными данными, которые вводятся до первого хода.
         *
         * Если в интерфейсе CG видно, как ваш алгоритм делает странный ход, можно быстро скопировать входные данные,
         * вставить в этот тест, и тем самым повторить проблему в контролируемых условиях.
         * Дальше можно отлаживать проблему привычными способами в IDE.     
         */
        [TestCase("Some|init|data", "Some input|copy pasted from|error stream")]
        public void Solve(string initInput, string stepInput)
        {
            var reader = new ConsoleReader(initInput + "|" + stepInput);
            var state = reader.ReadState();
            Console.WriteLine(state);

            var solver = new Solver();
            var move = solver.GetCommand(state, int.MaxValue);
            Console.WriteLine(move);
        }
    }
}