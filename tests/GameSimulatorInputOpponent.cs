using System.Collections.Generic;
using System.IO;

namespace bot;

public class GameSimulatorInputOpponent : GameSimulatorBase //Only the opponent's movements so far 
{
    private Queue<Dictionary<int, V>> opponentPositions;
    public GameSimulatorInputOpponent(string mapFileName, string gameStepsFile = null) : base(mapFileName)
    {
		opponentState = new State(30, 15);
        if (gameStepsFile != null)
        {
            opponentPositions = new Queue<Dictionary<int, V>>();
            ReadOpponentMovements(gameStepsFile);
        }
    }
    public override void ApplyCommands(RobotCommand[] commands)
    {
        EnqueueCommands(playerState, commands);
        EnqueueOpponentMovements();
        ApplyDigs();
        ApplyRequests();
        ApplyMoves();
        StepEnding();
    }

    private void EnqueueOpponentMovements()
    {
        if (opponentPositions.Count > 0)
        {
            var opponentToStepMovements = opponentPositions.Dequeue();
            foreach (var opponent in playerState.OpponentRobots)
            {
                moveCommands.Add((opponentState, opponent, new Move(opponentToStepMovements[opponent.Id])));
            }
        }
    }

    private void ReadOpponentMovements(string gameStepsFile)
    {
        var gameOpponentIndexToLocal = new Dictionary<string, int>();
        using (var file = new StreamReader(gameStepsFile))
        {
            while (true)
            {
                var input = file.ReadLine();
                if (input == null)
                    break;
                var datas = input.Split('|');
                var entityCounts = int.Parse(datas[mapHeight + 1].Split()[0]);
                var stepPositions = new Dictionary<int, V>();
                for (var i = 0; i < entityCounts; i++)
                {
                    var entityData = datas[mapHeight + 2 + i].Split();
                    if (entityData[1] == "1")
                    {
                        var pos = new V(int.Parse(entityData[2]), int.Parse(entityData[3]));
                        if (!gameOpponentIndexToLocal.TryGetValue(entityData[0], out var opponentIndex))
                        {
                            var opponent = new Robot(entitiesCount, pos, EntityType.OPPONENT_ROBOT);
                            opponentIndex = entitiesCount;
							gameOpponentIndexToLocal[entityData[0]] = opponentIndex;
                            playerState.OpponentRobots.Add(opponent);
                            entitiesCount++;
                        }

                        stepPositions[opponentIndex] = pos;
                    }
                }
                opponentPositions.Enqueue(stepPositions);
            }
        }
    }
}