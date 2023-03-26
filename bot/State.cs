using System.Collections.Generic;

namespace bot
{
    public class StateInit
    {
        public int PlaygroundWidth { get; }
        public int PlaygroundHeight { get; }

        public StateInit()
        {
            PlaygroundWidth = 30;
            PlaygroundHeight = 15;
        }

        public StateInit(string[] playground)
        {
            PlaygroundWidth = int.Parse(playground[0]);
            PlaygroundHeight = int.Parse(playground[1]);
        }

        public State NewState()
        {
            return new State(PlaygroundWidth, PlaygroundHeight);
        }
    }

    public class State
    {
        public int PlaygroundWidth { get; }
        public int PlaygroundHeight { get; }
        public int MyScore { get; set; }
        public int OpponentScore { get; set; }
        public PlaygroundCell[,] Cells { get; set; }
        public int RadarCooldown { get; set; }
        public int TrapCooldown { get; set; }
        public List<Robot> MyRobots { get; set; }
        public List<Robot> OpponentRobots { get; set; }
        public List<Entity> Radars { get; set; }
        public List<Entity> Traps { get; set; }

        public State(
            int playgroundWidth,
            int playgroundHeight
            )
        {
            PlaygroundWidth = playgroundWidth;
            PlaygroundHeight = playgroundHeight;
            Cells = new PlaygroundCell[playgroundWidth, playgroundHeight];
            MyRobots = new List<Robot>();
            OpponentRobots = new List<Robot>();
            Radars = new List<Entity>();
            Traps = new List<Entity>();
        }
    }
}