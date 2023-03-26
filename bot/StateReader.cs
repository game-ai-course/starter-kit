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
            var state = init.NewState();
            var inputs = Console.ReadLine().Split(' ');
            state.MyScore = int.Parse(inputs[0]); // Amount of ore delivered
            state.OpponentScore = int.Parse(inputs[1]);
            for (var i = 0; i < state.PlaygroundHeight; i++)
            {
                inputs = Console.ReadLine().Split(' ');
                for (var j = 0; j < state.PlaygroundHeight; j++)
                {
                    var ore = inputs[2 * j];// amount of ore or "?" if unknown
                    var hole = int.Parse(inputs[2 * j + 1]);// 1 if cell has a hole
                    state.Cells[j, i] = new PlaygroundCell(ore, hole == 1);
                }
            }
            inputs = Console.ReadLine().Split(' ');
            var entityCount = int.Parse(inputs[0]); // number of entities visible to you
            state.RadarCooldown = int.Parse(inputs[1]); // turns left until a new radar can be requested
            state.TrapCooldown = int.Parse(inputs[2]); // turns left until a new trap can be requested
            for (int i = 0; i < entityCount; i++)
            {
                AddEntity(state, Console.ReadLine());
            }
            return state;
        }

        // ReSharper disable once InconsistentNaming
        public static StateInit ReadInit(this ConsoleReader Console)
        {
            // Copy paste here the code for initialization input data (or delete if no initialization data in this game)
            return new StateInit(Console.ReadLine().Split());
        }

        private static void AddEntity(State state, string line)
        {
            var inputs = line.Split(' ');
            var id = int.Parse(inputs[0]); // unique id of the entity
            var type = (EntityType)int.Parse(inputs[1]); // 0 for your robot, 1 for other robot, 2 for radar, 3 for trap
            var item = (EntityType)int.Parse(inputs[4]); // if this entity is a robot, the item it is carrying (-1 for NONE, 2 for RADAR, 3 for TRAP, 4 for ORE)
            var coord = new V(int.Parse(inputs[2]), int.Parse(inputs[3])); // position of the entity
            switch (type)
            {
                case EntityType.MY_ROBOT:
                    state.MyRobots.Add(new Robot(id, coord, item));
                    break;
                case EntityType.OPPONENT_ROBOT:
                    state.OpponentRobots.Add(new Robot(id, coord, item));
                    break;
                case EntityType.RADAR:
                    state.Radars.Add(new Entity(id, coord, item));
                    break;
                case EntityType.TRAP:
                    state.Traps.Add(new Entity(id, coord, item));
                    break;
            }
        }
    }
}