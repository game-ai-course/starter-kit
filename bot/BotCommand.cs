namespace bot
{
    public class BotCommand
    {
        public string Message;

        public override string ToString()
        {
            return $"MOVE 1 2 {Message}";
        }
    }
}