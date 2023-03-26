using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Cache;

namespace bot
{
    public static class App
    {
        [SuppressMessage("ReSharper", "FunctionNeverReturns")]
        private static void Main(string[] args)
        {
            var reader = new ConsoleReader();
            var init = reader.ReadInit();
            var solver = new CrystalSolver(init);
            reader.FlushToStdErr();
            var first = true;
            while (true)
            {
                var state = reader.ReadState(init);
                var timer = new Countdown(first ? 1000 : 50);
                reader.FlushToStdErr();
                var commands = solver.GetCommands(state, timer); 
                foreach (var command in commands)
                {
                    Console.WriteLine(command);
                }
                Console.Error.WriteLine(timer);
                first = false;
            }
        }
    }
}