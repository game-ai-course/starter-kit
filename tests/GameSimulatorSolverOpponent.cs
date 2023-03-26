using System;
using System.Linq;

namespace bot;

public class GameSimulatorSolverOpponent : GameSimulatorBase
{
    private CrystalSolver opponent;

    public GameSimulatorSolverOpponent(string mapFileName) : base(mapFileName)
    {
        opponentState = new State(mapWidth, mapHeight);
        for (var i = 0; i < mapWidth; i++)
        {
            for (var j = 0; j < mapHeight; j++)
            {
                opponentState.Cells[i, j] = new PlaygroundCell("?", false);
            }
        }

        opponentState.MyRobots = playerState.OpponentRobots.ToList();
        opponentState.OpponentRobots = playerState.MyRobots.ToList();

        opponent = new CrystalSolver(new StateInit());
    }

    public override void ApplyCommands(RobotCommand[] commands)
    {
        EnqueueCommands(opponentState, opponent.GetCommands(opponentState, null));
        EnqueueCommands(playerState, commands);
        ApplyDigs(); 
        ApplyRequests();
        ApplyMoves();
        StepEnding();
    }
}