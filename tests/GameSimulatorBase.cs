using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace bot
{
    public abstract class GameSimulatorBase
    {
        public bool IsGameEnded { get; protected set; }
        protected int gameCounter = 200;
        protected int mapWidth;
        protected int mapHeight;
        protected int entitiesCount;
        private int[,] oreMap;
        private bool[,] trapMap;
        private bool[,] myRadarMap;
        private Robot[,] robotsMap;
        protected State playerState;
        protected State opponentState;
        protected Queue<Dictionary<int, V>> opponentPositions;
        protected List<(State state, Robot robot, Move command)> moveCommands = new List<(State, Robot, Move)>();
        protected List<(State state, Robot robot, Dig command)> digCommands = new List<(State, Robot, Dig)>();
        protected List<(State state, Robot robot, Request command)> requestCommands = new List<(State, Robot, Request)>();

        public GameSimulatorBase(string mapFileName)
        {
            using (var file = new StreamReader(mapFileName))
            {
                var input = file.ReadLine().Split();
                mapWidth = int.Parse(input[0]);
                mapHeight = int.Parse(input[1]);
                oreMap = new int[mapWidth, mapHeight];
                trapMap = new bool[mapWidth, mapHeight];
                myRadarMap = new bool[mapWidth, mapHeight];
                robotsMap = new Robot[mapWidth, mapHeight];

                for (var i = 0; i < mapHeight; i++)
                {
                    input = file.ReadLine().Split();
                    for (var j = 0; j < mapWidth; j++)
                    {
                        oreMap[j, i] = int.Parse(input[j]);
                    }
                }
            }

            playerState = new State(mapWidth, mapHeight);
            playerState.Cells = new PlaygroundCell[mapWidth, mapHeight];
            for (var i = 0; i < mapHeight; i++)
            {
                for (var j = 0; j < mapWidth; j++)
                {
                    playerState.Cells[j, i] = new PlaygroundCell("?", false);
                }
            }

            for (var i = 4; i < 13; i += 2)
            {
                playerState.MyRobots.Add(new Robot(entitiesCount, new V(0, i), EntityType.MY_ROBOT));
                entitiesCount++;
            }

            foreach (var robot in playerState.MyRobots.Concat(playerState.OpponentRobots))
            {
                robotsMap[robot.Pos.X, robot.Pos.Y] = robot;
            }
        }

        public State GetStepState() => playerState;

        public abstract void ApplyCommands(RobotCommand[] commands);

        public void EnqueueCommands(State state, RobotCommand[] commands)
        {
            if (IsGameEnded)
                return;
            
            for (var i = 0; i < commands.Length; i++)
            {
                var command = commands[i];
                var robot = state.MyRobots[i];
                if (robot.IsDead() || command.Type == CommandType.WAIT)
                    continue;

                switch (command.Type)
                {
                    case CommandType.MOVE:
                        moveCommands.Add((state, robot, (Move)command));
                        break;
                    case CommandType.DIG:
                        digCommands.Add((state, robot, (Dig)command));
                        break;
                    case CommandType.REQUEST:
                        requestCommands.Add((state, robot, (Request)command));
                        break;
                }
            }
        }

        protected void ApplyDigs()
        {
            foreach (var (state, robot, command) in digCommands)
            {
                if (!CheckAdjacent(state, robot, command))
                    continue; 

                var target = command.pos;
                if (trapMap[target.X, target.Y])
                    PulseTrap(target);

                if (DigBuryAndContinue(state, robot, target))
                {
                    if (oreMap[target.X, target.Y] == 0)
                    {
                        robot.Item = EntityType.NONE;
                    }
                    else
                    {
                        oreMap[target.X, target.Y]--;
                        robot.Item = EntityType.ORE;
                    }
                }
            }
            digCommands.Clear();
        }

        protected void ApplyRequests()
        {
            foreach (var (state, robot, req) in requestCommands)
            {
                if (req.item == EntityType.RADAR && state.RadarCooldown <= 0)
                {
                    robot.Item = EntityType.RADAR;
                    state.RadarCooldown = 6;
                }
                if (req.item == EntityType.TRAP && state.TrapCooldown <= 0)
                {
                    robot.Item = EntityType.TRAP;
                    state.TrapCooldown = 6;
                }
            }
            requestCommands.Clear();
        }

        protected void ApplyMoves()
        {
            foreach (var (state, robot, move) in moveCommands)
            {
                if (robot.Pos.MDistTo(move.pos) <= 4)
                {
                    robot.Pos = move.pos;
                }
                else
                {
                    //not described in the game definition
                    throw new NotImplementedException();
                }

                if (move.pos.X == 0 && robot.Item == EntityType.ORE)
                {
                    robot.Item = EntityType.NONE;
                    state.MyScore++;
                }
            }
        }

        protected void StepEnding()
        {
            DecrementCooldowns(playerState);
            DecrementCooldowns(opponentState);
            CopyOpponentData(playerState, opponentState);
            CopyOpponentData(opponentState, playerState);
            gameCounter--;
            IsGameEnded = gameCounter == 0;
        }

        private bool CheckAdjacent(State state, Robot robot, Dig command)
        {
            var adjacent = false;
            foreach (var step in robot.Pos.Area5())
            {
                if (step == command.pos)
                {
                    adjacent = true;
                    break;
                }
            }

            if (!adjacent)
            {
                moveCommands.Add((state, robot, new Move(command.pos, command.message)));
            }

            return adjacent;
        }

        private void PulseTrap(V target)
        {
            trapMap[target.X, target.Y] = false;
            foreach (var step in target.Area5())
            {
                var robot = robotsMap[step.X, step.Y];
                if (robot != null)
                {
                    robot.Pos = V.None;
                    robotsMap[step.X, step.Y] = null;
                }
            }

            foreach (var step in target.Area5())
            {
                if (trapMap[step.X, step.Y])
                {
                    PulseTrap(step);
                }
            }
        }

        private bool DigBuryAndContinue(State state, Robot robot, V target)
        {
            if (robot.Item == EntityType.RADAR)
            {
                myRadarMap[target.X, target.Y] = true;
                state.Radars.Add(new Entity(entitiesCount, target, EntityType.RADAR));
                entitiesCount++;
                foreach (var cell in GetRadarRange(robot.Pos))
                {
                    state.Cells[cell.X, cell.Y].OpenCell(oreMap[cell.X, cell.Y], cell == robot.Pos);
                }
            }
            else if (robot.Item == EntityType.TRAP)
            {
                trapMap[target.X, target.Y] = true;
                state.Traps.Add(new Entity(entitiesCount, target, EntityType.TRAP));
                entitiesCount++;
            }
            else if (robot.Item == EntityType.ORE)
            {
                oreMap[target.X, target.Y] += 1;
                robot.Item = EntityType.NONE;
                return false;
            }
            return true;
        }

        private IEnumerable<V> GetRadarRange(V pos)
        {
            foreach (var step in V.AllInRange(-4, 4))
            {
                var cell = step + pos;
                if (step.MLen < 5 && cell.X >= 0 && cell.X < mapWidth && cell.Y >= 0 && cell.Y < mapHeight)
                {
                    yield return cell;
                }
            }
        }

        private void DecrementCooldowns(State state)
        {
            if (state.RadarCooldown > 0)
                state.RadarCooldown--;
            if (state.TrapCooldown > 0)
                state.TrapCooldown--;
        }
		
		private void CopyOpponentData(State state, State opponent)
        {
            state.OpponentScore = opponent.MyScore;
            state.OpponentRobots = opponent.MyRobots;
        }
    }
}