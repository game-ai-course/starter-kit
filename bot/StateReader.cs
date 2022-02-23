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
            return new State();
        }

        // ReSharper disable once InconsistentNaming
        public static StateInit ReadInit(this ConsoleReader Console)
        {
            return new StateInit();
        }
    }
}