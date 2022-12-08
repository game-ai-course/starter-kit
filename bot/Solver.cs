namespace bot
{
    public class Solver
    {
        public BotCommand GetCommand(State state, Countdown countdown)
        {
            return new Move(V.Zero) { Message = "Nothing to do..." };
        }
    }
}   