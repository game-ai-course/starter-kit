using NUnit.Framework;

namespace bot
{
    [TestFixture]
    public class GameTests
    {
        [TestCase("map_example.txt", "states.txt")]
        public void Solve(string mapPath, string inputPath)
        {
            var simulator = new GameSimulatorInputOpponent(mapPath, inputPath);
            var solver = new CrystalSolver(new StateInit());

            while (!simulator.IsGameEnded)
            {
                var commands = solver.GetCommands(simulator.GetStepState(), null);
                simulator.ApplyCommands(commands);
            }
        }
    }
}