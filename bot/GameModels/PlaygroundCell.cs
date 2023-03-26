namespace bot
{
    public class PlaygroundCell
    {
        public int Ore { get; set; }
        public bool Hole { get; set; }
        public bool Known { get; set; }

        public PlaygroundCell(string ore, bool hole)
        {
            Hole = hole;
            Known = !"?".Equals(ore);
            if (Known)
            {
                Ore = int.Parse(ore);
            }
        }

        public void OpenCell(int ore, bool hole)
        {
            Hole = hole;
            Known = true;
            Ore = ore;
        }
    }
}