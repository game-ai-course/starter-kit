namespace bot
{
    public class Solver
    {
        public BotCommand GetCommand(State state, Countdown countdown)
        {
            // ISolver<State, SingleMoveSolution<BotCommand>> solver = null;
            // solver = solver.WithLogging(5);
            return new BotCommand();
        }
    }
}   