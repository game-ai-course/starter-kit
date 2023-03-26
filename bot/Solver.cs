using System.Collections.Generic;

namespace bot
{
    public class CrystalSolver
    {
        private List<V> oreCells;
        private bool[,] emptyCells;
        private bool[,] probablyEnemyTraps;
        private State previousState;
        private bool start = true;
        private int stepRandomDigs;

        public CrystalSolver(StateInit init)
        {
            emptyCells = new bool[init.PlaygroundWidth, init.PlaygroundHeight];
            probablyEnemyTraps = new bool[init.PlaygroundWidth, init.PlaygroundHeight];
        }

        public RobotCommand[] GetCommands(State state, Countdown countdown)
        {
            if (start)
            {
                return StartStep(state);
            }

            stepRandomDigs = 0;
            var commands = new RobotCommand[state.MyRobots.Count];
            for (var i = 0; i < commands.Length; i++) 
            {
                if (commands[i] != null)
                    continue;
                var myRobot = state.MyRobots[i];
                if (myRobot.IsDead())
                {
                    commands[i] = new Wait();
                    continue;
                }

                if (myRobot.Item == EntityType.RADAR)
                {
                    commands[i] = MoveRadarRobot(state, myRobot);
                }
                else
                {
                    commands[i] = new Wait();
                }
            }
            return commands;
        }

        private RobotCommand[] StartStep(State state)
        {
            var commands = new RobotCommand[state.MyRobots.Count];
            commands[0] = new Request(EntityType.RADAR);
            start = false;
            for (var i = 1; i < commands.Length; i++) 
            {
                var myRobot = state.MyRobots[i];
                commands[i] = new Move(myRobot.Pos + new V(4, 0));
            }
            return commands;
        }

        private RobotCommand MoveRadarRobot(State state, Robot robot)
        {
            if (robot.Pos.X > 0 && !state.Cells[robot.Pos.X, robot.Pos.Y].Known)
            {
                return new Dig(robot.Pos);
            }
            return new Move(robot.Pos + new V(4, 0));
        }
    }
}   